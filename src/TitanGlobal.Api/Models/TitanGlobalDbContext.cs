using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace TitanGlobal.Api.Models;

public partial class TitanGlobalDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public TitanGlobalDbContext(
        DbContextOptions<TitanGlobalDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<RegistroAuditoria> RegistrosAuditoria { get; set; } = null!;

    public DbSet<Usuario> Usuarios { get; set; } = null!;

    public DbSet<UsuarioRol> UsuariosRoles { get; set; } = null!;

    public DbSet<Rol> Roles { get; set; } = null!;

    public DbSet<PermisoRol> PermisosRoles { get; set; } = null!;

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

    public DbSet<DevolucionComercial> DevolucionesComerciales { get; set; } = null!;

    public DbSet<DetalleDevolucion> DetallesDevolucion { get; set; } = null!;

    public DbSet<SolicitudCompra> SolicitudesCompra { get; set; } = null!;

    public DbSet<DetalleSolicitudCompra> DetallesSolicitudCompra { get; set; } = null!;

    public DbSet<OrdenAbastecimiento> OrdenesAbastecimiento { get; set; } = null!;

    public DbSet<DetalleOrdenAbastecimiento> DetallesOrdenesAbastecimiento { get; set; } = null!;

    public DbSet<Producto> Productos { get; set; } = null!;

    public DbSet<OrdenCompra> OrdenesCompra { get; set; } = null!;

    public DbSet<DetalleOrdenCompra> DetallesOrdenCompra { get; set; } = null!;

    public DbSet<AlertaDescartada> AlertasDescartadas { get; set; } = null!;

    public DbSet<Vendedor> Vendedores { get; set; } = null!;

    public DbSet<AsignacionVenta> AsignacionesVentas { get; set; } = null!;

    public DbSet<LiquidacionComision> LiquidacionesComisiones { get; set; } = null!;

    public DbSet<CentroTrabajo> CentrosTrabajo { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurarAuditoria(modelBuilder);
        modelBuilder.Entity<Vendedor>()
            .Property(vendedor => vendedor.Codigo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<Vendedor>()
            .Property(vendedor => vendedor.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Vendedor>()
            .Property(vendedor => vendedor.Email)
            .HasMaxLength(150);

        modelBuilder.Entity<Vendedor>()
            .Property(vendedor => vendedor.PorcentajeComision)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<Vendedor>()
            .HasIndex(vendedor => vendedor.Codigo)
            .IsUnique();

        modelBuilder.Entity<AsignacionVenta>()
            .HasIndex(asignacion => asignacion.OrdenCompraId)
            .IsUnique();

        modelBuilder.Entity<AsignacionVenta>()
            .HasOne<OrdenCompra>()
            .WithMany()
            .HasForeignKey(asignacion => asignacion.OrdenCompraId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AsignacionVenta>()
            .HasOne<Vendedor>()
            .WithMany()
            .HasForeignKey(asignacion => asignacion.VendedorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AsignacionVenta>()
            .Property(asignacion => asignacion.FechaAsignacion)
            .HasColumnType("timestamp with time zone");

        modelBuilder.Entity<LiquidacionComision>()
            .Property(liquidacion => liquidacion.Ventas)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<LiquidacionComision>()
            .Property(liquidacion => liquidacion.Comision)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<LiquidacionComision>()
            .Property(liquidacion => liquidacion.Porcentaje)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<LiquidacionComision>()
            .Property(liquidacion => liquidacion.Estado)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<LiquidacionComision>()
            .Property(liquidacion => liquidacion.FechaCalculo)
            .HasColumnType("timestamp with time zone");

        modelBuilder.Entity<LiquidacionComision>()
            .Property(liquidacion => liquidacion.FechaPago)
            .HasColumnType("timestamp with time zone");

        modelBuilder.Entity<LiquidacionComision>()
            .HasIndex(liquidacion => new { liquidacion.VendedorId, liquidacion.Anio, liquidacion.Mes })
            .IsUnique();

        modelBuilder.Entity<LiquidacionComision>()
            .HasOne<Vendedor>()
            .WithMany()
            .HasForeignKey(liquidacion => liquidacion.VendedorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AlertaDescartada>()
            .Property(alerta => alerta.Clave)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<AlertaDescartada>()
            .HasIndex(alerta => alerta.Clave)
            .IsUnique();

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

        modelBuilder.Entity<DevolucionComercial>()
            .Property(devolucion => devolucion.Numero)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<DevolucionComercial>()
            .Property(devolucion => devolucion.Motivo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<DevolucionComercial>()
            .Property(devolucion => devolucion.Estado)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<DevolucionComercial>()
            .Property(devolucion => devolucion.Observaciones)
            .HasMaxLength(500);

        modelBuilder.Entity<DevolucionComercial>()
            .Property(devolucion => devolucion.MotivoRechazo)
            .HasMaxLength(500);

        modelBuilder.Entity<DevolucionComercial>()
            .HasIndex(devolucion => devolucion.Numero)
            .IsUnique();

        modelBuilder.Entity<DevolucionComercial>()
            .HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(devolucion => devolucion.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DevolucionComercial>()
            .HasOne<OrdenCompra>()
            .WithMany()
            .HasForeignKey(devolucion => devolucion.OrdenCompraId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DevolucionComercial>()
            .HasOne<ReclamoPostventa>()
            .WithMany()
            .HasForeignKey(devolucion => devolucion.ReclamoPostventaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DevolucionComercial>()
            .HasMany(devolucion => devolucion.Detalles)
            .WithOne(detalle => detalle.DevolucionComercial)
            .HasForeignKey(detalle => detalle.DevolucionComercialId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DetalleDevolucion>()
            .HasIndex(detalle => new { detalle.DevolucionComercialId, detalle.ProductoId })
            .IsUnique();

        modelBuilder.Entity<DetalleDevolucion>()
            .HasOne(detalle => detalle.Producto)
            .WithMany()
            .HasForeignKey(detalle => detalle.ProductoId)
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

        modelBuilder.Entity<Rol>()
            .Property(rol => rol.Codigo)
            .HasMaxLength(30)
            .IsRequired();

        modelBuilder.Entity<Rol>()
            .Property(rol => rol.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Rol>()
            .Property(rol => rol.Descripcion)
            .HasMaxLength(300);

        modelBuilder.Entity<Rol>()
            .HasIndex(rol => rol.Codigo)
            .IsUnique();

        modelBuilder.Entity<PermisoRol>()
            .HasIndex(permiso => new { permiso.RolId, permiso.ModuloNumero })
            .IsUnique();

        modelBuilder.Entity<PermisoRol>()
            .HasOne<Rol>()
            .WithMany()
            .HasForeignKey(permiso => permiso.RolId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.NombreUsuario)
            .HasMaxLength(30)
            .IsRequired();

        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.NombreCompleto)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.Email)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<Usuario>()
            .Property(usuario => usuario.FechaAlta)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        modelBuilder.Entity<Usuario>()
            .HasIndex(usuario => usuario.NombreUsuario)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(usuario => usuario.Email)
            .IsUnique();

        modelBuilder.Entity<UsuarioRol>()
            .HasIndex(enlace => new { enlace.UsuarioId, enlace.RolId })
            .IsUnique();

        modelBuilder.Entity<UsuarioRol>()
            .HasOne<Usuario>()
            .WithMany(usuario => usuario.Roles)
            .HasForeignKey(enlace => enlace.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Rol>()
            .HasMany(rol => rol.Usuarios)
            .WithOne()
            .HasForeignKey(enlace => enlace.RolId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CentroTrabajo>()
            .Property(centro => centro.Codigo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<CentroTrabajo>()
            .Property(centro => centro.Nombre)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<CentroTrabajo>()
            .Property(centro => centro.Tipo)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<CentroTrabajo>()
            .Property(centro => centro.Planta)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<CentroTrabajo>()
            .Property(centro => centro.CapacidadHorasDia)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<CentroTrabajo>()
            .Property(centro => centro.CostoHora)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<CentroTrabajo>()
            .Property(centro => centro.Responsable)
            .HasMaxLength(100);

        modelBuilder.Entity<CentroTrabajo>()
            .HasIndex(centro => centro.Codigo)
            .IsUnique();

    }
}
