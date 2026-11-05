using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TitanGlobal.Application.Services;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Infrastructure.Persistence;
using TitanGlobal.Infrastructure.Repositories.Implementation;
using TitanGlobal.Infrastructure.Services;

namespace TitanGlobal.Infrastructure.Configuration;

public static class InfrastructureServicesExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection.");
        services.AddDbContext<TitanGlobalDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddSingleton<IServicioContrasenas, ServicioContrasenas>();
        services.AddSingleton<IServicioTokens, ServicioTokens>();

        return services;
    }
}
