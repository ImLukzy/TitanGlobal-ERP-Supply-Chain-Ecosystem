using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo51_FacturacionElectronica;

public sealed class FacturasApiFactory : WebApplicationFactory<Program>
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
        contexto.Clientes.AddRange(
            new Cliente { Id = 1, Nombre = "Cliente Industrial", Ruc = "20111111111" },
            new Cliente { Id = 2, Nombre = "Comercio Regional", Ruc = "20222222222" });
        contexto.Productos.AddRange(
            new Producto { Id = 1, Nombre = "Producto uno", Categoria = "Manufactura", Precio = 10m },
            new Producto { Id = 2, Nombre = "Producto dos", Categoria = "Manufactura", Precio = 20m });
        contexto.OrdenesCompra.Add(new OrdenCompra
        {
            Id = 1,
            ClienteId = 1,
            NumeroOrden = "OC-FACT-01",
            Fecha = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        contexto.Impuestos.AddRange(
            new Impuesto { Id = 1, Codigo = "IGV", Nombre = "IGV", Tipo = "IGV", Pais = "PER", Activo = true },
            new Impuesto { Id = 2, Codigo = "ISC", Nombre = "ISC", Tipo = "OTRO", Pais = "PER", Activo = true },
            new Impuesto { Id = 3, Codigo = "INACTIVO", Nombre = "Inactivo", Tipo = "IGV", Pais = "PER", Activo = false });
        contexto.TasasImpuestos.AddRange(
            new TasaImpuesto
            {
                Id = 1,
                ImpuestoId = 1,
                Porcentaje = 18m,
                Activo = true,
                FechaInicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                FechaFin = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)
            },
            new TasaImpuesto
            {
                Id = 2,
                ImpuestoId = 1,
                Porcentaje = 17m,
                Activo = true,
                FechaInicio = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        contexto.SaveChanges();
    }
}
