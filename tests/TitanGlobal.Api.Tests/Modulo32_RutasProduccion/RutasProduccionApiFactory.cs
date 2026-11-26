using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo32_RutasProduccion;

public sealed class RutasProduccionApiFactory : WebApplicationFactory<Program>
{
    private readonly string _base = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TitanGlobalDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TitanGlobalDbContext>>();
            services.AddDbContext<TitanGlobalDbContext>(options => options.UseInMemoryDatabase(_base));
        });
    }

    public void PrepararDatos()
    {
        using IServiceScope scope = Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.Database.EnsureDeleted();
        contexto.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Producto uno", Categoria = "Manufactura", Precio = 10m },
            new Producto { Id = 2, Nombre = "Producto dos", Categoria = "Manufactura", Precio = 20m });
        contexto.CentrosTrabajo.AddRange(
            new CentroTrabajo { Id = 1, Codigo = "CT-A", Nombre = "Ensamblaje", Activo = true },
            new CentroTrabajo { Id = 2, Codigo = "CT-B", Nombre = "Envasado", Activo = true },
            new CentroTrabajo { Id = 3, Codigo = "CT-C", Nombre = "Corte", Activo = false },
            new CentroTrabajo { Id = 4, Codigo = "CT-D", Nombre = "Pulido", Activo = false });
        contexto.SaveChanges();
    }
}
