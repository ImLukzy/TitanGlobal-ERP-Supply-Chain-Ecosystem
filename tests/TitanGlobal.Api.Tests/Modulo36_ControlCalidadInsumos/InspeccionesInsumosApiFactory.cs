using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo36_ControlCalidadInsumos;

public sealed class InspeccionesInsumosApiFactory : WebApplicationFactory<Program>
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
        contexto.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Resina epóxica", Categoria = "Insumos", Precio = 10m },
            new Producto { Id = 2, Nombre = "Tornillo M6", Categoria = "Insumos", Precio = 1m });
        contexto.Proveedores.AddRange(
            new Proveedor { Id = 1, RazonSocial = "Andes Suministros S.A.C.", Ruc = "20600000001", Pais = "Peru", Activo = true },
            new Proveedor { Id = 2, RazonSocial = "Bolivar Químicos S.A.", Ruc = "20600000002", Pais = "Peru", Activo = true },
            new Proveedor { Id = 3, RazonSocial = "Cobre Austral SpA", Ruc = "76000000003", Pais = "Chile", Activo = false });
        contexto.SaveChanges();
    }

    public int AgregarDirecto(InspeccionInsumo inspeccion)
    {
        using IServiceScope scope = Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.InspeccionesInsumos.Add(inspeccion);
        contexto.SaveChanges();
        // Mismo formato que el alta por la API: el número sale del Id.
        inspeccion.Numero = $"CCI-{inspeccion.Id:D6}";
        contexto.SaveChanges();
        return inspeccion.Id;
    }

    public int ContarInspecciones()
    {
        using IServiceScope scope = Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return contexto.InspeccionesInsumos.Count();
    }

    public InspeccionInsumo Leer(int id)
    {
        using IServiceScope scope = Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        return contexto.InspeccionesInsumos.AsNoTracking().Single(inspeccion => inspeccion.Id == id);
    }
}
