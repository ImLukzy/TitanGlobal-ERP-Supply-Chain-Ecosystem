using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo34_OrdenesProduccion;

public sealed class OrdenesProduccionApiFactory : WebApplicationFactory<Program>
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
            new Producto { Id = 2, Nombre = "Producto dos", Categoria = "Manufactura", Precio = 20m },
            new Producto { Id = 3, Nombre = "Tornillo", Categoria = "Insumos", Precio = 1m },
            new Producto { Id = 4, Nombre = "Placa", Categoria = "Insumos", Precio = 5m },
            new Producto { Id = 5, Nombre = "Producto sin ruta", Categoria = "Manufactura", Precio = 30m });
        contexto.CentrosTrabajo.AddRange(
            new CentroTrabajo { Id = 1, Codigo = "CT-A", Nombre = "Corte", Activo = true },
            new CentroTrabajo { Id = 2, Codigo = "CT-B", Nombre = "Ensamblaje", Activo = true });
        contexto.ListasMateriales.AddRange(
            new ListaMateriales
            {
                Id = 10,
                ProductoId = 1,
                Version = 1,
                Estado = "Activa",
                DetallesListaMaterial = new List<DetalleListaMaterial>
                {
                    new DetalleListaMaterial { Id = 1, ComponenteProductoId = 4, Cantidad = 0.5m, UnidadMedida = "kg" },
                    new DetalleListaMaterial { Id = 2, ComponenteProductoId = 3, Cantidad = 2m, UnidadMedida = "unidad" }
                }
            },
            new ListaMateriales { Id = 11, ProductoId = 2, Version = 1, Estado = "Retirada" },
            new ListaMateriales { Id = 12, ProductoId = 5, Version = 1, Estado = "Activa" });
        contexto.RutasProduccion.AddRange(
            new RutaProduccion
            {
                Id = 20,
                ProductoId = 1,
                Version = 1,
                Estado = "Activa",
                OperacionesRutaProduccion = new List<OperacionRutaProduccion>
                {
                    new OperacionRutaProduccion
                    {
                        Id = 1, Secuencia = 2, Nombre = "Ensamblar", CentroTrabajoId = 2,
                        MinutosPreparacion = 5m, MinutosPorUnidad = 0.25m
                    },
                    new OperacionRutaProduccion
                    {
                        Id = 2, Secuencia = 1, Nombre = "Cortar", CentroTrabajoId = 1,
                        MinutosPreparacion = 10m, MinutosPorUnidad = 1.5m
                    }
                }
            },
            new RutaProduccion { Id = 21, ProductoId = 2, Version = 1, Estado = "Retirada" });
        contexto.OrdenesProduccion.AddRange(
            Orden(1, 1, 10, 20, 10, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3), "Planificada"),
            Orden(2, 1, 10, 20, 4, new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 6), "EnProceso"),
            Orden(3, 1, 10, 20, 8, new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 22), "Terminada"),
            Orden(4, 2, 11, 21, 3, new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 12), "Cancelada"));
        contexto.SaveChanges();
    }

    public void RetirarListaYRutaDelProductoUno()
    {
        using IServiceScope scope = Services.CreateScope();
        TitanGlobalDbContext contexto = scope.ServiceProvider.GetRequiredService<TitanGlobalDbContext>();
        contexto.ListasMateriales.Single(lista => lista.Id == 10).Estado = "Retirada";
        contexto.RutasProduccion.Single(ruta => ruta.Id == 20).Estado = "Retirada";
        contexto.SaveChanges();
    }

    private static OrdenProduccion Orden(
        int id, int productoId, int listaId, int rutaId, int cantidad, DateOnly inicio, DateOnly fin, string estado)
    {
        OrdenProduccion orden = new OrdenProduccion
        {
            Id = id,
            Numero = $"OP-{id:D6}",
            ProductoId = productoId,
            ListaMaterialesId = listaId,
            RutaProduccionId = rutaId,
            CantidadPlanificada = cantidad,
            FechaInicioPlanificada = inicio,
            FechaFinPlanificada = fin,
            Estado = estado
        };
        if (estado == "EnProceso" || estado == "Terminada")
        {
            orden.FechaInicioReal = new DateTime(2026, 11, 20, 13, 0, 0, DateTimeKind.Utc);
        }

        if (estado == "Terminada")
        {
            orden.CantidadProducida = 7;
            orden.FechaFinReal = new DateTime(2026, 11, 22, 18, 0, 0, DateTimeKind.Utc);
        }

        if (estado == "Cancelada")
        {
            orden.MotivoCancelacion = "Pedido anulado";
        }

        return orden;
    }
}
