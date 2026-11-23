using System.Net;
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
    private const string NumeroDuplicado = "Ya existe una nota con la misma serie y número.";
    private readonly HttpClient _cliente;

    public NotasCreditoDebitoApiTests(OrdenesCompraApiFactory fabrica)
    {
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerDatos(fabrica);
    }

    [Theory]
    [InlineData("Credito", "Factura", "PEN")]
    [InlineData("Debito", "Boleta", "USD")]
    public async Task AltaConsultaEdicionYCicloConservanLosImportesDelComprobante(
        string tipo, string tipoComprobante, string moneda)
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1, tipoComprobante, moneda);
        GuardarNotaCreditoDebitoRequestDto solicitud = Solicitud(factura.Id, tipo: tipo);
        solicitud.Serie = " nc52 ";
        solicitud.Motivo = " Ajuste informado ";
        using HttpResponseMessage alta = await _cliente.PostAsJsonAsync(RutaNotas, solicitud);
        NotaCreditoDebitoDetalleDto borrador = await LeerAsync<NotaCreditoDebitoDetalleDto>(alta, HttpStatusCode.Created);
        Assert.Equal($"{RutaNotas}/{borrador.Id}", alta.Headers.Location?.AbsolutePath);
        Assert.Equal(factura.Id, borrador.FacturaId);
        Assert.Equal(factura.ClienteId, borrador.ClienteId);
        Assert.Equal(tipoComprobante, borrador.FacturaTipoComprobante);
        Assert.Equal(SerieFacturas, borrador.FacturaSerie);
        Assert.Equal(1, borrador.FacturaNumero);
        Assert.Equal(tipo, borrador.Tipo);
        Assert.Equal("NC52", borrador.Serie);
        Assert.Equal(moneda, borrador.Moneda);
        Assert.Equal("Ajuste informado", borrador.Motivo);
        Assert.Equal(5.25m, borrador.Total);
        Assert.Equal("Borrador", borrador.Estado);
        Assert.Equal(DateTimeKind.Utc, borrador.FechaCreacion.Kind);
        Assert.Null(borrador.FechaEmision);

        using HttpResponseMessage consulta = await _cliente.GetAsync($"{RutaNotas}/{borrador.Id}");
        Assert.Equal(borrador.Id, (await LeerAsync<NotaCreditoDebitoDetalleDto>(consulta)).Id);

        solicitud.Importe = 8.50m;
        solicitud.Motivo = "Ajuste editado";
        using HttpResponseMessage edicion = await _cliente.PutAsJsonAsync($"{RutaNotas}/{borrador.Id}", solicitud);
        NotaCreditoDebitoDetalleDto editada = await LeerAsync<NotaCreditoDebitoDetalleDto>(edicion);
        Assert.Equal(8.50m, editada.Total);
        Assert.Equal("Ajuste editado", editada.Motivo);
        Assert.Equal(borrador.FechaCreacion, editada.FechaCreacion);

        using HttpResponseMessage emision = await _cliente.PostAsync($"{RutaNotas}/{borrador.Id}/emitir", null);
        NotaCreditoDebitoDetalleDto emitida = await LeerAsync<NotaCreditoDebitoDetalleDto>(emision);
        Assert.Equal("Emitida", emitida.Estado);
        Assert.Equal(DateTimeKind.Utc, emitida.FechaEmision?.Kind);
        Assert.Equal(8.50m, emitida.Total);
        await VerificarErrorAsync(
            await _cliente.PutAsJsonAsync($"{RutaNotas}/{borrador.Id}", solicitud),
            HttpStatusCode.Conflict, EstadoNotaInvalido);
        await VerificarErrorAsync(
            await _cliente.PostAsync($"{RutaNotas}/{borrador.Id}/emitir", null),
            HttpStatusCode.Conflict, EstadoNotaInvalido);

        await VerificarErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaFacturas}/{factura.Id}/anular", new { motivo = "Anulación solicitada" }),
            HttpStatusCode.Conflict, "No se puede anular un comprobante con notas emitidas.");
        using HttpResponseMessage facturaProtegida = await _cliente.GetAsync($"{RutaFacturas}/{factura.Id}");
        FacturaDetalleDto despuesDeRechazo = await LeerAsync<FacturaDetalleDto>(facturaProtegida);
        Assert.Equal("Emitida", despuesDeRechazo.Estado);
        VerificarImportes(factura, despuesDeRechazo);

        using HttpResponseMessage anulacion = await _cliente.PostAsJsonAsync(
            $"{RutaNotas}/{borrador.Id}/anular", new { motivo = " Rectificación interna " });
        NotaCreditoDebitoDetalleDto anulada = await LeerAsync<NotaCreditoDebitoDetalleDto>(anulacion);
        Assert.Equal("Anulada", anulada.Estado);
        Assert.Equal(emitida.FechaEmision, anulada.FechaEmision);
        Assert.Equal(DateTimeKind.Utc, anulada.FechaAnulacion?.Kind);
        Assert.Equal("Rectificación interna", anulada.MotivoAnulacion);
        await VerificarErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaNotas}/{borrador.Id}/anular", new { motivo = "Reintento" }),
            HttpStatusCode.Conflict, EstadoNotaInvalido);

        using HttpResponseMessage anularFactura = await _cliente.PostAsJsonAsync(
            $"{RutaFacturas}/{factura.Id}/anular", new { motivo = "Comprobante rectificado" });
        FacturaDetalleDto anuladaFactura = await LeerAsync<FacturaDetalleDto>(anularFactura);
        Assert.Equal("Anulada", anuladaFactura.Estado);
        VerificarImportes(factura, anuladaFactura);
    }

    [Fact]
    public async Task BorradorNoBloqueaAnularFacturaYNoPuedeEmitirseDespues()
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1);
        NotaCreditoDebitoDetalleDto nota = await CrearNotaAsync(Solicitud(factura.Id));
        await VerificarErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaNotas}/{nota.Id}/anular", new { motivo = "Borrador" }),
            HttpStatusCode.Conflict, EstadoNotaInvalido);

        using HttpResponseMessage anulacion = await _cliente.PostAsJsonAsync(
            $"{RutaFacturas}/{factura.Id}/anular", new { motivo = "Anulación de origen" });
        Assert.Equal("Anulada", (await LeerAsync<FacturaDetalleDto>(anulacion)).Estado);
        await VerificarErrorAsync(
            await _cliente.PostAsync($"{RutaNotas}/{nota.Id}/emitir", null),
            HttpStatusCode.Conflict, EstadoFacturaInvalido);
        await VerificarErrorAsync(
            await _cliente.PutAsJsonAsync($"{RutaNotas}/{nota.Id}", Solicitud(factura.Id)),
            HttpStatusCode.Conflict, EstadoFacturaInvalido);

        using HttpResponseMessage consulta = await _cliente.GetAsync($"{RutaNotas}/{nota.Id}");
        Assert.Equal("Borrador", (await LeerAsync<NotaCreditoDebitoDetalleDto>(consulta)).Estado);
    }

    [Fact]
    public async Task ListadoFiltraPorFacturaYPaginaPorFechaEIdDescendentes()
    {
        FacturaDetalleDto primera = await CrearFacturaAsync(1);
        FacturaDetalleDto segunda = await CrearFacturaAsync(2);
        NotaCreditoDebitoDetalleDto antigua = await CrearNotaAsync(Solicitud(primera.Id, 1));
        await CrearNotaAsync(Solicitud(segunda.Id, 2));
        NotaCreditoDebitoDetalleDto reciente = await CrearNotaAsync(Solicitud(primera.Id, 3));

        using HttpResponseMessage consulta1 = await _cliente.GetAsync($"{RutaNotas}?facturaId={primera.Id}&tamanoPagina=1");
        PaginaNotasCreditoDebitoDto pagina1 = await LeerAsync<PaginaNotasCreditoDebitoDto>(consulta1);
        Assert.Equal(1, pagina1.Pagina);
        Assert.Equal(1, pagina1.TamanoPagina);
        Assert.Equal(2, pagina1.TotalPaginas);
        Assert.Equal(2, pagina1.TotalRegistros);
        Assert.Equal(reciente.Id, Assert.Single(pagina1.Items).Id);

        using HttpResponseMessage consulta2 = await _cliente.GetAsync($"{RutaNotas}?facturaId={primera.Id}&tamanoPagina=1&pagina=2");
        PaginaNotasCreditoDebitoDto pagina2 = await LeerAsync<PaginaNotasCreditoDebitoDto>(consulta2);
        Assert.Equal(2, pagina2.Pagina);
        Assert.Equal(antigua.Id, Assert.Single(pagina2.Items).Id);
    }

    [Fact]
    public async Task LaNumeracionEsUnicaPorTipoSerieYNumeroYTieneAlcanceEntreFacturas()
    {
        FacturaDetalleDto primera = await CrearFacturaAsync(1);
        FacturaDetalleDto segunda = await CrearFacturaAsync(2);
        await CrearNotaAsync(Solicitud(primera.Id));
        await VerificarErrorAsync(
            await _cliente.PostAsJsonAsync(RutaNotas, Solicitud(segunda.Id)),
            HttpStatusCode.Conflict, NumeroDuplicado);
        NotaCreditoDebitoDetalleDto debito = await CrearNotaAsync(Solicitud(segunda.Id, tipo: "Debito"));
        Assert.Equal("Debito", debito.Tipo);

        NotaCreditoDebitoDetalleDto otroNumero = await CrearNotaAsync(Solicitud(segunda.Id, 2));
        await VerificarErrorAsync(
            await _cliente.PutAsJsonAsync($"{RutaNotas}/{otroNumero.Id}", Solicitud(segunda.Id)),
            HttpStatusCode.Conflict, NumeroDuplicado);
        await VerificarErrorAsync(
            await _cliente.PutAsJsonAsync($"{RutaNotas}/{otroNumero.Id}", Solicitud(primera.Id, 3)),
            HttpStatusCode.BadRequest, "Debe indicar una factura válida.");
        using HttpResponseMessage consulta = await _cliente.GetAsync($"{RutaNotas}/{otroNumero.Id}");
        NotaCreditoDebitoDetalleDto conservada = await LeerAsync<NotaCreditoDebitoDetalleDto>(consulta);
        Assert.Equal(2, conservada.Numero);
        Assert.Equal(segunda.Id, conservada.FacturaId);
    }

    [Fact]
    public async Task RechazaFacturaInexistenteYComprobanteEnBorrador()
    {
        FacturaDetalleDto borrador = await CrearFacturaAsync(1, emitir: false);
        await VerificarErrorAsync(
            await _cliente.PostAsJsonAsync(RutaNotas, Solicitud(int.MaxValue)),
            HttpStatusCode.NotFound, "La factura de origen no existe.");
        await VerificarErrorAsync(
            await _cliente.PostAsJsonAsync(RutaNotas, Solicitud(borrador.Id)),
            HttpStatusCode.Conflict, EstadoFacturaInvalido);
        using HttpResponseMessage consulta = await _cliente.GetAsync($"{RutaNotas}?facturaId={borrador.Id}");
        Assert.Empty((await LeerAsync<PaginaNotasCreditoDebitoDto>(consulta)).Items);
    }

    [Fact]
    public async Task ViolacionClaveFacturaAlGuardarDevuelveConflictoSinDejarNota()
    {
        using FabricaConFalloClaveFactura fabrica = new FabricaConFalloClaveFactura();
        using HttpClient cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        FacturaDetalleDto factura = await CrearFacturaAsync(1, cliente: cliente);

        await VerificarErrorAsync(
            await cliente.PostAsJsonAsync(RutaNotas, Solicitud(factura.Id)),
            HttpStatusCode.Conflict, "No se pudo guardar la nota porque una referencia ya no existe.");

        using IServiceScope alcance = fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = alcance.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Assert.Empty(await contexto.NotasCreditoDebito.ToListAsync());

        using HttpResponseMessage consulta = await cliente.GetAsync($"{RutaFacturas}/{factura.Id}");
        FacturaDetalleDto conservada = await LeerAsync<FacturaDetalleDto>(consulta);
        Assert.Equal("Emitida", conservada.Estado);
        VerificarImportes(factura, conservada);
    }

    [Fact]
    public async Task DevuelveElMismoLiteralParaNotaInexistenteEnDetalleYEscrituras()
    {
        string ruta = $"{RutaNotas}/{int.MaxValue}";
        await VerificarErrorAsync(await _cliente.GetAsync(ruta), HttpStatusCode.NotFound, NotaInexistente);
        await VerificarErrorAsync(
            await _cliente.PutAsJsonAsync(ruta, Solicitud(1)), HttpStatusCode.NotFound, NotaInexistente);
        await VerificarErrorAsync(
            await _cliente.PostAsync($"{ruta}/emitir", null), HttpStatusCode.NotFound, NotaInexistente);
        await VerificarErrorAsync(
            await _cliente.PostAsJsonAsync($"{ruta}/anular", new { motivo = "No existe" }),
            HttpStatusCode.NotFound, NotaInexistente);
    }

    [Theory]
    [InlineData("factura", "Debe indicar una factura válida.")]
    [InlineData("tipo", "El tipo de nota debe ser Credito o Debito.")]
    [InlineData("motivo", "El motivo de la nota es obligatorio.")]
    [InlineData("motivo-largo", "El motivo de la nota supera la longitud permitida.")]
    [InlineData("serie", "La serie o el número de la nota no son válidos.")]
    [InlineData("serie-larga", "La serie o el número de la nota no son válidos.")]
    [InlineData("numero", "La serie o el número de la nota no son válidos.")]
    [InlineData("importe", "El importe del ajuste no es válido.")]
    [InlineData("importe-escala", "El importe del ajuste no es válido.")]
    [InlineData("importe-maximo", "El importe del ajuste no es válido.")]
    public async Task AltaInvalidaDevuelveElLiteralSinGuardar(string campo, string mensaje)
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1);
        GuardarNotaCreditoDebitoRequestDto solicitud = Solicitud(factura.Id);
        switch (campo)
        {
            case "factura":
                solicitud.FacturaId = 0;
                break;
            case "tipo":
                solicitud.Tipo = "Otro";
                break;
            case "motivo":
                solicitud.Motivo = null;
                break;
            case "motivo-largo":
                solicitud.Motivo = new string('a', 501);
                break;
            case "serie":
                solicitud.Serie = " ";
                break;
            case "serie-larga":
                solicitud.Serie = new string('a', 11);
                break;
            case "numero":
                solicitud.Numero = 0;
                break;
            case "importe":
                solicitud.Importe = 0m;
                break;
            case "importe-escala":
                solicitud.Importe = 3.456m;
                break;
            case "importe-maximo":
                solicitud.Importe = 10000000000000000m;
                break;
        }

        await VerificarErrorAsync(
            await _cliente.PostAsJsonAsync(RutaNotas, solicitud), HttpStatusCode.BadRequest, mensaje);
        using HttpResponseMessage consulta = await _cliente.GetAsync($"{RutaNotas}?facturaId={factura.Id}");
        Assert.Equal(0, (await LeerAsync<PaginaNotasCreditoDebitoDto>(consulta)).TotalRegistros);
    }

    [Fact]
    public async Task AnulacionValidaMotivoAntesDeCambiarElEstado()
    {
        FacturaDetalleDto factura = await CrearFacturaAsync(1);
        NotaCreditoDebitoDetalleDto nota = await CrearNotaAsync(Solicitud(factura.Id));
        using HttpResponseMessage emision = await _cliente.PostAsync($"{RutaNotas}/{nota.Id}/emitir", null);
        Assert.Equal(HttpStatusCode.OK, emision.StatusCode);
        await VerificarErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaNotas}/{nota.Id}/anular", new { motivo = " " }),
            HttpStatusCode.BadRequest, "El motivo de anulación es obligatorio.");
        await VerificarErrorAsync(
            await _cliente.PostAsJsonAsync($"{RutaNotas}/{nota.Id}/anular", new { motivo = new string('a', 501) }),
            HttpStatusCode.BadRequest, "El motivo de la nota supera la longitud permitida.");
        using HttpResponseMessage consulta = await _cliente.GetAsync($"{RutaNotas}/{nota.Id}");
        Assert.Equal("Emitida", (await LeerAsync<NotaCreditoDebitoDetalleDto>(consulta)).Estado);
    }

    [Theory]
    [InlineData("pagina=0", "La paginación solicitada no es válida.")]
    [InlineData("tamanoPagina=101", "La paginación solicitada no es válida.")]
    [InlineData("pagina=2147483647&tamanoPagina=100", "La paginación solicitada no es válida.")]
    [InlineData("facturaId=0", "Debe indicar una factura válida.")]
    public async Task ListadoRechazaParametrosInvalidos(string consulta, string mensaje)
    {
        await VerificarErrorAsync(
            await _cliente.GetAsync($"{RutaNotas}?{consulta}"), HttpStatusCode.BadRequest, mensaje);
    }

    [Fact]
    public async Task ConsultasYCambiosExigenJwt()
    {
        _cliente.DefaultRequestHeaders.Authorization = null;
        const string sinSesion = "Debe iniciar sesión para acceder a este recurso.";
        await VerificarErrorAsync(await _cliente.GetAsync(RutaNotas), HttpStatusCode.Unauthorized, sinSesion);
        await VerificarErrorAsync(
            await _cliente.PostAsJsonAsync(RutaNotas, Solicitud(1)), HttpStatusCode.Unauthorized, sinSesion);
        await VerificarErrorAsync(
            await _cliente.PostAsync($"{RutaNotas}/1/emitir", null), HttpStatusCode.Unauthorized, sinSesion);
    }

    private async Task<FacturaDetalleDto> CrearFacturaAsync(
        int numero, string tipoComprobante = "Factura", string moneda = "PEN", bool emitir = true, HttpClient? cliente = null)
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
            Detalles = new List<GuardarDetalleFacturaRequestDto>
            {
                new GuardarDetalleFacturaRequestDto
                {
                    ProductoId = 1,
                    Descripcion = "Producto de prueba",
                    Cantidad = 2m,
                    PrecioUnitario = 50m,
                    DescuentoPorcentaje = 10m
                }
            }
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

    private async Task<NotaCreditoDebitoDetalleDto> CrearNotaAsync(GuardarNotaCreditoDebitoRequestDto solicitud)
    {
        using HttpResponseMessage respuesta = await _cliente.PostAsJsonAsync(RutaNotas, solicitud);
        return await LeerAsync<NotaCreditoDebitoDetalleDto>(respuesta, HttpStatusCode.Created);
    }

    private static GuardarNotaCreditoDebitoRequestDto Solicitud(int facturaId, int numero = 1, string tipo = "Credito")
    {
        return new GuardarNotaCreditoDebitoRequestDto
        {
            FacturaId = facturaId,
            Tipo = tipo,
            Serie = "NC52",
            Numero = numero,
            Motivo = "Ajuste informado",
            Importe = 5.25m
        };
    }

    private static void VerificarImportes(FacturaDetalleDto original, FacturaDetalleDto actual)
    {
        Assert.Equal(original.Subtotal, actual.Subtotal);
        Assert.Equal(original.TotalDescuento, actual.TotalDescuento);
        Assert.Equal(original.TotalImpuesto, actual.TotalImpuesto);
        Assert.Equal(original.Total, actual.Total);
        Assert.Equal(Assert.Single(original.Detalles).Total, Assert.Single(actual.Detalles).Total);
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
        contexto.NotasCreditoDebito.RemoveRange(contexto.NotasCreditoDebito.Where(nota => ids.Contains(nota.FacturaId)));
        contexto.DetallesFactura.RemoveRange(contexto.DetallesFactura.Where(detalle => ids.Contains(detalle.FacturaId)));
        contexto.Facturas.RemoveRange(facturas);
        contexto.SaveChanges();
    }

    private sealed class FabricaConFalloClaveFactura : OrdenesCompraApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(servicios =>
            {
                servicios.AddDbContext<TitanGlobalDbContext>(opciones => opciones
                    .UseInMemoryDatabase("Modulo52ReferenciaForanea")
                    .AddInterceptors(new ViolacionClaveFacturaInterceptor()));
            });
        }
    }

    private sealed class ViolacionClaveFacturaInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            DbContext? contexto = eventData.Context;
            if (contexto != null && contexto.ChangeTracker.Entries<NotaCreditoDebito>().Any(entrada => entrada.State == EntityState.Added))
            {
                // InMemory no comprueba la FK. Reproducir en SaveChangesAsync el fallo del proveedor para esta inserción.
                PostgresException postgres = new PostgresException(
                    "Violación de la clave foránea de la factura.", "ERROR", "ERROR", PostgresErrorCodes.ForeignKeyViolation,
                    tableName: "NotasCreditoDebito", constraintName: "FK_NotasCreditoDebito_Facturas_FacturaId");
                throw new DbUpdateException("Error al insertar la nota.", postgres);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
