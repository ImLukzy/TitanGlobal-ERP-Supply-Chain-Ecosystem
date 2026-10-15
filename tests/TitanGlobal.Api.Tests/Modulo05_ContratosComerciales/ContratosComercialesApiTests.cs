using System.Text.Json;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo05_ContratosComerciales.Dtos;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;

namespace TitanGlobal.Api.Tests.Modulo05_ContratosComerciales;

[Collection("Modulo04")]
public class ContratosComercialesApiTests
{
    private readonly OrdenesCompraApiFactory _fabrica;

    private readonly HttpClient _clienteHttp;

    public ContratosComercialesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerContratos();
    }

    [Fact]
    public async Task CrearAsignaNumeroFinalYListadoCalculaConsumoSinNPlusN()
    {
        GuardarContratoRequestDto solicitud = CrearSolicitud(1, "  Marco anual  ", "Credito30");
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/contratos-comerciales", solicitud);
        ContratoCreadoDto? creado = await respuesta.Content.ReadFromJsonAsync<ContratoCreadoDto>();
        PaginaContratosDto? listado = await _clienteHttp.GetFromJsonAsync<PaginaContratosDto>(
            "/api/v1/contratos-comerciales?clienteId=1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creado);
        Assert.Equal($"CC-{creado.Id:D6}", creado.Numero);
        Assert.NotNull(listado);
        ContratoListadoDto item = Assert.Single(listado.Items);
        Assert.Equal("Marco anual", item.Titulo);
        Assert.Equal(9000m, item.MontoConsumido);
        Assert.Equal(45m, item.PorcentajeConsumido);

        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Assert.Equal(0, await contexto.ContratosComerciales.CountAsync(contrato => contrato.Numero.StartsWith("TMP-")));
    }

    [Fact]
    public async Task ActivarDetectaSolapamientoYFinalizarRechazaBorrador()
    {
        ContratoComercial vigente = new ContratoComercial
        {
            Numero = "CC-DEV-001",
            ClienteId = 1,
            Titulo = "Vigente",
            FechaInicio = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc),
            FechaFin = DateTime.SpecifyKind(new DateTime(2026, 12, 31), DateTimeKind.Utc),
            MontoComprometido = 1000m,
            CondicionPago = "Contado",
            Estado = "Vigente"
        };
        using (IServiceScope alcance = _fabrica.Services.CreateScope())
        {
            TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            contexto.ContratosComerciales.Add(vigente);
            await contexto.SaveChangesAsync();
        }

        HttpResponseMessage crear = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/contratos-comerciales", CrearSolicitud(1, "Nuevo", "Contado"));
        ContratoCreadoDto? creado = await crear.Content.ReadFromJsonAsync<ContratoCreadoDto>();
        Assert.NotNull(creado);
        HttpResponseMessage activar = await _clienteHttp.PostAsync(
            $"/api/v1/contratos-comerciales/{creado.Id}/activar", null);
        Assert.Equal(HttpStatusCode.Conflict, activar.StatusCode);

        HttpResponseMessage finalizar = await _clienteHttp.PostAsync(
            $"/api/v1/contratos-comerciales/{creado.Id}/finalizar", null);
        Assert.Equal(HttpStatusCode.Conflict, finalizar.StatusCode);
    }

    [Fact]
    public async Task VencidoCalculaConsumoPorPeriodoPorcentajeSuperiorACienYSaldonegativo()
    {
        int contratoId;
        using (IServiceScope alcance = _fabrica.Services.CreateScope())
        {
            TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            ContratoComercial contrato = new ContratoComercial
            {
                Numero = "CC-DEV-ANT",
                ClienteId = 1,
                Titulo = "Contrato anterior",
                FechaInicio = DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc),
                FechaFin = DateTime.SpecifyKind(new DateTime(2025, 12, 31), DateTimeKind.Utc),
                MontoComprometido = 500m,
                CondicionPago = "Contado",
                Estado = "Vigente"
            };
            contexto.ContratosComerciales.Add(contrato);
            await contexto.SaveChangesAsync();
            contratoId = contrato.Id;
        }

        PaginaContratosDto? vencidos = await _clienteHttp.GetFromJsonAsync<PaginaContratosDto>(
            "/api/v1/contratos-comerciales?vencidos=true");
        ContratoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<ContratoDetalleDto>(
            $"/api/v1/contratos-comerciales/{contratoId}");

        Assert.NotNull(vencidos);
        ContratoListadoDto contratoVencido = Assert.Single(vencidos.Items);
        Assert.Equal("CC-DEV-ANT", contratoVencido.Numero);
        Assert.True(contratoVencido.Vencido);
        Assert.Equal(1000m, contratoVencido.MontoConsumido);
        Assert.Equal(200m, contratoVencido.PorcentajeConsumido);
        Assert.NotNull(detalle);
        Assert.Equal(-500m, detalle.Saldo);
        Assert.Equal(new[] { "OC-003" }, detalle.OrdenesPeriodo.Select(orden => orden.NumeroOrden).ToArray());
    }

    [Fact]
    public async Task FiltrosInvalidosDevuelvenCodigosYMensajesLiterales()
    {
        HttpResponseMessage estadoInvalido = await _clienteHttp.GetAsync(
            "/api/v1/contratos-comerciales?estado=Desconocido");
        JsonDocument? documentoEstado = await estadoInvalido.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(HttpStatusCode.BadRequest, estadoInvalido.StatusCode);
        Assert.NotNull(documentoEstado);
        Assert.Equal(400, documentoEstado.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal("El estado no es válido.", documentoEstado.RootElement.GetProperty("Message").GetString());
        Assert.False(documentoEstado.RootElement.TryGetProperty("Detail", out _));

        HttpResponseMessage paginaInvalida = await _clienteHttp.GetAsync(
            "/api/v1/contratos-comerciales?pagina=0");
        Assert.Equal(HttpStatusCode.BadRequest, paginaInvalida.StatusCode);
    }

    [Fact]
    public async Task CancelarRequiereMotivoYResumenCuentaEstados()
    {
        HttpResponseMessage crear = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/contratos-comerciales", CrearSolicitud(2, "Borrador", "Contado"));
        ContratoCreadoDto? creado = await crear.Content.ReadFromJsonAsync<ContratoCreadoDto>();
        Assert.NotNull(creado);

        HttpResponseMessage cancelarVacio = await _clienteHttp.PostAsJsonAsync(
            $"/api/v1/contratos-comerciales/{creado.Id}/cancelar",
            new CancelarContratoRequestDto { Motivo = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, cancelarVacio.StatusCode);

        HttpResponseMessage cancelar = await _clienteHttp.PostAsJsonAsync(
            $"/api/v1/contratos-comerciales/{creado.Id}/cancelar",
            new CancelarContratoRequestDto { Motivo = "  Cambio de plan  " });
        ContratoDetalleDto? detalle = await cancelar.Content.ReadFromJsonAsync<ContratoDetalleDto>();
        Assert.Equal(HttpStatusCode.OK, cancelar.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Cancelado", detalle.Estado);
        Assert.Equal("Cambio de plan", detalle.MotivoCancelacion);

        ResumenContratosDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenContratosDto>(
            "/api/v1/contratos-comerciales/resumen");
        Assert.NotNull(resumen);
        Assert.Equal(1, resumen.Cancelados);
    }

    [Theory]
    [InlineData("cliente", "El cliente 9999 no existe.")]
    [InlineData("titulo-vacio", "El título es obligatorio.")]
    [InlineData("titulo-largo", "El título no puede superar 200 caracteres.")]
    [InlineData("fecha-fin-anterior", "La fecha de fin debe ser posterior a la de inicio.")]
    [InlineData("fecha-fin-igual", "La fecha de fin debe ser posterior a la de inicio.")]
    [InlineData("monto-cero", "El monto comprometido debe ser mayor que 0.")]
    [InlineData("monto-negativo", "El monto comprometido debe ser mayor que 0.")]
    [InlineData("condicion", "La condición de pago no es válida.")]
    [InlineData("observaciones", "Las observaciones no pueden superar 500 caracteres.")]
    public async Task AltaInvalidaDevuelveMensajeLiteralYNoCreaNiModificaFilas(
        string caso, string mensaje)
    {
        List<(int Id, string Numero, string Estado)> antes = await ObtenerContratosAsync();
        GuardarContratoRequestDto solicitud = CrearSolicitud(1, "Contrato válido", "Contado");
        if (caso == "cliente")
        {
            solicitud.ClienteId = 9999;
        }
        else if (caso == "titulo-vacio")
        {
            solicitud.Titulo = "   ";
        }
        else if (caso == "titulo-largo")
        {
            solicitud.Titulo = new string('A', 201);
        }
        else if (caso == "fecha-fin-anterior")
        {
            solicitud.FechaFin = solicitud.FechaInicio.AddDays(-1);
        }
        else if (caso == "fecha-fin-igual")
        {
            solicitud.FechaFin = solicitud.FechaInicio;
        }
        else if (caso == "monto-cero")
        {
            solicitud.MontoComprometido = 0m;
        }
        else if (caso == "monto-negativo")
        {
            solicitud.MontoComprometido = -1m;
        }
        else if (caso == "condicion")
        {
            solicitud.CondicionPago = "Credito45";
        }
        else if (caso == "observaciones")
        {
            solicitud.Observaciones = new string('O', 501);
        }

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/contratos-comerciales", solicitud);
        await AssertarErrorAsync(respuesta, 400, mensaje);
        Assert.Equal(antes, await ObtenerContratosAsync());
    }

    [Theory]
    [InlineData("actualizar", "Vigente", "El contrato CC-TEST-001 ya está vigente.")]
    [InlineData("eliminar", "Finalizado", "El contrato CC-TEST-001 ya está finalizado.")]
    [InlineData("activar", "Finalizado", "El contrato CC-TEST-001 ya está finalizado.")]
    [InlineData("finalizar", "Borrador", "El contrato CC-TEST-001 ya está borrador.")]
    [InlineData("cancelar", "Finalizado", "El contrato CC-TEST-001 ya está finalizado.")]
    public async Task TransicionProhibidaDevuelve409LiteralYSinCambiarFilas(
        string accion, string estado, string mensaje)
    {
        ContratoComercial contrato = await AgregarContratoAsync(
            "CC-TEST-001", estado, 1, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        List<(int Id, string Numero, string Estado)> antes = await ObtenerContratosAsync();
        HttpResponseMessage respuesta;
        if (accion == "actualizar")
        {
            respuesta = await _clienteHttp.PutAsJsonAsync(
                $"/api/v1/contratos-comerciales/{contrato.Id}",
                CrearSolicitud(1, "Intento de cambio", "Contado"));
        }
        else if (accion == "eliminar")
        {
            respuesta = await _clienteHttp.DeleteAsync($"/api/v1/contratos-comerciales/{contrato.Id}");
        }
        else if (accion == "activar")
        {
            respuesta = await _clienteHttp.PostAsync(
                $"/api/v1/contratos-comerciales/{contrato.Id}/activar", null);
        }
        else if (accion == "finalizar")
        {
            respuesta = await _clienteHttp.PostAsync(
                $"/api/v1/contratos-comerciales/{contrato.Id}/finalizar", null);
        }
        else
        {
            respuesta = await _clienteHttp.PostAsJsonAsync(
                $"/api/v1/contratos-comerciales/{contrato.Id}/cancelar",
                new CancelarContratoRequestDto { Motivo = "Motivo válido" });
        }

        await AssertarErrorAsync(respuesta, 409, mensaje);
        Assert.Equal(antes, await ObtenerContratosAsync());
    }

    [Fact]
    public async Task ActivarContratoConPeriodoTerminadoDevuelve409LiteralYSinCambios()
    {
        ContratoComercial contrato = await AgregarContratoAsync(
            "CC-TEST-OLD", "Borrador", 1, new DateTime(2020, 1, 1), new DateTime(2020, 12, 31));
        List<(int Id, string Numero, string Estado)> antes = await ObtenerContratosAsync();

        HttpResponseMessage respuesta = await _clienteHttp.PostAsync(
            $"/api/v1/contratos-comerciales/{contrato.Id}/activar", null);

        await AssertarErrorAsync(respuesta, 409, "El contrato CC-TEST-OLD ya terminó su periodo.");
        Assert.Equal(antes, await ObtenerContratosAsync());
    }

    [Fact]
    public async Task SolapamientoEnUnSoloDiaDevuelve409LiteralYSinCambios()
    {
        await AgregarContratoAsync(
            "CC-TEST-BASE", "Vigente", 1, new DateTime(2026, 1, 1), new DateTime(2026, 6, 30));
        ContratoComercial candidato = await AgregarContratoAsync(
            "CC-TEST-NUEVO", "Borrador", 1, new DateTime(2026, 6, 30), new DateTime(2026, 12, 31));
        List<(int Id, string Numero, string Estado)> antes = await ObtenerContratosAsync();

        HttpResponseMessage respuesta = await _clienteHttp.PostAsync(
            $"/api/v1/contratos-comerciales/{candidato.Id}/activar", null);

        await AssertarErrorAsync(
            respuesta, 409, "El cliente ya tiene el contrato vigente CC-TEST-BASE en ese periodo.");
        Assert.Equal(antes, await ObtenerContratosAsync());
    }

    [Theory]
    [InlineData("Vigente", "2025-12-31")]
    [InlineData("Cancelado", "2026-06-30")]
    [InlineData("Finalizado", "2026-06-30")]
    public async Task ActivarPermitePeriodoAdyacenteOContratoNoVigente(
        string estadoExistente, string finExistente)
    {
        DateTime fechaFin = DateTime.Parse(finExistente);
        DateTime fechaInicioCandidato = estadoExistente == "Vigente"
            ? fechaFin.AddDays(1)
            : new DateTime(2026, 6, 30);
        await AgregarContratoAsync(
            "CC-TEST-BASE", estadoExistente, 1, new DateTime(2025, 1, 1), fechaFin);
        ContratoComercial candidato = await AgregarContratoAsync(
            "CC-TEST-NUEVO", "Borrador", 1, fechaInicioCandidato, new DateTime(2026, 12, 31));

        HttpResponseMessage respuesta = await _clienteHttp.PostAsync(
            $"/api/v1/contratos-comerciales/{candidato.Id}/activar", null);
        ContratoDetalleDto? detalle = await respuesta.Content.ReadFromJsonAsync<ContratoDetalleDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Vigente", detalle.Estado);
    }

    [Fact]
    public async Task ConsumoIncluyeAmbosDiasLimiteYExcluyeFueraDePeriodoYOtrosClientes()
    {
        DateTime fechaInicio = new DateTime(2026, 6, 10);
        DateTime fechaFin = new DateTime(2026, 6, 20);
        ContratoComercial contrato = await AgregarContratoAsync(
            "CC-TEST-LIMITES", "Borrador", 1, fechaInicio, fechaFin);
        using (IServiceScope alcance = _fabrica.Services.CreateScope())
        {
            TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            contexto.OrdenesCompra.AddRange(
                CrearOrden("OC-LIM-INICIO", 1, new DateTime(2026, 6, 10, 0, 0, 0), 100m),
                CrearOrden("OC-LIM-FIN", 1, new DateTime(2026, 6, 20, 23, 59, 59, DateTimeKind.Utc).AddTicks(9), 200m),
                CrearOrden("OC-LIM-ANTES", 1, new DateTime(2026, 6, 9, 23, 59, 59), 300m),
                CrearOrden("OC-LIM-DESPUES", 1, new DateTime(2026, 6, 21, 0, 0, 0), 400m),
                CrearOrden("OC-LIM-OTRO", 2, new DateTime(2026, 6, 15), 500m));
            await contexto.SaveChangesAsync();
        }

        ContratoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<ContratoDetalleDto>(
            $"/api/v1/contratos-comerciales/{contrato.Id}");

        Assert.NotNull(detalle);
        Assert.Equal(300m, detalle.MontoConsumido);
        Assert.Equal(30m, detalle.PorcentajeConsumido);
        Assert.Equal(new[] { "OC-LIM-FIN", "OC-LIM-INICIO" },
            detalle.OrdenesPeriodo.Select(orden => orden.NumeroOrden).ToArray());
    }

    [Fact]
    public async Task ListadoOrdenaFiltraBuscaYPaginaConTotal()
    {
        await AgregarContratoAsync(
            "CC-FILT-A", "Borrador", 1, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), "Archivo anual");
        await AgregarContratoAsync(
            "CC-FILT-B", "Vigente", 2, new DateTime(2026, 1, 1), new DateTime(2027, 12, 31), "Renovación beta");
        await AgregarContratoAsync(
            "CC-FILT-C", "Borrador", 1, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), "Contrato anual");
        await AgregarContratoAsync(
            "CC-FILT-D", "Vigente", 3, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "Contrato expirado");

        PaginaContratosDto? pagina = await _clienteHttp.GetFromJsonAsync<PaginaContratosDto>(
            "/api/v1/contratos-comerciales?pagina=1&tamanoPagina=2");
        PaginaContratosDto? estado = await _clienteHttp.GetFromJsonAsync<PaginaContratosDto>(
            "/api/v1/contratos-comerciales?estado=vIgEnTe");
        PaginaContratosDto? cliente = await _clienteHttp.GetFromJsonAsync<PaginaContratosDto>(
            "/api/v1/contratos-comerciales?clienteId=1");
        PaginaContratosDto? porNumero = await _clienteHttp.GetFromJsonAsync<PaginaContratosDto>(
            "/api/v1/contratos-comerciales?busqueda=cc-filt-a");
        PaginaContratosDto? porTitulo = await _clienteHttp.GetFromJsonAsync<PaginaContratosDto>(
            "/api/v1/contratos-comerciales?busqueda=renovación");
        PaginaContratosDto? porCliente = await _clienteHttp.GetFromJsonAsync<PaginaContratosDto>(
            "/api/v1/contratos-comerciales?busqueda=gamma");
        PaginaContratosDto? vencidos = await _clienteHttp.GetFromJsonAsync<PaginaContratosDto>(
            "/api/v1/contratos-comerciales?vencidos=true");

        Assert.NotNull(pagina);
        Assert.Equal(4, pagina.Total);
        Assert.Equal(new[] { "CC-FILT-C", "CC-FILT-B" }, pagina.Items.Select(item => item.Numero).ToArray());
        Assert.NotNull(estado);
        Assert.Equal(new[] { "CC-FILT-B", "CC-FILT-D" }, estado.Items.Select(item => item.Numero).ToArray());
        Assert.NotNull(cliente);
        Assert.Equal(new[] { "CC-FILT-C", "CC-FILT-A" }, cliente.Items.Select(item => item.Numero).ToArray());
        Assert.NotNull(porNumero);
        Assert.Equal("CC-FILT-A", Assert.Single(porNumero.Items).Numero);
        Assert.NotNull(porTitulo);
        Assert.Equal("CC-FILT-B", Assert.Single(porTitulo.Items).Numero);
        Assert.NotNull(porCliente);
        Assert.Equal("CC-FILT-D", Assert.Single(porCliente.Items).Numero);
        Assert.NotNull(vencidos);
        Assert.Equal("CC-FILT-D", Assert.Single(vencidos.Items).Numero);
    }

    [Fact]
    public async Task ResumenCuentaEstadosMontosPorcentajeYCondicionesEnOrdenFijo()
    {
        await AgregarContratoAsync(
            "CC-RES-CONTADO", "Vigente", 1, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), monto: 20000m, condicion: "Contado");
        await AgregarContratoAsync(
            "CC-RES-30", "Finalizado", 2, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), monto: 300m, condicion: "Credito30");
        await AgregarContratoAsync(
            "CC-RES-60", "Cancelado", 3, new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), monto: 400m, condicion: "Credito60");
        await AgregarContratoAsync(
            "CC-RES-90", "Borrador", 4, new DateTime(2027, 1, 1), new DateTime(2027, 12, 31), monto: 500m, condicion: "Credito90");

        ResumenContratosDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenContratosDto>(
            "/api/v1/contratos-comerciales/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(4, resumen.Total);
        Assert.Equal(1, resumen.Borradores);
        Assert.Equal(1, resumen.Vigentes);
        Assert.Equal(0, resumen.Vencidos);
        Assert.Equal(1, resumen.Finalizados);
        Assert.Equal(1, resumen.Cancelados);
        Assert.Equal(20000m, resumen.MontoComprometidoVigente);
        Assert.Equal(9000m, resumen.MontoConsumidoVigente);
        Assert.Equal(45m, resumen.PorcentajeConsumoVigente);
        Assert.Equal(new[] { "Contado", "Credito30", "Credito60", "Credito90" },
            resumen.PorCondicion.Select(item => item.CondicionPago).ToArray());
        Assert.Equal(new[] { 1, 1, 1, 1 }, resumen.PorCondicion.Select(item => item.Contratos).ToArray());
    }

    [Theory]
    [InlineData("cancelar-vacio", "El motivo de cancelación es obligatorio.")]
    [InlineData("cancelar-largo", "El motivo de cancelación no puede superar 500 caracteres.")]
    public async Task CancelacionInvalidaDevuelveLiteralYSinCambiarEstado(string caso, string mensaje)
    {
        ContratoComercial contrato = await AgregarContratoAsync(
            "CC-CANCEL-TEST", "Borrador", 1, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        List<(int Id, string Numero, string Estado)> antes = await ObtenerContratosAsync();
        string motivo = caso == "cancelar-vacio" ? "  " : new string('M', 501);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            $"/api/v1/contratos-comerciales/{contrato.Id}/cancelar",
            new CancelarContratoRequestDto { Motivo = motivo });

        await AssertarErrorAsync(respuesta, 400, mensaje);
        Assert.Equal(antes, await ObtenerContratosAsync());
    }

    [Fact]
    public async Task IdInexistenteDevuelve404LiteralSinDetail()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/api/v1/contratos-comerciales/9999");

        await AssertarErrorAsync(respuesta, 404, "El contrato 9999 no existe.");
    }

    [Theory]
    [InlineData("pagina", "La página debe ser mayor o igual a 1.")]
    [InlineData("tamanoPagina", "El tamaño de página debe estar entre 1 y 100.")]
    public async Task PaginacionInvalidaDevuelveCodigoYMensajeLiteral(string parametro, string mensaje)
    {
        string valor = parametro == "pagina" ? "0" : "101";
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(
            $"/api/v1/contratos-comerciales?{parametro}={valor}");

        await AssertarErrorAsync(respuesta, 400, mensaje);
    }

    private async Task<ContratoComercial> AgregarContratoAsync(
        string numero,
        string estado,
        int clienteId,
        DateTime fechaInicio,
        DateTime fechaFin,
        string titulo = "Contrato de prueba",
        decimal monto = 1000m,
        string condicion = "Contado")
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        ContratoComercial contrato = new ContratoComercial
        {
            Numero = numero,
            ClienteId = clienteId,
            Titulo = titulo,
            FechaInicio = DateTime.SpecifyKind(fechaInicio.Date, DateTimeKind.Utc),
            FechaFin = DateTime.SpecifyKind(fechaFin.Date, DateTimeKind.Utc),
            MontoComprometido = monto,
            CondicionPago = condicion,
            Estado = estado
        };
        contexto.ContratosComerciales.Add(contrato);
        await contexto.SaveChangesAsync();
        return contrato;
    }

    private async Task<List<(int Id, string Numero, string Estado)>> ObtenerContratosAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.ContratosComerciales
            .AsNoTracking()
            .OrderBy(contrato => contrato.Id)
            .Select(contrato => new ValueTuple<int, string, string>(
                contrato.Id, contrato.Numero, contrato.Estado))
            .ToListAsync();
    }

    private static OrdenCompra CrearOrden(string numero, int clienteId, DateTime fecha, decimal total)
    {
        return new OrdenCompra
        {
            NumeroOrden = numero,
            ClienteId = clienteId,
            Fecha = DateTime.SpecifyKind(fecha, DateTimeKind.Utc),
            Total = total
        };
    }

    private static async Task AssertarErrorAsync(
        HttpResponseMessage respuesta, int codigo, string mensaje)
    {
        JsonDocument? documento = await respuesta.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal((HttpStatusCode)codigo, respuesta.StatusCode);
        Assert.NotNull(documento);
        Assert.Equal(codigo, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }

    private void RestablecerContratos()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.ContratosComerciales.RemoveRange(contexto.ContratosComerciales);
        contexto.SaveChanges();
    }

    private static GuardarContratoRequestDto CrearSolicitud(int clienteId, string titulo, string condicion)
    {
        return new GuardarContratoRequestDto
        {
            ClienteId = clienteId,
            Titulo = titulo,
            FechaInicio = new DateTime(2026, 1, 1),
            FechaFin = new DateTime(2026, 12, 31),
            MontoComprometido = 20000m,
            CondicionPago = condicion
        };
    }
}
