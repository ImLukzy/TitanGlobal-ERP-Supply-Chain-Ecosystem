using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo15_OrdenesAbastecimiento;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo15_OrdenesAbastecimiento;

[Collection("Modulo04")]
public class OrdenesAbastecimientoApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public OrdenesAbastecimientoApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos();
    }

    [Fact]
    public async Task CreateManualAssignsFinalNumberAndDoesNotLeaveTemporaryNumber()
    {
        GuardarOrdenAbastecimientoRequestDto solicitud = CrearSolicitud(1, 7, 2, 10m);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/ordenes-abastecimiento", solicitud);
        OrdenAbastecimientoCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<OrdenAbastecimientoCreadaDto>();
        long ordenesTemporales = await ContarOrdenesTemporalesAsync();
        OrdenAbastecimiento? guardada = creada is null ? null : await ObtenerOrdenAsync(creada.Id);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creada);
        Assert.Matches("^OA-[0-9]{6}$", creada.Numero);
        Assert.NotNull(guardada);
        Assert.Equal(creada.Numero, guardada.Numero);
        Assert.Equal(0, ordenesTemporales);
    }

    [Fact]
    public async Task GenerateFromApprovedRequestCopiesProviderAndLinesAndAllowsNewAfterCancellation()
    {
        GenerarDesdeSolicitudRequestDto solicitud = new GenerarDesdeSolicitudRequestDto
        {
            FechaEntregaEstimada = DateTime.UtcNow.Date.AddDays(10)
        };
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/ordenes-abastecimiento/desde-solicitud/101",
            solicitud);
        OrdenAbastecimientoCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<OrdenAbastecimientoCreadaDto>();
        OrdenAbastecimientoDetalleDto? detalle = creada is null
            ? null
            : await _clienteHttp.GetFromJsonAsync<OrdenAbastecimientoDetalleDto>(
                $"/api/v1/ordenes-abastecimiento/{creada.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creada);
        Assert.NotNull(detalle);
        Assert.Equal(1, detalle.ProveedorId);
        Assert.Equal(101, detalle.SolicitudCompraId);
        LineaOrdenAbastecimientoDto linea = Assert.Single(detalle.Lineas);
        Assert.Equal(7, linea.ProductoId);
        Assert.Equal(2, linea.Cantidad);
        Assert.Equal(25m, linea.PrecioUnitario);

        HttpResponseMessage repetida = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/ordenes-abastecimiento/desde-solicitud/101",
            solicitud);
        await AssertBusinessErrorAsync(
            repetida,
            HttpStatusCode.Conflict,
            409,
            $"La solicitud SC-TEST-101 ya tiene la orden {creada.Numero}.");

        HttpResponseMessage anulada = await _clienteHttp.PostAsJsonAsync(
            $"/api/v1/ordenes-abastecimiento/{creada.Id}/anular",
            new AnularOrdenRequestDto { Motivo = "  Se reprograma  " });
        Assert.Equal(HttpStatusCode.OK, anulada.StatusCode);
        OrdenAbastecimientoDetalleDto? detalleAnulada = await anulada.Content.ReadFromJsonAsync<OrdenAbastecimientoDetalleDto>();
        Assert.NotNull(detalleAnulada);
        Assert.Equal("Se reprograma", detalleAnulada.MotivoAnulacion);

        HttpResponseMessage nueva = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/ordenes-abastecimiento/desde-solicitud/101",
            solicitud);
        Assert.Equal(HttpStatusCode.OK, nueva.StatusCode);
    }

    [Fact]
    public async Task RequestWithoutSuggestedProviderRequiresExplicitProvider()
    {
        GenerarDesdeSolicitudRequestDto solicitud = new GenerarDesdeSolicitudRequestDto
        {
            FechaEntregaEstimada = DateTime.UtcNow.Date.AddDays(10)
        };
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/ordenes-abastecimiento/desde-solicitud/102",
            solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "Indica el proveedor de la orden.");
    }

    [Fact]
    public async Task RequestGenerationValidatesMissingPendingInactiveAndZeroPriceCases()
    {
        DateTime fechaEntrega = DateTime.UtcNow.Date.AddDays(10);
        GenerarDesdeSolicitudRequestDto solicitud = new GenerarDesdeSolicitudRequestDto
        {
            FechaEntregaEstimada = fechaEntrega
        };
        HttpResponseMessage inexistente = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/ordenes-abastecimiento/desde-solicitud/9999",
            solicitud);
        await AssertBusinessErrorAsync(
            inexistente,
            HttpStatusCode.NotFound,
            404,
            "La solicitud de compra 9999 no existe.");

        HttpResponseMessage pendiente = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/ordenes-abastecimiento/desde-solicitud/103",
            solicitud);
        await AssertBusinessErrorAsync(
            pendiente,
            HttpStatusCode.Conflict,
            409,
            "La solicitud SC-TEST-103 no está aprobada.");

        HttpResponseMessage proveedorInactivo = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/ordenes-abastecimiento/desde-solicitud/101",
            new GenerarDesdeSolicitudRequestDto
            {
                ProveedorId = 2,
                FechaEntregaEstimada = fechaEntrega
            });
        await AssertBusinessErrorAsync(
            proveedorInactivo,
            HttpStatusCode.BadRequest,
            400,
            "El proveedor 2 está inactivo.");

        HttpResponseMessage precioCero = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/ordenes-abastecimiento/desde-solicitud/105",
            solicitud);
        await AssertBusinessErrorAsync(
            precioCero,
            HttpStatusCode.BadRequest,
            400,
            "El precio unitario debe ser mayor que 0.");
    }

    [Theory]
    [InlineData("proveedor-inexistente", "El proveedor 9999 no existe.")]
    [InlineData("proveedor-inactivo", "El proveedor 2 está inactivo.")]
    [InlineData("fecha-anterior", "La fecha de entrega estimada no puede ser anterior a hoy.")]
    [InlineData("observaciones-largas", "Las observaciones no pueden superar 500 caracteres.")]
    [InlineData("sin-lineas", "La orden debe tener al menos una línea.")]
    [InlineData("cantidad-cero", "La cantidad debe ser mayor que 0.")]
    [InlineData("precio-cero", "El precio unitario debe ser mayor que 0.")]
    [InlineData("producto-repetido", "El producto 1 está repetido en la orden.")]
    [InlineData("producto-inexistente", "El producto 9999 no existe.")]
    public async Task InvalidCreateReturnsLiteralBadRequestWithoutAddingRows(string caso, string mensaje)
    {
        long totalAntes = await ContarOrdenesAsync();
        GuardarOrdenAbastecimientoRequestDto solicitud = CrearSolicitud(1, 1, 1, 10m);
        AplicarCasoInvalido(solicitud, caso);

        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/ordenes-abastecimiento", solicitud);
        long totalDespues = await ContarOrdenesAsync();

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(totalAntes, totalDespues);
    }

    [Fact]
    public async Task UpdateReplacesLinesAndRecalculatesTotal()
    {
        int ordenId = await ObtenerIdOrdenAsync("OA-TEST-001");
        GuardarOrdenAbastecimientoRequestDto solicitud = CrearSolicitud(3, 2, 3, 12.5m);
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync(
            $"/api/v1/ordenes-abastecimiento/{ordenId}",
            solicitud);
        OrdenAbastecimientoDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<OrdenAbastecimientoDetalleDto>(
            $"/api/v1/ordenes-abastecimiento/{ordenId}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("OA-TEST-001", detalle.Numero);
        Assert.Equal(37.5m, detalle.MontoTotal);
        LineaOrdenAbastecimientoDto linea = Assert.Single(detalle.Lineas);
        Assert.Equal(2, linea.ProductoId);
        Assert.Equal(37.5m, linea.Subtotal);
    }

    [Fact]
    public async Task ReceiveWithoutBodySetsUtcDateAndRejectsSecondTransition()
    {
        int ordenId = await ObtenerIdOrdenAsync("OA-TEST-001");
        HttpResponseMessage respuesta = await _clienteHttp.PostAsync(
            $"/api/v1/ordenes-abastecimiento/{ordenId}/recibir",
            null);
        OrdenAbastecimientoDetalleDto? detalle = await respuesta.Content.ReadFromJsonAsync<OrdenAbastecimientoDetalleDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Recibida", detalle.Estado);
        Assert.NotNull(detalle.FechaRecepcion);
        Assert.Equal(DateTimeKind.Utc, detalle.FechaRecepcion.Value.Kind);

        HttpResponseMessage repetida = await _clienteHttp.PostAsync(
            $"/api/v1/ordenes-abastecimiento/{ordenId}/recibir",
            null);
        await AssertBusinessErrorAsync(
            repetida,
            HttpStatusCode.Conflict,
            409,
            "La orden OA-TEST-001 ya está recibida.");

        GuardarOrdenAbastecimientoRequestDto edicion = CrearSolicitud(1, 1, 1, 10m);
        HttpResponseMessage editarRecibida = await _clienteHttp.PutAsJsonAsync(
            $"/api/v1/ordenes-abastecimiento/{ordenId}",
            edicion);
        await AssertBusinessErrorAsync(
            editarRecibida,
            HttpStatusCode.Conflict,
            409,
            "La orden OA-TEST-001 ya está recibida.");

        HttpResponseMessage anularRecibida = await _clienteHttp.PostAsJsonAsync(
            $"/api/v1/ordenes-abastecimiento/{ordenId}/anular",
            new AnularOrdenRequestDto { Motivo = "Ya recibida" });
        await AssertBusinessErrorAsync(
            anularRecibida,
            HttpStatusCode.Conflict,
            409,
            "La orden OA-TEST-001 ya está recibida.");
    }

    [Fact]
    public async Task CancelRequiresTrimmedReasonAndRejectsOtherTransitions()
    {
        int ordenId = await ObtenerIdOrdenAsync("OA-TEST-001");
        HttpResponseMessage sinMotivo = await _clienteHttp.PostAsJsonAsync(
            $"/api/v1/ordenes-abastecimiento/{ordenId}/anular",
            new AnularOrdenRequestDto { Motivo = "  " });
        await AssertBusinessErrorAsync(
            sinMotivo,
            HttpStatusCode.BadRequest,
            400,
            "El motivo de anulación es obligatorio.");

        HttpResponseMessage motivoLargo = await _clienteHttp.PostAsJsonAsync(
            $"/api/v1/ordenes-abastecimiento/{ordenId}/anular",
            new AnularOrdenRequestDto { Motivo = new string('x', 501) });
        await AssertBusinessErrorAsync(
            motivoLargo,
            HttpStatusCode.BadRequest,
            400,
            "El motivo de anulación no puede superar 500 caracteres.");

        HttpResponseMessage anulada = await _clienteHttp.PostAsJsonAsync(
            $"/api/v1/ordenes-abastecimiento/{ordenId}/anular",
            new AnularOrdenRequestDto { Motivo = "  Ya no se requiere  " });
        OrdenAbastecimientoDetalleDto? detalle = await anulada.Content.ReadFromJsonAsync<OrdenAbastecimientoDetalleDto>();
        Assert.Equal(HttpStatusCode.OK, anulada.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Anulada", detalle.Estado);
        Assert.Equal("Ya no se requiere", detalle.MotivoAnulacion);

        HttpResponseMessage recibirAnulada = await _clienteHttp.PostAsync(
            $"/api/v1/ordenes-abastecimiento/{ordenId}/recibir",
            null);
        await AssertBusinessErrorAsync(
            recibirAnulada,
            HttpStatusCode.Conflict,
            409,
            "La orden OA-TEST-001 ya está anulada.");
    }

    [Fact]
    public async Task ListingFiltersDatesSearchStateAndPagination()
    {
        DateTime hoy = DateTime.UtcNow.Date;
        string desde = hoy.AddDays(-5).ToString("yyyy-MM-dd");
        string hasta = hoy.AddDays(-5).ToString("yyyy-MM-dd");
        PaginaOrdenesAbastecimientoDto? atrasadas = await _clienteHttp.GetFromJsonAsync<PaginaOrdenesAbastecimientoDto>(
            $"/api/v1/ordenes-abastecimiento?estado=emitida&busqueda=proveedor%20activo&fechaDesde={desde}&fechaHasta={hasta}");
        PaginaOrdenesAbastecimientoDto? porNumero = await _clienteHttp.GetFromJsonAsync<PaginaOrdenesAbastecimientoDto>(
            "/api/v1/ordenes-abastecimiento?busqueda=oa-test-003");
        PaginaOrdenesAbastecimientoDto? pagina = await _clienteHttp.GetFromJsonAsync<PaginaOrdenesAbastecimientoDto>(
            "/api/v1/ordenes-abastecimiento?pagina=2&tamanoPagina=1");

        Assert.NotNull(atrasadas);
        Assert.Equal(1, atrasadas.Total);
        Assert.True(atrasadas.Items[0].Atrasada);
        Assert.NotNull(porNumero);
        Assert.Equal(new[] { "OA-TEST-003" }, porNumero.Items.Select(orden => orden.Numero).ToArray());
        Assert.NotNull(pagina);
        Assert.Equal(3, pagina.Total);
        Assert.Equal("OA-TEST-002", pagina.Items[0].Numero);
    }

    [Fact]
    public async Task ListingRejectsInvalidPaginationStateAndDateRange()
    {
        HttpResponseMessage paginaInvalida = await _clienteHttp.GetAsync(
            "/api/v1/ordenes-abastecimiento?pagina=0");
        await AssertBusinessErrorAsync(
            paginaInvalida,
            HttpStatusCode.BadRequest,
            400,
            "La página debe ser mayor o igual a 1.");

        HttpResponseMessage estadoInvalido = await _clienteHttp.GetAsync(
            "/api/v1/ordenes-abastecimiento?estado=despachada");
        await AssertBusinessErrorAsync(
            estadoInvalido,
            HttpStatusCode.BadRequest,
            400,
            "El estado no es válido.");

        HttpResponseMessage rangoInvalido = await _clienteHttp.GetAsync(
            "/api/v1/ordenes-abastecimiento?fechaDesde=2026-03-10&fechaHasta=2026-03-01");
        await AssertBusinessErrorAsync(
            rangoInvalido,
            HttpStatusCode.BadRequest,
            400,
            "La fecha desde no puede ser posterior a la fecha hasta.");

        HttpResponseMessage tamanoInvalido = await _clienteHttp.GetAsync(
            "/api/v1/ordenes-abastecimiento?tamanoPagina=101");
        await AssertBusinessErrorAsync(
            tamanoInvalido,
            HttpStatusCode.BadRequest,
            400,
            "El tamaño de página debe estar entre 1 y 100.");
    }

    [Fact]
    public async Task AllDetailActionsReturnLiteralNotFoundForMissingOrder()
    {
        HttpResponseMessage detalle = await _clienteHttp.GetAsync("/api/v1/ordenes-abastecimiento/9999");
        await AssertBusinessErrorAsync(
            detalle,
            HttpStatusCode.NotFound,
            404,
            "La orden de abastecimiento 9999 no existe.");

        GuardarOrdenAbastecimientoRequestDto solicitud = CrearSolicitud(1, 1, 1, 10m);
        HttpResponseMessage actualizar = await _clienteHttp.PutAsJsonAsync(
            "/api/v1/ordenes-abastecimiento/9999",
            solicitud);
        await AssertBusinessErrorAsync(
            actualizar,
            HttpStatusCode.NotFound,
            404,
            "La orden de abastecimiento 9999 no existe.");

        HttpResponseMessage recibir = await _clienteHttp.PostAsync(
            "/api/v1/ordenes-abastecimiento/9999/recibir",
            null);
        await AssertBusinessErrorAsync(
            recibir,
            HttpStatusCode.NotFound,
            404,
            "La orden de abastecimiento 9999 no existe.");

        HttpResponseMessage anular = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/ordenes-abastecimiento/9999/anular",
            new AnularOrdenRequestDto { Motivo = "No existe" });
        await AssertBusinessErrorAsync(
            anular,
            HttpStatusCode.NotFound,
            404,
            "La orden de abastecimiento 9999 no existe.");
    }

    [Fact]
    public async Task SummaryReturnsExactStateAmountsAndTopProviders()
    {
        ResumenOrdenesAbastecimientoDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenOrdenesAbastecimientoDto>(
            "/api/v1/ordenes-abastecimiento/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(3, resumen.Total);
        Assert.Equal(1, resumen.Emitidas);
        Assert.Equal(1, resumen.Recibidas);
        Assert.Equal(1, resumen.Anuladas);
        Assert.Equal(1, resumen.Atrasadas);
        Assert.Equal(20m, resumen.MontoPorRecibir);
        Assert.Equal(20m, resumen.MontoRecibido);
        ProveedorMontoDto proveedor = Assert.Single(resumen.TopProveedores);
        Assert.Equal(1, proveedor.ProveedorId);
        Assert.Equal(2, proveedor.Ordenes);
        Assert.Equal(40m, proveedor.Monto);
    }

    [Fact]
    public async Task OpenApiContainsAllEightOperations()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        string documento = await respuesta.Content.ReadAsStringAsync();
        using JsonDocument json = JsonDocument.Parse(documento);
        JsonElement rutas = json.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty("/api/v1/ordenes-abastecimiento").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/ordenes-abastecimiento").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/ordenes-abastecimiento/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/ordenes-abastecimiento/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/ordenes-abastecimiento/desde-solicitud/{solicitudId}")
            .TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/ordenes-abastecimiento/{id}/recibir").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/ordenes-abastecimiento/{id}/anular").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/ordenes-abastecimiento/resumen").TryGetProperty("get", out _));
    }

    private void RestablecerDatos()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.DetallesOrdenesAbastecimiento.RemoveRange(contexto.DetallesOrdenesAbastecimiento);
        contexto.OrdenesAbastecimiento.RemoveRange(contexto.OrdenesAbastecimiento);
        contexto.DetallesSolicitudCompra.RemoveRange(contexto.DetallesSolicitudCompra);
        contexto.SolicitudesCompra.RemoveRange(contexto.SolicitudesCompra);
        contexto.Proveedores.RemoveRange(contexto.Proveedores);

        Proveedor[] proveedores =
        {
            new Proveedor { Id = 1, RazonSocial = "Proveedor Activo", Ruc = "20611111111", Pais = "Peru", Activo = true },
            new Proveedor { Id = 2, RazonSocial = "Proveedor Inactivo", Ruc = "20622222222", Pais = "Peru", Activo = false },
            new Proveedor { Id = 3, RazonSocial = "Proveedor Secundario", Ruc = "20633333333", Pais = "Chile", Activo = true }
        };
        contexto.Proveedores.AddRange(proveedores);

        SolicitudCompra solicitudAprobada = CrearSolicitudPrueba(101, "SC-TEST-101", "Aprobada", 1);
        solicitudAprobada.Detalles.Add(new DetalleSolicitudCompra
        {
            ProductoId = 7,
            Cantidad = 2,
            PrecioEstimado = 25m
        });
        SolicitudCompra solicitudSinProveedor = CrearSolicitudPrueba(102, "SC-TEST-102", "Aprobada", null);
        solicitudSinProveedor.Detalles.Add(new DetalleSolicitudCompra
        {
            ProductoId = 1,
            Cantidad = 1,
            PrecioEstimado = 50m
        });
        SolicitudCompra solicitudPendiente = CrearSolicitudPrueba(103, "SC-TEST-103", "Pendiente", 1);
        SolicitudCompra solicitudAprobadaSinOrden = CrearSolicitudPrueba(104, "SC-TEST-104", "Aprobada", 1);
        SolicitudCompra solicitudAprobadaPrecioCero = CrearSolicitudPrueba(105, "SC-TEST-105", "Aprobada", 1);
        solicitudAprobadaPrecioCero.Detalles.Add(new DetalleSolicitudCompra
        {
            ProductoId = 3,
            Cantidad = 1,
            PrecioEstimado = 0m
        });
        contexto.SolicitudesCompra.AddRange(
            solicitudAprobada,
            solicitudSinProveedor,
            solicitudPendiente,
            solicitudAprobadaSinOrden,
            solicitudAprobadaPrecioCero);

        DateTime hoy = DateTime.UtcNow.Date;
        contexto.OrdenesAbastecimiento.AddRange(
            CrearOrdenPrueba("OA-TEST-001", hoy.AddDays(-5), "Emitida", 1, hoy.AddDays(-1), 1, 2, 10m),
            CrearOrdenPrueba("OA-TEST-002", hoy.AddDays(-3), "Recibida", 1, hoy.AddDays(2), 2, 1, 20m),
            CrearOrdenPrueba("OA-TEST-003", hoy.AddDays(-1), "Anulada", 2, hoy.AddDays(5), 3, 1, 30m));
        contexto.SaveChanges();
    }

    private static SolicitudCompra CrearSolicitudPrueba(int id, string numero, string estado, int? proveedorId)
    {
        return new SolicitudCompra
        {
            Id = id,
            Numero = numero,
            Fecha = DateTime.UtcNow,
            Solicitante = "Equipo de pruebas",
            ProveedorId = proveedorId,
            Estado = estado
        };
    }

    private static OrdenAbastecimiento CrearOrdenPrueba(
        string numero,
        DateTime fecha,
        string estado,
        int proveedorId,
        DateTime entrega,
        int productoId,
        int cantidad,
        decimal precio)
    {
        return new OrdenAbastecimiento
        {
            Numero = numero,
            Fecha = fecha,
            ProveedorId = proveedorId,
            Estado = estado,
            FechaEntregaEstimada = entrega,
            FechaRecepcion = estado == "Recibida" ? DateTime.UtcNow : null,
            MotivoAnulacion = estado == "Anulada" ? "Prueba" : null,
            Detalles = new List<DetalleOrdenAbastecimiento>
            {
                new DetalleOrdenAbastecimiento
                {
                    ProductoId = productoId,
                    Cantidad = cantidad,
                    PrecioUnitario = precio
                }
            }
        };
    }

    private static GuardarOrdenAbastecimientoRequestDto CrearSolicitud(
        int proveedorId,
        int productoId,
        int cantidad,
        decimal precio)
    {
        return new GuardarOrdenAbastecimientoRequestDto
        {
            ProveedorId = proveedorId,
            FechaEntregaEstimada = DateTime.UtcNow.Date.AddDays(10),
            Lineas = new List<LineaOrdenAbastecimientoRequestDto>
            {
                new LineaOrdenAbastecimientoRequestDto
                {
                    ProductoId = productoId,
                    Cantidad = cantidad,
                    PrecioUnitario = precio
                }
            }
        };
    }

    private static void AplicarCasoInvalido(GuardarOrdenAbastecimientoRequestDto solicitud, string caso)
    {
        if (caso == "proveedor-inexistente")
        {
            solicitud.ProveedorId = 9999;
        }
        else if (caso == "proveedor-inactivo")
        {
            solicitud.ProveedorId = 2;
        }
        else if (caso == "fecha-anterior")
        {
            solicitud.FechaEntregaEstimada = DateTime.UtcNow.Date.AddDays(-1);
        }
        else if (caso == "observaciones-largas")
        {
            solicitud.Observaciones = new string('x', 501);
        }
        else if (caso == "sin-lineas")
        {
            solicitud.Lineas = new List<LineaOrdenAbastecimientoRequestDto>();
        }
        else if (caso == "cantidad-cero")
        {
            solicitud.Lineas![0].Cantidad = 0;
        }
        else if (caso == "precio-cero")
        {
            solicitud.Lineas![0].PrecioUnitario = 0m;
        }
        else if (caso == "producto-repetido")
        {
            solicitud.Lineas!.Add(new LineaOrdenAbastecimientoRequestDto
            {
                ProductoId = solicitud.Lineas[0].ProductoId,
                Cantidad = 1,
                PrecioUnitario = 10m
            });
        }
        else if (caso == "producto-inexistente")
        {
            solicitud.Lineas![0].ProductoId = 9999;
        }
    }

    private async Task<int> ObtenerIdOrdenAsync(string numero)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.OrdenesAbastecimiento
            .Where(orden => orden.Numero == numero)
            .Select(orden => orden.Id)
            .SingleAsync();
    }

    private async Task<OrdenAbastecimiento?> ObtenerOrdenAsync(int id)
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.OrdenesAbastecimiento.AsNoTracking().SingleOrDefaultAsync(orden => orden.Id == id);
    }

    private async Task<long> ContarOrdenesAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.OrdenesAbastecimiento.LongCountAsync();
    }

    private async Task<long> ContarOrdenesTemporalesAsync()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.OrdenesAbastecimiento.LongCountAsync(orden => orden.Numero.StartsWith("TMP-"));
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
