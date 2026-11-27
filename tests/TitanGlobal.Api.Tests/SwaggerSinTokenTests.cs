using System.Net;

namespace TitanGlobal.Api.Tests;

public class SwaggerSinTokenTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SwaggerSinTokenTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Swagger_se_abre_sin_token_y_la_api_sigue_exigiendolo()
    {
        HttpClient sinToken = _factory.CreateClient();
        sinToken.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage page = await sinToken.GetAsync("/swagger/index.html");
        HttpResponseMessage document = await sinToken.GetAsync("/swagger/v1/swagger.json");
        HttpResponseMessage api = await sinToken.GetAsync("/api/v1/clientes");

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal(HttpStatusCode.OK, document.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);
    }
}
