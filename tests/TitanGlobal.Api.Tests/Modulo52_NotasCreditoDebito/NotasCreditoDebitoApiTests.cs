using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;
using TitanGlobal.Application.DTOs.Modulo52_NotasCreditoDebito;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo52_NotasCreditoDebito;

[Collection("Modulo04")]
public class NotasCreditoDebitoApiTests
{
    private const string RutaNotas = "/api/v1/notas-credito-debito";
    private const string RutaFacturas = "/api/v1/facturas";
    private const string SerieFacturas = "F52-TEST";
    private const string EstadoNotaInvalido = "El estado de la nota no permite esta operación.";
    private const string EstadoFacturaInvalido = "El estado de la factura no permite registrar la nota.";
    private const string NotaInexistente = "La nota de crédito o débito no existe.";
    private const string MotivoInvalido = "El motivo debe tener entre 3 y 300 caracteres.";
    private const string ImporteInvalido = "La cantidad o el importe de la nota no es válido.";
    private const string DetalleInvalido = "Indique al menos una línea válida y sin duplicados.";
    private const string FiltroInvalido = "Los filtros o la paginación de notas no son válidos.";
    private const string SinPermiso = "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.";
    private readonly HttpClient _cliente;
    private readonly OrdenesCompraApiFactory _fabrica;

    public NotasCreditoDebitoApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos(fabrica);
    }

    [Theory]
    [InlineData("Credito", "Factura", "PEN", "NC-000001")]
    [InlineData("Debito", "Boleta", "USD", "ND-000001")]
    public async Task AltaEdicionEmisionYAnulacionConservanElComprobante(
        string tipo, string tipoComprobante, string moneda, string numero)
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1, tipoComprobante, moneda);
        CrearNotaCreditoDebitoRequestDto solicitud = Solicitud(factura, tipo);
        solicitud.Motivo = " Ajuste informado ";
        using HttpResponseMessage alta = await _cliente.PostAsJsonAsync(RutaNotas, solicitud);
        NotaCreditoDebitoDetalleDto nota = await LeerAsync<NotaCreditoDebitoDetalleDto>(alta, HttpStatusCode.Created);
        Assert.Equal($"{RutaNotas}/{nota.Id}", alta.Headers.Location?.AbsolutePath);
        Assert.Equal(numero, nota.Numero);
        Assert.Equal(factura.Id, nota.FacturaId);
        Assert.Equal(factura.ClienteId, nota.ClienteId);
        Assert.Equal(tipoComprobante, nota.FacturaTipoComprobante);
        Assert.Equal(moneda, nota.Moneda);
        Assert.Equal("Ajuste informado", nota.Motivo);
        Assert.Equal("Borrador", nota.Estado);
        Assert.Equal(5.25m, nota.Total);
        Assert.Equal(DateTimeKind.Utc, nota.FechaCreacion.Kind);
        Assert.Null(nota.FechaEmision);

        LineaNotaCreditoDebitoDto linea = Assert.Single(nota.Detalles);
        Assert.Equal(Assert.Single(factura.Detalles).Id, linea.DetalleFacturaId);
        Assert.Equal(0.25m, linea.Cantidad);
        ActualizarNotaCreditoDebitoRequestDto cambio = Cambio(factura, 0.5m, 17m);
        using HttpResponseMessage editada = await _cliente.PutAsJsonAsync($"{RutaNotas}/{nota.Id}", cambio);
        NotaCreditoDebitoDetalleDto actualizada = await LeerAsync<NotaCreditoDebitoDetalleDto>(editada);
        Assert.Equal(8.50m, actualizada.Total);
        Assert.Equal(nota.Numero, actualizada.Numero);
        Assert.Equal(nota.FechaCreacion, actualizada.FechaCreacion);
        Assert.Equal(linea.Id, Assert.Single(actualizada.Detalles).Id);

        NotaCreditoDebitoDetalleDto emitida = await EmitirNotaAsync(nota.Id);
        Assert.Equal("Emitida", emitida.Estado);
        Assert.Equal(DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("America/Lima"))), emitida.FechaEmision);

        await VerificarErrorAsync(await _cliente.PutAsJsonAsync($"{RutaNotas}/{nota.Id}", cambio),
            HttpStatusCode.Conflict, EstadoNotaInvalido);
        await VerificarErrorAsync(await _cliente.PostAsync($"{RutaNotas}/{nota.Id}/emitir", null),
            HttpStatusCode.Conflict, EstadoNotaInvalido);
        await VerificarErrorAsync(await _cliente.PostAsJsonAsync($"{RutaFacturas}/{factura.Id}/anular",
            new { motivo = "Motivo de anulación" }), HttpStatusCode.Conflict,
            "No se puede anular un comprobante con notas emitidas.");

        using HttpResponseMessage facturaOriginal = await _cliente.GetAsync($"{RutaFacturas}/{factura.Id}");
        FacturaDetalleDto comprobante = await LeerAsync<FacturaDetalleDto>(facturaOriginal);
        Assert.Equal("Emitida", comprobante.Estado);
        VerificarImportes(factura, comprobante);

        NotaCreditoDebitoDetalleDto anulada = await AnularNotaAsync(nota.Id);
        Assert.Equal("Anulada", anulada.Estado);
        Assert.Equal(emitida.FechaEmision, anulada.FechaEmision);
        Assert.Equal("Rectificación interna", anulada.MotivoAnulacion);
        Assert.Equal(DateTimeKind.Utc, anulada.FechaAnulacion?.Kind);
        Assert.Equal(linea.Id, Assert.Single(anulada.Detalles).Id);
        await VerificarErrorAsync(await _cliente.PostAsJsonAsync($"{RutaNotas}/{nota.Id}/anular",
            new { motivo = "Segunda anulación" }), HttpStatusCode.Conflict, EstadoNotaInvalido);
        using HttpResponseMessage borrado = await _cliente.DeleteAsync($"{RutaNotas}/{nota.Id}");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, borrado.StatusCode);
        using HttpResponseMessage reabrir = await _cliente.PostAsJsonAsync($"{RutaNotas}/{nota.Id}/reabrir", new { });
        Assert.Equal(HttpStatusCode.NotFound, reabrir.StatusCode);

        using HttpResponseMessage facturaAnulada = await _cliente.PostAsJsonAsync($"{RutaFacturas}/{factura.Id}/anular",
            new { motivo = "Motivo de anulación" });
        FacturaDetalleDto despues = await LeerAsync<FacturaDetalleDto>(facturaAnulada);
        Assert.Equal("Anulada", despues.Estado);
        VerificarImportes(factura, despues);
    }

    [Fact]
    public async Task BorradorNoBloqueaAnularFacturaYNoPuedeEmitirseDespues()
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1);
        NotaCreditoDebitoDetalleDto nota = await CrearNotaAsync(Solicitud(factura));

        await VerificarErrorAsync(await _cliente.PostAsJsonAsync($"{RutaNotas}/{nota.Id}/anular",
            new { motivo = "Motivo de anulación" }), HttpStatusCode.Conflict, EstadoNotaInvalido);
        using HttpResponseMessage anulacion = await _cliente.PostAsJsonAsync($"{RutaFacturas}/{factura.Id}/anular",
            new { motivo = "Comprobante anulado" });
        Assert.Equal("Anulada", (await LeerAsync<FacturaDetalleDto>(anulacion)).Estado);
        await VerificarErrorAsync(await _cliente.PostAsync($"{RutaNotas}/{nota.Id}/emitir", null),
            HttpStatusCode.Conflict, EstadoFacturaInvalido);
        Assert.Equal("Borrador", (await ConsultarNotaAsync(nota.Id)).Estado);
    }

    [Fact]
    public async Task ListadoFiltraPorFacturaTipoYEstadoConPaginacionEstable()
    {
        FacturaDetalleDto primera = await CrearFacturaAsync(1);
        FacturaDetalleDto segunda = await CrearFacturaAsync(2);
        NotaCreditoDebitoDetalleDto antigua = await CrearNotaAsync(Solicitud(primera));
        NotaCreditoDebitoDetalleDto nueva = await CrearNotaAsync(Solicitud(primera));
        await EmitirNotaAsync(nueva.Id);
        await CrearNotaAsync(Solicitud(primera, "Debito"));
        await CrearNotaAsync(Solicitud(segunda));
        using HttpResponseMessage consulta = await _cliente.GetAsync(
            $"{RutaNotas}?facturaId={primera.Id}&tipo=credito&tamanoPagina=1");
        PaginaNotasCreditoDebitoDto pagina = await LeerAsync<PaginaNotasCreditoDebitoDto>(consulta);
        Assert.Equal(2, pagina.TotalRegistros);
        Assert.Equal(2, pagina.TotalPaginas);
        Assert.Equal(nueva.Id, Assert.Single(pagina.Items).Id);
        using HttpResponseMessage consultaBorrador = await _cliente.GetAsync(
            $"{RutaNotas}?facturaId={primera.Id}&tipo=Credito&estado=borrador");
        PaginaNotasCreditoDebitoDto borradores = await LeerAsync<PaginaNotasCreditoDebitoDto>(consultaBorrador);
        Assert.Equal(antigua.Id, Assert.Single(borradores.Items).Id);
    }

    [Fact]
    public async Task NumeracionSeGeneraIndependienteYEntreFacturas()
    {
        FacturaDetalleDto primera = await CrearFacturaAsync(1);
        FacturaDetalleDto segunda = await CrearFacturaAsync(2);
        Assert.Equal("NC-000001", (await CrearNotaAsync(Solicitud(primera))).Numero);
        Assert.Equal("ND-000001", (await CrearNotaAsync(Solicitud(primera, "Debito"))).Numero);
        Assert.Equal("NC-000002", (await CrearNotaAsync(Solicitud(segunda))).Numero);
        Assert.Equal("ND-000002", (await CrearNotaAsync(Solicitud(segunda, "Debito"))).Numero);
    }

    [Fact]
    public async Task ExistenciaYPertenenciaSeCompruebanAntesDelEstado()
    {
        FacturaDetalleDto borrador = await CrearFacturaAsync(1, emitir: false);
        FacturaDetalleDto otra = await CrearFacturaAsync(2);
        CrearNotaCreditoDebitoRequestDto solicitud = Solicitud(borrador);
        solicitud.FacturaId = int.MaxValue;
        await VerificarErrorAsync(await _cliente.PostAsJsonAsync(RutaNotas, solicitud),
            HttpStatusCode.NotFound, "La factura de origen no existe.");
        solicitud.FacturaId = borrador.Id;
        solicitud.Detalles = new() { Linea(int.MaxValue, 1m, 5m) };
        await VerificarErrorAsync(await _cliente.PostAsJsonAsync(RutaNotas, solicitud),
            HttpStatusCode.NotFound, "El detalle de factura indicado no existe.");
        solicitud.Detalles = Solicitud(otra).Detalles;
        await VerificarErrorAsync(await _cliente.PostAsJsonAsync(RutaNotas, solicitud),
            HttpStatusCode.BadRequest, "El detalle indicado no pertenece a la factura de la nota.");
        await VerificarErrorAsync(await _cliente.PostAsJsonAsync(RutaNotas, Solicitud(borrador)),
            HttpStatusCode.Conflict, EstadoFacturaInvalido);
    }

    [Theory]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, "FK_NotasCreditoDebito_Facturas_FacturaId",
        "No se pudo guardar la nota porque una referencia ya no existe.")]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, "FK_DetallesNotaCreditoDebito_DetallesFactura_DetalleFacturaId",
        "No se pudo guardar la nota porque una referencia ya no existe.")]
    [InlineData(PostgresErrorCodes.UniqueViolation, "IX_NotasCreditoDebito_Numero", "Ya existe una nota con ese número.")]
    public async Task ViolacionDePersistenciaDevuelveConflictoSinDejarNota(string estadoSql, string restriccion, string mensaje)
    {
        using FabricaNotas fabrica = new FabricaNotas(new InterceptorNota(estadoSql, restriccion));
        using HttpClient cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        FacturaDetalleDto factura = await CrearFacturaAsync(1, cliente: cliente);
        await VerificarErrorAsync(await cliente.PostAsJsonAsync(RutaNotas, Solicitud(factura)), HttpStatusCode.Conflict, mensaje);
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Assert.Empty(contexto.NotasCreditoDebito);
        Assert.Empty(contexto.DetallesNotaCreditoDebito);
        using HttpResponseMessage consulta = await cliente.GetAsync($"{RutaFacturas}/{factura.Id}");
        VerificarImportes(factura, await LeerAsync<FacturaDetalleDto>(consulta));
    }

    [Fact]
    public async Task DevuelveMismoLiteralParaNotaInexistenteEnDetalleYEscrituras()
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1);
        string ruta = $"{RutaNotas}/{int.MaxValue}";
        await VerificarErrorAsync(await _cliente.GetAsync(ruta), HttpStatusCode.NotFound, NotaInexistente);
        await VerificarErrorAsync(await _cliente.PutAsJsonAsync(ruta, Cambio(factura, 1m, 1m)),
            HttpStatusCode.NotFound, NotaInexistente);
        await VerificarErrorAsync(await _cliente.PostAsync($"{ruta}/emitir", null), HttpStatusCode.NotFound, NotaInexistente);
        await VerificarErrorAsync(await _cliente.PostAsJsonAsync($"{ruta}/anular", new { motivo = "No existe" }),
            HttpStatusCode.NotFound, NotaInexistente);
    }

    [Theory]
    [InlineData("factura", "Debe indicar una factura válida.")]
    [InlineData("tipo", "El tipo de nota debe ser Credito o Debito.")]
    [InlineData("motivo", MotivoInvalido)]
    [InlineData("motivo-corto", MotivoInvalido)]
    [InlineData("motivo-largo", MotivoInvalido)]
    [InlineData("vacio", DetalleInvalido)]
    [InlineData("nulo", DetalleInvalido)]
    [InlineData("duplicado", DetalleInvalido)]
    [InlineData("linea", DetalleInvalido)]
    [InlineData("cantidad", ImporteInvalido)]
    [InlineData("cantidad-escala", ImporteInvalido)]
    [InlineData("cantidad-maximo", ImporteInvalido)]
    [InlineData("precio", ImporteInvalido)]
    [InlineData("precio-escala", ImporteInvalido)]
    [InlineData("precio-maximo", ImporteInvalido)]
    [InlineData("producto-maximo", ImporteInvalido)]
    public async Task FormaInvalidaDevuelveLiteralAntesDeGuardar(string campo, string mensaje)
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1);
        CrearNotaCreditoDebitoRequestDto solicitud = Solicitud(factura);
        List<DetalleNotaCreditoDebitoRequestDto?> detalles = Assert.IsType<List<DetalleNotaCreditoDebitoRequestDto?>>(solicitud.Detalles);
        DetalleNotaCreditoDebitoRequestDto linea = Assert.IsType<DetalleNotaCreditoDebitoRequestDto>(Assert.Single(detalles));
        switch (campo)
        {
            case "factura": solicitud.FacturaId = 0; break;
            case "tipo": solicitud.Tipo = "Otro"; break;
            case "motivo": solicitud.Motivo = null; break;
            case "motivo-corto": solicitud.Motivo = " ab "; break;
            case "motivo-largo": solicitud.Motivo = new string('x', 301); break;
            case "vacio": detalles.Clear(); break;
            case "nulo": detalles[0] = null; break;
            case "duplicado": detalles.Add(linea); break;
            case "linea": linea.DetalleFacturaId = 0; break;
            case "cantidad": linea.Cantidad = 0; break;
            case "cantidad-escala": linea.Cantidad = 0.12345m; break;
            case "cantidad-maximo": linea.Cantidad = 100000000000000m; break;
            case "precio": linea.PrecioUnitario = -1; break;
            case "precio-escala": linea.PrecioUnitario = 3.456m; break;
            case "precio-maximo": linea.PrecioUnitario = 10000000000000000m; break;
            case "producto-maximo":
                linea.Cantidad = 99999999999999.9999m;
                linea.PrecioUnitario = 9999999999999999.99m;
                break;
        }

        await VerificarErrorAsync(await _cliente.PostAsJsonAsync(RutaNotas, solicitud), HttpStatusCode.BadRequest, mensaje);
        using HttpResponseMessage consulta = await _cliente.GetAsync($"{RutaNotas}?facturaId={factura.Id}");
        Assert.Empty((await LeerAsync<PaginaNotasCreditoDebitoDto>(consulta)).Items);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public async Task AnulacionValidaMotivoAntesDeCambiarEstado(string motivo)
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1);
        NotaCreditoDebitoDetalleDto nota = await CrearNotaAsync(Solicitud(factura));
        await EmitirNotaAsync(nota.Id);
        await VerificarErrorAsync(await _cliente.PostAsJsonAsync($"{RutaNotas}/{nota.Id}/anular", new { motivo }),
            HttpStatusCode.BadRequest, MotivoInvalido);
        Assert.Equal("Emitida", (await ConsultarNotaAsync(nota.Id)).Estado);
    }

    [Theory]
    [InlineData("pagina=0", FiltroInvalido)]
    [InlineData("tamanoPagina=101", FiltroInvalido)]
    [InlineData("pagina=2147483647&tamanoPagina=100", FiltroInvalido)]
    [InlineData("tipo=otro", FiltroInvalido)]
    [InlineData("estado=otro", FiltroInvalido)]
    [InlineData("facturaId=0", "Debe indicar una factura válida.")]
    public async Task ListadoRechazaFiltrosYPaginacionInvalidos(string consulta, string mensaje)
    {
        await VerificarErrorAsync(await _cliente.GetAsync($"{RutaNotas}?{consulta}"), HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task ConsultasYCambiosExigenJwt()
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1);
        using HttpClient anonimo = _fabrica.CreateClient();
        anonimo.DefaultRequestHeaders.Authorization = null;
        const string mensaje = "Debe iniciar sesión para acceder a este recurso.";
        await VerificarErrorAsync(await anonimo.GetAsync(RutaNotas), HttpStatusCode.Unauthorized, mensaje);
        await VerificarErrorAsync(await anonimo.PostAsJsonAsync(RutaNotas, Solicitud(factura)),
            HttpStatusCode.Unauthorized, mensaje);
    }

    [Fact]
    public async Task FinanzasEscribeYConsultaSoloLee()
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1);
        using HttpClient finanzas = _fabrica.CreateClient();
        finanzas.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(_fabrica.Services, "FINANZAS"));
        NotaCreditoDebitoDetalleDto nota = await CrearNotaAsync(Solicitud(factura), finanzas);

        using HttpClient consulta = _fabrica.CreateClient();
        consulta.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TokenPruebas.Emitir(_fabrica.Services, "CONSULTA"));
        using HttpResponseMessage leer = await consulta.GetAsync($"{RutaNotas}/{nota.Id}");
        Assert.Equal(HttpStatusCode.OK, leer.StatusCode);
        using HttpResponseMessage listado = await consulta.GetAsync(RutaNotas);
        Assert.Equal(HttpStatusCode.OK, listado.StatusCode);
        await VerificarErrorAsync(await consulta.PostAsJsonAsync(RutaNotas, Solicitud(factura)),
            HttpStatusCode.Forbidden, SinPermiso);
        await VerificarErrorAsync(await consulta.PutAsJsonAsync($"{RutaNotas}/{nota.Id}", Cambio(factura, 1m, 5m)),
            HttpStatusCode.Forbidden, SinPermiso);
        await VerificarErrorAsync(await consulta.PostAsync($"{RutaNotas}/{nota.Id}/emitir", null),
            HttpStatusCode.Forbidden, SinPermiso);
        await VerificarErrorAsync(await consulta.PostAsJsonAsync($"{RutaNotas}/{nota.Id}/anular", new { motivo = "Anulación" }),
            HttpStatusCode.Forbidden, SinPermiso);
        using HttpResponseMessage editada = await finanzas.PutAsJsonAsync($"{RutaNotas}/{nota.Id}", Cambio(factura, 1m, 5m));
        Assert.Equal(HttpStatusCode.OK, editada.StatusCode);
        Assert.Equal("Emitida", (await EmitirNotaAsync(nota.Id, finanzas)).Estado);
        Assert.Equal("Anulada", (await AnularNotaAsync(nota.Id, finanzas)).Estado);
    }

    [Fact]
    public async Task CalculaPorLineaConTasaHistoricaYSinAceptarTotalesDelCliente()
    {
        using FabricaNotas fabrica = new FabricaNotas();
        using HttpClient cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Impuesto impuesto = new Impuesto
        {
            Codigo = "IGV",
            Nombre = "IGV",
            Tipo = "Porcentaje",
            Pais = "Perú",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        TasaImpuesto tasa = new TasaImpuesto
        {
            Impuesto = impuesto,
            Porcentaje = 18m,
            Activo = true,
            FechaInicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        contexto.TasasImpuestos.Add(tasa);
        await contexto.SaveChangesAsync();

        FacturaDetalleDto factura = await CrearFacturaAsync(1, moneda: "USD", cliente: cliente, impuestoId: impuesto.Id, lineas: 2);
        tasa.Porcentaje = 25m;
        await contexto.SaveChangesAsync();

        using HttpResponseMessage alta = await cliente.PostAsJsonAsync(RutaNotas, new
        {
            facturaId = factura.Id,
            tipo = "Credito",
            motivo = "Cálculo histórico",
            numero = "NC-999999",
            moneda = "PEN",
            total = 0.01m,
            detalles = new[]
            {
                new { detalleFacturaId = factura.Detalles[0].Id, cantidad = 1.0005m, precioUnitario = 10m, porcentajeImpuesto = 0, total = 99m },
                new { detalleFacturaId = factura.Detalles[1].Id, cantidad = 1m, precioUnitario = 12.25m, porcentajeImpuesto = 0, total = 99m }
            }
        });
        NotaCreditoDebitoDetalleDto nota = await LeerAsync<NotaCreditoDebitoDetalleDto>(alta, HttpStatusCode.Created);
        Assert.Equal("NC-000001", nota.Numero);
        Assert.Equal("USD", nota.Moneda);
        Assert.Equal(1.0005m, nota.Detalles[0].Cantidad);
        Assert.Equal(10.01m, nota.Detalles[0].BaseImponible);
        Assert.Equal(2.21m, nota.Detalles[1].ImporteImpuesto);
        Assert.All(nota.Detalles, detalle => Assert.Equal(18m, detalle.PorcentajeImpuesto));
        Assert.Equal(22.26m, nota.BaseImponible);
        Assert.Equal(4.01m, nota.ImporteImpuesto);
        Assert.Equal(26.27m, nota.Total);
        using HttpResponseMessage actual = await cliente.GetAsync($"{RutaFacturas}/{factura.Id}");
        VerificarImportes(factura, await LeerAsync<FacturaDetalleDto>(actual));
    }

    [Fact]
    public async Task CreditosSoloAcumulanEmitidasYAnularLiberaCantidad()
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1);
        NotaCreditoDebitoDetalleDto primera = await CrearNotaAsync(Solicitud(factura, cantidad: 1.5m, precio: 20m));
        NotaCreditoDebitoDetalleDto segunda = await CrearNotaAsync(Solicitud(factura, cantidad: 0.6m, precio: 20m));
        await EmitirNotaAsync(primera.Id);
        await VerificarErrorAsync(await _cliente.PostAsync($"{RutaNotas}/{segunda.Id}/emitir", null),
            HttpStatusCode.Conflict, "El crédito supera la cantidad disponible de la línea de factura.");
        Assert.Equal("Borrador", (await ConsultarNotaAsync(segunda.Id)).Estado);
        await AnularNotaAsync(primera.Id);
        Assert.Equal("Emitida", (await EmitirNotaAsync(segunda.Id)).Estado);
    }

    [Fact]
    public async Task CreditoUsaTotalFacturadoPorLineaYDebitoNoTieneTope()
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1, lineas: 2);
        Assert.Equal(90m, factura.Detalles[0].Total);
        NotaCreditoDebitoDetalleDto primera = await CrearNotaAsync(Solicitud(factura, cantidad: 1m, precio: 60m));
        await EmitirNotaAsync(primera.Id);
        NotaCreditoDebitoDetalleDto segunda = await CrearNotaAsync(Solicitud(factura, cantidad: 0.5m, precio: 62m));
        await VerificarErrorAsync(await _cliente.PostAsync($"{RutaNotas}/{segunda.Id}/emitir", null),
            HttpStatusCode.Conflict, "El crédito supera el importe disponible de la línea de factura.");
        using HttpResponseMessage corregir = await _cliente.PutAsJsonAsync($"{RutaNotas}/{segunda.Id}", Cambio(factura, 1m, 30m));
        Assert.Equal(HttpStatusCode.OK, corregir.StatusCode);
        Assert.Equal(30m, (await EmitirNotaAsync(segunda.Id)).Total);
        NotaCreditoDebitoDetalleDto debito = await CrearNotaAsync(Solicitud(factura, "Debito", 3m, 100m));
        Assert.Equal(300m, (await EmitirNotaAsync(debito.Id)).Total);
    }

    [Fact]
    public async Task EditarBorradorReemplazaLasLineasSinCambiarFolioNiOrigen()
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1, lineas: 2);
        NotaCreditoDebitoDetalleDto nota = await CrearNotaAsync(Solicitud(factura));
        ActualizarNotaCreditoDebitoRequestDto cambio = new ActualizarNotaCreditoDebitoRequestDto
        {
            Motivo = new string('x', 300),
            Detalles = new() { Linea(factura.Detalles[1].Id, 0.3333m, 40m) }
        };
        using HttpResponseMessage respuesta = await _cliente.PutAsJsonAsync($"{RutaNotas}/{nota.Id}", cambio);
        NotaCreditoDebitoDetalleDto actual = await LeerAsync<NotaCreditoDebitoDetalleDto>(respuesta);
        Assert.Equal(nota.Numero, actual.Numero);
        Assert.Equal(nota.FacturaId, actual.FacturaId);
        Assert.Equal(nota.Tipo, actual.Tipo);
        Assert.Equal(factura.Detalles[1].Id, Assert.Single(actual.Detalles).DetalleFacturaId);
        Assert.Equal(13.33m, actual.Total);
    }

    private async Task<FacturaDetalleDto> CrearFacturaAsync(
        int numero, string tipoComprobante = "Factura", string moneda = "PEN", bool emitir = true,
        HttpClient? cliente = null, int? impuestoId = null, int lineas = 1)
    {
        HttpClient clienteApi = cliente ?? _cliente;
        GuardarFacturaRequestDto solicitud = new GuardarFacturaRequestDto
        {
            ClienteId = 1,
            TipoComprobante = tipoComprobante,
            Serie = SerieFacturas,
            Numero = numero,
            Moneda = moneda,
            FechaEmision = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            Detalles = Enumerable.Range(1, lineas).Select(productoId => new GuardarDetalleFacturaRequestDto
            {
                ProductoId = productoId,
                Descripcion = "Producto de prueba",
                Cantidad = 2m,
                PrecioUnitario = 50m,
                DescuentoPorcentaje = 10m,
                ImpuestoId = impuestoId
            }).ToList()
        };

        using HttpResponseMessage alta = await clienteApi.PostAsJsonAsync(RutaFacturas, solicitud);
        FacturaDetalleDto factura = await LeerAsync<FacturaDetalleDto>(alta, HttpStatusCode.Created);
        if (!emitir)
        {
            return factura;
        }

        using HttpResponseMessage emision = await clienteApi.PostAsync($"{RutaFacturas}/{factura.Id}/emitir", null);
        return await LeerAsync<FacturaDetalleDto>(emision);
    }

    private async Task<NotaCreditoDebitoDetalleDto> CrearNotaAsync(CrearNotaCreditoDebitoRequestDto solicitud, HttpClient? cliente = null)
    {
        using HttpResponseMessage respuesta = await (cliente ?? _cliente).PostAsJsonAsync(RutaNotas, solicitud);
        return await LeerAsync<NotaCreditoDebitoDetalleDto>(respuesta, HttpStatusCode.Created);
    }

    private async Task<NotaCreditoDebitoDetalleDto> EmitirNotaAsync(int id, HttpClient? cliente = null)
    {
        using HttpResponseMessage respuesta = await (cliente ?? _cliente).PostAsync($"{RutaNotas}/{id}/emitir", null);
        return await LeerAsync<NotaCreditoDebitoDetalleDto>(respuesta);
    }

    private async Task<NotaCreditoDebitoDetalleDto> AnularNotaAsync(int id, HttpClient? cliente = null)
    {
        using HttpResponseMessage respuesta = await (cliente ?? _cliente).PostAsJsonAsync($"{RutaNotas}/{id}/anular",
            new { motivo = " Rectificación interna " });
        return await LeerAsync<NotaCreditoDebitoDetalleDto>(respuesta);
    }

    private async Task<NotaCreditoDebitoDetalleDto> ConsultarNotaAsync(int id)
    {
        using HttpResponseMessage consulta = await _cliente.GetAsync($"{RutaNotas}/{id}");
        return await LeerAsync<NotaCreditoDebitoDetalleDto>(consulta);
    }

    private static CrearNotaCreditoDebitoRequestDto Solicitud(FacturaDetalleDto factura,
        string tipo = "Credito", decimal cantidad = 0.25m, decimal precio = 21m)
    {
        return new CrearNotaCreditoDebitoRequestDto
        {
            FacturaId = factura.Id,
            Tipo = tipo,
            Motivo = "Ajuste informado",
            Detalles = new() { Linea(factura.Detalles[0].Id, cantidad, precio) }
        };
    }

    private static ActualizarNotaCreditoDebitoRequestDto Cambio(FacturaDetalleDto factura, decimal cantidad, decimal precio)
    {
        return new ActualizarNotaCreditoDebitoRequestDto
        {
            Motivo = "Ajuste corregido",
            Detalles = new() { Linea(factura.Detalles[0].Id, cantidad, precio) }
        };
    }

    private static DetalleNotaCreditoDebitoRequestDto Linea(int id, decimal cantidad, decimal precio)
    {
        return new DetalleNotaCreditoDebitoRequestDto { DetalleFacturaId = id, Cantidad = cantidad, PrecioUnitario = precio };
    }

    private static void VerificarImportes(FacturaDetalleDto original, FacturaDetalleDto actual)
    {
        Assert.Equal(original.Subtotal, actual.Subtotal);
        Assert.Equal(original.TotalDescuento, actual.TotalDescuento);
        Assert.Equal(original.TotalImpuesto, actual.TotalImpuesto);
        Assert.Equal(original.Total, actual.Total);
        Assert.Equal(original.Detalles.Select(detalle => detalle.Total), actual.Detalles.Select(detalle => detalle.Total));
    }

    private static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta, HttpStatusCode estado = HttpStatusCode.OK)
        where T : class
    {
        Assert.Equal(estado, respuesta.StatusCode);
        T? contenido = await respuesta.Content.ReadFromJsonAsync<T>();
        return Assert.IsType<T>(contenido);
    }

    private static async Task VerificarErrorAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string mensaje)
    {
        using (respuesta)
        {
            Assert.Equal(estado, respuesta.StatusCode);
            using JsonDocument json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            Assert.Equal((int)estado, json.RootElement.GetProperty("StatusCode").GetInt32());
            Assert.Equal(mensaje, json.RootElement.GetProperty("Message").GetString());
            Assert.False(json.RootElement.TryGetProperty("Detail", out _));
        }
    }

    private static void RestablecerDatos(OrdenesCompraApiFactory fabrica)
    {
        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        List<Factura> facturas = contexto.Facturas.Where(factura => factura.Serie == SerieFacturas).ToList();
        int[] ids = facturas.Select(factura => factura.Id).ToArray();
        List<NotaCreditoDebito> notas = contexto.NotasCreditoDebito.Where(nota => ids.Contains(nota.FacturaId)).ToList();
        int[] idsNotas = notas.Select(nota => nota.Id).ToArray();
        contexto.DetallesNotaCreditoDebito.RemoveRange(
            contexto.DetallesNotaCreditoDebito.Where(detalle => idsNotas.Contains(detalle.NotaCreditoDebitoId)));
        contexto.NotasCreditoDebito.RemoveRange(notas);
        contexto.DetallesFactura.RemoveRange(contexto.DetallesFactura.Where(detalle => ids.Contains(detalle.FacturaId)));
        contexto.Facturas.RemoveRange(facturas);
        contexto.SaveChanges();
    }

    private sealed class FabricaNotas : OrdenesCompraApiFactory
    {
        private readonly string _base = $"Modulo52-{Guid.NewGuid():N}";
        private readonly SaveChangesInterceptor? _interceptor;

        public FabricaNotas(SaveChangesInterceptor? interceptor = null)
        {
            _interceptor = interceptor;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(servicios =>
            {
                servicios.AddDbContext<TitanGlobalDbContext>(opciones =>
                {
                    opciones.UseInMemoryDatabase(_base);
                    if (_interceptor is not null)
                    {
                        opciones.AddInterceptors(_interceptor);
                    }
                });
            });
        }
    }

    private sealed class InterceptorNota : SaveChangesInterceptor
    {
        private readonly string _estado;
        private readonly string _restriccion;

        public InterceptorNota(string estado, string restriccion)
        {
            _estado = estado;
            _restriccion = restriccion;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            DbContext? contexto = eventData.Context;
            if (contexto != null && contexto.ChangeTracker.Entries<NotaCreditoDebito>().Any(entrada => entrada.State == EntityState.Added))
            {
                PostgresException postgres = new PostgresException(
                    "Restricción de nota.", "ERROR", "ERROR", _estado, constraintName: _restriccion);
                throw new DbUpdateException("Error al insertar la nota.", postgres);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
