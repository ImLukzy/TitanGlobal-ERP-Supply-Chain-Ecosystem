using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Repositories.Implementations;
using TitanGlobal.Api.Repositories.Interfaces;
using TitanGlobal.Api.Services;

namespace TitanGlobal.Api.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddTitanGlobal(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection.");
        services.AddHttpContextAccessor();
        services.AddDbContext<TitanGlobalDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IAuthService, AuthService>();

        foreach (Type type in typeof(DependencyInjectionExtensions).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("UseCase", StringComparison.Ordinal)))
        {
            services.AddScoped(type);
        }

        return services;
    }
}
