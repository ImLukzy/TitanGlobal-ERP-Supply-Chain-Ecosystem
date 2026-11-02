using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo03_Cotizaciones;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo03_Cotizaciones;

[Collection("Modulo04")]
public class CotizacionesApiTests
{
    private readonly HttpClient _clienteHttp;
    private readonly OrdenesCompraApiFactory _fabrica;

    public CotizacionesApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerCotizaciones();
    }

    [Fact]
    public async Task ListadoAplicaBusquedaEstadoClienteFechasVencidasYOrden()
    {
        PaginaCotizacionesDto? lista = await _clienteHttp.GetFromJsonAsync<PaginaCotizacionesDto>(
            "/api/v1/cotizaciones?busqueda=acme&estado=eMiTiDa&clienteId=1&pagina=1&tamanoPagina=1");
        PaginaCotizacionesDto? vencidas = await _clienteHttp.GetFromJsonAsync<PaginaCotizacionesDto>(
            "/api/v1/cotizaciones?vencidas=true");
        PaginaCotizacionesDto? porFecha = await _clienteHttp.GetFromJsonAsync<PaginaCotizacionesDto>(
            $"/api/v1/cotizaciones?fechaDesde={DateTime.UtcNow:yyyy-MM-dd}&fechaHasta={DateTime.UtcNow:yyyy-MM-dd}");
        PaginaCotizacionesDto? ordenadas = await _clienteHttp.GetFromJsonAsync<PaginaCotizacionesDto>(
            "/api/v1/cotizaciones?estado=emitida");

        Assert.NotNull(lista);
        Assert.Equal(1, lista.Total);
        Assert.Single(lista.Items);
        Assert.Equal(1, lista.Items[0].Id);
        Assert.Equal("COT-DEV-001", lista.Items[0].Numero);
        Assert.False(lista.Items[0].Vencida);
        Assert.Equal(180m, lista.Items[0].Total);
        Assert.NotNull(vencidas);
        Assert.Equal(new[] { "COT-DEV-002" }, vencidas.Items.Select(cotizacion => cotizacion.Numero).ToArray());
        Assert.NotNull(porFecha);
        Assert.Equal(new[] { "COT-DEV-001" }, porFecha.Items.Select(cotizacion => cotizacion.Numero).ToArray());
        Assert.NotNull(ordenadas);
        Assert.Equal(new[] { "COT-DEV-001", "COT-DEV-002" },
            ordenadas.Items.Select(cotizacion => cotizacion.Numero).ToArray());
    }

    [Fact]
    public async Task ConsultaDetalleCalculaSubtotalDescuentoImporteYOrdenRelacionada()
    {
        CotizacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CotizacionDetalleDto>(
            "/api/v1/cotizaciones/1");

        Assert.NotNull(detalle);
        Assert.Equal("COT-DEV-001", detalle.Numero);
        Assert.Equal("Acme Industrial", detalle.ClienteNombre);
        Assert.Equal("Emitida", detalle.Estado);
        Assert.Equal(200m, detalle.Subtotal);
        Assert.Equal(20m, detalle.Descuento);
        Assert.Equal(180m, detalle.Total);
        LineaCotizacionDto linea = Assert.Single(detalle.Lineas);
        Assert.Equal("Laptop Pro", linea.ProductoNombre);
        Assert.Equal(200m, linea.Subtotal);
        Assert.Equal(20m, linea.Descuento);
        Assert.Equal(180m, linea.Importe);
        Assert.Null(detalle.OrdenCompraNumero);
    }

    [Fact]
    public async Task AltaAsignaNumeroAtomicoYNormalizaCamposOpcionales()
    {
        GuardarCotizacionRequestDto solicitud = CrearSolicitud(5, 1, 1, 1m, 12.5m);
        solicitud.Observaciones = "   ";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/cotizaciones", solicitud);
        CotizacionCreadaDto? creada = await respuesta.Content.ReadFromJsonAsync<CotizacionCreadaDto>();
        CotizacionDetalleDto? detalle = creada is null
            ? null
            : await _clienteHttp.GetFromJsonAsync<CotizacionDetalleDto>($"/api/v1/cotizaciones/{creada.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(creada);
        Assert.Matches("^COT-[0-9]{6}$", creada.Numero);
        Assert.Equal($"COT-{creada.Id:D6}", creada.Numero);
        Assert.NotNull(detalle);
        Assert.Null(detalle.Observaciones);
        Assert.Equal(1m, detalle.Subtotal);
        Assert.Equal(0.13m, detalle.Descuento);
        Assert.Equal(0.87m, detalle.Total);
    }

    [Fact]
    public async Task EdicionReemplazaLineasMientrasEstaEmitida()
    {
        GuardarCotizacionRequestDto solicitud = CrearSolicitud(2, 3, 2, 50m, 10m);
        solicitud.Observaciones = "  Editada  ";
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/cotizaciones/1", solicitud);
        CotizacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CotizacionDetalleDto>(
            "/api/v1/cotizaciones/1");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Editada", detalle.Observaciones);
        Assert.Single(detalle.Lineas);
        Assert.Equal(100m, detalle.Subtotal);
        Assert.Equal(10m, detalle.Descuento);
        Assert.Equal(90m, detalle.Total);
        Assert.Equal(3, detalle.Lineas[0].ProductoId);
    }

    [Fact]
    public async Task AceptarCreaOrdenDeCompraConDetallesYEnlaceAtomico()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsync("/api/v1/cotizaciones/1/aceptar", null);
        CotizacionDetalleDto? detalle = await respuesta.Content.ReadFromJsonAsync<CotizacionDetalleDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Aceptada", detalle.Estado);
        Assert.Equal(180m, detalle.Total);
        Assert.NotNull(detalle.OrdenCompraId);
        Assert.Equal("OC-COT-DEV-001", detalle.OrdenCompraNumero);
        Assert.NotNull(detalle.FechaDecision);
        Assert.Equal(DateTimeKind.Utc, detalle.FechaDecision.Value.Kind);
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        OrdenCompra orden = await contexto.OrdenesCompra.SingleAsync(entidad => entidad.Id == detalle.OrdenCompraId);
        DetalleOrdenCompra linea = await contexto.DetallesOrdenCompra.SingleAsync(
            entidad => entidad.OrdenCompraId == orden.Id);
        Assert.Equal("OC-COT-DEV-001", orden.NumeroOrden);
        Assert.Equal("Generada desde la cotización COT-DEV-001", orden.Descripcion);
        Assert.Equal(1, orden.ClienteId);
        Assert.Equal(180m, orden.Total);
        Assert.Equal(2, linea.Cantidad);
        Assert.Equal(90m, linea.PrecioUnitario);

        HttpResponseMessage segundaAceptacion = await _clienteHttp.PostAsync(
            "/api/v1/cotizaciones/1/aceptar", null);
        await AssertarErrorAsync(
            segundaAceptacion,
            HttpStatusCode.Conflict,
            409,
            "La cotización COT-DEV-001 ya está aceptada.");
        Assert.Equal(1, await ContarOrdenesCotizacionAsync());
    }

    [Fact]
    public async Task EliminarCotizacionEmitidaBorraLaCotizacion()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync("/api/v1/cotizaciones/2");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(3, await ContarCotizacionesAsync());
    }

    [Fact]
    public async Task RechazarGuardaMotivoYFechaUtc()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/cotizaciones/1/rechazar", new RechazarCotizacionRequestDto { Motivo = "  Precio alto  " });
        CotizacionDetalleDto? detalle = await respuesta.Content.ReadFromJsonAsync<CotizacionDetalleDto>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(detalle);
        Assert.Equal("Rechazada", detalle.Estado);
        Assert.Equal("Precio alto", detalle.MotivoRechazo);
        Assert.NotNull(detalle.FechaDecision);
        Assert.Equal(DateTimeKind.Utc, detalle.FechaDecision.Value.Kind);
    }

    [Fact]
    public async Task ResumenCalculaEstadosMontosTasaYTopDeClientes()
    {
        ResumenCotizacionesDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenCotizacionesDto>(
            "/api/v1/cotizaciones/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(4, resumen.Total);
        Assert.Equal(2, resumen.Emitidas);
        Assert.Equal(1, resumen.Vencidas);
        Assert.Equal(1, resumen.Aceptadas);
        Assert.Equal(1, resumen.Rechazadas);
        Assert.Equal(180m, resumen.MontoVigente);
        Assert.Equal(80m, resumen.MontoAceptado);
        Assert.Equal(50m, resumen.TasaAceptacion);
        Assert.Equal(3, resumen.TopClientes.Count);
        Assert.Equal(3, resumen.TopClientes[0].ClienteId);
        Assert.Equal(2, resumen.TopClientes[0].Cotizaciones);
        Assert.Equal(80m, resumen.TopClientes[0].MontoAceptado);
    }

    [Theory]
    [InlineData(9999, "El cliente 9999 no existe.")]
    public async Task ClienteInexistenteDevuelve400SinCrearFila(int clienteId, string mensaje)
    {
        GuardarCotizacionRequestDto solicitud = CrearSolicitud(clienteId, 1, 1, 10m, 0m);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/cotizaciones", solicitud);

        await AssertarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(4, await ContarCotizacionesAsync());
    }

    [Fact]
    public async Task FechaValidezAnteriorAHoyDevuelve400SinCrearFila()
    {
        GuardarCotizacionRequestDto solicitud = CrearSolicitud(1, 1, 1, 10m, 0m);
        solicitud.FechaValidez = DateTime.UtcNow.AddDays(-1);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/cotizaciones", solicitud);

        await AssertarErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La fecha de validez no puede ser anterior a hoy.");
        Assert.Equal(4, await ContarCotizacionesAsync());
    }

    [Fact]
    public async Task ObservacionesVaciasSeNormalizanYLongitudExcesivaSeRechaza()
    {
        GuardarCotizacionRequestDto solicitud = CrearSolicitud(1, 1, 1, 10m, 0m);
        solicitud.Observaciones = new string('O', 501);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/cotizaciones", solicitud);

        await AssertarErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "Las observaciones no pueden superar 500 caracteres.");
        Assert.Equal(4, await ContarCotizacionesAsync());
    }

    [Fact]
    public async Task ActualizacionInvalidaNoCambiaCotizacionNiSusLineas()
    {
        GuardarCotizacionRequestDto solicitud = CrearSolicitud(2, 9999, 1, 10m, 0m);
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/cotizaciones/1", solicitud);

        await AssertarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, "El producto 9999 no existe.");
        CotizacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CotizacionDetalleDto>(
            "/api/v1/cotizaciones/1");
        Assert.NotNull(detalle);
        Assert.Equal("Acme Industrial", detalle.ClienteNombre);
        Assert.Equal(180m, detalle.Total);
        Assert.Equal(1, Assert.Single(detalle.Lineas).ProductoId);
        Assert.Equal(4, await ContarCotizacionesAsync());
    }

    [Fact]
    public async Task LineasInvalidasDevuelvenMensajesLiteralesYNoGuardan()
    {
        GuardarCotizacionRequestDto sinLineas = CrearSolicitud(1, 1, 0, 10m, 0m);
        sinLineas.Lineas = new List<LineaCotizacionRequestDto>();
        await AssertarAltaInvalidaAsync(sinLineas, "La cotización debe tener al menos una línea.");
        await AssertarAltaInvalidaAsync(CrearSolicitud(1, 1, 0, 10m, 0m), "La cantidad debe ser mayor que 0.");
        await AssertarAltaInvalidaAsync(CrearSolicitud(1, 1, 1, 0m, 0m), "El precio unitario debe ser mayor que 0.");
        await AssertarAltaInvalidaAsync(CrearSolicitud(1, 1, 1, 10m, 100.01m),
            "El descuento debe estar entre 0 y 100.");
        GuardarCotizacionRequestDto repetido = CrearSolicitud(1, 1, 1, 10m, 0m);
        repetido.Lineas!.Add(new LineaCotizacionRequestDto
        {
            ProductoId = 1,
            Cantidad = 1,
            PrecioUnitario = 20m,
            DescuentoPorcentaje = 0m
        });
        await AssertarAltaInvalidaAsync(repetido, "El producto 1 está repetido en la cotización.");
        await AssertarAltaInvalidaAsync(CrearSolicitud(1, 9999, 1, 10m, 0m), "El producto 9999 no existe.");
    }

    [Fact]
    public async Task RechazoExigeMotivoYLongitudMaxima()
    {
        HttpResponseMessage vacio = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/cotizaciones/1/rechazar", new RechazarCotizacionRequestDto { Motivo = " " });
        await AssertarErrorAsync(vacio, HttpStatusCode.BadRequest, 400, "El motivo de rechazo es obligatorio.");
        HttpResponseMessage largo = await _clienteHttp.PostAsJsonAsync(
            "/api/v1/cotizaciones/1/rechazar", new RechazarCotizacionRequestDto { Motivo = new string('M', 501) });
        await AssertarErrorAsync(
            largo,
            HttpStatusCode.BadRequest,
            400,
            "El motivo de rechazo no puede superar 500 caracteres.");
        CotizacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CotizacionDetalleDto>(
            "/api/v1/cotizaciones/1");
        Assert.NotNull(detalle);
        Assert.Equal("Emitida", detalle.Estado);
        Assert.Equal(4, await ContarCotizacionesAsync());
    }

    [Fact]
    public async Task AceptarCotizacionVencidaDevuelve409YNoCreaOrden()
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsync("/api/v1/cotizaciones/2/aceptar", null);

        await AssertarErrorAsync(respuesta, HttpStatusCode.Conflict, 409,
            "La cotización COT-DEV-002 está vencida.");
        Assert.Equal(0, await ContarOrdenesCotizacionAsync());
        CotizacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CotizacionDetalleDto>(
            "/api/v1/cotizaciones/2");
        Assert.NotNull(detalle);
        Assert.Equal("Emitida", detalle.Estado);
        Assert.Null(detalle.OrdenCompraId);
    }

    [Theory]
    [InlineData("PUT", "/api/v1/cotizaciones/3")]
    [InlineData("POST", "/api/v1/cotizaciones/3/aceptar")]
    [InlineData("POST", "/api/v1/cotizaciones/3/rechazar")]
    [InlineData("DELETE", "/api/v1/cotizaciones/3")]
    public async Task CotizacionDecididaNoAdmiteCambios(string metodo, string ruta)
    {
        HttpResponseMessage respuesta;
        if (metodo == "PUT")
        {
            respuesta = await _clienteHttp.PutAsJsonAsync(ruta, CrearSolicitud(1, 1, 1, 20m, 0m));
        }
        else if (metodo == "POST" && ruta.EndsWith("aceptar", StringComparison.Ordinal))
        {
            respuesta = await _clienteHttp.PostAsync(ruta, null);
        }
        else if (metodo == "POST")
        {
            respuesta = await _clienteHttp.PostAsJsonAsync(ruta, new RechazarCotizacionRequestDto { Motivo = "No" });
        }
        else
        {
            respuesta = await _clienteHttp.DeleteAsync(ruta);
        }

        await AssertarErrorAsync(respuesta, HttpStatusCode.Conflict, 409,
            "La cotización COT-DEV-003 ya está rechazada.");
        Assert.Equal(0, await ContarOrdenesCotizacionAsync());
    }

    [Fact]
    public async Task NumeroDeOrdenExistenteImpideAceptarSinCambios()
    {
        using (IServiceScope alcance = _fabrica.Services.CreateScope())
        {
            TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
            contexto.OrdenesCompra.Add(new OrdenCompra
            {
                NumeroOrden = "OC-COT-DEV-001",
                Fecha = DateTime.UtcNow,
                ClienteId = 1,
                Total = 1m
            });
            await contexto.SaveChangesAsync();
        }

        HttpResponseMessage respuesta = await _clienteHttp.PostAsync("/api/v1/cotizaciones/1/aceptar", null);

        await AssertarErrorAsync(respuesta, HttpStatusCode.Conflict, 409,
            "Ya existe una orden de compra con el número OC-COT-DEV-001.");
        CotizacionDetalleDto? detalle = await _clienteHttp.GetFromJsonAsync<CotizacionDetalleDto>(
            "/api/v1/cotizaciones/1");
        Assert.NotNull(detalle);
        Assert.Equal("Emitida", detalle.Estado);
        Assert.Null(detalle.OrdenCompraId);
        Assert.Equal(1, await ContarOrdenesCotizacionAsync());
    }

    [Theory]
    [InlineData("GET", "/api/v1/cotizaciones/9999")]
    [InlineData("PUT", "/api/v1/cotizaciones/9999")]
    [InlineData("DELETE", "/api/v1/cotizaciones/9999")]
    public async Task IdInexistenteDevuelve404Literal(string metodo, string ruta)
    {
        HttpResponseMessage respuesta;
        if (metodo == "GET")
        {
            respuesta = await _clienteHttp.GetAsync(ruta);
        }
        else if (metodo == "PUT")
        {
            respuesta = await _clienteHttp.PutAsJsonAsync(ruta, CrearSolicitud(1, 1, 1, 10m, 0m));
        }
        else
        {
            respuesta = await _clienteHttp.DeleteAsync(ruta);
        }

        await AssertarErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "La cotización 9999 no existe.");
    }

    [Fact]
    public async Task FiltrosInvalidosDevuelven400Literal()
    {
        HttpResponseMessage estado = await _clienteHttp.GetAsync("/api/v1/cotizaciones?estado=pendiente");
        HttpResponseMessage fechas = await _clienteHttp.GetAsync(
            "/api/v1/cotizaciones?fechaDesde=2026-05-01&fechaHasta=2026-04-01");

        await AssertarErrorAsync(estado, HttpStatusCode.BadRequest, 400, "El estado no es válido.");
        await AssertarErrorAsync(fechas, HttpStatusCode.BadRequest, 400,
            "La fecha desde no puede ser posterior a la fecha hasta.");
    }

    [Fact]
    public async Task OpenApiIncluyeOchoOperacionesDeCotizaciones()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        string documento = await respuesta.Content.ReadAsStringAsync();
        using JsonDocument json = JsonDocument.Parse(documento);
        JsonElement paths = json.RootElement.GetProperty("paths");
        (string Ruta, string Verbo)[] operaciones =
        {
            ("/api/v1/cotizaciones", "get"),
            ("/api/v1/cotizaciones", "post"),
            ("/api/v1/cotizaciones/{id}", "get"),
            ("/api/v1/cotizaciones/{id}", "put"),
            ("/api/v1/cotizaciones/{id}", "delete"),
            ("/api/v1/cotizaciones/{id}/aceptar", "post"),
            ("/api/v1/cotizaciones/{id}/rechazar", "post"),
            ("/api/v1/cotizaciones/resumen", "get")
        };

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        foreach ((string ruta, string verbo) in operaciones)
        {
            Assert.True(paths.TryGetProperty(ruta, out JsonElement operacionesRuta));
            Assert.True(operacionesRuta.TryGetProperty(verbo, out _));
        }
    }

    private void RestablecerCotizaciones()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.DetallesCotizacion.RemoveRange(contexto.DetallesCotizacion);
        contexto.SaveChanges();
        contexto.Cotizaciones.RemoveRange(contexto.Cotizaciones);
        contexto.SaveChanges();
        List<OrdenCompra> ordenesAnteriores = contexto.OrdenesCompra
            .Where(orden => orden.NumeroOrden.StartsWith("OC-COT-"))
            .ToList();
        contexto.DetallesOrdenCompra.RemoveRange(
            contexto.DetallesOrdenCompra.Where(detalle => ordenesAnteriores.Select(orden => orden.Id).Contains(detalle.OrdenCompraId)));
        contexto.OrdenesCompra.RemoveRange(ordenesAnteriores);
        contexto.SaveChanges();

        DateTime hoyUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        Cotizacion[] cotizaciones =
        {
            new Cotizacion
            {
                Id = 1,
                Numero = "COT-DEV-001",
                Fecha = hoyUtc,
                ClienteId = 1,
                FechaValidez = hoyUtc.AddDays(30),
                Estado = "Emitida",
                Observaciones = "Cotización vigente"
            },
            new Cotizacion
            {
                Id = 2,
                Numero = "COT-DEV-002",
                Fecha = hoyUtc.AddDays(-20),
                ClienteId = 2,
                FechaValidez = hoyUtc.AddDays(-1),
                Estado = "Emitida"
            },
            new Cotizacion
            {
                Id = 3,
                Numero = "COT-DEV-003",
                Fecha = hoyUtc.AddDays(-10),
                ClienteId = 3,
                FechaValidez = hoyUtc.AddDays(10),
                Estado = "Rechazada",
                MotivoRechazo = "Presupuesto insuficiente",
                FechaDecision = hoyUtc.AddDays(-9)
            },
            new Cotizacion
            {
                Id = 4,
                Numero = "COT-DEV-004",
                Fecha = hoyUtc.AddDays(-5),
                ClienteId = 3,
                FechaValidez = hoyUtc.AddDays(15),
                Estado = "Aceptada",
                FechaDecision = hoyUtc.AddDays(-4)
            }
        };
        contexto.Cotizaciones.AddRange(cotizaciones);
        contexto.DetallesCotizacion.AddRange(
            new DetalleCotizacion
            {
                CotizacionId = 1,
                ProductoId = 1,
                Cantidad = 2,
                PrecioUnitario = 100m,
                DescuentoPorcentaje = 10m
            },
            new DetalleCotizacion
            {
                CotizacionId = 2,
                ProductoId = 2,
                Cantidad = 1,
                PrecioUnitario = 50m,
                DescuentoPorcentaje = 0m
            },
            new DetalleCotizacion
            {
                CotizacionId = 3,
                ProductoId = 3,
                Cantidad = 1,
                PrecioUnitario = 100m,
                DescuentoPorcentaje = 0m
            },
            new DetalleCotizacion
            {
                CotizacionId = 4,
                ProductoId = 4,
                Cantidad = 1,
                PrecioUnitario = 80m,
                DescuentoPorcentaje = 0m
            });
        contexto.SaveChanges();
    }

    private async Task<int> ContarCotizacionesAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Cotizaciones.CountAsync();
    }

    private async Task<int> ContarOrdenesCotizacionAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.OrdenesCompra.CountAsync(orden => orden.NumeroOrden.StartsWith("OC-COT-"));
    }

    private async Task AssertarAltaInvalidaAsync(GuardarCotizacionRequestDto solicitud, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/cotizaciones", solicitud);
        await AssertarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(4, await ContarCotizacionesAsync());
    }

    private static GuardarCotizacionRequestDto CrearSolicitud(
        int clienteId,
        int productoId,
        int cantidad,
        decimal precio,
        decimal descuento)
    {
        return new GuardarCotizacionRequestDto
        {
            ClienteId = clienteId,
            FechaValidez = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(30), DateTimeKind.Utc),
            Observaciones = "Cotización de prueba",
            Lineas = new List<LineaCotizacionRequestDto>
            {
                new LineaCotizacionRequestDto
                {
                    ProductoId = productoId,
                    Cantidad = cantidad,
                    PrecioUnitario = precio,
                    DescuentoPorcentaje = descuento
                }
            }
        };
    }

    private static async Task AssertarErrorAsync(
        HttpResponseMessage respuesta,
        HttpStatusCode estado,
        int codigo,
        string mensaje)
    {
        string contenido = await respuesta.Content.ReadAsStringAsync();
        using JsonDocument json = JsonDocument.Parse(contenido);
        Assert.Equal(estado, respuesta.StatusCode);
        Assert.Equal(codigo, json.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, json.RootElement.GetProperty("Message").GetString());
        Assert.False(json.RootElement.TryGetProperty("Detail", out _));
    }
}
