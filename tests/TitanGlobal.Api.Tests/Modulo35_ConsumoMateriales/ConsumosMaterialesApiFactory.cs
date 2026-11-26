using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Infrastructure.Persistence;

namespace TitanGlobal.Api.Tests.Modulo35_ConsumoMateriales;

public sealed class ConsumosMaterialesApiFactory : WebApplicationFactory<Program>
{
    private readonly string _base = Guid.NewGuid().ToString();

    public static DateOnly HoyEnPeru()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
    }

    public static DateTime InicioDelDiaEnPeru(DateOnly dia)
    {
        return new DateTime(dia.Year, dia.Month, dia.Day, 5, 0, 0, DateTimeKind.Utc);
    }

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
            new Producto { Id = 3, Nombre = "Tornillo", Categoria = "Insumos", Precio = 1m },
            new Producto { Id = 4, Nombre = "Placa", Categoria = "Insumos", Precio = 5m },
            new Producto { Id = 6, Nombre = "Cable", Categoria = "Insumos", Precio = 2m });
        contexto.CentrosTrabajo.Add(new CentroTrabajo { Id = 1, Codigo = "CT-A", Nombre = "Corte", Activo = true });
        contexto.ListasMateriales.Add(new ListaMateriales
        {
            Id = 10,
            ProductoId = 1,
            Version = 1,
            Estado = "Activa",
            DetallesListaMaterial = new List<DetalleListaMaterial>
            {
                new DetalleListaMaterial { Id = 1, ComponenteProductoId = 3, Cantidad = 2m, UnidadMedida = "unidad" },
                new DetalleListaMaterial { Id = 2, ComponenteProductoId = 4, Cantidad = 0.5m, UnidadMedida = "kg" }
            }
        });
        contexto.RutasProduccion.Add(new RutaProduccion { Id = 20, ProductoId = 1, Version = 1, Estado = "Activa" });
        contexto.OrdenesProduccion.AddRange(
            Orden(1, 10, "EnProceso"),
            Orden(2, 4, "Terminada"),
            Orden(3, 6, "Planificada"));

        DateOnly hoy = HoyEnPeru();
        contexto.ConsumosMateriales.AddRange(
            Consumo(1, 1, 3, 12m, "unidad", InicioDelDiaEnPeru(hoy).AddHours(5), "Registrado"),
            Consumo(2, 1, 3, 5m, "unidad", InicioDelDiaEnPeru(hoy.AddDays(-1)).AddHours(5), "Anulado"),
            Consumo(3, 1, 4, 6m, "kg", InicioDelDiaEnPeru(hoy.AddDays(-6)).AddSeconds(-1), "Registrado"),
            Consumo(4, 2, 4, 2m, "kg", new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc), "Registrado"),
            Consumo(5, 1, 4, 1.5m, "kg", InicioDelDiaEnPeru(hoy.AddDays(-6)), "Registrado"));
        contexto.SaveChanges();
    }

    private static OrdenProduccion Orden(int id, int cantidad, string estado)
    {
        return new OrdenProduccion
        {
            Id = id,
            Numero = $"OP-{id:D6}",
            ProductoId = 1,
            ListaMaterialesId = 10,
            RutaProduccionId = 20,
            CantidadPlanificada = cantidad,
            FechaInicioPlanificada = new DateOnly(2026, 11, 1),
            FechaFinPlanificada = new DateOnly(2026, 11, 30),
            Estado = estado
        };
    }

    private static ConsumoMaterial Consumo(
        int id, int ordenId, int componenteId, decimal cantidad, string unidad, DateTime fecha, string estado)
    {
        ConsumoMaterial consumo = new ConsumoMaterial
        {
            Id = id,
            Numero = $"CM-{id:D6}",
            OrdenProduccionId = ordenId,
            ComponenteProductoId = componenteId,
            Cantidad = cantidad,
            UnidadMedida = unidad,
            FechaRegistro = fecha,
            Responsable = id == 1 ? "Ana Torres" : null,
            Estado = estado
        };
        if (estado == "Anulado")
        {
            consumo.FechaAnulacion = fecha.AddHours(1);
            consumo.MotivoAnulacion = "Error de digitación";
        }

        return consumo;
    }
}
