using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;
using TitanGlobal.Api.Tests.Modulo06_ListasPrecios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo10_DevolucionesComerciales;

public class DevolucionesComercialesApiTests : IDisposable
{
    private readonly ListasPreciosApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public DevolucionesComercialesApiTests()
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
    public async Task CrearAsignaNumeroFinalYNoDejaNumeroTemporal()
    {
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, SolicitudValida());
        DevolucionCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<DevolucionCreadaDto>();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creada);
        Assert.Equal("DEV-000001", creada.Numero);
        List<string> numeros = await ConsultarAsync(contexto => contexto.DevolucionesComerciales
            .Select(devolucion => devolucion.Numero)
            .ToListAsync());
        Assert.Equal(new[] { "DEV-000001" }, numeros);
        Assert.DoesNotContain(numeros, numero => numero.StartsWith("TMP-", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("cliente", 400, "El cliente 999 no existe.")]
    [InlineData("orden", 400, "La orden de compra 999 no existe.")]
    [InlineData("cliente-orden", 400, "La orden OC-01 no pertenece al cliente.")]
    [InlineData("reclamo", 400, "El reclamo 999 no existe.")]
    [InlineData("reclamo-orden", 400, "El reclamo REC-000002 no es de la orden OC-01.")]
    [InlineData("motivo", 400, "El motivo no es válido.")]
    [InlineData("observaciones", 400, "Las observaciones no pueden superar 500 caracteres.")]
    [InlineData("lineas-vacias", 400, "La devolución debe tener al menos una línea.")]
    [InlineData("cantidad", 400, "La cantidad debe ser mayor que 0.")]
    [InlineData("repetido", 400, "El producto 1 está repetido en la devolución.")]
    [InlineData("producto", 400, "El producto 999 no existe.")]
    [InlineData("producto-orden", 400, "El producto 2 no está en la orden OC-02.")]
    [InlineData("disponible", 409, "Solo quedan 8 unidades de Laptop Pro por devolver en la orden OC-01.")]
    public async Task SolicitudInvalidaDevuelveMensajeLiteralYNoCreaFilas(string caso, int codigo, string mensaje)
    {
        GuardarDevolucionRequestDto solicitud = SolicitudValida();
        ConfigurarCasoInvalido(solicitud, caso);
        int filasAntes = await ContarDevolucionesAsync();

        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, solicitud);

        await AfirmarErrorAsync(respuesta, (HttpStatusCode)codigo, codigo, mensaje);
        Assert.Equal(filasAntes, await ContarDevolucionesAsync());
    }

    [Fact]
    public async Task DisponibleYDetalleUsanPrecioMedioPonderadoDeLineasRepetidas()
    {
        List<ProductoDisponibleDto>? disponibles = await _cliente.GetFromJsonAsync<List<ProductoDisponibleDto>>(
            $"{Ruta}/ordenes/1/disponible");
        HttpResponseMessage creada = await _cliente.PostAsJsonAsync(Ruta, SolicitudValida());
        DevolucionCreadaDto? dto = await creada.Content.ReadFromJsonAsync<DevolucionCreadaDto>();
        DevolucionDetalleDto? detalle = await _cliente.GetFromJsonAsync<DevolucionDetalleDto>($"{Ruta}/{dto!.Id}");

        Assert.NotNull(disponibles);
        ProductoDisponibleDto laptop = Assert.Single(disponibles, item => item.ProductoId == 1);
        Assert.Equal(8, laptop.Comprado);
        Assert.Equal(0, laptop.Devuelto);
        Assert.Equal(8, laptop.Disponible);
        Assert.Equal(118.75m, laptop.PrecioUnitario);
        Assert.Equal(HttpStatusCode.OK, creada.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal(237.50m, detalle.Total);
        Assert.Equal(118.75m, Assert.Single(detalle.Lineas).PrecioUnitario);
        Assert.Equal(237.50m, detalle.Lineas[0].Importe);
    }

    [Fact]
    public async Task DisponibleReflejaSolicitadasAprobadasYRecibidasComoComprometidas()
    {
        await InsertarDevolucionAsync(80, "Solicitada", "Defecto", 1, 2);
        await InsertarDevolucionAsync(81, "Aprobada", "Otro", 1, 3);
        await InsertarDevolucionAsync(82, "Recibida", "Arrepentimiento", 1, 1);
        await InsertarDevolucionAsync(83, "Rechazada", "ErrorPedido", 1, 1);

        List<ProductoDisponibleDto>? disponibles = await _cliente.GetFromJsonAsync<List<ProductoDisponibleDto>>(
            $"{Ruta}/ordenes/1/disponible");

        Assert.NotNull(disponibles);
        ProductoDisponibleDto laptop = Assert.Single(disponibles, item => item.ProductoId == 1);
        Assert.Equal(8, laptop.Comprado);
        Assert.Equal(6, laptop.Devuelto);
        Assert.Equal(2, laptop.Disponible);
    }

    [Fact]
    public async Task RechazarLiberaLasUnidadesComprometidas()
    {
        int id = await InsertarDevolucionAsync(80, "Solicitada", "Defecto", 1, 8);
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{Ruta}/{id}/rechazar",
            new RechazarDevolucionRequestDto { Motivo = "No corresponde" });
        List<ProductoDisponibleDto>? disponibles = await _cliente.GetFromJsonAsync<List<ProductoDisponibleDto>>(
            $"{Ruta}/ordenes/1/disponible");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(disponibles);
        ProductoDisponibleDto laptop = Assert.Single(disponibles, item => item.ProductoId == 1);
        Assert.Equal(0, laptop.Devuelto);
        Assert.Equal(8, laptop.Disponible);
    }

    [Fact]
    public async Task ActualizarNoRestaLaDevolucionActualDelDisponible()
    {
        int id = await InsertarDevolucionAsync(80, "Solicitada", "Defecto", 1, 5);
        GuardarDevolucionRequestDto solicitud = SolicitudValida();
        solicitud.Lineas[0].Cantidad = 8;

        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{Ruta}/{id}", solicitud);

        DevolucionDetalleDto? detalle = await _cliente.GetFromJsonAsync<DevolucionDetalleDto>($"{Ruta}/{id}");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal(8, detalle.Unidades);
        Assert.Equal(950m, detalle.Total);
    }

    [Fact]
    public async Task ActualizacionQueExcedeDisponibleNoCambiaEncabezadoNiLineas()
    {
        int id = await InsertarDevolucionAsync(90, "Solicitada", "Defecto", 1, 2);
        GuardarDevolucionRequestDto solicitud = SolicitudValida();
        solicitud.Lineas[0].Cantidad = 9;
        solicitud.Observaciones = "Cambio que debe rechazarse";

        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{Ruta}/{id}", solicitud);
        await AfirmarErrorAsync(respuesta, HttpStatusCode.Conflict, 409,
            "Solo quedan 8 unidades de Laptop Pro por devolver en la orden OC-01.");
        DevolucionDetalleDto? detalle = await _cliente.GetFromJsonAsync<DevolucionDetalleDto>($"{Ruta}/{id}");

        Assert.NotNull(detalle);
        Assert.Equal("Solicitada", detalle.Estado);
        Assert.Null(detalle.Observaciones);
        Assert.Equal(2, Assert.Single(detalle.Lineas).Cantidad);
    }

    [Fact]
    public async Task MotivoSeGuardaConValorCanonico()
    {
        GuardarDevolucionRequestDto solicitud = SolicitudValida();
        solicitud.Motivo = "aRrEpEnTiMiEnTo";
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, solicitud);
        DevolucionCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<DevolucionCreadaDto>();
        DevolucionDetalleDto? detalle = await _cliente.GetFromJsonAsync<DevolucionDetalleDto>($"{Ruta}/{creada!.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Arrepentimiento", detalle.Motivo);
    }

    [Theory]
    [InlineData("editar")]
    [InlineData("eliminar")]
    public async Task EdicionYEliminacionSePermitenSoloEnSolicitada(string accion)
    {
        int id = await InsertarDevolucionAsync(90, "Solicitada", "Defecto", 1, 1);

        HttpResponseMessage respuesta = await EjecutarAccionAsync(accion, id, "Solicitada");

        if (accion == "eliminar")
        {
            Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
            Assert.Equal(0, await ContarDevolucionesAsync());
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            Assert.Equal("Solicitada", await ObtenerEstadoAsync(id));
            DevolucionDetalleDto? detalle = await _cliente.GetFromJsonAsync<DevolucionDetalleDto>($"{Ruta}/{id}");
            Assert.NotNull(detalle);
            Assert.Equal(2, detalle.Unidades);
        }
    }

    [Theory]
    [InlineData("solicitada", "Solicitada")]
    [InlineData("APROBADA", "Aprobada")]
    [InlineData("recibida", "Recibida")]
    [InlineData("rechazada", "Rechazada")]
    public async Task FiltroEstadoIgnoraMayusculas(string filtro, string estado)
    {
        await InsertarDevolucionAsync(80, estado, "Defecto", 1, 1);

        PaginaDevolucionesDto? pagina = await _cliente.GetFromJsonAsync<PaginaDevolucionesDto>(
            $"{Ruta}?estado={filtro}");

        Assert.NotNull(pagina);
        Assert.Equal(1, pagina.Total);
        Assert.Equal(estado, Assert.Single(pagina.Items).Estado);
    }

    [Theory]
    [InlineData("estado", 400, "El estado no es válido.")]
    [InlineData("motivo", 400, "El motivo no es válido.")]
    [InlineData("pagina", 400, "La página debe ser mayor o igual a 1.")]
    [InlineData("tamano", 400, "El tamaño de página debe estar entre 1 y 100.")]
    public async Task FiltrosInvalidosDevuelvenErrorLiteral(string caso, int codigo, string mensaje)
    {
        string consulta = caso switch
        {
            "estado" => "?estado=desconocido",
            "motivo" => "?motivo=desconocido",
            "pagina" => "?pagina=0",
            _ => "?tamanoPagina=101"
        };

        HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + consulta);

        await AfirmarErrorAsync(respuesta, (HttpStatusCode)codigo, codigo, mensaje);
    }

    [Theory]
    [InlineData("OC-01")]
    [InlineData("Acme")]
    [InlineData("DEV-000001")]
    public async Task BusquedaEncuentraPorDevolucionOrdenOCliente(string busqueda)
    {
        await InsertarDevolucionAsync(1, "Solicitada", "Defecto", 1, 1);

        PaginaDevolucionesDto? pagina = await _cliente.GetFromJsonAsync<PaginaDevolucionesDto>(
            $"{Ruta}?busqueda={Uri.EscapeDataString(busqueda)}");

        Assert.NotNull(pagina);
        Assert.Equal(1, pagina.Total);
        Assert.Equal("DEV-000001", Assert.Single(pagina.Items).Numero);
    }

    [Fact]
    public async Task FiltroMotivoYClienteYOrdenacionYPaginacionMantienenTotal()
    {
        DateTime fecha = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        await InsertarDevolucionAsync(1, "Solicitada", "Defecto", 1, 1, fecha);
        await InsertarDevolucionAsync(2, "Aprobada", "Defecto", 1, 1, fecha);
        await InsertarDevolucionAsync(3, "Recibida", "Otro", 1, 2, fecha.AddDays(-1));

        PaginaDevolucionesDto? pagina = await _cliente.GetFromJsonAsync<PaginaDevolucionesDto>(
            $"{Ruta}?clienteId=1&motivo=defecto&pagina=1&tamanoPagina=1");

        Assert.NotNull(pagina);
        Assert.Equal(2, pagina.Total);
        Assert.Equal(1, pagina.Pagina);
        Assert.Equal(1, pagina.TamanoPagina);
        Assert.Equal("DEV-000002", Assert.Single(pagina.Items).Numero);
    }

    [Theory]
    [InlineData("aprobar", "Aprobada", "Aprobada", "Solicitada")]
    [InlineData("aprobar", "Recibida", "Recibida", "Solicitada")]
    [InlineData("aprobar", "Rechazada", "Rechazada", "Solicitada")]
    [InlineData("recibir", "Solicitada", "Solicitada", "Aprobada")]
    [InlineData("recibir", "Recibida", "Recibida", "Aprobada")]
    [InlineData("recibir", "Rechazada", "Rechazada", "Aprobada")]
    [InlineData("rechazar", "Recibida", "Recibida", "Solicitada")]
    [InlineData("rechazar", "Rechazada", "Rechazada", "Aprobada")]
    [InlineData("editar", "Aprobada", "Aprobada", "Solicitada")]
    [InlineData("editar", "Recibida", "Recibida", "Solicitada")]
    [InlineData("editar", "Rechazada", "Rechazada", "Solicitada")]
    [InlineData("eliminar", "Aprobada", "Aprobada", "Solicitada")]
    [InlineData("eliminar", "Recibida", "Recibida", "Solicitada")]
    [InlineData("eliminar", "Rechazada", "Rechazada", "Solicitada")]
    public async Task TransicionProhibidaDevuelve409LiteralYNoCambiaEstado(
        string accion,
        string estado,
        string estadoLegible,
        string estadoRequerido)
    {
        int id = await InsertarDevolucionAsync(90, estado, "Defecto", 1, 1);

        HttpResponseMessage respuesta = await EjecutarAccionAsync(accion, id, estadoRequerido);

        await AfirmarErrorAsync(respuesta, HttpStatusCode.Conflict, 409,
            $"La devolución DEV-000090 está {estadoLegible.ToLowerInvariant()}.");
        Assert.Equal(estado, await ObtenerEstadoAsync(id));
    }

    [Theory]
    [InlineData("aprobar", "Solicitada", "Aprobada")]
    [InlineData("recibir", "Aprobada", "Recibida")]
    [InlineData("rechazar", "Solicitada", "Rechazada")]
    [InlineData("rechazar-aprobada", "Aprobada", "Rechazada")]
    public async Task TransicionPermitidaGuardaEstadoYFecha(string accion, string estado, string esperado)
    {
        string accionReal = accion == "rechazar-aprobada" ? "rechazar" : accion;
        int id = await InsertarDevolucionAsync(90, estado, "Defecto", 1, 1);

        HttpResponseMessage respuesta = await EjecutarAccionAsync(accionReal, id, estado);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(esperado, await ObtenerEstadoAsync(id));
        DevolucionDetalleDto? detalle = await _cliente.GetFromJsonAsync<DevolucionDetalleDto>($"{Ruta}/{id}");
        Assert.NotNull(detalle);
        if (accionReal == "aprobar")
        {
            Assert.NotNull(detalle.FechaAprobacion);
        }

        if (accionReal == "recibir")
        {
            Assert.NotNull(detalle.FechaRecepcion);
        }

        if (accionReal == "rechazar")
        {
            Assert.NotNull(detalle.FechaRechazo);
        }
    }

    [Theory]
    [InlineData("rechazar-vacio", "El motivo de rechazo es obligatorio.")]
    [InlineData("rechazar-largo", "El motivo de rechazo no puede superar 500 caracteres.")]
    public async Task RechazoInvalidoNoCambiaEstado(string caso, string mensaje)
    {
        int id = await InsertarDevolucionAsync(90, "Solicitada", "Defecto", 1, 1);
        string motivo = caso == "rechazar-vacio" ? " " : new string('X', 501);

        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{Ruta}/{id}/rechazar",
            new RechazarDevolucionRequestDto { Motivo = motivo });

        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal("Solicitada", await ObtenerEstadoAsync(id));
    }

    [Theory]
    [InlineData("obtener")]
    [InlineData("actualizar")]
    [InlineData("aprobar")]
    [InlineData("recibir")]
    [InlineData("rechazar")]
    [InlineData("eliminar")]
    public async Task IdentificadorInexistenteDevuelve404Literal(string accion)
    {
        HttpResponseMessage respuesta = await EjecutarAccionAsync(accion, 999, "Solicitada");

        await AfirmarErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "La devolución 999 no existe.");
    }

    [Fact]
    public async Task OrdenInexistenteEnDisponiblesDevuelve404Literal()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync($"{Ruta}/ordenes/999/disponible");

        await AfirmarErrorAsync(respuesta, HttpStatusCode.NotFound, 404,
            "La orden de compra 999 no existe.");
    }

    [Fact]
    public async Task ResumenCuentaSoloReembolsosRecibidosYTasaExacta()
    {
        await InsertarDevolucionAsync(1, "Recibida", "Defecto", 1, 2);
        await InsertarDevolucionAsync(2, "Aprobada", "Defecto", 1, 1);
        await InsertarDevolucionAsync(3, "Rechazada", "ErrorPedido", 1, 2);
        await InsertarDevolucionAsync(4, "Solicitada", "Arrepentimiento", 1, 1, ordenId: 1, productoId: 2);

        ResumenDevolucionesDto? resumen = await _cliente.GetFromJsonAsync<ResumenDevolucionesDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(4, resumen.Total);
        Assert.Equal(1, resumen.Solicitadas);
        Assert.Equal(1, resumen.Aprobadas);
        Assert.Equal(1, resumen.Recibidas);
        Assert.Equal(1, resumen.Rechazadas);
        Assert.Equal(237.50m, resumen.MontoReembolsado);
        Assert.Equal(14.29m, resumen.TasaDevolucion);
        Assert.Equal(new[] { "Defecto", "ErrorPedido", "Arrepentimiento" },
            resumen.PorMotivo.Select(item => item.Motivo));
        Assert.Equal(new[] { 3, 2, 1 }, resumen.PorMotivo.Select(item => item.Unidades));
    }

    [Fact]
    public async Task ResumenSinVentasDevuelveTasaCero()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.DetallesOrdenCompra.RemoveRange(contexto.DetallesOrdenCompra);
        await contexto.SaveChangesAsync();

        ResumenDevolucionesDto? resumen = await _cliente.GetFromJsonAsync<ResumenDevolucionesDto>($"{Ruta}/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(0m, resumen.TasaDevolucion);
    }

    [Fact]
    public async Task OpenApiPublicaLasDiezOperaciones()
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");
        string[] operaciones = new[]
        {
            "get:/api/v1/devoluciones-comerciales",
            "post:/api/v1/devoluciones-comerciales",
            "get:/api/v1/devoluciones-comerciales/{id}",
            "put:/api/v1/devoluciones-comerciales/{id}",
            "post:/api/v1/devoluciones-comerciales/{id}/aprobar",
            "post:/api/v1/devoluciones-comerciales/{id}/recibir",
            "post:/api/v1/devoluciones-comerciales/{id}/rechazar",
            "delete:/api/v1/devoluciones-comerciales/{id}",
            "get:/api/v1/devoluciones-comerciales/ordenes/{ordenId}/disponible",
            "get:/api/v1/devoluciones-comerciales/resumen"
        };

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        foreach (string operacion in operaciones)
        {
            string[] partes = operacion.Split(':', 2);
            Assert.True(rutas.GetProperty(partes[1]).TryGetProperty(partes[0], out _), operacion);
        }
    }

    private const string Ruta = "/api/v1/devoluciones-comerciales";

    private static GuardarDevolucionRequestDto SolicitudValida()
    {
        return new GuardarDevolucionRequestDto
        {
            ClienteId = 1,
            OrdenCompraId = 1,
            ReclamoPostventaId = 1,
            Motivo = "Defecto",
            Observaciones = "Solicitud de devolución",
            Lineas = new List<LineaGuardarDevolucionDto>
            {
                new LineaGuardarDevolucionDto { ProductoId = 1, Cantidad = 2 }
            }
        };
    }

    private static void ConfigurarCasoInvalido(GuardarDevolucionRequestDto solicitud, string caso)
    {
        switch (caso)
        {
            case "cliente":
                solicitud.ClienteId = 999;
                break;
            case "orden":
                solicitud.OrdenCompraId = 999;
                solicitud.ReclamoPostventaId = null;
                break;
            case "cliente-orden":
                solicitud.ClienteId = 2;
                solicitud.ReclamoPostventaId = null;
                break;
            case "reclamo":
                solicitud.ReclamoPostventaId = 999;
                break;
            case "reclamo-orden":
                solicitud.ReclamoPostventaId = 2;
                break;
            case "motivo":
                solicitud.Motivo = "invalido";
                break;
            case "observaciones":
                solicitud.Observaciones = new string('X', 501);
                break;
            case "lineas-vacias":
                solicitud.Lineas.Clear();
                break;
            case "cantidad":
                solicitud.Lineas[0].Cantidad = 0;
                break;
            case "repetido":
                solicitud.Lineas.Add(new LineaGuardarDevolucionDto { ProductoId = 1, Cantidad = 1 });
                break;
            case "producto":
                solicitud.Lineas[0].ProductoId = 999;
                break;
            case "producto-orden":
                solicitud.OrdenCompraId = 2;
                solicitud.ReclamoPostventaId = null;
                solicitud.Lineas[0].ProductoId = 2;
                break;
            case "disponible":
                solicitud.Lineas[0].Cantidad = 9;
                break;
        }
    }

    private async Task<HttpResponseMessage> EjecutarAccionAsync(string accion, int id, string estado)
    {
        if (accion == "obtener")
        {
            return await _cliente.GetAsync($"{Ruta}/{id}");
        }

        if (accion == "actualizar" || accion == "editar")
        {
            return await _cliente.PutAsJsonAsync($"{Ruta}/{id}", SolicitudValida());
        }

        if (accion == "eliminar")
        {
            return await _cliente.DeleteAsync($"{Ruta}/{id}");
        }

        if (accion == "rechazar")
        {
            return await _cliente.PostAsJsonAsync(
                $"{Ruta}/{id}/rechazar",
                new RechazarDevolucionRequestDto { Motivo = "Motivo de prueba" });
        }

        string ruta = accion == "aprobar" ? "aprobar" : "recibir";
        return await _cliente.PostAsync($"{Ruta}/{id}/{ruta}", null);
    }

    private async Task<int> InsertarDevolucionAsync(
        int id,
        string estado,
        string motivo,
        int clienteId,
        int cantidad,
        DateTime? fecha = null,
        int ordenId = 1,
        int productoId = 1)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        DevolucionComercial devolucion = new DevolucionComercial
        {
            Id = id,
            Numero = $"DEV-{id:D6}",
            Fecha = fecha ?? DateTime.UtcNow,
            ClienteId = clienteId,
            OrdenCompraId = ordenId,
            Motivo = motivo,
            Estado = estado,
            Detalles = new List<DetalleDevolucion>
            {
                new DetalleDevolucion { ProductoId = productoId, Cantidad = cantidad }
            }
        };
        contexto.DevolucionesComerciales.Add(devolucion);
        await contexto.SaveChangesAsync();
        return id;
    }

    private void PrepararDatos()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.DetallesOrdenCompra.AddRange(
            new DetalleOrdenCompra { Id = 1, OrdenCompraId = 1, ProductoId = 1, Cantidad = 5, PrecioUnitario = 100m },
            new DetalleOrdenCompra { Id = 2, OrdenCompraId = 1, ProductoId = 1, Cantidad = 3, PrecioUnitario = 150m },
            new DetalleOrdenCompra { Id = 3, OrdenCompraId = 1, ProductoId = 2, Cantidad = 2, PrecioUnitario = 40m },
            new DetalleOrdenCompra { Id = 4, OrdenCompraId = 2, ProductoId = 1, Cantidad = 4, PrecioUnitario = 120m });
        contexto.ReclamosPostventa.AddRange(
            new ReclamoPostventa
            {
                Id = 1,
                Numero = "REC-000001",
                Fecha = DateTime.UtcNow,
                ClienteId = 1,
                OrdenCompraId = 1,
                Tipo = "Defecto",
                Prioridad = "Alta",
                Descripcion = "Reclamo sobre orden 1",
                Estado = "Abierto",
                FechaLimite = DateTime.UtcNow.AddDays(2)
            },
            new ReclamoPostventa
            {
                Id = 2,
                Numero = "REC-000002",
                Fecha = DateTime.UtcNow,
                ClienteId = 1,
                OrdenCompraId = 2,
                Tipo = "Retraso",
                Prioridad = "Media",
                Descripcion = "Reclamo sobre orden 2",
                Estado = "Abierto",
                FechaLimite = DateTime.UtcNow.AddDays(5)
            });
        contexto.SaveChanges();
    }

    private async Task<string> ObtenerEstadoAsync(int id)
    {
        return await ConsultarAsync(contexto => contexto.DevolucionesComerciales
            .Where(devolucion => devolucion.Id == id)
            .Select(devolucion => devolucion.Estado)
            .SingleAsync());
    }

    private async Task<int> ContarDevolucionesAsync()
    {
        return await ConsultarAsync(contexto => contexto.DevolucionesComerciales.CountAsync());
    }

    private async Task<T> ConsultarAsync<T>(Func<TitanGlobalDbContext, Task<T>> consulta)
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await consulta(contexto);
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
