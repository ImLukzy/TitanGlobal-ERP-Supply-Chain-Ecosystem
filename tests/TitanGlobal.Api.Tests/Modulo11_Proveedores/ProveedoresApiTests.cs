using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo11_Proveedores.Dtos;
using TitanGlobal.Api.Tests.Modulo04_OrdenesCompra;

namespace TitanGlobal.Api.Tests.Modulo11_Proveedores;

[Collection("Modulo04")]
public class ProveedoresApiTests
{
    private readonly HttpClient _clienteHttp;

    private readonly OrdenesCompraApiFactory _fabrica;

    public ProveedoresApiTests(OrdenesCompraApiFactory fabrica)
    {
        _fabrica = fabrica;
        _clienteHttp = fabrica.CreateClient();
        fabrica.SeedDatabase();
        RestablecerProveedores();
    }

    [Fact]
    public async Task ListFiltersByBusinessNameAndRucAndPaginatesInStableOrder()
    {
        PaginaProveedoresDto? porRazonSocial = await _clienteHttp.GetFromJsonAsync<PaginaProveedoresDto>(
            "/api/v1/proveedores?busqueda=andes");
        PaginaProveedoresDto? porRuc = await _clienteHttp.GetFromJsonAsync<PaginaProveedoresDto>(
            "/api/v1/proveedores?busqueda=20600000002");
        PaginaProveedoresDto? segundaPagina = await _clienteHttp.GetFromJsonAsync<PaginaProveedoresDto>(
            "/api/v1/proveedores?pagina=2&tamanoPagina=2");

        Assert.NotNull(porRazonSocial);
        Assert.Equal(new[] { 1 }, porRazonSocial.Items.Select(proveedor => proveedor.Id).ToArray());
        Assert.NotNull(porRuc);
        Assert.Equal(new[] { 2 }, porRuc.Items.Select(proveedor => proveedor.Id).ToArray());
        Assert.NotNull(segundaPagina);
        Assert.Equal(6, segundaPagina.Total);
        Assert.Equal(new[] { 3, 2 }, segundaPagina.Items.Select(proveedor => proveedor.Id).ToArray());
    }

    [Fact]
    public async Task ListFiltersByCountryWithoutCaseAndByActiveValue()
    {
        PaginaProveedoresDto? porPais = await _clienteHttp.GetFromJsonAsync<PaginaProveedoresDto>(
            "/api/v1/proveedores?pais=pErU");
        PaginaProveedoresDto? inactivos = await _clienteHttp.GetFromJsonAsync<PaginaProveedoresDto>(
            "/api/v1/proveedores?activo=false");

        Assert.NotNull(porPais);
        Assert.Equal(3, porPais.Total);
        Assert.All(porPais.Items, proveedor => Assert.Equal("Peru", proveedor.Pais));
        Assert.NotNull(inactivos);
        Assert.Equal(1, inactivos.Total);
        Assert.Equal(5, Assert.Single(inactivos.Items).Id);
    }

    [Theory]
    [InlineData("/api/v1/proveedores?pagina=0", "La página debe ser mayor o igual a 1.")]
    [InlineData("/api/v1/proveedores?tamanoPagina=101", "El tamaño de página debe estar entre 1 y 100.")]
    public async Task InvalidPaginationReturnsBadRequestWithLiteralMessage(string ruta, string mensaje)
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync(ruta);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task DetailReturnsAllProviderFields()
    {
        ProveedorDetalleDto? proveedor = await _clienteHttp.GetFromJsonAsync<ProveedorDetalleDto>("/api/v1/proveedores/1");

        Assert.NotNull(proveedor);
        Assert.Equal("Andes Suministros S.A.C.", proveedor.RazonSocial);
        Assert.Equal("20600000001", proveedor.Ruc);
        Assert.Equal("Peru", proveedor.Pais);
        Assert.Equal("ventas@andes.example", proveedor.Email);
        Assert.Equal("+51 1 555-0101", proveedor.Telefono);
        Assert.True(proveedor.Activo);
    }

    [Fact]
    public async Task CountriesReturnsDistinctSortedValues()
    {
        List<string>? paises = await _clienteHttp.GetFromJsonAsync<List<string>>("/api/v1/proveedores/paises");

        Assert.NotNull(paises);
        Assert.Equal(new[] { "Chile", "China", "Peru" }, paises);
    }

    [Fact]
    public async Task SummaryReturnsExactCountsAndCountryOrder()
    {
        ResumenProveedoresDto? resumen = await _clienteHttp.GetFromJsonAsync<ResumenProveedoresDto>(
            "/api/v1/proveedores/resumen");

        Assert.NotNull(resumen);
        Assert.Equal(6, resumen.TotalProveedores);
        Assert.Equal(5, resumen.Activos);
        Assert.Equal(1, resumen.Inactivos);
        Assert.Equal(2, resumen.SinEmail);
        Assert.Equal(new[] { "Peru", "Chile", "China" }, resumen.PorPais.Select(pais => pais.Pais).ToArray());
        Assert.Equal(new[] { 3, 2, 1 }, resumen.PorPais.Select(pais => pais.TotalProveedores).ToArray());
        Assert.Equal(new[] { 3, 1, 1 }, resumen.PorPais.Select(pais => pais.Activos).ToArray());
    }

    [Theory]
    [InlineData("", "20612345678", "Peru", "La razón social es obligatoria.")]
    [InlineData("   ", "20612345678", "Peru", "La razón social es obligatoria.")]
    [InlineData("Proveedor", "20612345678", "", "El país es obligatorio.")]
    [InlineData("Proveedor", "20612345678", "   ", "El país es obligatorio.")]
    [InlineData("Proveedor", "1234567890", "Peru", "El RUC debe tener 11 dígitos numéricos.")]
    [InlineData("Proveedor", "2061234567X", "Peru", "El RUC debe tener 11 dígitos numéricos.")]
    public async Task RequiredFieldsAndRucAreValidatedInSpecifiedOrder(
        string razonSocial,
        string ruc,
        string pais,
        string mensaje)
    {
        GuardarProveedorRequestDto solicitud = CrearSolicitud(razonSocial, ruc, pais);
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/proveedores", solicitud);

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.BadRequest, 400, mensaje);
    }

    [Fact]
    public async Task OverlongBusinessNameReturnsBadRequest()
    {
        GuardarProveedorRequestDto solicitud = CrearSolicitud(new string('R', 201), "20612345678", "Peru");
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/proveedores", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "La razón social no puede superar 200 caracteres.");
    }

    [Fact]
    public async Task OverlongCountryReturnsBadRequest()
    {
        GuardarProveedorRequestDto solicitud = CrearSolicitud("Proveedor", "20612345678", new string('P', 101));
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/proveedores", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El país no puede superar 100 caracteres.");
    }

    [Theory]
    [InlineData("no-es-email")]
    [InlineData("correo con espacios@example.com")]
    public async Task InvalidEmailReturnsBadRequest(string email)
    {
        GuardarProveedorRequestDto solicitud = CrearSolicitud("Proveedor", "20612345678", "Peru");
        solicitud.Email = email;
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/proveedores", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El email no tiene un formato válido.");
    }

    [Fact]
    public async Task EmailAboveLimitReturnsBadRequest()
    {
        GuardarProveedorRequestDto solicitud = CrearSolicitud("Proveedor", "20612345678", "Peru");
        solicitud.Email = $"{new string('a', 140)}@example.com";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/proveedores", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El email no tiene un formato válido.");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345abc")]
    [InlineData("123456789012345678901")]
    public async Task InvalidPhoneReturnsBadRequest(string telefono)
    {
        GuardarProveedorRequestDto solicitud = CrearSolicitud("Proveedor", "20612345678", "Peru");
        solicitud.Telefono = telefono;
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/proveedores", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.BadRequest,
            400,
            "El teléfono no tiene un formato válido.");
    }

    [Fact]
    public async Task RucSpacesAreTrimmedAndBlankEmailAndPhoneBecomeNull()
    {
        GuardarProveedorRequestDto solicitud = CrearSolicitud("  Proveedor nuevo  ", " 20612345678 ", " Peru ");
        solicitud.Email = "   ";
        solicitud.Telefono = "   ";
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/proveedores", solicitud);
        Dictionary<string, int>? resultado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);

        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        Proveedor? proveedor = await contexto.Proveedores.SingleOrDefaultAsync(entidad => entidad.Id == resultado["id"]);

        Assert.NotNull(proveedor);
        Assert.Equal("Proveedor nuevo", proveedor.RazonSocial);
        Assert.Equal("20612345678", proveedor.Ruc);
        Assert.Equal("Peru", proveedor.Pais);
        Assert.Null(proveedor.Email);
        Assert.Null(proveedor.Telefono);
        Assert.True(proveedor.Activo);
    }

    [Fact]
    public async Task DuplicateRucReturnsConflictWithLiteralMessage()
    {
        GuardarProveedorRequestDto solicitud = CrearSolicitud("Proveedor duplicado", " 20600000001 ", "Peru");
        HttpResponseMessage respuesta = await _clienteHttp.PostAsJsonAsync("/api/v1/proveedores", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un proveedor con el RUC 20600000001.");
    }

    [Fact]
    public async Task UpdateMayKeepItsOwnRuc()
    {
        GuardarProveedorRequestDto solicitud = CrearSolicitud("Andes Actualizado", "20600000001", "Peru");
        solicitud.Email = "nuevo@andes.example";
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/proveedores/1", solicitud);
        Dictionary<string, int>? resultado = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, int>>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.NotNull(resultado);
        Assert.Equal(1, resultado["id"]);
    }

    [Fact]
    public async Task UpdateWithAnotherProvidersRucReturnsConflict()
    {
        GuardarProveedorRequestDto solicitud = CrearSolicitud("Andes Actualizado", "20600000002", "Peru");
        HttpResponseMessage respuesta = await _clienteHttp.PutAsJsonAsync("/api/v1/proveedores/1", solicitud);

        await AssertBusinessErrorAsync(
            respuesta,
            HttpStatusCode.Conflict,
            409,
            "Ya existe un proveedor con el RUC 20600000002.");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MissingProviderReturnsNotFoundWithLiteralMessage(string metodo)
    {
        HttpResponseMessage respuesta;
        if (metodo == "GET")
        {
            respuesta = await _clienteHttp.GetAsync("/api/v1/proveedores/9999");
        }
        else if (metodo == "PUT")
        {
            respuesta = await _clienteHttp.PutAsJsonAsync(
                "/api/v1/proveedores/9999",
                CrearSolicitud("Proveedor ausente", "20612345678", "Peru"));
        }
        else
        {
            respuesta = await _clienteHttp.DeleteAsync("/api/v1/proveedores/9999");
        }

        await AssertBusinessErrorAsync(respuesta, HttpStatusCode.NotFound, 404, "El proveedor 9999 no existe.");
    }

    [Fact]
    public async Task DeleteProviderReturnsNoContent()
    {
        HttpResponseMessage respuesta = await _clienteHttp.DeleteAsync("/api/v1/proveedores/6");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Fact]
    public async Task OpenApiContainsSevenProviderOperations()
    {
        HttpResponseMessage respuesta = await _clienteHttp.GetAsync("/openapi/v1.json");
        using JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        JsonElement rutas = documento.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True(rutas.GetProperty("/api/v1/proveedores").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/proveedores").TryGetProperty("post", out _));
        Assert.True(rutas.GetProperty("/api/v1/proveedores/{id}").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/proveedores/{id}").TryGetProperty("put", out _));
        Assert.True(rutas.GetProperty("/api/v1/proveedores/{id}").TryGetProperty("delete", out _));
        Assert.True(rutas.GetProperty("/api/v1/proveedores/paises").TryGetProperty("get", out _));
        Assert.True(rutas.GetProperty("/api/v1/proveedores/resumen").TryGetProperty("get", out _));
    }

    private void RestablecerProveedores()
    {
        using IServiceScope scope = _fabrica.Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.Proveedores.RemoveRange(contexto.Proveedores);
        contexto.Proveedores.AddRange(
            new Proveedor
            {
                Id = 1,
                RazonSocial = "Andes Suministros S.A.C.",
                Ruc = "20600000001",
                Pais = "Peru",
                Email = "ventas@andes.example",
                Telefono = "+51 1 555-0101",
                Activo = true
            },
            new Proveedor
            {
                Id = 2,
                RazonSocial = "Minerales del Sur S.A.C.",
                Ruc = "20600000002",
                Pais = "Peru",
                Email = "contacto@minerales.example",
                Telefono = "01 555 0102",
                Activo = true
            },
            new Proveedor
            {
                Id = 3,
                RazonSocial = "Logística Pacífico S.A.C.",
                Ruc = "20600000003",
                Pais = "Peru",
                Activo = true
            },
            new Proveedor
            {
                Id = 4,
                RazonSocial = "Cobre Austral SpA",
                Ruc = "76000000004",
                Pais = "Chile",
                Email = "compras@cobreaustral.example",
                Telefono = "+56 2 555-0104",
                Activo = true
            },
            new Proveedor
            {
                Id = 5,
                RazonSocial = "Servicios Andinos SpA",
                Ruc = "76000000005",
                Pais = "Chile",
                Telefono = "+56 2 555-0105",
                Activo = false
            },
            new Proveedor
            {
                Id = 6,
                RazonSocial = "Shenzhen Parts Ltd.",
                Ruc = "91000000006",
                Pais = "China",
                Email = "sales@shenzhen.example",
                Telefono = "+86 755 555 0106",
                Activo = true
            });
        contexto.SaveChanges();
    }

    private static GuardarProveedorRequestDto CrearSolicitud(string razonSocial, string ruc, string pais)
    {
        return new GuardarProveedorRequestDto
        {
            RazonSocial = razonSocial,
            Ruc = ruc,
            Pais = pais
        };
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
