using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo09_ReclamosPostventa.Dtos;
using TitanGlobal.Api.Tests.Modulo06_ListasPrecios;

namespace TitanGlobal.Api.Tests.Modulo09_ReclamosPostventa;

public class ReclamosPostventaApiTests : IDisposable
{
    private readonly ListasPreciosApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public ReclamosPostventaApiTests()
    {
        _fabrica = new ListasPreciosApiFactory();
        _cliente = _fabrica.CreateClient();
        _fabrica.SeedDatabase();
        PrepararDatos();
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _fabrica.Dispose();
    }

    [Fact]
    public async Task CrearAsignaNumeroFinalYNoDejaFilaTemporal()
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/reclamos-postventa", SolicitudValida());
        ReclamoCreadoDto? creado = await respuesta.Content.ReadFromJsonAsync<ReclamoCreadoDto>();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creado);
        Assert.Equal("REC-000001", creado.Numero);
        List<string> numeros = await ConsultarAsync(contexto => contexto.ReclamosPostventa
            .Select(reclamo => reclamo.Numero)
            .ToListAsync());
        Assert.Single(numeros);
        Assert.Equal("REC-000001", numeros[0]);
        Assert.DoesNotContain(numeros, numero => numero.StartsWith("TMP-", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(999, 1, null, "Defecto", "Alta", "Descripción válida", "El cliente 999 no existe.")]
    [InlineData(1, 999, null, "Defecto", "Alta", "Descripción válida", "La orden de compra 999 no existe.")]
    [InlineData(2, 1, null, "Defecto", "Alta", "Descripción válida", "La orden OC-01 no pertenece al cliente.")]
    [InlineData(1, 1, 999, "Defecto", "Alta", "Descripción válida", "El producto 999 no existe.")]
    [InlineData(1, 1, 2, "Defecto", "Alta", "Descripción válida", "El producto 2 no está en la orden OC-01.")]
    [InlineData(1, 1, null, "Incorrecto", "Alta", "Descripción válida", "El tipo de reclamo no es válido.")]
    [InlineData(1, 1, null, "Defecto", "Urgente", "Descripción válida", "La prioridad no es válida.")]
    [InlineData(1, 1, null, "Defecto", "Alta", "   ", "La descripción es obligatoria.")]
    public async Task AltaInvalidaDevuelveErrorLiteralSinCrearFilas(
        int clienteId,
        int ordenId,
        int? productoId,
        string tipo,
        string prioridad,
        string descripcion,
        string mensaje)
    {
        int antes = await ContarAsync();
        GuardarReclamoRequestDto solicitud = SolicitudValida();
        solicitud.ClienteId = clienteId;
        solicitud.OrdenCompraId = ordenId;
        solicitud.ProductoId = productoId;
        solicitud.Tipo = tipo;
        solicitud.Prioridad = prioridad;
        solicitud.Descripcion = descripcion;

        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/reclamos-postventa", solicitud);

        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(antes, await ContarAsync());
    }

    [Fact]
    public async Task DescripcionSobreElLimiteSeRechazaSinCrearFila()
    {
        int antes = await ContarAsync();
        GuardarReclamoRequestDto solicitud = SolicitudValida();
        solicitud.Descripcion = new string('X', 1001);
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/reclamos-postventa", solicitud);

        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400,
            "La descripción no puede superar 1000 caracteres.");
        Assert.Equal(antes, await ContarAsync());
    }

    [Theory]
    [InlineData("Alta", 2)]
    [InlineData("Media", 5)]
    [InlineData("Baja", 10)]
    public async Task FechaLimiteUsaDiasDePrioridad(string prioridad, int dias)
    {
        GuardarReclamoRequestDto solicitud = SolicitudValida();
        solicitud.Prioridad = prioridad;
        HttpResponseMessage alta = await _cliente.PostAsJsonAsync("/api/v1/reclamos-postventa", solicitud);
        ReclamoCreadoDto? creado = await alta.Content.ReadFromJsonAsync<ReclamoCreadoDto>();
        ReclamoDetalleDto? detalle = await _cliente.GetFromJsonAsync<ReclamoDetalleDto>(
            $"/api/v1/reclamos-postventa/{creado!.Id}");
        Assert.NotNull(detalle);
        Assert.Equal(TimeSpan.FromDays(dias), detalle.FechaLimite - detalle.Fecha);
    }

    [Fact]
    public async Task TipoYPrioridadSeGuardanEnFormatoCanonico()
    {
        GuardarReclamoRequestDto solicitud = SolicitudValida();
        solicitud.Tipo = "fAcTuRaCiOn";
        solicitud.Prioridad = "mEdIa";
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/reclamos-postventa", solicitud);
        ReclamoCreadoDto? creado = await respuesta.Content.ReadFromJsonAsync<ReclamoCreadoDto>();
        ReclamoDetalleDto? detalle = await _cliente.GetFromJsonAsync<ReclamoDetalleDto>(
            $"/api/v1/reclamos-postventa/{creado!.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Facturacion", detalle.Tipo);
        Assert.Equal("Media", detalle.Prioridad);
    }

    [Fact]
    public async Task EditarAbiertoRecalculaPlazoAlCambiarPrioridad()
    {
        ReclamoCreadoDto creado = await CrearAsync();
        ReclamoDetalleDto inicial = (await _cliente.GetFromJsonAsync<ReclamoDetalleDto>(
            $"/api/v1/reclamos-postventa/{creado.Id}"))!;
        GuardarReclamoRequestDto actualizacion = SolicitudValida();
        actualizacion.Prioridad = "Baja";
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync(
            $"/api/v1/reclamos-postventa/{creado.Id}", actualizacion);
        ReclamoDetalleDto? final = await _cliente.GetFromJsonAsync<ReclamoDetalleDto>(
            $"/api/v1/reclamos-postventa/{creado.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(final);
        Assert.Equal("Baja", final.Prioridad);
        Assert.Equal(TimeSpan.FromDays(10), final.FechaLimite - final.Fecha);
        Assert.NotEqual(inicial.FechaLimite, final.FechaLimite);
    }

    [Theory]
    [InlineData("editar", "EnProceso", "en proceso")]
    [InlineData("editar", "Resuelto", "resuelto")]
    [InlineData("editar", "Cerrado", "cerrado")]
    [InlineData("editar", "Rechazado", "rechazado")]
    [InlineData("iniciar", "EnProceso", "en proceso")]
    [InlineData("iniciar", "Resuelto", "resuelto")]
    [InlineData("iniciar", "Cerrado", "cerrado")]
    [InlineData("iniciar", "Rechazado", "rechazado")]
    [InlineData("resolver", "Resuelto", "resuelto")]
    [InlineData("resolver", "Cerrado", "cerrado")]
    [InlineData("resolver", "Rechazado", "rechazado")]
    [InlineData("cerrar", "Abierto", "abierto")]
    [InlineData("cerrar", "EnProceso", "en proceso")]
    [InlineData("cerrar", "Cerrado", "cerrado")]
    [InlineData("cerrar", "Rechazado", "rechazado")]
    [InlineData("rechazar", "Resuelto", "resuelto")]
    [InlineData("rechazar", "Cerrado", "cerrado")]
    [InlineData("rechazar", "Rechazado", "rechazado")]
    public async Task TransicionProhibidaDevuelve409LiteralYNoCambiaEstado(
        string accion,
        string estado,
        string estadoLegible)
    {
        int id = await InsertarReclamoAsync(90, estado, "Alta", "Defecto", DateTime.UtcNow.AddDays(-1));
        HttpResponseMessage respuesta = await EjecutarAccionAsync(accion, id);

        await AfirmarErrorAsync(respuesta, HttpStatusCode.Conflict, 409,
            $"El reclamo REC-000090 está {estadoLegible}.");
        Assert.Equal(estado, await ObtenerEstadoAsync(id));
    }

    [Theory]
    [InlineData("editar", "Abierto", "Abierto")]
    [InlineData("iniciar", "Abierto", "EnProceso")]
    [InlineData("resolver", "Abierto", "Resuelto")]
    [InlineData("resolver", "EnProceso", "Resuelto")]
    [InlineData("cerrar", "Resuelto", "Cerrado")]
    [InlineData("rechazar", "Abierto", "Rechazado")]
    [InlineData("rechazar", "EnProceso", "Rechazado")]
    public async Task TransicionPermitidaGuardaElNuevoEstado(string accion, string estado, string esperado)
    {
        int id = await InsertarReclamoAsync(91, estado, "Media", "Retraso", DateTime.UtcNow.AddDays(1));
        HttpResponseMessage respuesta = await EjecutarAccionAsync(accion, id);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(esperado, await ObtenerEstadoAsync(id));
        ReclamoDetalleDto detalle = (await _cliente.GetFromJsonAsync<ReclamoDetalleDto>(
            $"/api/v1/reclamos-postventa/{id}"))!;
        if (accion == "iniciar")
        {
            Assert.NotNull(detalle.FechaInicio);
        }

        if (accion == "resolver")
        {
            Assert.NotNull(detalle.FechaResolucion);
            Assert.True(detalle.AtendidoATiempo);
        }

        if (accion == "cerrar" || accion == "rechazar")
        {
            Assert.NotNull(detalle.FechaCierre);
        }
    }

    [Theory]
    [InlineData("resolver-vacia", "La resolución es obligatoria.")]
    [InlineData("resolver-larga", "La resolución no puede superar 1000 caracteres.")]
    [InlineData("rechazar-vacia", "El motivo de rechazo es obligatorio.")]
    [InlineData("rechazar-largo", "El motivo de rechazo no puede superar 500 caracteres.")]
    public async Task AccionConTextoInvalidoDevuelveLiteralYNoCambiaEstado(string caso, string mensaje)
    {
        int id = await InsertarReclamoAsync(92, "Abierto", "Alta", "Defecto", DateTime.UtcNow.AddDays(2));
        object solicitud = caso switch
        {
            "resolver-vacia" => new ResolverReclamoRequestDto { Resolucion = " " },
            "resolver-larga" => new ResolverReclamoRequestDto { Resolucion = new string('X', 1001) },
            "rechazar-vacia" => new RechazarReclamoRequestDto { Motivo = " " },
            _ => new RechazarReclamoRequestDto { Motivo = new string('X', 501) }
        };
        string ruta = caso.StartsWith("resolver", StringComparison.Ordinal) ? "resolver" : "rechazar";
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/reclamos-postventa/{id}/{ruta}", solicitud);

        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal("Abierto", await ObtenerEstadoAsync(id));
    }

    [Fact]
    public async Task ActualizacionInvalidaNoCambiaLosDatosGuardados()
    {
        ReclamoCreadoDto creado = await CrearAsync();
        GuardarReclamoRequestDto actualizacion = SolicitudValida();
        actualizacion.Descripcion = string.Empty;
        actualizacion.Prioridad = "Baja";
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync(
            $"/api/v1/reclamos-postventa/{creado.Id}", actualizacion);

        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, "La descripción es obligatoria.");
        ReclamoDetalleDto detalle = (await _cliente.GetFromJsonAsync<ReclamoDetalleDto>(
            $"/api/v1/reclamos-postventa/{creado.Id}"))!;
        Assert.Equal("Descripción válida", detalle.Descripcion);
        Assert.Equal("Alta", detalle.Prioridad);
    }

    [Fact]
    public async Task IdentificadorInexistenteDevuelve404Literal()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/v1/reclamos-postventa/999");
        await AfirmarErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "El reclamo 999 no existe.");
    }

    [Theory]
    [InlineData("?estado=noExiste", "El estado no es válido.")]
    [InlineData("?tipo=noExiste", "El tipo de reclamo no es válido.")]
    [InlineData("?prioridad=noExiste", "La prioridad no es válida.")]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    public async Task FiltroInvalidoDevuelveErrorLiteral(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/v1/reclamos-postventa" + consulta);
        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task ListadoFiltraBuscaOrdenaYPaginaConTotal()
    {
        DateTime ahora = DateTime.UtcNow;
        await InsertarReclamoAsync(101, "Abierto", "Baja", "Defecto", ahora.AddDays(10), "REC-000101");
        await InsertarReclamoAsync(102, "EnProceso", "Alta", "Retraso", ahora.AddDays(2), "REC-000102");
        await InsertarReclamoAsync(103, "Abierto", "Media", "Faltante", ahora.AddDays(5), "REC-000103");
        ReclamoPostventa vencido = CrearReclamo(104, "Abierto", "Alta", "Facturacion", ahora.AddDays(-1), "REC-000104");
        vencido.OrdenCompraId = 5;
        vencido.ClienteId = 2;
        await InsertarAsync(vencido);

        PaginaReclamosDto? pagina = await _cliente.GetFromJsonAsync<PaginaReclamosDto>(
            "/api/v1/reclamos-postventa?pagina=1&tamanoPagina=2");
        PaginaReclamosDto? estado = await _cliente.GetFromJsonAsync<PaginaReclamosDto>(
            "/api/v1/reclamos-postventa?estado=abierto");
        PaginaReclamosDto? tipo = await _cliente.GetFromJsonAsync<PaginaReclamosDto>(
            "/api/v1/reclamos-postventa?tipo=FALTANTE");
        PaginaReclamosDto? prioridad = await _cliente.GetFromJsonAsync<PaginaReclamosDto>(
            "/api/v1/reclamos-postventa?prioridad=alta");
        PaginaReclamosDto? cliente = await _cliente.GetFromJsonAsync<PaginaReclamosDto>(
            "/api/v1/reclamos-postventa?clienteId=2");
        PaginaReclamosDto? vencidos = await _cliente.GetFromJsonAsync<PaginaReclamosDto>(
            "/api/v1/reclamos-postventa?vencidos=true");
        PaginaReclamosDto? porNumero = await _cliente.GetFromJsonAsync<PaginaReclamosDto>(
            "/api/v1/reclamos-postventa?busqueda=REC-000104");
        PaginaReclamosDto? porOrden = await _cliente.GetFromJsonAsync<PaginaReclamosDto>(
            "/api/v1/reclamos-postventa?busqueda=OC-05");
        PaginaReclamosDto? porCliente = await _cliente.GetFromJsonAsync<PaginaReclamosDto>(
            "/api/v1/reclamos-postventa?busqueda=beta%20comercio");

        Assert.NotNull(pagina);
        Assert.Equal(4, pagina.Total);
        Assert.Equal(new[] { 104, 102 }, pagina.Items.Select(item => item.Id).ToArray());
        Assert.Equal(2, pagina.TamanoPagina);
        Assert.Equal(new[] { "REC-000104", "REC-000103", "REC-000101" }, estado!.Items.Select(item => item.Numero).ToArray());
        Assert.Equal(new[] { "REC-000103" }, tipo!.Items.Select(item => item.Numero).ToArray());
        Assert.Equal(2, prioridad!.Total);
        Assert.Equal(new[] { "REC-000104", "REC-000102" }, prioridad.Items.Select(item => item.Numero).ToArray());
        Assert.Equal(new[] { "REC-000104" }, cliente!.Items.Select(item => item.Numero).ToArray());
        Assert.Equal(new[] { "REC-000104" }, vencidos!.Items.Select(item => item.Numero).ToArray());
        Assert.Equal(new[] { "REC-000104" }, porNumero!.Items.Select(item => item.Numero).ToArray());
        Assert.Equal(new[] { "REC-000104" }, porOrden!.Items.Select(item => item.Numero).ToArray());
        Assert.Equal(new[] { "REC-000104" }, porCliente!.Items.Select(item => item.Numero).ToArray());
    }

    [Fact]
    public async Task VencidoRequierePlazoEstrictamenteAnteriorYEstadoAbiertoOEnProceso()
    {
        DateTime ahora = DateTime.UtcNow;
        await InsertarReclamoAsync(110, "Abierto", "Alta", "Defecto", ahora.AddSeconds(30));
        await InsertarReclamoAsync(111, "Resuelto", "Alta", "Defecto", ahora.AddDays(-1));
        PaginaReclamosDto? respuesta = await _cliente.GetFromJsonAsync<PaginaReclamosDto>(
            "/api/v1/reclamos-postventa?vencidos=true");
        Assert.NotNull(respuesta);
        Assert.Empty(respuesta.Items);
    }

    [Fact]
    public async Task ListadoDesempataPorFechaLimiteYDespuesPorId()
    {
        DateTime fechaLimite = DateTime.UtcNow.AddDays(2);
        await InsertarReclamoAsync(302, "Abierto", "Alta", "Defecto", fechaLimite);
        await InsertarReclamoAsync(301, "Abierto", "Alta", "Defecto", fechaLimite);
        await InsertarReclamoAsync(303, "Abierto", "Media", "Defecto", fechaLimite.AddDays(-2));

        PaginaReclamosDto? listado = await _cliente.GetFromJsonAsync<PaginaReclamosDto>(
            "/api/v1/reclamos-postventa?tamanoPagina=10");

        Assert.NotNull(listado);
        Assert.Equal(new[] { 301, 302, 303 }, listado.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task ResumenVacioDevuelveMetricasEnCero()
    {
        ResumenReclamosDto? resumen = await _cliente.GetFromJsonAsync<ResumenReclamosDto>(
            "/api/v1/reclamos-postventa/resumen");
        Assert.NotNull(resumen);
        Assert.Equal(0, resumen.Total);
        Assert.Empty(resumen.PorTipo);
        Assert.Equal(0m, resumen.HorasPromedioResolucion);
        Assert.Equal(0m, resumen.CumplimientoPlazo);
    }

    [Fact]
    public async Task ResumenCalculaConteosHorasCumplimientoYOrdenDeTipos()
    {
        DateTime ahora = DateTime.UtcNow;
        await InsertarAsync(CrearReclamo(201, "Abierto", "Alta", "Defecto", ahora.AddDays(-1)));
        await InsertarAsync(CrearReclamo(202, "EnProceso", "Media", "Retraso", ahora.AddDays(1)));
        ReclamoPostventa resuelto = CrearReclamo(203, "Resuelto", "Alta", "Faltante", ahora.AddHours(-3),
            fecha: ahora.AddHours(-6));
        resuelto.FechaResolucion = ahora.AddHours(-4);
        await InsertarAsync(resuelto);
        ReclamoPostventa cerrado = CrearReclamo(204, "Cerrado", "Media", "Facturacion", ahora.AddHours(-48),
            fecha: ahora.AddHours(-48));
        cerrado.FechaResolucion = ahora.AddHours(-24);
        await InsertarAsync(cerrado);
        ReclamoPostventa rechazado = CrearReclamo(205, "Rechazado", "Baja", "Defecto", ahora.AddDays(-3));
        rechazado.MotivoRechazo = "No aplica";
        await InsertarAsync(rechazado);

        ResumenReclamosDto? resumen = await _cliente.GetFromJsonAsync<ResumenReclamosDto>(
            "/api/v1/reclamos-postventa/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(5, resumen.Total);
        Assert.Equal(1, resumen.Abiertos);
        Assert.Equal(1, resumen.EnProceso);
        Assert.Equal(1, resumen.Resueltos);
        Assert.Equal(1, resumen.Cerrados);
        Assert.Equal(1, resumen.Rechazados);
        Assert.Equal(1, resumen.Vencidos);
        Assert.Equal(13m, resumen.HorasPromedioResolucion);
        Assert.Equal(50m, resumen.CumplimientoPlazo);
        ReclamoDetalleDto? detalleResuelto = await _cliente.GetFromJsonAsync<ReclamoDetalleDto>(
            "/api/v1/reclamos-postventa/203");
        ReclamoDetalleDto? detalleCerrado = await _cliente.GetFromJsonAsync<ReclamoDetalleDto>(
            "/api/v1/reclamos-postventa/204");
        Assert.True(detalleResuelto!.AtendidoATiempo);
        Assert.False(detalleCerrado!.AtendidoATiempo);
        Assert.Equal(new[] { "Defecto", "Retraso", "Faltante", "Facturacion" },
            resumen.PorTipo.Select(item => item.Tipo).ToArray());
        Assert.Equal(new[] { 2, 1, 1, 1 }, resumen.PorTipo.Select(item => item.Reclamos).ToArray());
    }

    [Fact]
    public async Task OpenApiExponeLasNueveOperaciones()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement paths = documento.RootElement.GetProperty("paths");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(paths.GetProperty("/api/v1/reclamos-postventa").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty("/api/v1/reclamos-postventa").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/v1/reclamos-postventa/{id}").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty("/api/v1/reclamos-postventa/{id}").TryGetProperty("put", out _));
        Assert.True(paths.GetProperty("/api/v1/reclamos-postventa/{id}/iniciar").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/v1/reclamos-postventa/{id}/resolver").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/v1/reclamos-postventa/{id}/cerrar").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/v1/reclamos-postventa/{id}/rechazar").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/v1/reclamos-postventa/resumen").TryGetProperty("get", out _));
    }

    private static GuardarReclamoRequestDto SolicitudValida()
    {
        return new GuardarReclamoRequestDto
        {
            ClienteId = 1,
            OrdenCompraId = 1,
            ProductoId = 1,
            Tipo = "Defecto",
            Prioridad = "Alta",
            Descripcion = "Descripción válida"
        };
    }

    private async Task<ReclamoCreadoDto> CrearAsync()
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/reclamos-postventa", SolicitudValida());
        string contenido = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        ReclamoCreadoDto? creado = JsonSerializer.Deserialize<ReclamoCreadoDto>(contenido,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(creado);
        return creado;
    }

    private async Task<HttpResponseMessage> EjecutarAccionAsync(string accion, int id)
    {
        if (accion == "editar")
        {
            return await _cliente.PutAsJsonAsync($"/api/v1/reclamos-postventa/{id}", SolicitudValida());
        }

        if (accion == "iniciar" || accion == "cerrar")
        {
            return await _cliente.PostAsync($"/api/v1/reclamos-postventa/{id}/{accion}", null);
        }

        if (accion == "resolver")
        {
            return await _cliente.PostAsJsonAsync($"/api/v1/reclamos-postventa/{id}/resolver",
                new ResolverReclamoRequestDto { Resolucion = "Solución aplicada" });
        }

        return await _cliente.PostAsJsonAsync($"/api/v1/reclamos-postventa/{id}/rechazar",
            new RechazarReclamoRequestDto { Motivo = "No corresponde" });
    }

    private async Task<int> InsertarReclamoAsync(
        int id,
        string estado,
        string prioridad,
        string tipo,
        DateTime fechaLimite,
        string? numero = null,
        DateTime? fecha = null)
    {
        ReclamoPostventa reclamo = CrearReclamo(id, estado, prioridad, tipo, fechaLimite, numero, fecha);
        await InsertarAsync(reclamo);
        return id;
    }

    private static ReclamoPostventa CrearReclamo(
        int id,
        string estado,
        string prioridad,
        string tipo,
        DateTime fechaLimite,
        string? numero = null,
        DateTime? fecha = null)
    {
        return new ReclamoPostventa
        {
            Id = id,
            Numero = numero ?? $"REC-{id:D6}",
            Fecha = DateTime.SpecifyKind(fecha ?? DateTime.UtcNow.AddDays(-1), DateTimeKind.Utc),
            ClienteId = 1,
            OrdenCompraId = 1,
            ProductoId = 1,
            Tipo = tipo,
            Prioridad = prioridad,
            Descripcion = "Reclamo de prueba",
            Estado = estado,
            FechaLimite = DateTime.SpecifyKind(fechaLimite, DateTimeKind.Utc),
            FechaInicio = estado == "EnProceso" ? DateTime.UtcNow : null,
            FechaResolucion = estado == "Resuelto" || estado == "Cerrado" ? DateTime.UtcNow : null,
            FechaCierre = estado == "Cerrado" || estado == "Rechazado" ? DateTime.UtcNow : null,
            MotivoRechazo = estado == "Rechazado" ? "No corresponde" : null,
            Resolucion = estado == "Resuelto" || estado == "Cerrado" ? "Solución aplicada" : null
        };
    }

    private async Task InsertarAsync(ReclamoPostventa reclamo)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        await contexto.ReclamosPostventa.AddAsync(reclamo);
        await contexto.SaveChangesAsync();
    }

    private async Task<int> ContarAsync()
    {
        return await ConsultarAsync(contexto => contexto.ReclamosPostventa.CountAsync());
    }

    private async Task<string> ObtenerEstadoAsync(int id)
    {
        return await ConsultarAsync(async contexto =>
            (await contexto.ReclamosPostventa.SingleAsync(reclamo => reclamo.Id == id)).Estado);
    }

    private async Task<T> ConsultarAsync<T>(Func<TitanGlobalDbContext, Task<T>> consulta)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await consulta(contexto);
    }

    private void PrepararDatos()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        if (!contexto.DetallesOrdenCompra.Any(detalle => detalle.OrdenCompraId == 1 && detalle.ProductoId == 1))
        {
            contexto.DetallesOrdenCompra.Add(new DetalleOrdenCompra
            {
                OrdenCompraId = 1,
                ProductoId = 1,
                Cantidad = 1,
                PrecioUnitario = 2500m
            });
        }

        contexto.SaveChanges();
    }

    private static async Task AfirmarErrorAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode estado,
        int codigo,
        string mensaje)
    {
        string json = await respuesta.Content.ReadAsStringAsync();
        using JsonDocument documento = JsonDocument.Parse(json);
        JsonElement raiz = documento.RootElement;
        Assert.Equal(estado, respuesta.StatusCode);
        Assert.Equal(codigo, raiz.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, raiz.GetProperty("Message").GetString());
        Assert.False(raiz.TryGetProperty("Detail", out _));
    }
}
