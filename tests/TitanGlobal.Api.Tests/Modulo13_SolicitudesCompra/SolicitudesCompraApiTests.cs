using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo13_SolicitudesCompra;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo13_SolicitudesCompra;

[Collection("Modulo04")]
public class SolicitudesCompraApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public SolicitudesCompraApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Fact]
    public async Task CreateAssignsNumberPendingStateAndUtcDate()
    {
        GuardarSolicitudRequestDto solicitud = CrearSolicitud("  Ana Compras  ", 1);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/solicitudes-compra", solicitud);
        SolicitudCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<SolicitudCreadaDto>();
        SolicitudDetalleDto? detalle = creada is null
            ? null
            : await _clienteHttp.GetFromJsonAsync<SolicitudDetalleDto>($"/api/v1/solicitudes-compra/{creada.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creada);
        Assert.Equal($"SC-{creada.Id:D6}", creada.Numero);
        Assert.NotNull(detalle);
        Assert.Equal("Ana Compras", detalle.Solicitante);
        Assert.Equal(creada.Numero, detalle.Numero);
        Assert.Equal("Pendiente", detalle.Estado);
        Assert.Equal(DateTimeKind.Utc, detalle.Fecha.Kind);
        Assert.Null(detalle.FechaDecision);

        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        SolicitudCompra? guardada = await contexto.SolicitudesCompra
            .AsNoTracking()
            .SingleOrDefaultAsync(entidad => entidad.Id == creada.Id);
        long solicitudesTemporales = await contexto.SolicitudesCompra
            .LongCountAsync(entidad => entidad.Numero.StartsWith("TMP-"));

        Assert.NotNull(guardada);
        Assert.Matches("^SC-[0-9]{6}$", guardada.Numero);
        Assert.Equal(creada.Numero, guardada.Numero);
        Assert.Equal(0, solicitudesTemporales);
    }

    [Fact]
    public async Task UpdateReplacesLinesAndRecalculatesTotal()
    {
        GuardarSolicitudRequestDto solicitud = CrearSolicitud("Ana Compras actualizada", 1);
        solicitud.Lineas = new List<LineaSolicitudRequestDto>
        {
            new LineaSolicitudRequestDto { ProductoId = 2, Cantidad = 3, PrecioEstimado = 12.50m }
        };
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/solicitudes-compra/1", solicitud);
        SolicitudDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<SolicitudDetalleDto>("/api/v1/solicitudes-compra/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("SC-000001", detalle.Numero);
        Assert.Equal("Ana Compras actualizada", detalle.Solicitante);
        LineaSolicitudDto linea = Assert.Single(detalle.Lineas);
        Assert.Equal(2, linea.ProductoId);
        Assert.Equal(37.50m, detalle.MontoEstimado);
        Assert.Equal(37.50m, linea.Subtotal);
    }

    [Fact]
    public async Task ApproveSetsStateAndUtcDecisionDate()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsync("/api/v1/solicitudes-compra/1/aprobar", null);
        SolicitudDetalleDto? detalle = await respuesta.Content.ReadFromJsonAsync<SolicitudDetalleDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Aprobada", detalle.Estado);
        Assert.NotNull(detalle.FechaDecision);
        Assert.Equal(DateTimeKind.Utc, detalle.FechaDecision.Value.Kind);
    }

    [Fact]
    public async Task RejectTrimsReasonAndSetsUtcDecisionDate()
    {
        RechazarSolicitudRequestDto solicitudRechazo = new RechazarSolicitudRequestDto { Motivo = "  Falta presupuesto  " };
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/solicitudes-compra/1/rechazar",
            solicitudRechazo);
        SolicitudDetalleDto? detalle = await respuesta.Content.ReadFromJsonAsync<SolicitudDetalleDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Rechazada", detalle.Estado);
        Assert.Equal("Falta presupuesto", detalle.MotivoRechazo);
        Assert.NotNull(detalle.FechaDecision);
        Assert.Equal(DateTimeKind.Utc, detalle.FechaDecision.Value.Kind);
    }

    [Fact]
    public async Task ListFiltersByCaseInsensitiveStateSupplierSearchAndInclusiveDateRange()
    {
        PaginaSolicitudesDto? porEstadoYProveedor = await _clienteHttp.GetFromJsonAsync<PaginaSolicitudesDto>(
            "/api/v1/solicitudes-compra?estado=pEnDiEnTe&proveedorId=1");
        PaginaSolicitudesDto? porNumero = await _clienteHttp.GetFromJsonAsync<PaginaSolicitudesDto>(
            "/api/v1/solicitudes-compra?busqueda=sc-000002");
        PaginaSolicitudesDto? porSolicitante = await _clienteHttp.GetFromJsonAsync<PaginaSolicitudesDto>(
            "/api/v1/solicitudes-compra?busqueda=ANA");
        PaginaSolicitudesDto? porRango = await _clienteHttp.GetFromJsonAsync<PaginaSolicitudesDto>(
            "/api/v1/solicitudes-compra?fechaDesde=2026-02-15&fechaHasta=2026-02-15");

        Assert.NotNull(porEstadoYProveedor);
        Assert.Equal(new[] { 1 }, porEstadoYProveedor.Items.Select(solicitud => solicitud.Id).ToArray());
        Assert.NotNull(porNumero);
        Assert.Equal(new[] { 2 }, porNumero.Items.Select(solicitud => solicitud.Id).ToArray());
        Assert.NotNull(porSolicitante);
        Assert.Equal(new[] { 1 }, porSolicitante.Items.Select(solicitud => solicitud.Id).ToArray());
        Assert.NotNull(porRango);
        Assert.Equal(new[] { 2 }, porRango.Items.Select(solicitud => solicitud.Id).ToArray());
        Assert.Equal("Pendiente", porRango.Items[0].Estado);
    }

    [Fact]
    public async Task ListOrdersByDateThenIdAndReturnsPaginationTotal()
    {
        PaginaSolicitudesDto? pagina = await _clienteHttp.GetFromJsonAsync<PaginaSolicitudesDto>(
            "/api/v1/solicitudes-compra?pagina=2&tamanoPagina=2");

        Assert.NotNull(pagina);
        Assert.Equal(5, pagina.Total);
        Assert.Equal(2, pagina.Pagina);
        Assert.Equal(2, pagina.TamanoPagina);
        Assert.Equal(new[] { 3, 2 }, pagina.Items.Select(solicitud => solicitud.Id).ToArray());
    }

    [Fact]
    public async Task DetailReturnsLineSubtotalsAndTotal()
    {
        SolicitudDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<SolicitudDetalleDto>(
            "/api/v1/solicitudes-compra/1");

        Assert.NotNull(detalle);
        Assert.Equal("SC-000001", detalle.Numero);
        Assert.Equal("Ana Compras", detalle.Solicitante);
        Assert.Equal("Andes Suministros S.A.C.", detalle.ProveedorNombre);
        Assert.Equal(DateTimeKind.Utc, detalle.Fecha.Kind);
        Assert.Equal(245m, detalle.MontoEstimado);
        Assert.Equal(new[] { 1, 3 }, detalle.Lineas.Select(linea => linea.ProductoId).ToArray());
        Assert.Equal(new[] { "Laptop Pro", "Mouse Óptico" }, detalle.Lineas.Select(linea => linea.ProductoNombre).ToArray());
        Assert.Equal(new[] { 200m, 45m }, detalle.Lineas.Select(linea => linea.Subtotal).ToArray());
    }

    [Fact]
    public async Task SummaryReturnsExactStatesAmountsAndProductRankingExcludingRejected()
    {
        ResumenSolicitudesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenSolicitudesDto>(
            "/api/v1/solicitudes-compra/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(5, resumen.Total);
        Assert.Equal(2, resumen.Pendientes);
        Assert.Equal(2, resumen.Aprobadas);
        Assert.Equal(1, resumen.Rechazadas);
        Assert.Equal(285m, resumen.MontoPendiente);
        Assert.Equal(480m, resumen.MontoAprobado);
        Assert.Equal(new[] { 2, 1, 3, 4 }, resumen.TopProductosSolicitados.Select(producto => producto.ProductoId).ToArray());
        Assert.Equal(new[] { 7, 5, 3, 2 }, resumen.TopProductosSolicitados.Select(producto => producto.Unidades).ToArray());
        Assert.Equal(new[] { "Laptop Básica", "Laptop Pro", "Mouse Óptico", "Teclado Mecánico" }, resumen.TopProductosSolicitados.Select(producto => producto.Nombre).ToArray());
    }

    [Theory]
    [InlineData("solicitante-vacio", "El solicitante es obligatorio.")]
    [InlineData("solicitante-largo", "El solicitante no puede superar 100 caracteres.")]
    [InlineData("observaciones-largas", "Las observaciones no pueden superar 500 caracteres.")]
    [InlineData("sin-lineas", "La solicitud debe tener al menos una línea.")]
    [InlineData("cantidad-cero", "La cantidad debe ser mayor que 0.")]
    [InlineData("precio-negativo", "El precio estimado no puede ser negativo.")]
    [InlineData("producto-repetido", "El producto 1 está repetido en la solicitud.")]
    [InlineData("producto-inexistente", "El producto 9999 no existe.")]
    [InlineData("proveedor-inexistente", "El proveedor 9999 no existe.")]
    [InlineData("proveedor-inactivo", "El proveedor 2 está inactivo.")]
    public async Task InvalidCreateReturnsLiteralBadRequestAndDoesNotAddRows(string caso, string mensaje)
    {
        long totalAntes = await ContarSolicitudesAsync();
        GuardarSolicitudRequestDto solicitud = CrearSolicitud("Ana Compras", 1);
        AplicarCasoInvalido(solicitud, caso);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/solicitudes-compra", solicitud);
        long totalDespues = await ContarSolicitudesAsync();

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(totalAntes, totalDespues);
    }

    [Fact]
    public async Task InvalidUpdateReturnsBadRequestWithoutChangingExistingRequest()
    {
        GuardarSolicitudRequestDto solicitud = CrearSolicitud("Nombre que no debe guardarse", 1);
        Assert.NotNull(solicitud.Lineas);
        solicitud.Lineas[0].Cantidad = 0;

        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/solicitudes-compra/1", solicitud);
        SolicitudDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<SolicitudDetalleDto>(
            "/api/v1/solicitudes-compra/1");

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La cantidad debe ser mayor que 0.");
        Assert.NotNull(detalle);
        Assert.Equal("Ana Compras", detalle.Solicitante);
        Assert.Equal(245m, detalle.MontoEstimado);
    }

    [Fact]
    public async Task BlankObservationsBecomeNull()
    {
        GuardarSolicitudRequestDto solicitud = CrearSolicitud("Nueva solicitud", 1);
        solicitud.Observaciones = "   ";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/solicitudes-compra", solicitud);
        SolicitudCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<SolicitudCreadaDto>();
        SolicitudDetalleDto? detalle = creada is null
            ? null
            : await _clienteHttp.GetFromJsonAsync<SolicitudDetalleDto>($"/api/v1/solicitudes-compra/{creada.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Null(detalle.Observaciones);
    }

    [Theory]
    [InlineData("", "El motivo de rechazo es obligatorio.")]
    [InlineData("   ", "El motivo de rechazo es obligatorio.")]
    [InlineData("largo", "El motivo de rechazo no puede superar 500 caracteres.")]
    public async Task InvalidRejectionReturnsBadRequestWithoutChangingRequest(string motivo, string mensaje)
    {
        RechazarSolicitudRequestDto solicitud = new RechazarSolicitudRequestDto
        {
            Motivo = motivo == "largo" ? new string('M', 501) : motivo
        };
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/solicitudes-compra/1/rechazar",
            solicitud);
        SolicitudDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<SolicitudDetalleDto>(
            "/api/v1/solicitudes-compra/1");

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.NotNull(detalle);
        Assert.Equal("Pendiente", detalle.Estado);
        Assert.Null(detalle.FechaDecision);
    }

    [Theory]
    [InlineData("/api/v1/solicitudes-compra?estado=archivada", "El estado no es válido.")]
    [InlineData("/api/v1/solicitudes-compra?fechaDesde=2026-03-01&fechaHasta=2026-02-01", "La fecha desde no puede ser posterior a la fecha hasta.")]
    public async Task InvalidFiltersReturnBadRequestWithLiteralMessage(string ruta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(ruta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("APROBAR")]
    [InlineData("RECHAZAR")]
    public async Task MissingRequestReturnsNotFoundWithLiteralMessage(string metodo)
    {
        HttpResponseMessage respuesta;
        if (metodo == "GET")
        {
            respuesta = await _clienteHttp.GetAsync("/api/v1/solicitudes-compra/9999");
        }
        else if (metodo == "PUT")
        {
            respuesta = await _clienteHttp.PutAsJsonAsync(
                "/api/v1/solicitudes-compra/9999",
                CrearSolicitud("Solicitud nueva", 1));
        }
        else if (metodo == "DELETE")
        {
            respuesta = await _clienteHttp.DeleteAsync("/api/v1/solicitudes-compra/9999");
        }
        else if (metodo == "APROBAR")
        {
            respuesta = await _clienteHttp.PostAsync("/api/v1/solicitudes-compra/9999/aprobar", null);
        }
        else
        {
            respuesta = await _clienteHttp.PostAsJsonAsync(
                "/api/v1/solicitudes-compra/9999/rechazar",
                new RechazarSolicitudRequestDto { Motivo = "Sin presupuesto" });
        }

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.NotFound,
            404,
            "La solicitud de compra 9999 no existe.");
    }

    [Theory]
    [InlineData(3, "EDITAR")]
    [InlineData(5, "EDITAR")]
    [InlineData(3, "BORRAR")]
    [InlineData(5, "BORRAR")]
    [InlineData(3, "APROBAR")]
    [InlineData(5, "APROBAR")]
    [InlineData(3, "RECHAZAR")]
    [InlineData(5, "RECHAZAR")]
    public async Task FinalRequestsCannotBeEditedDeletedOrDecidedAgain(int id, string accion)
    {
        HttpResponseMessage respuesta = await EjecutarAccionAsync(id, accion);
        string estadoEsperado = id == 3 ? "aprobada" : "rechazada";
        string mensaje = $"La solicitud SC-{id:D6} ya está {estadoEsperado}.";

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.Conflict, 409, mensaje);
        SolicitudDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<SolicitudDetalleDto>(
            $"/api/v1/solicitudes-compra/{id}");
        Assert.NotNull(detalle);
        Assert.Equal(id == 3 ? "Aprobada" : "Rechazada", detalle.Estado);
    }

    [Fact]
    public async Task DeletePendingRequestAlsoRemovesItsLines()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync("/api/v1/solicitudes-compra/1");
        long lineasRestantes = await ContarLineasDeSolicitudAsync(1);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(0, lineasRestantes);
    }

    [Fact]
    public async Task OpenApiContainsAllEightRequestOperations()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty("/api/v1/solicitudes-compra").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/solicitudes-compra").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/solicitudes-compra/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/solicitudes-compra/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/solicitudes-compra/{id}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty("/api/v1/solicitudes-compra/{id}/aprobar").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/solicitudes-compra/{id}/rechazar").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/solicitudes-compra/resumen").TryGetProperty("get", out _));
    }

    private async Task<HttpResponseMessage> EjecutarAccionAsync(int id, string accion)
    {
        if (accion == "EDITAR")
        {
            return await _clienteHttp.PutAsJsonAsync(
                $"/api/v1/solicitudes-compra/{id}",
                CrearSolicitud("Cambio bloqueado", 1));
        }

        if (accion == "BORRAR")
        {
            return await _clienteHttp.DeleteAsync($"/api/v1/solicitudes-compra/{id}");
        }

        if (accion == "APROBAR")
        {
            return await _clienteHttp.PostAsync($"/api/v1/solicitudes-compra/{id}/aprobar", null);
        }

        return await _clienteHttp.PostAsJsonAsync(
            $"/api/v1/solicitudes-compra/{id}/rechazar",
            new RechazarSolicitudRequestDto { Motivo = "Nuevo motivo" });
    }

    private void RestablecerDatos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.DetallesSolicitudCompra.RemoveRange(contexto.DetallesSolicitudCompra);
        contexto.SolicitudesCompra.RemoveRange(contexto.SolicitudesCompra);
        contexto.SaveChanges();
        contexto.Proveedores.RemoveRange(contexto.Proveedores);
        Proveedor proveedorActivo = new Proveedor
        {
            Id = 1,
            RazonSocial = "Andes Suministros S.A.C.",
            Ruc = "20600000001",
            Pais = "Peru",
            Activo = true
        };
        Proveedor proveedorInactivo = new Proveedor
        {
            Id = 2,
            RazonSocial = "Proveedor Inactivo",
            Ruc = "20600000002",
            Pais = "Peru",
            Activo = false
        };
        contexto.Proveedores.AddRange(proveedorActivo, proveedorInactivo);
        contexto.SaveChanges();
        Producto[] productos = contexto.Productos.OrderBy(producto => producto.Id).ToArray();

        SolicitudCompra[] solicitudes =
        {
            CrearSolicitudSemilla(contexto, productos, 1, "Ana Compras", DateTime.SpecifyKind(new DateTime(2026, 1, 10, 9, 0, 0), DateTimeKind.Utc), "Pendiente", 1, null, 1, 2, 100m, 3, 3, 15m),
            CrearSolicitudSemilla(contexto, productos, 2, "Luis Almacén", DateTime.SpecifyKind(new DateTime(2026, 2, 15, 14, 30, 0), DateTimeKind.Utc), "Pendiente", null, null, 2, 1, 40m),
            CrearSolicitudSemilla(contexto, productos, 3, "María Planta", DateTime.SpecifyKind(new DateTime(2026, 3, 10, 11, 0, 0), DateTimeKind.Utc), "Aprobada", 1, DateTime.SpecifyKind(new DateTime(2026, 3, 11, 10, 0, 0), DateTimeKind.Utc), 1, 3, 100m, 2, 5, 10m),
            CrearSolicitudSemilla(contexto, productos, 4, "José Operaciones", DateTime.SpecifyKind(new DateTime(2026, 4, 20, 8, 0, 0), DateTimeKind.Utc), "Aprobada", 1, DateTime.SpecifyKind(new DateTime(2026, 4, 21, 8, 0, 0), DateTimeKind.Utc), 2, 1, 90m, 4, 2, 20m),
            CrearSolicitudSemilla(contexto, productos, 5, "Elena Taller", DateTime.SpecifyKind(new DateTime(2026, 5, 1, 12, 0, 0), DateTimeKind.Utc), "Rechazada", 1, DateTime.SpecifyKind(new DateTime(2026, 5, 2, 12, 0, 0), DateTimeKind.Utc), 1, 100, 100m, 2, 100, 10m)
        };
        solicitudes[4].MotivoRechazo = "Solicitud fuera de presupuesto";
        contexto.SolicitudesCompra.AddRange(solicitudes);
        contexto.SaveChanges();
    }

    private static SolicitudCompra CrearSolicitudSemilla(
        TitanGlobalDbContext contexto,
        Producto[] productos,
        int id,
        string solicitante,
        DateTime fecha,
        string estado,
        int? proveedorId,
        DateTime? fechaDecision,
        int productoIdUno,
        int cantidadUno,
        decimal precioUno,
        int? productoIdDos = null,
        int cantidadDos = 0,
        decimal precioDos = 0m)
    {
        SolicitudCompra solicitud = new SolicitudCompra
        {
            Id = id,
            Numero = $"SC-{id:D6}",
            Fecha = fecha,
            Solicitante = solicitante,
            ProveedorId = proveedorId,
            Estado = estado,
            FechaDecision = fechaDecision,
            MotivoRechazo = estado == "Rechazada" ? "Solicitud fuera de presupuesto" : null,
            Proveedor = proveedorId.HasValue
                ? contexto.Proveedores.Single(proveedor => proveedor.Id == proveedorId.Value)
                : null
        };
        AgregarLinea(solicitud, productos, productoIdUno, cantidadUno, precioUno);
        if (productoIdDos.HasValue)
        {
            AgregarLinea(solicitud, productos, productoIdDos.Value, cantidadDos, precioDos);
        }

        return solicitud;
    }

    private static void AgregarLinea(
        SolicitudCompra solicitud,
        Producto[] productos,
        int productoId,
        int cantidad,
        decimal precio)
    {
        Producto producto = productos.Single(entidad => entidad.Id == productoId);
        solicitud.Detalles.Add(new DetalleSolicitudCompra
        {
            SolicitudCompra = solicitud,
            Producto = producto,
            ProductoId = productoId,
            Cantidad = cantidad,
            PrecioEstimado = precio
        });
    }

    private static GuardarSolicitudRequestDto CrearSolicitud(string solicitante, int? proveedorId)
    {
        return new GuardarSolicitudRequestDto
        {
            Solicitante = solicitante,
            ProveedorId = proveedorId,
            Observaciones = "Compra operativa",
            Lineas = new List<LineaSolicitudRequestDto>
            {
                new LineaSolicitudRequestDto { ProductoId = 1, Cantidad = 1, PrecioEstimado = 250m }
            }
        };
    }

    private static void AplicarCasoInvalido(GuardarSolicitudRequestDto solicitud, string caso)
    {
        if (caso == "solicitante-vacio")
        {
            solicitud.Solicitante = "   ";
        }
        else if (caso == "solicitante-largo")
        {
            solicitud.Solicitante = new string('S', 101);
        }
        else if (caso == "observaciones-largas")
        {
            solicitud.Observaciones = new string('O', 501);
        }
        else if (caso == "sin-lineas")
        {
            solicitud.Lineas = new List<LineaSolicitudRequestDto>();
        }
        else if (caso == "cantidad-cero")
        {
            solicitud.Lineas![0].Cantidad = 0;
        }
        else if (caso == "precio-negativo")
        {
            solicitud.Lineas![0].PrecioEstimado = -1m;
        }
        else if (caso == "producto-repetido")
        {
            solicitud.Lineas!.Add(new LineaSolicitudRequestDto { ProductoId = 1, Cantidad = 1, PrecioEstimado = 1m });
        }
        else if (caso == "producto-inexistente")
        {
            solicitud.Lineas![0].ProductoId = 9999;
        }
        else if (caso == "proveedor-inexistente")
        {
            solicitud.ProveedorId = 9999;
        }
        else if (caso == "proveedor-inactivo")
        {
            solicitud.ProveedorId = 2;
        }
    }

    private async Task<long> ContarSolicitudesAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();

        return await contexto.SolicitudesCompra.LongCountAsync();
    }

    private async Task<long> ContarLineasDeSolicitudAsync(int solicitudId)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();

        return await contexto.DetallesSolicitudCompra.LongCountAsync(detalle => detalle.SolicitudCompraId == solicitudId);
    }

    private static async Task AssertBusinessErrorAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode codigoHttp,
        int estado,
        string mensaje)
    {
        using JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

        Assert.Equal(codigoHttp, respuesta.StatusCode);
        Assert.Equal(estado, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, documento.RootElement.GetProperty("Message").GetString());
        Assert.False(documento.RootElement.TryGetProperty("Detail", out _));
    }
}
