using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Application.UseCases.Modulo00_Sistema;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests;

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthReturnsOk()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/v1/sistema/salud");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Bloqueado")]
    [InlineData("SinPermiso")]
    public async Task DeniedRolesReturnForbiddenWithLiteralBody(string role)
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/sistema/salud");
        request.Headers.Add("X-User-Role", role);

        HttpResponseMessage response = await _client.SendAsync(request);
        JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(403, document.RootElement.GetProperty("StatusCode").GetInt32());
        string responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("operación", responseBody);
        Assert.DoesNotContain("operaci\\u00F3n", responseBody);
        Assert.Equal(
            "Acceso denegado: Su rol no tiene permisos para ejecutar esta operación.",
            document.RootElement.GetProperty("Message").GetString());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task EmptyBodyReturnsBadRequestBeforeRoleCheckWithLiteralBody(string method)
    {
        HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/test-probe/echo");
        request.Headers.Add("X-User-Role", "Bloqueado");

        HttpResponseMessage response = await _client.SendAsync(request);
        JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(400, document.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(
            "El cuerpo de la petición (Body) no puede estar vacío.",
            document.RootElement.GetProperty("Message").GetString());
    }

    [Theory]
    [InlineData("/api/v1/clientes")]
    [InlineData("/api/v1/solicitudes-compra/1/rechazar")]
    [InlineData("/api/v1/ordenes-abastecimiento/1/anular")]
    public async Task EmptyBodyOnActionsThatRequireBodyReturnsLiteralBadRequest(string ruta)
    {
        HttpResponseMessage respuesta = await _client.PostAsync(ruta, null);
        JsonDocument documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(400, documento.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal(
            "El cuerpo de la petición (Body) no puede estar vacío.",
            documento.RootElement.GetProperty("Message").GetString());
    }

    [Fact]
    public async Task BodyIsReadableAfterValidation()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/v1/test-probe/echo",
            new ProbeBody { Value = "visible" });
        ProbeBody? body = await response.Content.ReadFromJsonAsync<ProbeBody>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("visible", body?.Value);
    }

    [Fact]
    public async Task ControllerExceptionReturnsJson500()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/v1/test-probe/error");
        JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(500, document.RootElement.GetProperty("StatusCode").GetInt32());
        Assert.Equal("Ocurrió un error interno en el servidor.", document.RootElement.GetProperty("Message").GetString());
        Assert.Equal("probe failure", document.RootElement.GetProperty("Detail").GetString());
    }

    [Fact]
    public void UseCasesAreRegisteredByReflection()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ConsultarSaludUseCase useCase = scope.ServiceProvider.GetRequiredService<ConsultarSaludUseCase>();

        Assert.NotNull(useCase);
    }

    [Fact]
    public async Task UnitOfWorkRepositoryQueriesInMemoryProducts()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Producto product = new Producto
        {
            Nombre = "Producto de prueba",
            Categoria = "Pruebas",
            Precio = 12.50m
        };

        await unitOfWork.Repository<Producto>().AddAsync(product);
        await unitOfWork.CompleteAsync();

        Producto? persisted = await unitOfWork.Repository<Producto>()
            .AsQueryable()
            .SingleOrDefaultAsync(item => item.Nombre == "Producto de prueba");

        Assert.NotNull(persisted);
        Assert.Equal(12.50m, persisted.Precio);
    }

    [Fact]
    public async Task OpenApiIncludesHealthEndpoint()
    {
        HttpResponseMessage response = await _client.GetAsync("/openapi/v1.json");
        string document = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/v1/sistema/salud", document);
    }

    [Fact]
    public async Task Swagger_is_available_in_development()
    {
        HttpResponseMessage response = await _client.GetAsync("/swagger/v1/swagger.json");
        string document = await response.Content.ReadAsStringAsync();
        HttpResponseMessage page = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/v1/clientes", document);
        Assert.Contains("X-User-Role", document);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        TokenPruebas.AutenticarComoAdministrador(client, Services);
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TitanGlobalDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TitanGlobalDbContext>>();
            services.AddDbContext<TitanGlobalDbContext>(options => options.UseInMemoryDatabase("TitanGlobalTests"));
            services.AddControllers().AddApplicationPart(typeof(ProbeController).Assembly);
        });
    }
}

public sealed class ProbeBody
{
    public string Value { get; set; } = string.Empty;
}

[ApiController]
[Route("api/v1/test-probe")]
public sealed class ProbeController : ControllerBase
{
    [HttpPost("echo")]
    [HttpPut("echo")]
    public IActionResult Echo([FromBody] ProbeBody body)
    {
        return Ok(body);
    }

    [HttpGet("error")]
    public IActionResult Error()
    {
        throw new InvalidOperationException("probe failure");
    }
}
