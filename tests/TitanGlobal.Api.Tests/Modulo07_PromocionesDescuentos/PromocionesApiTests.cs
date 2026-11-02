using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo07_PromocionesDescuentos;
using TitanGlobal.Api.Tests.Modulo06_ListasPrecios;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo07_PromocionesDescuentos;

public class PromocionesApiTests : IDisposable
{
    private readonly ListasPreciosApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public PromocionesApiTests()
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

    [Theory]
    [InlineData("", "Promo", "Producto", 1, null, 10, null, "El código es obligatorio.")]
    [InlineData("P!", "Promo", "Producto", 1, null, 10, null, "El código solo admite letras, números y guiones (2 a 20 caracteres).")]
    [InlineData("PROMO", " ", "Producto", 1, null, 10, null, "El nombre de la promoción es obligatorio.")]
    [InlineData("PROMO", "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX", "Producto", 1, null, 10, null, "El nombre de la promoción no puede superar 100 caracteres.")]
    [InlineData("PROMO", "Promo", "NoValido", 1, null, 10, null, "El tipo de promoción no es válido.")]
    [InlineData("PROMO", "Promo", "Producto", null, null, 10, null, "La promoción de producto requiere un producto.")]
    [InlineData("PROMO", "Promo", "Producto", 999, null, 10, null, "El producto 999 no existe.")]
    [InlineData("PROMO", "Promo", "Producto", 1, "10", 10, null, "Una promoción de producto no lleva monto mínimo.")]
    [InlineData("PROMO", "Promo", "MontoMinimo", null, null, 10, null, "El monto mínimo debe ser mayor que 0.")]
    [InlineData("PROMO", "Promo", "MontoMinimo", null, "0", 10, null, "El monto mínimo debe ser mayor que 0.")]
    [InlineData("PROMO", "Promo", "MontoMinimo", 1, "10", 10, null, "Una promoción por monto mínimo no lleva producto.")]
    [InlineData("PROMO", "Promo", "Producto", 1, null, 0, null, "El descuento debe ser mayor que 0 y como máximo 100.")]
    [InlineData("PROMO", "Promo", "Producto", 1, null, 100.01, null, "El descuento debe ser mayor que 0 y como máximo 100.")]
    [InlineData("PROMO", "Promo", "Producto", 1, null, 10, 999, "El segmento 999 no existe.")]
    [InlineData("PROMO", "Promo", "Producto", 1, null, 10, null, "La fecha de fin no puede ser anterior a la de inicio.", true)]
    public async Task AltaInvalidaDevuelveErrorLiteralYNoCreaFilas(
        string codigo,
        string nombre,
        string tipo,
        int? productoId,
        string? montoMinimo,
        double porcentaje,
        int? segmentoId,
        string mensaje,
        bool fechaInvalida = false)
    {
        int antes = await ContarPromocionesAsync();
        GuardarPromocionRequestDto solicitud = SolicitudValida(codigo, nombre, tipo, productoId,
            montoMinimo is not null ? (decimal?)Convert.ToDecimal(montoMinimo, System.Globalization.CultureInfo.InvariantCulture) : null,
            Convert.ToDecimal(porcentaje));
        solicitud.SegmentoClienteId = segmentoId;
        if (fechaInvalida)
        {
            solicitud.FechaFin = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        }

        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/promociones", solicitud);
        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(antes, await ContarPromocionesAsync());
    }

    [Fact]
    public async Task CodigoYTipoSeGuardanCanonicosYPutPuedeConservarSuCodigo()
    {
        GuardarPromocionRequestDto solicitud = SolicitudValida("  promo-local  ", "Oferta local", "producto", 1, null, 12.5m);
        HttpResponseMessage alta = await _cliente.PostAsJsonAsync("/api/v1/promociones", solicitud);
        PromocionCreadaDto? creada = await alta.Content.ReadFromJsonAsync<PromocionCreadaDto>();
        Assert.Equal(HttpStatusCode.OK, alta.StatusCode);
        Assert.NotNull(creada);
        solicitud.Codigo = "promo-local";
        solicitud.Tipo = "PRODUCTO";
        HttpResponseMessage actualizacion = await _cliente.PutAsJsonAsync($"/api/v1/promociones/{creada.Id}", solicitud);
        Assert.Equal(HttpStatusCode.OK, actualizacion.StatusCode);
        PromocionDetalleDto? detalle = await _cliente.GetFromJsonAsync<PromocionDetalleDto>(
            $"/api/v1/promociones/{creada.Id}");
        Assert.NotNull(detalle);
        Assert.Equal("PROMO-LOCAL", detalle.Codigo);
        Assert.Equal("Producto", detalle.Tipo);
    }

    [Fact]
    public async Task CodigoDuplicadoDevuelve409LiteralSinCrearFilas()
    {
        await CrearPromocionAsync("EXISTENTE", "Producto", 1, null, 10m);
        int antes = await ContarPromocionesAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/promociones",
            SolicitudValida("existente", "Duplicada", "Producto", 1, null, 5m));
        await AfirmarErrorAsync(respuesta, HttpStatusCode.Conflict, 409,
            "Ya existe una promoción con el código EXISTENTE.");
        Assert.Equal(antes, await ContarPromocionesAsync());
    }

    [Fact]
    public async Task PutInvalidoNoCambiaLaPromocion()
    {
        int id = await CrearPromocionAsync("ORIGINAL", "Producto", 1, null, 10m);
        GuardarPromocionRequestDto solicitud = SolicitudValida("CAMBIO", "Cambio", "Producto", 1, null, 101m);
        HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"/api/v1/promociones/{id}", solicitud);
        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400,
            "El descuento debe ser mayor que 0 y como máximo 100.");
        PromocionDetalleDto? detalle = await _cliente.GetFromJsonAsync<PromocionDetalleDto>(
            $"/api/v1/promociones/{id}");
        Assert.NotNull(detalle);
        Assert.Equal("ORIGINAL", detalle.Codigo);
        Assert.Equal(10m, detalle.PorcentajeDescuento);
    }

    [Fact]
    public async Task IdentificadorInexistenteDevuelve404LiteralSinDetalle()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/v1/promociones/999");
        await AfirmarErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "La promoción 999 no existe.");
    }

    [Fact]
    public async Task EvaluacionDeSeedCalculaImportesExactosParaAcmeYBeta()
    {
        await SembrarPromocionesEjemploAsync();
        EvaluacionCompraDto? acme = await EvaluarAsync(1, new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 2, PrecioUnitario = 2500m } });
        EvaluacionCompraDto? beta = await EvaluarAsync(2, new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 2, PrecioUnitario = 2500m } });
        Assert.NotNull(acme);
        Assert.NotNull(beta);
        Assert.Equal("VIP", acme.SegmentoCodigo);
        Assert.Equal("LAPTOP-VIP-15", Assert.Single(acme.Lineas).Promocion!.Codigo);
        Assert.Equal(750m, acme.Lineas[0].Descuento);
        Assert.Equal(4250m, acme.TotalTrasLineas);
        Assert.Equal("COMPRA-3000", acme.PromocionCompra!.Codigo);
        Assert.Equal(212.5m, acme.DescuentoCompra);
        Assert.Equal(4037.5m, acme.Total);
        Assert.Equal("LAPTOP-10", Assert.Single(beta.Lineas).Promocion!.Codigo);
        Assert.Equal(500m, beta.Lineas[0].Descuento);
        Assert.Equal(4500m, beta.TotalTrasLineas);
        Assert.Equal(225m, beta.DescuentoCompra);
        Assert.Equal(4275m, beta.Total);
    }

    [Fact]
    public async Task GanaMayorPorcentajeYEnEmpateMenorIdPorLineaYCompra()
    {
        int productoMenorId = await CrearPromocionAsync("PROD-EMPATE-A", "Producto", 1, null, 10m);
        await CrearPromocionAsync("PROD-EMPATE-B", "Producto", 1, null, 10m);
        await CrearPromocionAsync("PROD-MAYOR", "Producto", 1, null, 20m);
        int compraMenorId = await CrearPromocionAsync("COMPRA-EMPATE-A", "MontoMinimo", null, 1m, 5m);
        await CrearPromocionAsync("COMPRA-EMPATE-B", "MontoMinimo", null, 1m, 5m);
        EvaluacionCompraDto? evaluacion = await EvaluarAsync(5,
            new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m } });
        Assert.NotNull(evaluacion);
        Assert.Equal("PROD-MAYOR", Assert.Single(evaluacion.Lineas).Promocion!.Codigo);
        Assert.Equal("COMPRA-EMPATE-A", evaluacion.PromocionCompra!.Codigo);
        Assert.True(productoMenorId < compraMenorId);
    }

    [Fact]
    public async Task EnEmpateDePorcentajePorProductoGanaLaPromocionConMenorId()
    {
        await CrearPromocionAsync("PRODUCTO-EMPATE-A", "Producto", 1, null, 10m);
        await CrearPromocionAsync("PRODUCTO-EMPATE-B", "Producto", 1, null, 10m);
        EvaluacionCompraDto? evaluacion = await EvaluarAsync(5,
            new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m } });

        Assert.NotNull(evaluacion);
        Assert.Equal("PRODUCTO-EMPATE-A", Assert.Single(evaluacion.Lineas).Promocion!.Codigo);
    }

    [Fact]
    public async Task PromocionDeSegmentoNoAplicaAClienteDeOtroSegmento()
    {
        await CrearPromocionAsync("SOLO-VIP", "Producto", 1, null, 50m, 1);
        EvaluacionCompraDto? evaluacion = await EvaluarAsync(2,
            new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m } });
        Assert.NotNull(evaluacion);
        Assert.Null(Assert.Single(evaluacion.Lineas).Promocion);
        Assert.Equal(100m, evaluacion.Total);
    }

    [Fact]
    public async Task PromocionesInactivasOFueraDeFechaNoAplicanYLimitesIncluyenDias()
    {
        DateTime inicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime fin = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        await CrearPromocionAsync("INACTIVA", "Producto", 1, null, 90m, activa: false);
        await CrearPromocionAsync("VIGENTE", "Producto", 1, null, 10m, inicio: inicio, fin: fin);
        EvaluacionCompraDto? diaAntes = await EvaluarAsync(5,
            new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m } }, inicio.AddDays(-1));
        EvaluacionCompraDto? diaInicio = await EvaluarAsync(5,
            new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m } }, inicio);
        EvaluacionCompraDto? diaFin = await EvaluarAsync(5,
            new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m } }, fin);
        EvaluacionCompraDto? diaDespues = await EvaluarAsync(5,
            new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 1, PrecioUnitario = 100m } }, fin.AddDays(1));
        Assert.Null(Assert.Single(diaAntes!.Lineas).Promocion);
        Assert.Equal(10m, Assert.Single(diaInicio!.Lineas).Promocion!.PorcentajeDescuento);
        Assert.Equal(10m, Assert.Single(diaFin!.Lineas).Promocion!.PorcentajeDescuento);
        Assert.Null(Assert.Single(diaDespues!.Lineas).Promocion);
    }

    [Fact]
    public async Task MontoMinimoSeEvaluaTrasDescuentoDeLineaIncluyendoElLimiteExacto()
    {
        await CrearPromocionAsync("LINEA-10", "Producto", 1, null, 10m);
        await CrearPromocionAsync("COMPRA-900", "MontoMinimo", null, 900m, 5m);
        EvaluacionCompraDto? justo = await EvaluarAsync(5,
            new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 1, PrecioUnitario = 1000m } });
        EvaluacionCompraDto? unCentavoMenos = await EvaluarAsync(5,
            new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 1, PrecioUnitario = 999m } });
        Assert.Equal(900m, justo!.TotalTrasLineas);
        Assert.Equal("COMPRA-900", justo.PromocionCompra!.Codigo);
        Assert.Equal(899.1m, unCentavoMenos!.TotalTrasLineas);
        Assert.Null(unCentavoMenos.PromocionCompra);
    }

    [Fact]
    public async Task SinPromocionesTotalEsIgualAlSubtotal()
    {
        EvaluacionCompraDto? evaluacion = await EvaluarAsync(5,
            new[] { new LineaCompraRequestDto { ProductoId = 1, Cantidad = 2, PrecioUnitario = 12.35m } });
        Assert.NotNull(evaluacion);
        Assert.Equal(24.7m, evaluacion.Subtotal);
        Assert.Equal(24.7m, evaluacion.Total);
        Assert.Equal(0m, evaluacion.DescuentoTotal);
        Assert.Null(evaluacion.PromocionCompra);
    }

    [Theory]
    [InlineData(999, "El cliente 999 no existe.", "cliente")]
    [InlineData(1, "La compra debe tener al menos una línea.", "vacio")]
    [InlineData(1, "La cantidad debe ser mayor que 0.", "cantidad")]
    [InlineData(1, "El precio unitario debe ser mayor que 0.", "precio")]
    [InlineData(1, "El producto 999 no existe.", "producto")]
    public async Task EvaluacionInvalidaDevuelveErrorLiteralSinDetail(
        int clienteId,
        string mensaje,
        string caso)
    {
        EvaluarCompraRequestDto solicitud = new EvaluarCompraRequestDto { ClienteId = clienteId };
        if (caso == "cantidad")
        {
            solicitud.Lineas.Add(new LineaCompraRequestDto { ProductoId = 1, Cantidad = 0, PrecioUnitario = 10m });
        }
        else if (caso == "precio")
        {
            solicitud.Lineas.Add(new LineaCompraRequestDto { ProductoId = 1, Cantidad = 1, PrecioUnitario = 0m });
        }
        else if (caso == "producto")
        {
            solicitud.Lineas.Add(new LineaCompraRequestDto { ProductoId = 999, Cantidad = 1, PrecioUnitario = 10m });
        }

        int antes = await ContarPromocionesAsync();
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/promociones/evaluar", solicitud);
        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
        Assert.Equal(antes, await ContarPromocionesAsync());
    }

    [Theory]
    [InlineData("?tipo=desconocido", "El tipo de promoción no es válido.")]
    [InlineData("?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    public async Task FiltrosInvalidosDevuelvenErrorLiteral(string consulta, string mensaje)
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/api/v1/promociones" + consulta);
        await AfirmarErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task FiltrosOrdenYResumenDevuelvenValoresExactos()
    {
        await SembrarPromocionesEjemploAsync();
        PaginaPromocionesDto? listado = await _cliente.GetFromJsonAsync<PaginaPromocionesDto>(
            "/api/v1/promociones?pagina=1&tamanoPagina=2");
        PaginaPromocionesDto? tipo = await _cliente.GetFromJsonAsync<PaginaPromocionesDto>(
            "/api/v1/promociones?tipo=productO&vigentes=true&pagina=1&tamanoPagina=20");
        PaginaPromocionesDto? busquedaCodigo = await _cliente.GetFromJsonAsync<PaginaPromocionesDto>(
            "/api/v1/promociones?busqueda=compra-3000");
        PaginaPromocionesDto? busquedaNombre = await _cliente.GetFromJsonAsync<PaginaPromocionesDto>(
            "/api/v1/promociones?busqueda=laptop%2010");
        PaginaPromocionesDto? segmento = await _cliente.GetFromJsonAsync<PaginaPromocionesDto>(
            "/api/v1/promociones?segmentoId=1");
        PromocionDetalleDto? detalle = await _cliente.GetFromJsonAsync<PromocionDetalleDto>("/api/v1/promociones/1");
        ResumenPromocionesDto? resumen = await _cliente.GetFromJsonAsync<ResumenPromocionesDto>("/api/v1/promociones/resumen");
        Assert.NotNull(listado);
        Assert.Equal(4, listado.Total);
        Assert.Equal(new[] { "COMPRA-3000", "LAPTOP-10" }, listado.Items.Select(item => item.Codigo).ToArray());
        Assert.Equal(3, listado.Items[0].Id);
        Assert.Equal(1, listado.Items[1].Id);
        Assert.NotNull(tipo);
        Assert.Equal(2, tipo.Total);
        Assert.All(tipo.Items, promocion => Assert.True(promocion.Vigente));
        Assert.NotNull(busquedaCodigo);
        Assert.Equal(new[] { "COMPRA-3000" }, busquedaCodigo.Items.Select(item => item.Codigo).ToArray());
        Assert.NotNull(busquedaNombre);
        Assert.Equal(new[] { "LAPTOP-10" }, busquedaNombre.Items.Select(item => item.Codigo).ToArray());
        Assert.NotNull(segmento);
        Assert.Equal(new[] { "LAPTOP-VIP-15" }, segmento.Items.Select(item => item.Codigo).ToArray());
        Assert.NotNull(detalle);
        Assert.Equal("LAPTOP-10", detalle.Codigo);
        Assert.Equal("Laptop Pro", detalle.ProductoNombre);
        Assert.NotNull(resumen);
        Assert.Equal(4, resumen.Total);
        Assert.Equal(4, resumen.Activas);
        Assert.Equal(3, resumen.Vigentes);
        Assert.Equal(3, resumen.DeProducto);
        Assert.Equal(1, resumen.DeMontoMinimo);
        Assert.Equal(1, resumen.DeSegmento);
        Assert.Equal(10m, resumen.PorcentajePromedioVigentes);
    }

    [Fact]
    public async Task OpenApiIncluyeLasSieteOperacionesDePromociones()
    {
        HttpResponseMessage respuesta = await _cliente.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement paths = documento.RootElement.GetProperty("paths");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(paths.GetProperty("/api/v1/promociones").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty("/api/v1/promociones").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/v1/promociones/{id}").TryGetProperty("get", out _));
        Assert.True(paths.GetProperty("/api/v1/promociones/{id}").TryGetProperty("put", out _));
        Assert.True(paths.GetProperty("/api/v1/promociones/{id}").TryGetProperty("delete", out _));
        Assert.True(paths.GetProperty("/api/v1/promociones/evaluar").TryGetProperty("post", out _));
        Assert.True(paths.GetProperty("/api/v1/promociones/resumen").TryGetProperty("get", out _));
    }

    private void PrepararDatos()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        if (!contexto.Productos.Any(producto => producto.Id == 3))
        {
            contexto.Productos.Add(new Producto
            {
                Id = 3,
                Nombre = "Mouse Óptico",
                Categoria = "Perifericos",
                Precio = 50m
            });
        }

        contexto.SaveChanges();
    }

    private async Task SembrarPromocionesEjemploAsync()
    {
        await CrearPromocionAsync("LAPTOP-10", "Producto", 1, null, 10m, nombre: "Laptop 10 %");
        await CrearPromocionAsync("LAPTOP-VIP-15", "Producto", 1, null, 15m, 1);
        await CrearPromocionAsync("COMPRA-3000", "MontoMinimo", null, 3000m, 5m);
        await CrearPromocionAsync("VERANO-2025", "Producto", 3, null, 20m,
            inicio: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            fin: new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc));
    }

    private async Task<int> CrearPromocionAsync(
        string codigo,
        string tipo,
        int? productoId,
        decimal? montoMinimo,
        decimal porcentaje,
        int? segmentoId = null,
        bool activa = true,
        DateTime? inicio = null,
        DateTime? fin = null,
        string? nombre = null)
    {
        GuardarPromocionRequestDto solicitud = SolicitudValida(codigo, nombre ?? codigo, tipo, productoId, montoMinimo, porcentaje);
        solicitud.SegmentoClienteId = segmentoId;
        solicitud.Activa = activa;
        solicitud.FechaInicio = inicio ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        solicitud.FechaFin = fin ?? new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/promociones", solicitud);
        string cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.True(respuesta.StatusCode == HttpStatusCode.OK, cuerpo);
        PromocionCreadaDto? creada = JsonSerializer.Deserialize<PromocionCreadaDto>(cuerpo,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(creada);
        return creada.Id;
    }

    private async Task<EvaluacionCompraDto?> EvaluarAsync(
        int clienteId,
        LineaCompraRequestDto[] lineas,
        DateTime? fecha = null)
    {
        EvaluarCompraRequestDto solicitud = new EvaluarCompraRequestDto
        {
            ClienteId = clienteId,
            Fecha = fecha,
            Lineas = lineas.ToList()
        };
        HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync("/api/v1/promociones/evaluar", solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<EvaluacionCompraDto>();
    }

    private static GuardarPromocionRequestDto SolicitudValida(
        string codigo,
        string nombre,
        string tipo,
        int? productoId,
        decimal? montoMinimo,
        decimal porcentaje)
    {
        return new GuardarPromocionRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            ProductoId = productoId,
            MontoMinimo = montoMinimo,
            PorcentajeDescuento = porcentaje,
            FechaInicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            FechaFin = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            Activa = true
        };
    }

    private async Task<int> ContarPromocionesAsync()
    {
        using IServiceScope alcance = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return await contexto.Promociones.CountAsync();
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
