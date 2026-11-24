using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;
using TitanGlobal.Application.DTOs.Modulo48_TransportistasTerceros;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo48_TransportistasTerceros;

[Collection("Modulo04")]
public class TransportistasTercerosApiTests
{
    private readonly HttpClient _cliente;

    public TransportistasTercerosApiTests(OrdenesCompraApiFactory fabrica)
    {
        _cliente = fabrica.CreateClient();
        fabrica.SeedDatabase();
        using IServiceScope scope = fabrica.Services.CreateScope();
        TitanGlobalDbContext db = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        db.FletesTerceros.RemoveRange(db.FletesTerceros);
        db.TransportistasTerceros.RemoveRange(db.TransportistasTerceros);
        db.SaveChanges();
        db.TransportistasTerceros.AddRange(
            new TransportistaTercero { Id = 1, RazonSocial = "Andes", DocumentoFiscal = "DOC-1", Pais = "Perú", TipoServicio = "Ambos", Activo = true },
            new TransportistaTercero { Id = 2, RazonSocial = "Brasil", DocumentoFiscal = "DOC-1", Pais = "Brasil", TipoServicio = "3PL", Activo = true },
            new TransportistaTercero { Id = 3, RazonSocial = "Cerrado", DocumentoFiscal = "DOC-3", Pais = "Perú", TipoServicio = "Flete", Activo = false });
        db.SaveChanges();
    }

    [Theory]
    [InlineData("RazonSocial", "", "La razón social es obligatoria.")]
    [InlineData("DocumentoFiscal", " ", "El documento fiscal es obligatorio.")]
    [InlineData("Pais", "", "El país es obligatorio.")]
    [InlineData("TipoServicio", "Otro", "El tipo de servicio debe ser 3PL, Flete o Ambos.")]
    [InlineData("Email", "invalido", "El email no es válido.")]
    public async Task MaestroValidaLiteralesEnAltaYEdicion(string campo, string valor, string mensaje)
    {
        GuardarTransportistaTerceroRequestDto datos = Maestro();
        typeof(GuardarTransportistaTerceroRequestDto).GetProperty(campo)!.SetValue(datos, valor);
        await Error(await _cliente.PostAsJsonAsync("/api/v1/transportistas-terceros", datos), 400, mensaje);
        await Error(await _cliente.PutAsJsonAsync("/api/v1/transportistas-terceros/1", datos), 400, mensaje);
    }

    [Theory]
    [InlineData("RazonSocial", 200, "La razón social no puede superar 200 caracteres.")]
    [InlineData("DocumentoFiscal", 30, "El documento fiscal no puede superar 30 caracteres.")]
    [InlineData("Pais", 100, "El país no puede superar 100 caracteres.")]
    [InlineData("Contacto", 100, "El contacto no puede superar 100 caracteres.")]
    [InlineData("Email", 150, "El email no puede superar 150 caracteres.")]
    [InlineData("Telefono", 20, "El teléfono no puede superar 20 caracteres.")]
    [InlineData("Observaciones", 500, "Las observaciones no pueden superar 500 caracteres.")]
    public async Task MaestroRechazaLongitudesEnAmbasOperaciones(string campo, int limite, string mensaje)
    {
        GuardarTransportistaTerceroRequestDto datos = Maestro();
        typeof(GuardarTransportistaTerceroRequestDto).GetProperty(campo)!.SetValue(datos, new string('x', limite + 1));
        await Error(await _cliente.PostAsJsonAsync("/api/v1/transportistas-terceros", datos), 400, mensaje);
        await Error(await _cliente.PutAsJsonAsync("/api/v1/transportistas-terceros/1", datos), 400, mensaje);
    }

    [Fact]
    public async Task MaestroNormalizaActualizaYEliminaSinHistorial()
    {
        GuardarTransportistaTerceroRequestDto datos = Maestro();
        datos.RazonSocial = "  Nuevo  "; datos.TipoServicio = " fLeTe "; datos.Contacto = " "; datos.Activo = false;
        int id = await Id(await _cliente.PostAsJsonAsync("/api/v1/transportistas-terceros", datos));
        TransportistaTerceroDetalleDto? detalle = await _cliente.GetFromJsonAsync<TransportistaTerceroDetalleDto>($"/api/v1/transportistas-terceros/{id}");
        Assert.NotNull(detalle); Assert.Equal("Nuevo", detalle.RazonSocial); Assert.Equal("Flete", detalle.TipoServicio); Assert.False(detalle.Activo); Assert.Null(detalle.Contacto);
        datos.Activo = true;
        Assert.Equal(id, await Id(await _cliente.PutAsJsonAsync($"/api/v1/transportistas-terceros/{id}", datos)));
        Assert.Equal(HttpStatusCode.NoContent, (await _cliente.DeleteAsync($"/api/v1/transportistas-terceros/{id}")).StatusCode);
        await Error(await _cliente.GetAsync($"/api/v1/transportistas-terceros/{id}"), 404, $"El transportista {id} no existe.");
    }

    [Fact]
    public async Task DocumentoEsUnicoPorPaisSinDistinguirMayusculas()
    {
        GuardarTransportistaTerceroRequestDto datos = Maestro(); datos.DocumentoFiscal = " doc-1 "; datos.Pais = " perú ";
        await Error(await _cliente.PostAsJsonAsync("/api/v1/transportistas-terceros", datos), 409, "Ya existe un transportista con el documento fiscal doc-1 en perú.");
        await Error(await _cliente.PutAsJsonAsync("/api/v1/transportistas-terceros/2", datos), 409, "Ya existe un transportista con el documento fiscal doc-1 en perú.");
        datos.Pais = "Chile";
        await Id(await _cliente.PostAsJsonAsync("/api/v1/transportistas-terceros", datos));
        datos.Pais = "Perú";
        Assert.Equal(1, await Id(await _cliente.PutAsJsonAsync("/api/v1/transportistas-terceros/1", datos)));
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MaestroInexistente(string metodo)
    {
        HttpResponseMessage respuesta = metodo == "GET" ? await _cliente.GetAsync("/api/v1/transportistas-terceros/9999") : metodo == "PUT" ? await _cliente.PutAsJsonAsync("/api/v1/transportistas-terceros/9999", new GuardarTransportistaTerceroRequestDto()) : await _cliente.DeleteAsync("/api/v1/transportistas-terceros/9999");
        await Error(respuesta, 404, "El transportista 9999 no existe.");
    }

    [Fact]
    public async Task FiltrosPaginacionYResumenSonCoherentes()
    {
        PaginaTransportistasTercerosDto? pagina = await _cliente.GetFromJsonAsync<PaginaTransportistasTercerosDto>("/api/v1/transportistas-terceros?busqueda=DOC-1&pais=PERÚ&tipoServicio=ambos&activo=true&tamanoPagina=1");
        Assert.NotNull(pagina); Assert.Equal(1, pagina.Total); Assert.Equal(1, Assert.Single(pagina.Items).Id);
        PaginaTransportistasTercerosDto? inactivos = await _cliente.GetFromJsonAsync<PaginaTransportistasTercerosDto>("/api/v1/transportistas-terceros?activo=false");
        Assert.Equal(3, Assert.Single(inactivos!.Items).Id);
        PaginaTransportistasTercerosDto? ultima = await _cliente.GetFromJsonAsync<PaginaTransportistasTercerosDto>("/api/v1/transportistas-terceros?pagina=2&tamanoPagina=2");
        Assert.Equal(3, ultima!.Total); Assert.Equal(3, Assert.Single(ultima.Items).Id);
        PaginaTransportistasTercerosDto? enorme = await _cliente.GetFromJsonAsync<PaginaTransportistasTercerosDto>("/api/v1/transportistas-terceros?pagina=2147483647");
        Assert.Empty(enorme!.Items);
        ResumenTransportistasTercerosDto? resumen = await _cliente.GetFromJsonAsync<ResumenTransportistasTercerosDto>("/api/v1/transportistas-terceros/resumen");
        Assert.Equal(3, resumen!.TotalTransportistas); Assert.Equal(2, resumen.Activos); Assert.Equal(1, resumen.Inactivos); Assert.Equal(3, resumen.PorTipoServicio.Sum(t => t.TotalTransportistas));
    }

    [Theory]
    [InlineData("transportistas-terceros?pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("transportistas-terceros?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("transportistas-terceros?tipoServicio=otro", "El tipo de servicio debe ser 3PL, Flete o Ambos.")]
    [InlineData("fletes-terceros?pagina=0", "El número de página debe ser mayor que cero.")]
    [InlineData("fletes-terceros?tamanoPagina=0", "El tamaño de página debe estar entre 1 y 100.")]
    [InlineData("fletes-terceros?estado=otro", "El estado debe ser Registrado o Cancelado.")]
    [InlineData("fletes-terceros?transportistaId=0", "El transportista es obligatorio.")]
    public async Task FiltrosInvalidos(string ruta, string mensaje)
    { await Error(await _cliente.GetAsync("/api/v1/" + ruta), 400, mensaje); }

    [Theory]
    [InlineData("Referencia", "", "La referencia del flete es obligatoria.")]
    [InlineData("Origen", " ", "El origen es obligatorio.")]
    [InlineData("Destino", "", "El destino es obligatorio.")]
    [InlineData("Moneda", "P3N", "La moneda debe tener tres letras.")]
    public async Task FleteValidaCampos(string campo, string valor, string mensaje)
    {
        RegistrarFleteTerceroRequestDto datos = Flete();
        typeof(RegistrarFleteTerceroRequestDto).GetProperty(campo)!.SetValue(datos, valor);
        await Error(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros", datos), 400, mensaje);
    }

    [Theory]
    [InlineData("Referencia", 30, "La referencia del flete no puede superar 30 caracteres.")]
    [InlineData("Origen", 120, "El origen no puede superar 120 caracteres.")]
    [InlineData("Destino", 120, "El destino no puede superar 120 caracteres.")]
    [InlineData("Observaciones", 500, "Las observaciones no pueden superar 500 caracteres.")]
    public async Task FleteValidaLongitudes(string campo, int limite, string mensaje)
    {
        RegistrarFleteTerceroRequestDto datos = Flete();
        typeof(RegistrarFleteTerceroRequestDto).GetProperty(campo)!.SetValue(datos, new string('x', limite + 1));
        await Error(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros", datos), 400, mensaje);
    }

    [Theory]
    [InlineData("0", "El importe debe ser mayor que cero.")]
    [InlineData("-1", "El importe debe ser mayor que cero.")]
    [InlineData("1.001", "El importe no puede tener más de 2 decimales.")]
    [InlineData("10000000000000000", "El importe no puede superar 9999999999999999.99.")]
    public async Task FleteNoRedondeaNiAceptaDesbordamientos(string importe, string mensaje)
    {
        RegistrarFleteTerceroRequestDto datos = Flete(); datos.Importe = decimal.Parse(importe, System.Globalization.CultureInfo.InvariantCulture);
        await Error(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros", datos), 400, mensaje);
    }

    [Theory]
    [InlineData(0, 400, "El transportista es obligatorio.")]
    [InlineData(9999, 404, "El transportista 9999 no existe.")]
    [InlineData(3, 409, "No se pueden registrar fletes para un transportista inactivo.")]
    public async Task FleteExigeTransportistaDisponible(int id, int estado, string mensaje)
    { RegistrarFleteTerceroRequestDto datos = Flete(); datos.TransportistaId = id; await Error(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros", datos), estado, mensaje); }

    [Fact]
    public async Task FechaRequeridaYNormalizacionUtcConPrecisionDecimalCompleta()
    {
        RegistrarFleteTerceroRequestDto datos = Flete(); datos.Fecha = DateTime.MinValue;
        await Error(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros", datos), 400, "La fecha del flete no es válida.");
        datos.Fecha = new DateTime(2026, 11, 21); datos.Importe = 9999999999999999.99m; datos.Moneda = " pen "; datos.Referencia = "  EXT-1 ";
        int id = await Id(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros", datos));
        FleteTerceroDetalleDto? detalle = await _cliente.GetFromJsonAsync<FleteTerceroDetalleDto>($"/api/v1/fletes-terceros/{id}");
        Assert.NotNull(detalle); Assert.Equal("9999999999999999.99", detalle.Importe); Assert.Equal("PEN", detalle.Moneda); Assert.Equal("EXT-1", detalle.Referencia); Assert.Equal(DateTimeKind.Utc, detalle.Fecha.Kind); Assert.Equal("Andes", detalle.TransportistaRazonSocial); Assert.Null(detalle.MotivoCancelacion);
    }

    [Fact]
    public async Task HistorialImpideBajaInclusoTrasCancelarYReservaReferencia()
    {
        int id = await Id(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros", Flete()));
        await Error(await _cliente.DeleteAsync("/api/v1/transportistas-terceros/1"), 409, "No se puede eliminar un transportista con fletes registrados.");
        GuardarTransportistaTerceroRequestDto maestro = Maestro(); maestro.DocumentoFiscal = "DOC-1"; maestro.Activo = false;
        await Id(await _cliente.PutAsJsonAsync("/api/v1/transportistas-terceros/1", maestro));
        await Error(await _cliente.PostAsJsonAsync($"/api/v1/fletes-terceros/{id}/cancelacion", new { motivo = " " }), 400, "El motivo de cancelación es obligatorio.");
        await Error(await _cliente.PostAsJsonAsync($"/api/v1/fletes-terceros/{id}/cancelacion", new { motivo = new string('x', 501) }), 400, "El motivo de cancelación no puede superar 500 caracteres.");
        await Id(await _cliente.PostAsJsonAsync($"/api/v1/fletes-terceros/{id}/cancelacion", new { motivo = "  Ruta suspendida  " }));
        FleteTerceroDetalleDto? detalle = await _cliente.GetFromJsonAsync<FleteTerceroDetalleDto>($"/api/v1/fletes-terceros/{id}");
        Assert.Equal("Cancelado", detalle!.Estado); Assert.Equal("Ruta suspendida", detalle.MotivoCancelacion); Assert.Equal(DateTimeKind.Utc, detalle.FechaCancelacion!.Value.Kind);
        await Error(await _cliente.PostAsJsonAsync($"/api/v1/fletes-terceros/{id}/cancelacion", new { motivo = "" }), 409, $"El flete {id} ya está cancelado.");
        await Error(await _cliente.DeleteAsync("/api/v1/transportistas-terceros/1"), 409, "No se puede eliminar un transportista con fletes registrados.");
        maestro.Activo = true; await Id(await _cliente.PutAsJsonAsync("/api/v1/transportistas-terceros/1", maestro));
        RegistrarFleteTerceroRequestDto duplicado = Flete(); duplicado.Referencia = " ext-1 ";
        await Error(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros", duplicado), 409, "Ya existe un flete con la referencia ext-1 para este transportista.");
        duplicado.TransportistaId = 2; await Id(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros", duplicado));
        PaginaFletesTercerosDto? filtrados = await _cliente.GetFromJsonAsync<PaginaFletesTercerosDto>("/api/v1/fletes-terceros?busqueda=lima&transportistaId=1&estado=cancelado");
        Assert.Equal(id, Assert.Single(filtrados!.Items).Id);
    }

    [Fact]
    public async Task FleteInexistenteNoPermiteConsultaNiCancelacion()
    {
        await Error(await _cliente.GetAsync("/api/v1/fletes-terceros/9999"), 404, "El flete 9999 no existe.");
        await Error(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros/9999/cancelacion", new { motivo = "" }), 404, "El flete 9999 no existe.");
    }

    [Fact]
    public async Task LimitesExactosSeConservanSinTruncar()
    {
        GuardarTransportistaTerceroRequestDto maestro = Maestro();
        maestro.RazonSocial = new string('r', 200);
        maestro.DocumentoFiscal = new string('d', 30);
        maestro.Pais = new string('p', 100);
        maestro.Contacto = new string('c', 100);
        maestro.Telefono = new string('t', 20);
        maestro.Observaciones = new string('o', 500);
        int transportistaId = await Id(await _cliente.PostAsJsonAsync("/api/v1/transportistas-terceros", maestro));
        TransportistaTerceroDetalleDto? detalle = await _cliente.GetFromJsonAsync<TransportistaTerceroDetalleDto>($"/api/v1/transportistas-terceros/{transportistaId}");
        Assert.Equal(maestro.RazonSocial, detalle!.RazonSocial);
        Assert.Equal(maestro.DocumentoFiscal, detalle.DocumentoFiscal);
        Assert.Equal(maestro.Pais, detalle.Pais);
        Assert.Equal(maestro.Contacto, detalle.Contacto);
        Assert.Equal(maestro.Telefono, detalle.Telefono);
        Assert.Equal(maestro.Observaciones, detalle.Observaciones);
        RegistrarFleteTerceroRequestDto datos = Flete();
        datos.Referencia = new string('r', 30); datos.Origen = new string('o', 120); datos.Destino = new string('d', 120); datos.Observaciones = new string('n', 500); datos.Importe = 0.01m;
        int fleteId = await Id(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros", datos));
        FleteTerceroDetalleDto? flete = await _cliente.GetFromJsonAsync<FleteTerceroDetalleDto>($"/api/v1/fletes-terceros/{fleteId}");
        Assert.Equal(datos.Referencia, flete!.Referencia); Assert.Equal(datos.Origen, flete.Origen); Assert.Equal(datos.Destino, flete.Destino); Assert.Equal(datos.Observaciones, flete.Observaciones); Assert.Equal("0.01", flete.Importe);
    }

    [Fact]
    public async Task FletesPaginanPorFechaEIdDescendenteSinDesbordar()
    {
        for (int indice = 1; indice <= 3; indice++)
        {
            RegistrarFleteTerceroRequestDto datos = Flete(); datos.Referencia = $"F-{indice}"; datos.Fecha = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc);
            await Id(await _cliente.PostAsJsonAsync("/api/v1/fletes-terceros", datos));
        }
        PaginaFletesTercerosDto? primera = await _cliente.GetFromJsonAsync<PaginaFletesTercerosDto>("/api/v1/fletes-terceros?tamanoPagina=2");
        PaginaFletesTercerosDto? segunda = await _cliente.GetFromJsonAsync<PaginaFletesTercerosDto>("/api/v1/fletes-terceros?tamanoPagina=2&pagina=2");
        PaginaFletesTercerosDto? enorme = await _cliente.GetFromJsonAsync<PaginaFletesTercerosDto>("/api/v1/fletes-terceros?pagina=2147483647");
        Assert.Equal(new[] { "F-3", "F-2" }, primera!.Items.Select(f => f.Referencia).ToArray()); Assert.Equal("F-1", Assert.Single(segunda!.Items).Referencia); Assert.Empty(enorme!.Items); Assert.Equal(3, primera.Total);
    }

    [Theory]
    [InlineData("2026-11-21T00:00:00-05:00", "2026-11-21T05:00:00Z")]
    [InlineData("2099-12-31", "2099-12-31T00:00:00Z")]
    public async Task FleteNormalizaOffsetYPreservaFechasFuturas(string fecha, string esperada)
    {
        string json = JsonSerializer.Serialize(new { transportistaId = 1, referencia = "UTC", fecha, origen = "Lima", destino = "Callao", importe = 1.25m, moneda = "PEN" });
        HttpResponseMessage respuesta = await _cliente.PostAsync("/api/v1/fletes-terceros", new StringContent(json, Encoding.UTF8, "application/json"));
        int id = await Id(respuesta);
        FleteTerceroDetalleDto? detalle = await _cliente.GetFromJsonAsync<FleteTerceroDetalleDto>($"/api/v1/fletes-terceros/{id}");
        Assert.Equal(DateTime.Parse(esperada, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal), detalle!.Fecha);
        Assert.Equal(DateTimeKind.Utc, detalle.Fecha.Kind);
    }

    [Fact]
    public async Task OpenApiExponeDiezOperacionesSinEditarNiEliminarFletes()
    {
        using JsonDocument documento = JsonDocument.Parse(await _cliente.GetStringAsync("/openapi/v1.json"));
        JsonElement rutas = documento.RootElement.GetProperty("paths");
        foreach ((string ruta, string verbo) in new[] {
            ("/api/v1/transportistas-terceros", "get"), ("/api/v1/transportistas-terceros", "post"),
            ("/api/v1/transportistas-terceros/{id}", "get"), ("/api/v1/transportistas-terceros/{id}", "put"), ("/api/v1/transportistas-terceros/{id}", "delete"),
            ("/api/v1/transportistas-terceros/resumen", "get"), ("/api/v1/fletes-terceros", "get"), ("/api/v1/fletes-terceros", "post"),
            ("/api/v1/fletes-terceros/{id}", "get"), ("/api/v1/fletes-terceros/{id}/cancelacion", "post") })
        { Assert.True(rutas.GetProperty(ruta).TryGetProperty(verbo, out _)); }
        Assert.False(rutas.GetProperty("/api/v1/fletes-terceros/{id}").TryGetProperty("put", out _));
        Assert.False(rutas.GetProperty("/api/v1/fletes-terceros/{id}").TryGetProperty("delete", out _));
    }

    private static GuardarTransportistaTerceroRequestDto Maestro()
    { return new GuardarTransportistaTerceroRequestDto { RazonSocial = "Nuevo", DocumentoFiscal = "DOC-NUEVO", Pais = "Perú", TipoServicio = "Ambos", Activo = true }; }

    private static RegistrarFleteTerceroRequestDto Flete()
    { return new RegistrarFleteTerceroRequestDto { TransportistaId = 1, Referencia = "EXT-1", Fecha = DateTime.UtcNow, Origen = "Lima", Destino = "Callao", Importe = 12.30m, Moneda = "PEN" }; }

    private static async Task<int> Id(HttpResponseMessage respuesta)
    { Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); Dictionary<string, int>? dato = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>(); Assert.NotNull(dato); return dato["id"]; }

    private static async Task Error(HttpResponseMessage respuesta, int estado, string mensaje)
    {
        using JsonDocument json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(estado, (int)respuesta.StatusCode); Assert.Equal(estado, json.RootElement.GetProperty("StatusCode").GetInt32()); Assert.Equal(mensaje, json.RootElement.GetProperty("Message").GetString()); Assert.False(json.RootElement.TryGetProperty("Detail", out _));
    }
}
