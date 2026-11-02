using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Arquitectura;

public class ArquitecturaTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ArquitecturaTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Domain_NoDependeDeTecnologiaNiDeOtrasCapas()
    {
        List<string> referencias = ObtenerReferencias(typeof(Cliente).Assembly);

        Assert.DoesNotContain(referencias, referencia => referencia.StartsWith("TitanGlobal.", StringComparison.Ordinal));
        Assert.DoesNotContain(referencias, referencia => referencia.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referencias, referencia => referencia.StartsWith("Npgsql", StringComparison.Ordinal));
        Assert.DoesNotContain(referencias, referencia => referencia.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_SoloDependeDeDomainYDeEfCore()
    {
        List<string> referencias = ObtenerReferencias(typeof(ReglaNegocioException).Assembly);

        Assert.Contains("TitanGlobal.Domain", referencias);
        Assert.DoesNotContain("TitanGlobal.Infrastructure", referencias);
        Assert.DoesNotContain("TitanGlobal.Api", referencias);
        Assert.DoesNotContain(referencias, referencia => referencia.StartsWith("Npgsql", StringComparison.Ordinal));
        Assert.DoesNotContain(referencias, referencia => referencia.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Infrastructure_NoDependeDeApiNiDeAspNetCore()
    {
        List<string> referencias = ObtenerReferencias(typeof(TitanGlobalDbContext).Assembly);

        Assert.DoesNotContain("TitanGlobal.Api", referencias);
        Assert.DoesNotContain(referencias, referencia => referencia.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void UseCases_SoloExistenEnApplication()
    {
        Assert.NotEmpty(ObtenerUseCases(typeof(ReglaNegocioException).Assembly));
        Assert.Empty(ObtenerUseCases(typeof(Program).Assembly));
        Assert.Empty(ObtenerUseCases(typeof(TitanGlobalDbContext).Assembly));
    }

    [Fact]
    public void UseCases_SeRegistranPorReflexion()
    {
        using IServiceScope alcance = _factory.Services.CreateScope();

        foreach (Type useCase in ObtenerUseCases(typeof(ReglaNegocioException).Assembly))
        {
            Assert.NotNull(alcance.ServiceProvider.GetService(useCase));
        }
    }

    private static List<string> ObtenerReferencias(Assembly ensamblado)
    {
        return ensamblado.GetReferencedAssemblies()
            .Select(referencia => referencia.Name ?? string.Empty)
            .ToList();
    }

    private static List<Type> ObtenerUseCases(Assembly ensamblado)
    {
        return ensamblado.GetTypes()
            .Where(tipo => tipo.IsClass && !tipo.IsAbstract && tipo.Name.EndsWith("UseCase", StringComparison.Ordinal))
            .ToList();
    }
}
