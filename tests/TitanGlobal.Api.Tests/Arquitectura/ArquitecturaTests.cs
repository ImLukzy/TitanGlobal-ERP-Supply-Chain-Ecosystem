using System.Reflection;
using MediatR;
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
    public void Handlers_SoloExistenEnApplication()
    {
        Assert.NotEmpty(ObtenerHandlers(typeof(ReglaNegocioException).Assembly));
        Assert.Empty(ObtenerHandlers(typeof(Program).Assembly));
        Assert.Empty(ObtenerHandlers(typeof(TitanGlobalDbContext).Assembly));
    }

    [Fact]
    public void Handlers_SeRegistranEnMediatR()
    {
        using IServiceScope alcance = _factory.Services.CreateScope();

        foreach (Type handler in ObtenerHandlers(typeof(ReglaNegocioException).Assembly))
        {
            foreach (Type contrato in ObtenerContratosHandler(handler))
            {
                Assert.NotNull(alcance.ServiceProvider.GetService(contrato));
            }
        }
    }

    [Fact]
    public void Requests_EstanEnQueriesOCommandsConUnSoloHandlerEnElMismoArchivo()
    {
        Assembly application = typeof(ReglaNegocioException).Assembly;
        List<Type> handlers = ObtenerHandlers(application);
        List<Type> requests = application.GetTypes()
            .Where(tipo => tipo.IsClass && !tipo.IsAbstract && typeof(IBaseRequest).IsAssignableFrom(tipo))
            .ToList();

        Assert.NotEmpty(requests);
        foreach (Type request in requests)
        {
            string espacio = request.Namespace ?? string.Empty;
            bool esQuery = espacio.EndsWith(".Queries", StringComparison.Ordinal) && request.Name.EndsWith("Query", StringComparison.Ordinal);
            bool esCommand = espacio.EndsWith(".Commands", StringComparison.Ordinal) && request.Name.EndsWith("Command", StringComparison.Ordinal);
            Assert.True(esQuery || esCommand, $"{request.FullName} debe ser una Query en Queries o un Command en Commands.");

            List<Type> propios = handlers
                .Where(handler => ObtenerContratosHandler(handler).Any(contrato => contrato.GetGenericArguments()[0] == request))
                .ToList();
            Type handlerUnico = Assert.Single(propios);
            Assert.Equal(request.Name + "Handler", handlerUnico.Name);
            Assert.Equal(request.Namespace, handlerUnico.Namespace);
        }
    }

    [Fact]
    public void UseCasesHeredados_NoExistenFueraDeApplication()
    {
        Assert.Empty(ObtenerUseCases(typeof(Program).Assembly));
        Assert.Empty(ObtenerUseCases(typeof(TitanGlobalDbContext).Assembly));
    }

    private static List<string> ObtenerReferencias(Assembly ensamblado)
    {
        return ensamblado.GetReferencedAssemblies()
            .Select(referencia => referencia.Name ?? string.Empty)
            .ToList();
    }

    private static List<Type> ObtenerHandlers(Assembly ensamblado)
    {
        return ensamblado.GetTypes()
            .Where(tipo => tipo.IsClass && !tipo.IsAbstract && ObtenerContratosHandler(tipo).Count > 0)
            .ToList();
    }

    private static List<Type> ObtenerContratosHandler(Type tipo)
    {
        return tipo.GetInterfaces()
            .Where(contrato => contrato.IsGenericType
                && (contrato.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                    || contrato.GetGenericTypeDefinition() == typeof(IRequestHandler<>)))
            .ToList();
    }

    private static List<Type> ObtenerUseCases(Assembly ensamblado)
    {
        return ensamblado.GetTypes()
            .Where(tipo => tipo.IsClass && !tipo.IsAbstract && tipo.Name.EndsWith("UseCase", StringComparison.Ordinal))
            .ToList();
    }
}
