using Microsoft.EntityFrameworkCore;

namespace TitanGlobal.Api.Models;

public class TitanGlobalDbContext : DbContext
{
    public TitanGlobalDbContext(DbContextOptions<TitanGlobalDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes { get; set; } = null!;

    public DbSet<Proveedor> Proveedores { get; set; } = null!;

    public DbSet<Almacen> Almacenes { get; set; } = null!;

    public DbSet<SolicitudCompra> SolicitudesCompra { get; set; } = null!;

    public DbSet<DetalleSolicitudCompra> DetallesSolicitudCompra { get; set; } = null!;

    public DbSet<OrdenAbastecimiento> OrdenesAbastecimiento { get; set; } = null!;

    public DbSet<DetalleOrdenAbastecimiento> DetallesOrdenesAbastecimiento { get; set; } = null!;

    public DbSet<Producto> Productos { get; set; } = null!;

    public DbSet<OrdenCompra> OrdenesCompra { get; set; } = null!;

    public DbSet<DetalleOrdenCompra> DetallesOrdenCompra { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Almacen>()
            .Property(almacen => almacen.Codigo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Almacen>()
            .Property(almacen => almacen.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<Almacen>()
            .Property(almacen => almacen.Tipo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Almacen>()
            .Property(almacen => almacen.Direccion)
            .HasMaxLength(300)
            .IsRequired();

        modelBuilder.Entity<Almacen>()
            .Property(almacen => almacen.Ciudad)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Almacen>()
            .Property(almacen => almacen.CapacidadM3)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Almacen>()
            .Property(almacen => almacen.Responsable)
            .HasMaxLength(100);

        modelBuilder.Entity<Almacen>()
            .HasIndex(almacen => almacen.Codigo)
            .IsUnique();

        modelBuilder.Entity<Proveedor>()
            .Property(proveedor => proveedor.RazonSocial)
            .HasMaxLength(200);

        modelBuilder.Entity<Proveedor>()
            .Property(proveedor => proveedor.Ruc)
            .HasMaxLength(11);

        modelBuilder.Entity<Proveedor>()
            .Property(proveedor => proveedor.Pais)
            .HasMaxLength(100);

        modelBuilder.Entity<Proveedor>()
            .Property(proveedor => proveedor.Email)
            .HasMaxLength(150);

        modelBuilder.Entity<Proveedor>()
            .Property(proveedor => proveedor.Telefono)
            .HasMaxLength(20);

        modelBuilder.Entity<Proveedor>()
            .HasIndex(proveedor => proveedor.Ruc)
            .IsUnique();

        modelBuilder.Entity<SolicitudCompra>()
            .Property(solicitud => solicitud.Numero)
            .HasMaxLength(20);

        modelBuilder.Entity<SolicitudCompra>()
            .Property(solicitud => solicitud.Solicitante)
            .HasMaxLength(100);

        modelBuilder.Entity<SolicitudCompra>()
            .Property(solicitud => solicitud.Estado)
            .HasMaxLength(20);

        modelBuilder.Entity<SolicitudCompra>()
            .Property(solicitud => solicitud.Observaciones)
            .HasMaxLength(500);

        modelBuilder.Entity<SolicitudCompra>()
            .Property(solicitud => solicitud.MotivoRechazo)
            .HasMaxLength(500);

        modelBuilder.Entity<SolicitudCompra>()
            .HasIndex(solicitud => solicitud.Numero)
            .IsUnique();

        modelBuilder.Entity<SolicitudCompra>()
            .HasOne(solicitud => solicitud.Proveedor)
            .WithMany(proveedor => proveedor.SolicitudesCompra)
            .HasForeignKey(solicitud => solicitud.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SolicitudCompra>()
            .HasMany(solicitud => solicitud.Detalles)
            .WithOne(detalle => detalle.SolicitudCompra)
            .HasForeignKey(detalle => detalle.SolicitudCompraId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DetalleSolicitudCompra>()
            .Property(detalle => detalle.PrecioEstimado)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<DetalleSolicitudCompra>()
            .HasOne(detalle => detalle.Producto)
            .WithMany()
            .HasForeignKey(detalle => detalle.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrdenAbastecimiento>()
            .Property(orden => orden.Numero)
            .HasMaxLength(20);

        modelBuilder.Entity<OrdenAbastecimiento>()
            .Property(orden => orden.Estado)
            .HasMaxLength(20);

        modelBuilder.Entity<OrdenAbastecimiento>()
            .Property(orden => orden.Observaciones)
            .HasMaxLength(500);

        modelBuilder.Entity<OrdenAbastecimiento>()
            .Property(orden => orden.MotivoAnulacion)
            .HasMaxLength(500);

        modelBuilder.Entity<OrdenAbastecimiento>()
            .HasIndex(orden => orden.Numero)
            .IsUnique();

        modelBuilder.Entity<OrdenAbastecimiento>()
            .HasOne(orden => orden.Proveedor)
            .WithMany(proveedor => proveedor.OrdenesAbastecimiento)
            .HasForeignKey(orden => orden.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrdenAbastecimiento>()
            .HasOne(orden => orden.SolicitudCompra)
            .WithMany(solicitud => solicitud.OrdenesAbastecimiento)
            .HasForeignKey(orden => orden.SolicitudCompraId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrdenAbastecimiento>()
            .HasMany(orden => orden.Detalles)
            .WithOne(detalle => detalle.OrdenAbastecimiento)
            .HasForeignKey(detalle => detalle.OrdenAbastecimientoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DetalleOrdenAbastecimiento>()
            .Property(detalle => detalle.PrecioUnitario)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<DetalleOrdenAbastecimiento>()
            .HasOne(detalle => detalle.Producto)
            .WithMany()
            .HasForeignKey(detalle => detalle.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<Cliente>()
            .HasMany(cliente => cliente.OrdenesCompra)
            .WithOne(orden => orden.Cliente)
            .HasForeignKey(orden => orden.ClienteId);

        modelBuilder.Entity<Cliente>()
            .HasIndex(cliente => cliente.Ruc)
            .IsUnique();

        modelBuilder.Entity<Producto>()
            .Property(producto => producto.Precio)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<DetalleOrdenCompra>()
            .Property(detalle => detalle.PrecioUnitario)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<OrdenCompra>()
            .Property(orden => orden.Total)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<OrdenCompra>()
            .HasIndex(orden => orden.NumeroOrden)
            .IsUnique();

        modelBuilder.Entity<OrdenCompra>()
            .HasMany(orden => orden.Detalles)
            .WithOne(detalle => detalle.OrdenCompra)
            .HasForeignKey(detalle => detalle.OrdenCompraId);

        modelBuilder.Entity<Producto>()
            .HasMany(producto => producto.DetallesOrdenCompra)
            .WithOne(detalle => detalle.Producto)
            .HasForeignKey(detalle => detalle.ProductoId);
    }
}
