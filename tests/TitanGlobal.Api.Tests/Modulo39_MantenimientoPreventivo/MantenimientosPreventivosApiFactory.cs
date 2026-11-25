using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo39_MantenimientoPreventivo;

public sealed class MantenimientosPreventivosApiFactory : WebApplicationFactory<Program>
{
    private readonly string _base = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TitanGlobalDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TitanGlobalDbContext>>();
            // EF InMemory ignora TransactionScope; el smoke contra PostgreSQL comprueba que el alta es atómica.
            services.AddDbContext<TitanGlobalDbContext>(options => options
                .UseInMemoryDatabase(_base)
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });
    }

    public void PrepararDatos()
    {
        using IServiceScope scope = Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.Database.EnsureDeleted();
        contexto.CentrosTrabajo.AddRange(
            new CentroTrabajo { Id = 1, Codigo = "CT-A", Nombre = "Ensamblaje", Tipo = "Linea", Planta = "Lima", CapacidadHorasDia = 8m, Activo = true },
            new CentroTrabajo { Id = 2, Codigo = "CT-B", Nombre = "Envasado", Tipo = "Celda", Planta = "Lima", CapacidadHorasDia = 8m, Activo = true },
            new CentroTrabajo { Id = 3, Codigo = "CT-C", Nombre = "Corte", Tipo = "Celda", Planta = "Lima", CapacidadHorasDia = 8m, Activo = false },
            new CentroTrabajo { Id = 4, Codigo = "CT-D", Nombre = "Pulido", Tipo = "Celda", Planta = "Lima", CapacidadHorasDia = 8m, Activo = true });
        contexto.SaveChanges();
    }

    public int AgregarDirecto(MantenimientoPreventivo mantenimiento)
    {
        using IServiceScope scope = Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.MantenimientosPreventivos.Add(mantenimiento);
        contexto.SaveChanges();
        return mantenimiento.Id;
    }

    public int ContarMantenimientos()
    {
        using IServiceScope scope = Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return contexto.MantenimientosPreventivos.Count();
    }
}
