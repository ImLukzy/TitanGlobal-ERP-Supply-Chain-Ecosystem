using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo51_FacturacionElectronica;

public sealed class FacturasApiTests : IClassFixture<FacturasApiFactory>, IDisposable
{
    private const string Ruta = "/api/v1/facturas";
    private readonly FacturasApiFactory _fabrica;
    private readonly HttpClient _cliente;

    public FacturasApiTests(FacturasApiFactory fabrica)
    {
        _fabrica = fabrica;
        _fabrica.PrepararDatos();
        _cliente = fabrica.CreateClient();
        TokenPruebas.AutenticarComoAdministrador(_cliente, fabrica.Services);
    }

    [Theory]
    [InlineData(" fActuRa ", "Factura")]
    [InlineData(" bOlEtA ", "Boleta")]
    public async Task CrearYConsultarNormalizaYCalculaCadaImporte(string tipo, string tipoEsperado)
    {
        GuardarFacturaRequestDto solicitud = Solicitud();
        solicitud.TipoComprobante = tipo;
        solicitud.Serie = " f001 ";
        solicitud.Moneda = " pen ";
        solicitud.FechaEmision = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Unspecified);
        solicitud.Observaciones = "  Pedido especial  ";
        solicitud.Detalles[0].Cantidad = 1.005m;
        solicitud.Detalles[0].PrecioUnitario = 1m;
        solicitud.Detalles[0].DescuentoPorcentaje = 50m;

        FacturaDetalleDto creada = await CrearAsync(solicitud);
        FacturaDetalleDto factura = await ObtenerAsync(creada.Id);
        Assert.Equal("Borrador", factura.Estado);
        Assert.Equal(tipoEsperado, factura.TipoComprobante);
        Assert.Equal("F001-00000001", factura.NumeroComprobante);
        Assert.Equal("PEN", factura.Moneda);
        Assert.Equal(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc), factura.FechaEmision);
        Assert.Equal(DateTimeKind.Utc, factura.FechaEmision.Kind);
        Assert.Equal("Cliente Industrial", factura.ClienteNombre);
        Assert.Equal("20111111111", factura.ClienteRuc);
        Assert.Equal("OC-FACT-01", factura.NumeroOrdenCompra);
        Assert.Equal("Pedido especial", factura.Observaciones);
        Assert.Null(factura.FechaAnulacion);
        Assert.Null(factura.MotivoAnulacion);
        Assert.Equal((1.01m, 0.51m, 0.09m, 0.59m), (factura.Subtotal, factura.TotalDescuento, factura.TotalImpuesto, factura.Total));
        DetalleFacturaDto detalle = Assert.Single(factura.Detalles);
        Assert.Equal("Producto uno", detalle.ProductoNombre);
        Assert.Equal("Producto de prueba", detalle.Descripcion);
        Assert.Equal("IGV", detalle.ImpuestoNombre);
        Assert.Equal(18m, detalle.PorcentajeImpuesto);
        Assert.Equal((1.01m, 0.51m, 0.50m, 0.09m, 0.59m),
            (detalle.Subtotal, detalle.ImporteDescuento, detalle.BaseImponible, detalle.ImporteImpuesto, detalle.Total));
    }

    [Fact]
    public async Task EditarBorradorReemplazaDetallesYUsaLaTasaVigente()
    {
        FacturaDetalleDto creada = await CrearAsync(Solicitud());
        GuardarFacturaRequestDto cambio = Solicitud();
        cambio.FechaEmision = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        cambio.Detalles.Add(new GuardarDetalleFacturaRequestDto
        {
            ProductoId = 2,
            Descripcion = "Sin impuesto",
            Cantidad = 2m,
            PrecioUnitario = 5m
        });

        using HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{Ruta}/{creada.Id}", cambio);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        FacturaDetalleDto editada = await ObtenerAsync(creada.Id);
        Assert.Equal("Borrador", editada.Estado);
        Assert.Equal(creada.FechaCreacion, editada.FechaCreacion);
        Assert.Equal(new[] { 1, 2 }, editada.Detalles.Select(detalle => detalle.ProductoId));
        Assert.DoesNotContain(editada.Detalles, detalle => detalle.Id == creada.Detalles[0].Id);
        Assert.Equal((17m, 3.40m), (editada.Detalles[0].PorcentajeImpuesto, editada.Detalles[0].ImporteImpuesto));
        Assert.Null(editada.Detalles[1].ImpuestoId);
        Assert.Equal(0m, editada.Detalles[1].ImporteImpuesto);
        Assert.Equal(33.40m, editada.Total);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnulacionLogicaConservaImportesYBloqueaTransiciones(bool emitirPrimero)
    {
        FacturaDetalleDto creada = await CrearAsync(Solicitud());
        if (emitirPrimero)
        {
            await EmitirAsync(creada.Id);
            Assert.Equal("Emitida", (await ObtenerAsync(creada.Id)).Estado);
            using HttpResponseMessage editar = await _cliente.PutAsJsonAsync($"{Ruta}/{creada.Id}", Solicitud());
            await VerificarErrorAsync(editar, HttpStatusCode.Conflict, "Solo se pueden modificar facturas en estado Borrador.");
            using HttpResponseMessage emitir = await _cliente.PostAsync($"{Ruta}/{creada.Id}/emitir", null);
            await VerificarErrorAsync(emitir, HttpStatusCode.Conflict, "Solo se pueden emitir facturas en estado Borrador.");
        }

        await AnularAsync(creada.Id);
        FacturaDetalleDto anulada = await ObtenerAsync(creada.Id);
        Assert.Equal("Anulada", anulada.Estado);
        Assert.Equal("Registro duplicado", anulada.MotivoAnulacion);
        Assert.NotNull(anulada.FechaAnulacion);
        Assert.Equal(DateTimeKind.Utc, anulada.FechaAnulacion.Value.Kind);
        Assert.Equal(creada.Total, anulada.Total);
        Assert.Equal(Assert.Single(creada.Detalles).Id, Assert.Single(anulada.Detalles).Id);

        using HttpResponseMessage repetir = await _cliente.PostAsJsonAsync(
            $"{Ruta}/{creada.Id}/anular", new AnularFacturaRequestDto { Motivo = "Otro motivo" });
        await VerificarErrorAsync(repetir, HttpStatusCode.Conflict, "La factura ya se encuentra anulada.");
        using HttpResponseMessage emitirAnulada = await _cliente.PostAsync($"{Ruta}/{creada.Id}/emitir", null);
        await VerificarErrorAsync(emitirAnulada, HttpStatusCode.Conflict, "Solo se pueden emitir facturas en estado Borrador.");
        using HttpResponseMessage editarAnulada = await _cliente.PutAsJsonAsync($"{Ruta}/{creada.Id}", Solicitud());
        await VerificarErrorAsync(editarAnulada, HttpStatusCode.Conflict, "Solo se pueden modificar facturas en estado Borrador.");
        Assert.Equal(anulada.MotivoAnulacion, (await ObtenerAsync(creada.Id)).MotivoAnulacion);
    }

    [Fact]
    public async Task ListadoCombinaFiltrosYPaginaEnOrdenEstable()
    {
        FacturaDetalleDto primera = await CrearAsync(Solicitud());
        GuardarFacturaRequestDto solicitud = Solicitud(2);
        solicitud.FechaEmision = primera.FechaEmision.AddDays(1);
        FacturaDetalleDto segunda = await CrearAsync(solicitud);
        await EmitirAsync(segunda.Id);
        solicitud = Solicitud(3, "USD");
        solicitud.TipoComprobante = "Boleta";
        solicitud.ClienteId = 2;
        solicitud.OrdenCompraId = null;
        FacturaDetalleDto tercera = await CrearAsync(solicitud);

        PaginaFacturasDto? pagina = await _cliente.GetFromJsonAsync<PaginaFacturasDto>($"{Ruta}?pagina=2&tamanoPagina=2");
        Assert.NotNull(pagina);
        Assert.Equal((2, 2, 3, 2), (pagina.Pagina, pagina.TamanoPagina, pagina.TotalRegistros, pagina.TotalPaginas));
        Assert.Equal(primera.Id, Assert.Single(pagina.Items).Id);

        PaginaFacturasDto? nombre = await _cliente.GetFromJsonAsync<PaginaFacturasDto>($"{Ruta}?busqueda=Industrial");
        Assert.NotNull(nombre);
        Assert.Equal(new[] { segunda.Id, primera.Id }, nombre.Items.Select(factura => factura.Id));
        PaginaFacturasDto? ruc = await _cliente.GetFromJsonAsync<PaginaFacturasDto>($"{Ruta}?busqueda=20222222222");
        Assert.NotNull(ruc);
        Assert.Equal(tercera.Id, Assert.Single(ruc.Items).Id);

        string filtros = "busqueda=f001-00000002&clienteId=1&tipoComprobante=factura&estado=emitida&moneda=pen"
            + "&fechaDesde=2026-06-02T12:00:00Z&fechaHasta=2026-06-02T12:00:00Z";
        PaginaFacturasDto? filtradas = await _cliente.GetFromJsonAsync<PaginaFacturasDto>($"{Ruta}?{filtros}");
        Assert.NotNull(filtradas);
        FacturaListadoDto item = Assert.Single(filtradas.Items);
        Assert.Equal(segunda.Id, item.Id);
        Assert.Equal("F001-00000002", item.NumeroComprobante);
        Assert.Equal(1, item.CantidadDetalles);
    }

    [Fact]
    public async Task ResumenCuentaEstadosYSumaSoloEmitidasDeLaMonedaSolicitada()
    {
        await CrearAsync(Solicitud());
        GuardarFacturaRequestDto solicitud = Solicitud(2);
        solicitud.Detalles[0].DescuentoPorcentaje = 10m;
        await EmitirAsync((await CrearAsync(solicitud)).Id);
        solicitud = Solicitud(3);
        solicitud.TipoComprobante = "Boleta";
        await AnularAsync((await CrearAsync(solicitud)).Id);
        await EmitirAsync((await CrearAsync(Solicitud(4, "USD"))).Id);

        ResumenFacturasDto? pen = await _cliente.GetFromJsonAsync<ResumenFacturasDto>($"{Ruta}/resumen");
        Assert.NotNull(pen);
        Assert.Equal("PEN", pen.Moneda);
        Assert.Equal((3, 2, 1, 1, 1, 1), (pen.TotalComprobantes, pen.TotalFacturas, pen.TotalBoletas,
            pen.TotalBorradores, pen.TotalEmitidas, pen.TotalAnuladas));
        Assert.Equal((20m, 2m, 3.24m, 21.24m),
            (pen.SubtotalFacturado, pen.TotalDescuentos, pen.TotalImpuestos, pen.TotalFacturado));

        ResumenFacturasDto? usd = await _cliente.GetFromJsonAsync<ResumenFacturasDto>($"{Ruta}/resumen?moneda=usd");
        Assert.NotNull(usd);
        Assert.Equal(("USD", 1, 23.60m), (usd.Moneda, usd.TotalEmitidas, usd.TotalFacturado));
        ResumenFacturasDto? vacio = await _cliente.GetFromJsonAsync<ResumenFacturasDto>(
            $"{Ruta}/resumen?fechaDesde=2026-06-02T00:00:00Z");
        Assert.NotNull(vacio);
        Assert.Equal((0, 0m), (vacio.TotalComprobantes, vacio.TotalFacturado));
    }

    [Fact]
    public async Task DuplicadoEs409EnAltaYEdicionInclusoDespuesDeAnular()
    {
        FacturaDetalleDto primera = await CrearAsync(Solicitud());
        await AnularAsync(primera.Id);
        GuardarFacturaRequestDto duplicada = Solicitud();
        duplicada.Serie = " f001 ";
        using HttpResponseMessage alta = await _cliente.PostAsJsonAsync(Ruta, duplicada);
        await VerificarErrorAsync(alta, HttpStatusCode.Conflict, "Ya existe un comprobante con la misma serie y número.");

        FacturaDetalleDto segunda = await CrearAsync(Solicitud(2));
        using HttpResponseMessage editar = await _cliente.PutAsJsonAsync($"{Ruta}/{segunda.Id}", duplicada);
        await VerificarErrorAsync(editar, HttpStatusCode.Conflict, "Ya existe un comprobante con la misma serie y número.");
        FacturaDetalleDto conservada = await ObtenerAsync(segunda.Id);
        Assert.Equal(2, conservada.Numero);
        Assert.Equal(Assert.Single(segunda.Detalles).Id, Assert.Single(conservada.Detalles).Id);

        duplicada.TipoComprobante = "Boleta";
        Assert.Equal("Boleta", (await CrearAsync(duplicada)).TipoComprobante);
    }

    [Theory]
    [InlineData("GET", "/9999")]
    [InlineData("PUT", "/9999")]
    [InlineData("POST", "/9999/emitir")]
    [InlineData("POST", "/9999/anular")]
    public async Task OperarSobreUnComprobanteInexistenteEs404(string metodo, string ruta)
    {
        using HttpRequestMessage peticion = new(new HttpMethod(metodo), Ruta + ruta);
        if (metodo == "PUT")
        {
            peticion.Content = JsonContent.Create(Solicitud());
        }
        if (ruta.EndsWith("/anular", StringComparison.Ordinal))
        {
            peticion.Content = JsonContent.Create(new AnularFacturaRequestDto { Motivo = "Corrección" });
        }
        using HttpResponseMessage respuesta = await _cliente.SendAsync(peticion);
        await VerificarErrorAsync(respuesta, HttpStatusCode.NotFound, "La factura solicitada no existe.");
    }

    [Theory]
    [InlineData("clienteId", "0", 400, "Debe seleccionar un cliente válido.")]
    [InlineData("clienteId", "99", 404, "El cliente seleccionado no existe.")]
    [InlineData("ordenCompraId", "0", 400, "La orden de compra no es válida.")]
    [InlineData("ordenCompraId", "99", 404, "La orden de compra seleccionada no existe.")]
    [InlineData("clienteId", "2", 400, "La orden de compra no pertenece al cliente seleccionado.")]
    [InlineData("tipoComprobante", "\"ticket\"", 400, "El tipo de comprobante debe ser Factura o Boleta.")]
    [InlineData("serie", "\"  \"", 400, "La serie del comprobante es obligatoria.")]
    [InlineData("serie", "\"AB123456789\"", 400, "La serie no puede superar los 10 caracteres.")]
    [InlineData("numero", "0", 400, "El número del comprobante debe ser mayor que cero.")]
    [InlineData("fechaEmision", "null", 400, "La fecha de emisión es obligatoria.")]
    [InlineData("fechaVencimiento", "\"2026-05-31T12:00:00Z\"", 400, "La fecha de vencimiento no puede ser anterior a la fecha de emisión.")]
    [InlineData("moneda", "\"AB1\"", 400, "La moneda debe tener un código de tres letras.")]
    [InlineData("detalles", "[]", 400, "La factura debe contener al menos un producto.")]
    [InlineData("detalles", "[null]", 400, "El detalle de la factura no es válido.")]
    public async Task CabeceraInvalidaConservaCodigoYMensajeLiteral(string campo, string json, int estado, string mensaje)
    {
        JsonObject solicitud = JsonSerializer.SerializeToNode(Solicitud(), JsonSerializerOptions.Web)!.AsObject();
        solicitud[campo] = JsonNode.Parse(json);
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, solicitud);
        await VerificarErrorAsync(respuesta, (HttpStatusCode)estado, mensaje);
    }

    [Theory]
    [InlineData("productoId", "0", 400, "Debe seleccionar un producto válido.")]
    [InlineData("productoId", "99", 404, "No se encontró el producto con ID 99.")]
    [InlineData("impuestoId", "0", 400, "El impuesto seleccionado no es válido.")]
    [InlineData("impuestoId", "99", 404, "No se encontró el impuesto con ID 99.")]
    [InlineData("impuestoId", "2", 400, "Por el momento solo se admite el IGV como impuesto de facturación.")]
    [InlineData("impuestoId", "3", 400, "El impuesto seleccionado se encuentra inactivo.")]
    [InlineData("descripcion", "\" \"", 400, "La descripción del producto es obligatoria.")]
    [InlineData("cantidad", "0", 400, "La cantidad del producto no es válida.")]
    [InlineData("cantidad", "1.00001", 400, "La cantidad admite como máximo cuatro decimales.")]
    [InlineData("precioUnitario", "-1", 400, "El precio unitario no es válido.")]
    [InlineData("precioUnitario", "10.001", 400, "El precio unitario admite como máximo dos decimales.")]
    [InlineData("precioUnitario", "9999999999999999.99", 400, "El subtotal del producto supera el importe permitido.")]
    [InlineData("descuentoPorcentaje", "101", 400, "El descuento debe estar entre 0 y 100.")]
    [InlineData("descuentoPorcentaje", "10.001", 400, "El descuento admite como máximo dos decimales.")]
    public async Task DetalleInvalidoConservaCodigoYMensajeLiteral(string campo, string json, int estado, string mensaje)
    {
        JsonObject solicitud = JsonSerializer.SerializeToNode(Solicitud(), JsonSerializerOptions.Web)!.AsObject();
        solicitud["detalles"]!.AsArray()[0]!.AsObject()[campo] = JsonNode.Parse(json);
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, solicitud);
        await VerificarErrorAsync(respuesta, (HttpStatusCode)estado, mensaje);
    }

    [Fact]
    public async Task ExigeTasaVigenteYRespetaLimitesDeTextoEImporte()
    {
        GuardarFacturaRequestDto solicitud = Solicitud();
        solicitud.FechaEmision = new DateTime(2025, 12, 31, 12, 0, 0, DateTimeKind.Utc);
        using HttpResponseMessage sinTasa = await _cliente.PostAsJsonAsync(Ruta, solicitud);
        await VerificarErrorAsync(sinTasa, HttpStatusCode.BadRequest, "El impuesto no tiene una tasa vigente para la fecha de emisión.");
        solicitud.FechaEmision = Solicitud().FechaEmision;
        solicitud.Observaciones = new string('A', 501);
        using HttpResponseMessage observaciones = await _cliente.PostAsJsonAsync(Ruta, solicitud);
        await VerificarErrorAsync(observaciones, HttpStatusCode.BadRequest, "El texto no puede superar los 500 caracteres.");
        solicitud.Observaciones = null;
        solicitud.Detalles[0].Descripcion = new string('A', 301);
        using HttpResponseMessage descripcion = await _cliente.PostAsJsonAsync(Ruta, solicitud);
        await VerificarErrorAsync(descripcion, HttpStatusCode.BadRequest, "La descripción no puede superar los 300 caracteres.");
        solicitud.Detalles = Solicitud().Detalles;
        solicitud.Detalles[0].Cantidad = 1m;
        solicitud.Detalles[0].PrecioUnitario = 9999999999999999.99m;
        using HttpResponseMessage importe = await _cliente.PostAsJsonAsync(Ruta, solicitud);
        await VerificarErrorAsync(importe, HttpStatusCode.BadRequest, "El importe calculado supera los límites permitidos.");
    }

    [Theory]
    [InlineData("?", "pagina=0", "La página debe ser mayor que cero.")]
    [InlineData("?", "tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("?", "pagina=2147483647&tamanoPagina=100", "La página solicitada supera el límite permitido.")]
    [InlineData("?", "clienteId=0", "El cliente seleccionado no es válido.")]
    [InlineData("?", "estado=pendiente", "El estado debe ser Borrador, Emitida o Anulada.")]
    [InlineData("?", "moneda=P1", "La moneda debe tener un código de tres letras.")]
    [InlineData("/resumen?", "moneda=P1", "La moneda debe tener un código de tres letras.")]
    [InlineData("?", "fechaDesde=2026-07-01&fechaHasta=2026-06-01", "La fecha inicial no puede ser posterior a la fecha final.")]
    [InlineData("/resumen?", "fechaDesde=2026-07-01&fechaHasta=2026-06-01", "La fecha inicial no puede ser posterior a la fecha final.")]
    public async Task FiltrosInvalidosDevuelven400(string ruta, string consulta, string mensaje)
    {
        using HttpResponseMessage respuesta = await _cliente.GetAsync(Ruta + ruta + consulta);
        await VerificarErrorAsync(respuesta, HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task EmisionRechazaTotalCeroYCompruebaLosImportesPersistidos()
    {
        GuardarFacturaRequestDto solicitud = Solicitud();
        solicitud.Detalles[0].DescuentoPorcentaje = 100m;
        FacturaDetalleDto gratis = await CrearAsync(solicitud);
        using HttpResponseMessage totalCero = await _cliente.PostAsync($"{Ruta}/{gratis.Id}/emitir", null);
        await VerificarErrorAsync(totalCero, HttpStatusCode.BadRequest, "El total de la factura debe ser mayor que cero para emitirla.");
        Assert.Equal("Borrador", (await ObtenerAsync(gratis.Id)).Estado);

        FacturaDetalleDto creada = await CrearAsync(Solicitud(2));
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Factura persistida = await contexto.Facturas.SingleAsync(factura => factura.Id == creada.Id);
        persistida.Subtotal += 1m;
        persistida.Total += 1m;
        await contexto.SaveChangesAsync();

        using HttpResponseMessage inconsistente = await _cliente.PostAsync($"{Ruta}/{creada.Id}/emitir", null);
        await VerificarErrorAsync(inconsistente, HttpStatusCode.Conflict, "Los importes de la factura no coinciden con sus detalles.");
        Assert.Equal("Borrador", (await ObtenerAsync(creada.Id)).Estado);

        contexto.DetallesFactura.RemoveRange(contexto.DetallesFactura.Where(detalle => detalle.FacturaId == creada.Id));
        await contexto.SaveChangesAsync();
        using HttpResponseMessage sinDetalles = await _cliente.PostAsync($"{Ruta}/{creada.Id}/emitir", null);
        await VerificarErrorAsync(sinDetalles, HttpStatusCode.BadRequest, "No se puede emitir una factura sin productos.");
    }

    [Fact]
    public async Task AnulacionValidaMotivoAntesDeCambiarEstado()
    {
        FacturaDetalleDto creada = await CrearAsync(Solicitud());
        using HttpResponseMessage motivoVacio = await _cliente.PostAsJsonAsync(
            $"{Ruta}/{creada.Id}/anular", new AnularFacturaRequestDto { Motivo = "   " });
        await VerificarErrorAsync(motivoVacio, HttpStatusCode.BadRequest, "El motivo de anulación es obligatorio.");
        using HttpResponseMessage motivoLargo = await _cliente.PostAsJsonAsync(
            $"{Ruta}/{creada.Id}/anular", new AnularFacturaRequestDto { Motivo = new string('M', 501) });
        await VerificarErrorAsync(motivoLargo, HttpStatusCode.BadRequest, "El motivo de anulación no puede superar los 500 caracteres.");
        Assert.Equal("Borrador", (await ObtenerAsync(creada.Id)).Estado);
    }

    [Theory]
    [InlineData("GET", "", false)]
    [InlineData("GET", "/resumen", false)]
    [InlineData("GET", "/1", false)]
    [InlineData("POST", "", true)]
    [InlineData("PUT", "/1", true)]
    [InlineData("POST", "/1/emitir", false)]
    [InlineData("POST", "/1/anular", true)]
    public async Task LaSesionEsObligatoriaEnTodoElContrato(string metodo, string ruta, bool requiereCuerpo)
    {
        _cliente.DefaultRequestHeaders.Authorization = null;
        using HttpRequestMessage peticion = new(new HttpMethod(metodo), Ruta + ruta);
        if (requiereCuerpo)
        {
            peticion.Content = ruta.EndsWith("/anular", StringComparison.Ordinal)
                ? JsonContent.Create(new AnularFacturaRequestDto { Motivo = "Corrección" })
                : JsonContent.Create(Solicitud());
        }
        using HttpResponseMessage respuesta = await _cliente.SendAsync(peticion);
        await VerificarErrorAsync(respuesta, HttpStatusCode.Unauthorized, "Debe iniciar sesión para acceder a este recurso.");
    }

    [Fact]
    public async Task UsuarioAutenticadoSinRolPuedeLeerYEscribir()
    {
        _cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(_fabrica.Services));
        FacturaDetalleDto creada = await CrearAsync(Solicitud());
        using HttpResponseMessage editar = await _cliente.PutAsJsonAsync($"{Ruta}/{creada.Id}", Solicitud());
        Assert.Equal(HttpStatusCode.OK, editar.StatusCode);
        await EmitirAsync(creada.Id);
        await AnularAsync(creada.Id);
        Assert.Equal("Anulada", (await ObtenerAsync(creada.Id)).Estado);
    }

    [Theory]
    [InlineData("Bloqueado")]
    [InlineData("SinPermiso")]
    public async Task MiddlewareAplica403AunqueElTokenSeaValido(string rol)
    {
        _cliente.DefaultRequestHeaders.Add("X-User-Role", rol);
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, Solicitud());
        await VerificarErrorAsync(respuesta, HttpStatusCode.Forbidden, "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.");
    }

    public void Dispose()
    {
        _cliente.Dispose();
    }

    private static GuardarFacturaRequestDto Solicitud(int numero = 1, string moneda = "PEN")
    {
        return new GuardarFacturaRequestDto
        {
            ClienteId = 1,
            OrdenCompraId = 1,
            TipoComprobante = "Factura",
            Serie = "F001",
            Numero = numero,
            FechaEmision = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            Moneda = moneda,
            Detalles = new List<GuardarDetalleFacturaRequestDto>
            {
                new GuardarDetalleFacturaRequestDto
                {
                    ProductoId = 1,
                    ImpuestoId = 1,
                    Descripcion = "  Producto de prueba  ",
                    Cantidad = 2m,
                    PrecioUnitario = 10m
                }
            }
        };
    }

    private async Task<FacturaDetalleDto> CrearAsync(GuardarFacturaRequestDto solicitud)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(Ruta, solicitud);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        FacturaDetalleDto? factura = await respuesta.Content.ReadFromJsonAsync<FacturaDetalleDto>();
        Assert.NotNull(factura);
        Assert.Equal($"{Ruta}/{factura.Id}", respuesta.Headers.Location?.AbsolutePath);
        return factura;
    }

    private async Task<FacturaDetalleDto> ObtenerAsync(int id)
    {
        FacturaDetalleDto? factura = await _cliente.GetFromJsonAsync<FacturaDetalleDto>($"{Ruta}/{id}");
        Assert.NotNull(factura);
        return factura;
    }

    private async Task EmitirAsync(int id)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsync($"{Ruta}/{id}/emitir", null);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Emitida", (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estado").GetString());
    }

    private async Task AnularAsync(int id)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(
            $"{Ruta}/{id}/anular", new AnularFacturaRequestDto { Motivo = "  Registro duplicado  " });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("Anulada", (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estado").GetString());
    }

    private static async Task VerificarErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string mensaje)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        JsonElement cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)estado, cuerpo.GetProperty("StatusCode").GetInt32());
        Assert.Equal(mensaje, cuerpo.GetProperty("Message").GetString());
        Assert.False(cuerpo.TryGetProperty("Detail", out _));
    }
}
