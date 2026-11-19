// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El constructor y la auditoría están en ../config/.
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Infrastructure.Persistence;

public partial class TitanGlobalDbContext : DbContext
{
    public virtual DbSet<AlertaDescartada> AlertasDescartadas { get; set; } = null!;

    public virtual DbSet<Almacen> Almacenes { get; set; } = null!;

    public virtual DbSet<AsignacionVenta> AsignacionesVentas { get; set; } = null!;

    public virtual DbSet<CategoriaProducto> CategoriasProducto { get; set; } = null!;

    public virtual DbSet<CentroTrabajo> CentrosTrabajo { get; set; } = null!;

    public virtual DbSet<Cliente> Clientes { get; set; } = null!;

    public virtual DbSet<Conductor> Conductores { get; set; } = null!;

    public virtual DbSet<ContratoComercial> ContratosComerciales { get; set; } = null!;

    public virtual DbSet<Cotizacion> Cotizaciones { get; set; } = null!;

    public virtual DbSet<DetalleCotizacion> DetallesCotizacion { get; set; } = null!;

    public virtual DbSet<DetalleDevolucion> DetallesDevolucion { get; set; } = null!;

    public virtual DbSet<DetalleOrdenAbastecimiento> DetallesOrdenesAbastecimiento { get; set; } = null!;

    public virtual DbSet<DetalleOrdenCompra> DetallesOrdenCompra { get; set; } = null!;

    public virtual DbSet<DetalleSolicitudCompra> DetallesSolicitudCompra { get; set; } = null!;

    public virtual DbSet<DevolucionComercial> DevolucionesComerciales { get; set; } = null!;

    public virtual DbSet<Empleado> Empleados { get; set; } = null!;

    public virtual DbSet<Impuesto> Impuestos { get; set; } = null!;

    public virtual DbSet<ItemListaPrecios> ItemsListaPrecios { get; set; } = null!;

    public virtual DbSet<LiquidacionComision> LiquidacionesComisiones { get; set; } = null!;

    public virtual DbSet<ListaPrecios> ListasPrecios { get; set; } = null!;

    public virtual DbSet<OrdenAbastecimiento> OrdenesAbastecimiento { get; set; } = null!;

    public virtual DbSet<OrdenCompra> OrdenesCompra { get; set; } = null!;

    public virtual DbSet<PermisoRol> PermisosRoles { get; set; } = null!;

    public virtual DbSet<Producto> Productos { get; set; } = null!;

    public virtual DbSet<Promocion> Promociones { get; set; } = null!;

    public virtual DbSet<Proveedor> Proveedores { get; set; } = null!;

    public virtual DbSet<ReclamoPostventa> ReclamosPostventa { get; set; } = null!;

    public virtual DbSet<RegistroAuditoria> RegistrosAuditoria { get; set; } = null!;

    public virtual DbSet<Rol> Roles { get; set; } = null!;

    public virtual DbSet<SegmentoCliente> SegmentosClientes { get; set; } = null!;

    public virtual DbSet<SolicitudCompra> SolicitudesCompra { get; set; } = null!;

    public virtual DbSet<TasaImpuesto> TasasImpuestos { get; set; } = null!;

    public virtual DbSet<Usuario> Usuarios { get; set; } = null!;

    public virtual DbSet<UsuarioRol> UsuariosRoles { get; set; } = null!;

    public virtual DbSet<VehiculoFlota> VehiculosFlota { get; set; } = null!;

    public virtual DbSet<Vendedor> Vendedores { get; set; } = null!;

    public virtual DbSet<ZonaAlmacenaje> ZonasAlmacenaje { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlertaDescartada>(entity =>
        {
            entity.HasIndex(e => e.Clave, "IX_AlertasDescartadas_Clave").IsUnique();

            entity.Property(e => e.Clave).HasMaxLength(100);
        });

        modelBuilder.Entity<Almacen>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_Almacenes_Codigo").IsUnique();

            entity.Property(e => e.CapacidadM3).HasPrecision(18, 2);
            entity.Property(e => e.Ciudad).HasMaxLength(100);
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Direccion).HasMaxLength(300);
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.Responsable).HasMaxLength(100);
            entity.Property(e => e.Tipo).HasMaxLength(20);
        });

        modelBuilder.Entity<AsignacionVenta>(entity =>
        {
            entity.HasIndex(e => e.OrdenCompraId, "IX_AsignacionesVentas_OrdenCompraId").IsUnique();

            entity.HasIndex(e => e.VendedorId, "IX_AsignacionesVentas_VendedorId");

            entity.HasOne(d => d.OrdenCompra).WithOne(p => p.AsignacionVenta)
                .HasForeignKey<AsignacionVenta>(d => d.OrdenCompraId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Vendedor).WithMany(p => p.AsignacionesVentas)
                .HasForeignKey(d => d.VendedorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CategoriaProducto>(entity =>
        {
            entity.HasIndex(e => e.Nombre, "IX_CategoriasProducto_Nombre").IsUnique();

            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<CentroTrabajo>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_CentrosTrabajo_Codigo").IsUnique();

            entity.Property(e => e.CapacidadHorasDia).HasPrecision(5, 2);
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.CostoHora).HasPrecision(18, 2);
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.Planta).HasMaxLength(100);
            entity.Property(e => e.Responsable).HasMaxLength(100);
            entity.Property(e => e.Tipo).HasMaxLength(20);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasIndex(e => e.Ruc, "IX_Clientes_Ruc").IsUnique();
        });

        modelBuilder.Entity<Conductor>(entity =>
        {
            entity.HasIndex(e => e.DocumentoLicencia, "IX_Conductores_DocumentoLicencia").IsUnique();

            entity.Property(e => e.DocumentoLicencia).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
        });

        modelBuilder.Entity<ContratoComercial>(entity =>
        {
            entity.HasIndex(e => e.ClienteId, "IX_ContratosComerciales_ClienteId");

            entity.HasIndex(e => e.Numero, "IX_ContratosComerciales_Numero").IsUnique();

            entity.Property(e => e.CondicionPago).HasMaxLength(20);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.MontoComprometido).HasPrecision(18, 2);
            entity.Property(e => e.MotivoCancelacion).HasMaxLength(500);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Titulo).HasMaxLength(200);

            entity.HasOne(d => d.Cliente).WithMany(p => p.ContratosComerciales)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cotizacion>(entity =>
        {
            entity.HasIndex(e => e.ClienteId, "IX_Cotizaciones_ClienteId");

            entity.HasIndex(e => e.Numero, "IX_Cotizaciones_Numero").IsUnique();

            entity.HasIndex(e => e.OrdenCompraId, "IX_Cotizaciones_OrdenCompraId");

            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(500);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Observaciones).HasMaxLength(500);

            entity.HasOne(d => d.Cliente).WithMany(p => p.Cotizaciones)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.OrdenCompra).WithMany(p => p.Cotizaciones)
                .HasForeignKey(d => d.OrdenCompraId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetalleCotizacion>(entity =>
        {
            entity.HasIndex(e => e.CotizacionId, "IX_DetallesCotizacion_CotizacionId");

            entity.HasIndex(e => e.ProductoId, "IX_DetallesCotizacion_ProductoId");

            entity.Property(e => e.DescuentoPorcentaje).HasPrecision(5, 2);
            entity.Property(e => e.PrecioUnitario).HasPrecision(18, 2);

            entity.HasOne(d => d.Cotizacion).WithMany(p => p.Detalles).HasForeignKey(d => d.CotizacionId);

            entity.HasOne(d => d.Producto).WithMany(p => p.DetallesCotizacion)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetalleDevolucion>(entity =>
        {
            entity.HasIndex(e => new { e.DevolucionComercialId, e.ProductoId }, "IX_DetallesDevolucion_DevolucionComercialId_ProductoId").IsUnique();

            entity.HasIndex(e => e.ProductoId, "IX_DetallesDevolucion_ProductoId");

            entity.HasOne(d => d.DevolucionComercial).WithMany(p => p.Detalles)
                .HasForeignKey(d => d.DevolucionComercialId)
                .HasConstraintName("FK_DetallesDevolucion_DevolucionesComerciales_DevolucionComerc~");

            entity.HasOne(d => d.Producto).WithMany(p => p.DetallesDevolucion)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetalleOrdenAbastecimiento>(entity =>
        {
            entity.HasIndex(e => e.OrdenAbastecimientoId, "IX_DetallesOrdenesAbastecimiento_OrdenAbastecimientoId");

            entity.HasIndex(e => e.ProductoId, "IX_DetallesOrdenesAbastecimiento_ProductoId");

            entity.Property(e => e.PrecioUnitario).HasPrecision(18, 2);

            entity.HasOne(d => d.OrdenAbastecimiento).WithMany(p => p.Detalles)
                .HasForeignKey(d => d.OrdenAbastecimientoId)
                .HasConstraintName("FK_DetallesOrdenesAbastecimiento_OrdenesAbastecimiento_OrdenAb~");

            entity.HasOne(d => d.Producto).WithMany(p => p.DetallesOrdenesAbastecimiento)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetalleOrdenCompra>(entity =>
        {
            entity.HasIndex(e => e.OrdenCompraId, "IX_DetallesOrdenCompra_OrdenCompraId");

            entity.HasIndex(e => e.ProductoId, "IX_DetallesOrdenCompra_ProductoId");

            entity.Property(e => e.PrecioUnitario).HasPrecision(18, 2);

            entity.HasOne(d => d.OrdenCompra).WithMany(p => p.Detalles).HasForeignKey(d => d.OrdenCompraId);

            entity.HasOne(d => d.Producto).WithMany(p => p.DetallesOrdenCompra).HasForeignKey(d => d.ProductoId);
        });

        modelBuilder.Entity<DetalleSolicitudCompra>(entity =>
        {
            entity.HasIndex(e => e.ProductoId, "IX_DetallesSolicitudCompra_ProductoId");

            entity.HasIndex(e => e.SolicitudCompraId, "IX_DetallesSolicitudCompra_SolicitudCompraId");

            entity.Property(e => e.PrecioEstimado).HasPrecision(18, 2);

            entity.HasOne(d => d.Producto).WithMany(p => p.DetallesSolicitudCompra)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.SolicitudCompra).WithMany(p => p.Detalles).HasForeignKey(d => d.SolicitudCompraId);
        });

        modelBuilder.Entity<DevolucionComercial>(entity =>
        {
            entity.HasIndex(e => e.ClienteId, "IX_DevolucionesComerciales_ClienteId");

            entity.HasIndex(e => e.Numero, "IX_DevolucionesComerciales_Numero").IsUnique();

            entity.HasIndex(e => e.OrdenCompraId, "IX_DevolucionesComerciales_OrdenCompraId");

            entity.HasIndex(e => e.ReclamoPostventaId, "IX_DevolucionesComerciales_ReclamoPostventaId");

            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Motivo).HasMaxLength(20);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(500);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Observaciones).HasMaxLength(500);

            entity.HasOne(d => d.Cliente).WithMany(p => p.DevolucionesComerciales)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.OrdenCompra).WithMany(p => p.DevolucionesComerciales)
                .HasForeignKey(d => d.OrdenCompraId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ReclamoPostventa).WithMany(p => p.DevolucionesComerciales)
                .HasForeignKey(d => d.ReclamoPostventaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasIndex(e => e.Legajo, "IX_Empleados_Legajo").IsUnique();

            entity.HasIndex(e => new { e.TipoDocumento, e.NumeroDocumento }, "IX_Empleados_TipoDocumento_NumeroDocumento").IsUnique();

            entity.Property(e => e.Apellidos).HasMaxLength(100);
            entity.Property(e => e.Area).HasMaxLength(100);
            entity.Property(e => e.Cargo).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Legajo).HasMaxLength(20);
            entity.Property(e => e.MotivoCese).HasMaxLength(300);
            entity.Property(e => e.Nombres).HasMaxLength(100);
            entity.Property(e => e.NumeroDocumento).HasMaxLength(20);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.TipoDocumento).HasMaxLength(10);
        });

        modelBuilder.Entity<Impuesto>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_Impuestos_Codigo").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(30);
            entity.Property(e => e.Descripcion).HasMaxLength(250);
            entity.Property(e => e.Nombre).HasMaxLength(120);
            entity.Property(e => e.Pais).HasMaxLength(3);
            entity.Property(e => e.Tipo).HasMaxLength(20);
        });

        modelBuilder.Entity<ItemListaPrecios>(entity =>
        {
            entity.HasIndex(e => new { e.ListaPreciosId, e.ProductoId }, "IX_ItemsListaPrecios_ListaPreciosId_ProductoId").IsUnique();

            entity.HasIndex(e => e.ProductoId, "IX_ItemsListaPrecios_ProductoId");

            entity.Property(e => e.Precio).HasPrecision(18, 2);

            entity.HasOne(d => d.ListaPrecios).WithMany(p => p.Items).HasForeignKey(d => d.ListaPreciosId);

            entity.HasOne(d => d.Producto).WithMany(p => p.ItemsListaPrecios)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LiquidacionComision>(entity =>
        {
            entity.HasIndex(e => new { e.VendedorId, e.Anio, e.Mes }, "IX_LiquidacionesComisiones_VendedorId_Anio_Mes").IsUnique();

            entity.Property(e => e.Comision).HasPrecision(18, 2);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Porcentaje).HasPrecision(5, 2);
            entity.Property(e => e.Ventas).HasPrecision(18, 2);

            entity.HasOne(d => d.Vendedor).WithMany(p => p.LiquidacionesComisiones)
                .HasForeignKey(d => d.VendedorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ListaPrecios>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_ListasPrecios_Codigo").IsUnique();

            entity.HasIndex(e => e.Prioridad, "IX_ListasPrecios_Prioridad").IsUnique();

            entity.HasIndex(e => e.SegmentoClienteId, "IX_ListasPrecios_SegmentoClienteId");

            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(100);

            entity.HasOne(d => d.SegmentoCliente).WithMany(p => p.ListasPrecios)
                .HasForeignKey(d => d.SegmentoClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrdenAbastecimiento>(entity =>
        {
            entity.HasIndex(e => e.Numero, "IX_OrdenesAbastecimiento_Numero").IsUnique();

            entity.HasIndex(e => e.ProveedorId, "IX_OrdenesAbastecimiento_ProveedorId");

            entity.HasIndex(e => e.SolicitudCompraId, "IX_OrdenesAbastecimiento_SolicitudCompraId");

            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.MotivoAnulacion).HasMaxLength(500);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Observaciones).HasMaxLength(500);

            entity.HasOne(d => d.Proveedor).WithMany(p => p.OrdenesAbastecimiento)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.SolicitudCompra).WithMany(p => p.OrdenesAbastecimiento)
                .HasForeignKey(d => d.SolicitudCompraId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrdenCompra>(entity =>
        {
            entity.HasIndex(e => e.ClienteId, "IX_OrdenesCompra_ClienteId");

            entity.HasIndex(e => e.NumeroOrden, "IX_OrdenesCompra_NumeroOrden").IsUnique();

            entity.Property(e => e.Total).HasPrecision(18, 2);

            entity.HasOne(d => d.Cliente).WithMany(p => p.OrdenesCompra).HasForeignKey(d => d.ClienteId);
        });

        modelBuilder.Entity<PermisoRol>(entity =>
        {
            entity.HasIndex(e => new { e.RolId, e.ModuloNumero }, "IX_PermisosRoles_RolId_ModuloNumero").IsUnique();

            entity.HasOne(d => d.Rol).WithMany(p => p.PermisosRoles).HasForeignKey(d => d.RolId);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.Property(e => e.Precio).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Promocion>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_Promociones_Codigo").IsUnique();

            entity.HasIndex(e => e.ProductoId, "IX_Promociones_ProductoId");

            entity.HasIndex(e => e.SegmentoClienteId, "IX_Promociones_SegmentoClienteId");

            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.MontoMinimo).HasPrecision(18, 2);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PorcentajeDescuento).HasPrecision(5, 2);
            entity.Property(e => e.Tipo).HasMaxLength(20);

            entity.HasOne(d => d.Producto).WithMany(p => p.Promociones)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.SegmentoCliente).WithMany(p => p.Promociones)
                .HasForeignKey(d => d.SegmentoClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasIndex(e => e.Ruc, "IX_Proveedores_Ruc").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Pais).HasMaxLength(100);
            entity.Property(e => e.RazonSocial).HasMaxLength(200);
            entity.Property(e => e.Ruc).HasMaxLength(11);
            entity.Property(e => e.Telefono).HasMaxLength(20);
        });

        modelBuilder.Entity<ReclamoPostventa>(entity =>
        {
            entity.HasIndex(e => e.ClienteId, "IX_ReclamosPostventa_ClienteId");

            entity.HasIndex(e => e.Numero, "IX_ReclamosPostventa_Numero").IsUnique();

            entity.HasIndex(e => e.OrdenCompraId, "IX_ReclamosPostventa_OrdenCompraId");

            entity.HasIndex(e => e.ProductoId, "IX_ReclamosPostventa_ProductoId");

            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(500);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Prioridad).HasMaxLength(10);
            entity.Property(e => e.Resolucion).HasMaxLength(1000);
            entity.Property(e => e.Tipo).HasMaxLength(20);

            entity.HasOne(d => d.Cliente).WithMany(p => p.ReclamosPostventa)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.OrdenCompra).WithMany(p => p.ReclamosPostventa)
                .HasForeignKey(d => d.OrdenCompraId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Producto).WithMany(p => p.ReclamosPostventa)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RegistroAuditoria>(entity =>
        {
            entity.HasIndex(e => new { e.Entidad, e.EntidadId }, "IX_RegistrosAuditoria_Entidad_EntidadId");

            entity.HasIndex(e => e.Fecha, "IX_RegistrosAuditoria_Fecha");

            entity.Property(e => e.Accion).HasMaxLength(20);
            entity.Property(e => e.Entidad).HasMaxLength(100);
            entity.Property(e => e.EntidadId).HasMaxLength(50);
            entity.Property(e => e.Ruta).HasMaxLength(200);
            entity.Property(e => e.Usuario).HasMaxLength(30);
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_Roles_Codigo").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(30);
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<SegmentoCliente>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_SegmentosClientes_Codigo").IsUnique();

            entity.HasIndex(e => e.Prioridad, "IX_SegmentosClientes_Prioridad").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.MontoMinimo).HasPrecision(18, 2);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<SolicitudCompra>(entity =>
        {
            entity.HasIndex(e => e.Numero, "IX_SolicitudesCompra_Numero").IsUnique();

            entity.HasIndex(e => e.ProveedorId, "IX_SolicitudesCompra_ProveedorId");

            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(500);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Solicitante).HasMaxLength(100);

            entity.HasOne(d => d.Proveedor).WithMany(p => p.SolicitudesCompra)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TasaImpuesto>(entity =>
        {
            entity.HasIndex(e => new { e.ImpuestoId, e.FechaInicio }, "IX_TasasImpuestos_ImpuestoId_FechaInicio");

            entity.Property(e => e.Observacion).HasMaxLength(250);
            entity.Property(e => e.Porcentaje).HasPrecision(18, 6);

            entity.HasOne(d => d.Impuesto).WithMany(p => p.Tasas).HasForeignKey(d => d.ImpuestoId);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(e => e.Email, "IX_Usuarios_Email").IsUnique();

            entity.HasIndex(e => e.NombreUsuario, "IX_Usuarios_NombreUsuario").IsUnique();

            entity.Property(e => e.ContrasenaHash).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.NombreCompleto).HasMaxLength(100);
            entity.Property(e => e.NombreUsuario).HasMaxLength(30);
        });

        modelBuilder.Entity<UsuarioRol>(entity =>
        {
            entity.HasIndex(e => e.RolId, "IX_UsuariosRoles_RolId");

            entity.HasIndex(e => new { e.UsuarioId, e.RolId }, "IX_UsuariosRoles_UsuarioId_RolId").IsUnique();

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Usuario).WithMany(p => p.Roles).HasForeignKey(d => d.UsuarioId);
        });

        modelBuilder.Entity<VehiculoFlota>(entity =>
        {
            entity.HasIndex(e => e.Placa, "IX_VehiculosFlota_Placa").IsUnique();

            entity.Property(e => e.CapacidadKg).HasPrecision(18, 2);
            entity.Property(e => e.Marca).HasMaxLength(100);
            entity.Property(e => e.Modelo).HasMaxLength(100);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Placa).HasMaxLength(20);
            entity.Property(e => e.Tipo).HasMaxLength(50);
        });

        modelBuilder.Entity<Vendedor>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_Vendedores_Codigo").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.PorcentajeComision).HasPrecision(5, 2);
        });

        modelBuilder.Entity<ZonaAlmacenaje>(entity =>
        {
            entity.HasIndex(e => new { e.AlmacenId, e.Codigo }, "IX_ZonasAlmacenaje_AlmacenId_Codigo").IsUnique();

            entity.Property(e => e.CapacidadM3).HasPrecision(18, 2);
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Temperatura).HasMaxLength(20);
            entity.Property(e => e.Tipo).HasMaxLength(20);

            entity.HasOne(d => d.Almacen).WithMany(p => p.Zonas)
                .HasForeignKey(d => d.AlmacenId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
