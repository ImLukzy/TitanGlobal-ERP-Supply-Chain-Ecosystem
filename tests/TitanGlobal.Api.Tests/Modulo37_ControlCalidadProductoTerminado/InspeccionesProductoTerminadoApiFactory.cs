using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo37_ControlCalidadProductoTerminado;

public sealed class InspeccionesProductoTerminadoApiFactory : WebApplicationFactory<Program>
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
        contexto.ListasMateriales.Add(new ListaMateriales { Id = 10, ProductoId = 1, Version = 1, Estado = "Activa" });
        contexto.RutasProduccion.Add(new RutaProduccion { Id = 20, ProductoId = 1, Version = 1, Estado = "Activa" });
        contexto.OrdenesProduccion.AddRange(
            Orden(1, 1, "Terminada", 100, new DateTime(2026, 11, 20, 18, 0, 0, DateTimeKind.Utc)),
            Orden(2, 2, "Terminada", 50, new DateTime(2026, 11, 21, 18, 0, 0, DateTimeKind.Utc)),
            Orden(3, 1, "EnProceso", null, null),
            Orden(4, 1, "Terminada", 10, new DateTime(2026, 11, 22, 18, 0, 0, DateTimeKind.Utc)),
            Orden(5, 2, "Terminada", 5, new DateTime(2026, 11, 19, 18, 0, 0, DateTimeKind.Utc)),
            Orden(6, 1, "Terminada", 0, new DateTime(2026, 11, 26, 0, 30, 0, DateTimeKind.Utc)));
        contexto.InspeccionesProductoTerminado.AddRange(
            Inspeccion(1, 1, new DateOnly(2026, 11, 21), 40, 5m, "Aprobada", 1),
            Inspeccion(2, 1, new DateOnly(2026, 11, 22), 30, 5m, "Rechazada", 3),
            Inspeccion(3, 1, new DateOnly(2026, 11, 23), 20, 2.5m, "Pendiente", null),
            Inspeccion(4, 2, new DateOnly(2026, 11, 22), 25, 10m, "Aprobada", 0),
            Inspeccion(5, 5, new DateOnly(2026, 11, 20), 5, 0m, "Anulada", null));
        contexto.SaveChanges();
    }

    private static OrdenProduccion Orden(int id, int productoId, string estado, int? producida, DateTime? finReal)
    {
        return new OrdenProduccion
        {
            Id = id,
            Numero = $"OP-{id:D6}",
            ProductoId = productoId,
            ListaMaterialesId = 10,
            RutaProduccionId = 20,
            CantidadPlanificada = 100,
            CantidadProducida = producida,
            FechaInicioPlanificada = new DateOnly(2026, 11, 1),
            FechaFinPlanificada = new DateOnly(2026, 11, 30),
            FechaInicioReal = new DateTime(2026, 11, 2, 13, 0, 0, DateTimeKind.Utc),
            FechaFinReal = finReal,
            Estado = estado
        };
    }

    private static InspeccionProductoTerminado Inspeccion(
        int id, int ordenId, DateOnly fecha, int cantidad, decimal tolerancia, string estado, int? noConforme)
    {
        InspeccionProductoTerminado inspeccion = new InspeccionProductoTerminado
        {
            Id = id,
            Numero = $"CCP-{id:D6}",
            OrdenProduccionId = ordenId,
            FechaInspeccion = fecha,
            CantidadInspeccionada = cantidad,
            ToleranciaNoConformesPorcentaje = tolerancia,
            Inspector = "María Quispe",
            Estado = estado,
            CantidadNoConforme = noConforme
        };
        if (noConforme.HasValue)
        {
            inspeccion.FechaDictamen = fecha.ToDateTime(new TimeOnly(15, 0), DateTimeKind.Utc);
            inspeccion.Observaciones = estado == "Rechazada" ? "Rayas en la carcasa" : null;
        }

        if (estado == "Anulada")
        {
            inspeccion.MotivoAnulacion = "Orden equivocada";
        }

        return inspeccion;
    }
}
