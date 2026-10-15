using Microsoft.EntityFrameworkCore;

namespace TitanGlobal.Api.Models;

public class TitanGlobalDbContext : DbContext
{
    public TitanGlobalDbContext(DbContextOptions<TitanGlobalDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes { get; set; } = null!;

    public DbSet<ContratoComercial> ContratosComerciales { get; set; } = null!;

    public DbSet<SegmentoCliente> SegmentosClientes { get; set; } = null!;

    public DbSet<Cotizacion> Cotizaciones { get; set; } = null!;

    public DbSet<DetalleCotizacion> DetallesCotizacion { get; set; } = null!;

    public DbSet<Proveedor> Proveedores { get; set; } = null!;

    public DbSet<Almacen> Almacenes { get; set; } = null!;

    public DbSet<ZonaAlmacenaje> ZonasAlmacenaje { get; set; } = null!;

    public DbSet<ListaPrecios> ListasPrecios { get; set; } = null!;

    public DbSet<ItemListaPrecios> ItemsListaPrecios { get; set; } = null!;

    public DbSet<Promocion> Promociones { get; set; } = null!;

    public DbSet<ReclamoPostventa> ReclamosPostventa { get; set; } = null!;

    public DbSet<SolicitudCompra> SolicitudesCompra { get; set; } = null!;

    public DbSet<DetalleSolicitudCompra> DetallesSolicitudCompra { get; set; } = null!;

    public DbSet<OrdenAbastecimiento> OrdenesAbastecimiento { get; set; } = null!;

    public DbSet<DetalleOrdenAbastecimiento> DetallesOrdenesAbastecimiento { get; set; } = null!;

    public DbSet<Producto> Productos { get; set; } = null!;

    public DbSet<OrdenCompra> OrdenesCompra { get; set; } = null!;

    public DbSet<DetalleOrdenCompra> DetallesOrdenCompra { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cotizacion>()
            .Property(cotizacion => cotizacion.Numero)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Cotizacion>()
            .Property(cotizacion => cotizacion.Estado)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Cotizacion>()
            .Property(cotizacion => cotizacion.Observaciones)
            .HasMaxLength(500);

        modelBuilder.Entity<Cotizacion>()
            .Property(cotizacion => cotizacion.MotivoRechazo)
            .HasMaxLength(500);

        modelBuilder.Entity<Cotizacion>()
            .HasIndex(cotizacion => cotizacion.Numero)
            .IsUnique();

        modelBuilder.Entity<Cotizacion>()
            .HasOne(cotizacion => cotizacion.Cliente)
            .WithMany()
            .HasForeignKey(cotizacion => cotizacion.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cotizacion>()
            .HasOne(cotizacion => cotizacion.OrdenCompra)
            .WithMany()
            .HasForeignKey(cotizacion => cotizacion.OrdenCompraId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cotizacion>()
            .HasMany(cotizacion => cotizacion.Detalles)
            .WithOne(detalle => detalle.Cotizacion)
            .HasForeignKey(detalle => detalle.CotizacionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DetalleCotizacion>()
            .Property(detalle => detalle.PrecioUnitario)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<DetalleCotizacion>()
            .Property(detalle => detalle.DescuentoPorcentaje)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<DetalleCotizacion>()
            .HasOne(detalle => detalle.Producto)
            .WithMany()
            .HasForeignKey(detalle => detalle.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SegmentoCliente>()
            .Property(segmento => segmento.Codigo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<SegmentoCliente>()
            .Property(segmento => segmento.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<SegmentoCliente>()
            .Property(segmento => segmento.Descripcion)
            .HasMaxLength(300);

        modelBuilder.Entity<SegmentoCliente>()
            .Property(segmento => segmento.MontoMinimo)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<SegmentoCliente>()
            .HasIndex(segmento => segmento.Codigo)
            .IsUnique();

        modelBuilder.Entity<SegmentoCliente>()
            .HasIndex(segmento => segmento.Prioridad)
            .IsUnique();
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

        modelBuilder.Entity<ZonaAlmacenaje>()
            .Property(zona => zona.Codigo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<ZonaAlmacenaje>()
            .Property(zona => zona.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<ZonaAlmacenaje>()
            .Property(zona => zona.Tipo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<ZonaAlmacenaje>()
            .Property(zona => zona.Temperatura)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<ZonaAlmacenaje>()
            .Property(zona => zona.CapacidadM3)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ZonaAlmacenaje>()
            .HasIndex(zona => new { zona.AlmacenId, zona.Codigo })
            .IsUnique();

        modelBuilder.Entity<ZonaAlmacenaje>()
            .HasOne(zona => zona.Almacen)
            .WithMany(almacen => almacen.Zonas)
            .HasForeignKey(zona => zona.AlmacenId)
            .OnDelete(DeleteBehavior.Restrict);

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


        modelBuilder.Entity<ContratoComercial>()
            .Property(contrato => contrato.Numero)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<ContratoComercial>()
            .Property(contrato => contrato.Titulo)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<ContratoComercial>()
            .Property(contrato => contrato.MontoComprometido)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ContratoComercial>()
            .Property(contrato => contrato.CondicionPago)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<ContratoComercial>()
            .Property(contrato => contrato.Estado)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<ContratoComercial>()
            .Property(contrato => contrato.Observaciones)
            .HasMaxLength(500);

        modelBuilder.Entity<ContratoComercial>()
            .Property(contrato => contrato.MotivoCancelacion)
            .HasMaxLength(500);

        modelBuilder.Entity<ContratoComercial>()
            .HasIndex(contrato => contrato.Numero)
            .IsUnique();

        modelBuilder.Entity<ContratoComercial>()
            .HasOne(contrato => contrato.Cliente)
            .WithMany()
            .HasForeignKey(contrato => contrato.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cliente>()
            .HasMany(cliente => cliente.OrdenesCompra)
            .WithOne(orden => orden.Cliente)
            .HasForeignKey(orden => orden.ClienteId);

        modelBuilder.Entity<Promocion>()
            .Property(promocion => promocion.Codigo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Promocion>()
            .Property(promocion => promocion.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Promocion>()
            .Property(promocion => promocion.Tipo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Promocion>()
            .Property(promocion => promocion.MontoMinimo)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Promocion>()
            .Property(promocion => promocion.PorcentajeDescuento)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<Promocion>()
            .HasIndex(promocion => promocion.Codigo)
            .IsUnique();

        modelBuilder.Entity<Promocion>()
            .HasOne<Producto>()
            .WithMany()
            .HasForeignKey(promocion => promocion.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Promocion>()
            .HasOne(promocion => promocion.SegmentoCliente)
            .WithMany()
            .HasForeignKey(promocion => promocion.SegmentoClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ListaPrecios>()
            .Property(lista => lista.Codigo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<ListaPrecios>()
            .Property(lista => lista.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<ListaPrecios>()
            .HasIndex(lista => lista.Codigo)
            .IsUnique();

        modelBuilder.Entity<ListaPrecios>()
            .HasIndex(lista => lista.Prioridad)
            .IsUnique();

        modelBuilder.Entity<ListaPrecios>()
            .HasOne(lista => lista.SegmentoCliente)
            .WithMany()
            .HasForeignKey(lista => lista.SegmentoClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ListaPrecios>()
            .HasMany(lista => lista.Items)
            .WithOne(item => item.ListaPrecios)
            .HasForeignKey(item => item.ListaPreciosId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReclamoPostventa>()
            .Property(reclamo => reclamo.Numero)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<ReclamoPostventa>()
            .Property(reclamo => reclamo.Tipo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<ReclamoPostventa>()
            .Property(reclamo => reclamo.Prioridad)
            .HasMaxLength(10)
            .IsRequired();

        modelBuilder.Entity<ReclamoPostventa>()
            .Property(reclamo => reclamo.Descripcion)
            .HasMaxLength(1000)
            .IsRequired();

        modelBuilder.Entity<ReclamoPostventa>()
            .Property(reclamo => reclamo.Estado)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<ReclamoPostventa>()
            .Property(reclamo => reclamo.Resolucion)
            .HasMaxLength(1000);

        modelBuilder.Entity<ReclamoPostventa>()
            .Property(reclamo => reclamo.MotivoRechazo)
            .HasMaxLength(500);

        modelBuilder.Entity<ReclamoPostventa>()
            .HasIndex(reclamo => reclamo.Numero)
            .IsUnique();

        modelBuilder.Entity<ReclamoPostventa>()
            .HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(reclamo => reclamo.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReclamoPostventa>()
            .HasOne<OrdenCompra>()
            .WithMany()
            .HasForeignKey(reclamo => reclamo.OrdenCompraId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReclamoPostventa>()
            .HasOne<Producto>()
            .WithMany()
            .HasForeignKey(reclamo => reclamo.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ItemListaPrecios>()
            .Property(item => item.Precio)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<ItemListaPrecios>()
            .HasIndex(item => new { item.ListaPreciosId, item.ProductoId })
            .IsUnique();

        modelBuilder.Entity<ItemListaPrecios>()
            .HasOne<Producto>()
            .WithMany()
            .HasForeignKey(item => item.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

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
