// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El constructor y la auditoría están en ../config/.
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Infrastructure.Persistence;

public partial class TitanGlobalDbContext : DbContext
{
    public virtual DbSet<AccionCorrectivaSst> AccionesCorrectivasSst { get; set; } = null!;

    public virtual DbSet<AdendaContrato> AdendasContrato { get; set; } = null!;

    public virtual DbSet<AfiliacionPension> AfiliacionesPension { get; set; } = null!;

    public virtual DbSet<AlertaDescartada> AlertasDescartadas { get; set; } = null!;

    public virtual DbSet<Almacen> Almacenes { get; set; } = null!;

    public virtual DbSet<AsignacionTurno> AsignacionesTurno { get; set; } = null!;

    public virtual DbSet<AsignacionVenta> AsignacionesVentas { get; set; } = null!;

    public virtual DbSet<Ausencia> Ausencias { get; set; } = null!;

    public virtual DbSet<BonoPlanilla> BonosPlanilla { get; set; } = null!;

    public virtual DbSet<CalificacionEvaluacion> CalificacionesEvaluacion { get; set; } = null!;

    public virtual DbSet<CategoriaProducto> CategoriasProducto { get; set; } = null!;

    public virtual DbSet<CentroTrabajo> CentrosTrabajo { get; set; } = null!;

    public virtual DbSet<CertificacionCurso> CertificacionesCurso { get; set; } = null!;

    public virtual DbSet<CicloEvaluacion> CiclosEvaluacion { get; set; } = null!;

    public virtual DbSet<Cliente> Clientes { get; set; } = null!;

    public virtual DbSet<CompetenciaEvaluacion> CompetenciasEvaluacion { get; set; } = null!;

    public virtual DbSet<Conductor> Conductores { get; set; } = null!;

    public virtual DbSet<ConsolidacionCarga> ConsolidacionesCarga { get; set; } = null!;

    public virtual DbSet<ContratoComercial> ContratosComerciales { get; set; } = null!;

    public virtual DbSet<ContratoLaboral> ContratosLaborales { get; set; } = null!;

    public virtual DbSet<Cotizacion> Cotizaciones { get; set; } = null!;

    public virtual DbSet<CuotaPrestamo> CuotasPrestamo { get; set; } = null!;

    public virtual DbSet<Curso> Cursos { get; set; } = null!;

    public virtual DbSet<DescuentoCuotaPrestamo> DescuentosCuotaPrestamo { get; set; } = null!;

    public virtual DbSet<DetalleCotizacion> DetallesCotizacion { get; set; } = null!;

    public virtual DbSet<DetalleDevolucion> DetallesDevolucion { get; set; } = null!;

    public virtual DbSet<DetalleListaMaterial> DetallesListaMaterial { get; set; } = null!;

    public virtual DbSet<DetalleOrdenAbastecimiento> DetallesOrdenesAbastecimiento { get; set; } = null!;

    public virtual DbSet<DetalleOrdenCompra> DetallesOrdenCompra { get; set; } = null!;

    public virtual DbSet<DetalleSolicitudCompra> DetallesSolicitudCompra { get; set; } = null!;

    public virtual DbSet<DevolucionComercial> DevolucionesComerciales { get; set; } = null!;

    public virtual DbSet<Empleado> Empleados { get; set; } = null!;

    public virtual DbSet<EntregaEpp> EntregasEpp { get; set; } = null!;

    public virtual DbSet<EquipoProteccion> EquiposProteccion { get; set; } = null!;

    public virtual DbSet<Evaluacion> Evaluaciones { get; set; } = null!;

    public virtual DbSet<EvaluacionProveedor> EvaluacionesProveedor { get; set; } = null!;

    public virtual DbSet<ExamenMedico> ExamenesMedicos { get; set; } = null!;

    public virtual DbSet<FleteTercero> FletesTerceros { get; set; } = null!;

    public virtual DbSet<GuiaRemision> GuiasRemision { get; set; } = null!;

    public virtual DbSet<Impuesto> Impuestos { get; set; } = null!;

    public virtual DbSet<IncidenteSst> IncidentesSst { get; set; } = null!;

    public virtual DbSet<InscripcionCurso> InscripcionesCurso { get; set; } = null!;

    public virtual DbSet<ItemListaPrecios> ItemsListaPrecios { get; set; } = null!;

    public virtual DbSet<JornadaAsistencia> JornadasAsistencia { get; set; } = null!;

    public virtual DbSet<LineaConsolidacionCarga> LineasConsolidacionCarga { get; set; } = null!;

    public virtual DbSet<LineaGuiaRemision> LineasGuiaRemision { get; set; } = null!;

    public virtual DbSet<LiquidacionComision> LiquidacionesComisiones { get; set; } = null!;

    public virtual DbSet<ListaMateriales> ListasMateriales { get; set; } = null!;

    public virtual DbSet<ListaPrecios> ListasPrecios { get; set; } = null!;

    public virtual DbSet<Marcacion> Marcaciones { get; set; } = null!;

    public virtual DbSet<OperacionRutaProduccion> OperacionesRutaProduccion { get; set; } = null!;

    public virtual DbSet<OrdenAbastecimiento> OrdenesAbastecimiento { get; set; } = null!;

    public virtual DbSet<OrdenCompra> OrdenesCompra { get; set; } = null!;

    public virtual DbSet<ParametroPension> ParametrosPension { get; set; } = null!;

    public virtual DbSet<ParametroPlanilla> ParametrosPlanilla { get; set; } = null!;

    public virtual DbSet<PermisoRol> PermisosRoles { get; set; } = null!;

    public virtual DbSet<Planilla> Planillas { get; set; } = null!;

    public virtual DbSet<PlanillaDetalle> PlanillaDetalles { get; set; } = null!;

    public virtual DbSet<Prestamo> Prestamos { get; set; } = null!;

    public virtual DbSet<Producto> Productos { get; set; } = null!;

    public virtual DbSet<ProgramacionCurso> ProgramacionesCurso { get; set; } = null!;

    public virtual DbSet<Promocion> Promociones { get; set; } = null!;

    public virtual DbSet<Proveedor> Proveedores { get; set; } = null!;

    public virtual DbSet<ReclamoPostventa> ReclamosPostventa { get; set; } = null!;

    public virtual DbSet<RegistroAuditoria> RegistrosAuditoria { get; set; } = null!;

    public virtual DbSet<Rol> Roles { get; set; } = null!;

    public virtual DbSet<RutaDespacho> RutasDespacho { get; set; } = null!;

    public virtual DbSet<RutaProduccion> RutasProduccion { get; set; } = null!;

    public virtual DbSet<SegmentoCliente> SegmentosClientes { get; set; } = null!;

    public virtual DbSet<SolicitudCompra> SolicitudesCompra { get; set; } = null!;

    public virtual DbSet<TasaImpuesto> TasasImpuestos { get; set; } = null!;

    public virtual DbSet<TramoQuintaCategoria> TramosQuintaCategoria { get; set; } = null!;

    public virtual DbSet<TransportistaTercero> TransportistasTerceros { get; set; } = null!;

    public virtual DbSet<Turno> Turnos { get; set; } = null!;

    public virtual DbSet<Usuario> Usuarios { get; set; } = null!;

    public virtual DbSet<UsuarioRol> UsuariosRoles { get; set; } = null!;

    public virtual DbSet<VehiculoFlota> VehiculosFlota { get; set; } = null!;

    public virtual DbSet<Vendedor> Vendedores { get; set; } = null!;

    public virtual DbSet<ZonaAlmacenaje> ZonasAlmacenaje { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccionCorrectivaSst>(entity =>
        {
            entity.HasIndex(e => e.IncidenteSstId, "IX_AccionesCorrectivasSst_IncidenteSstId");

            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Responsable).HasMaxLength(150);

            entity.HasOne(d => d.IncidenteSst).WithMany(p => p.AccionesCorrectivasSst)
                .HasForeignKey(d => d.IncidenteSstId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdendaContrato>(entity =>
        {
            entity.HasIndex(e => new { e.ContratoLaboralId, e.FechaVigencia }, "IX_AdendasContrato_ContratoLaboralId_FechaVigencia").IsUnique();

            entity.Property(e => e.Cargo).HasMaxLength(100);
            entity.Property(e => e.SueldoBasico).HasPrecision(12, 2);

            entity.HasOne(d => d.ContratoLaboral).WithMany(p => p.AdendasContrato)
                .HasForeignKey(d => d.ContratoLaboralId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AfiliacionPension>(entity =>
        {
            entity.HasIndex(e => e.EmpleadoId, "IX_AfiliacionesPension_EmpleadoId").IsUnique();

            entity.HasIndex(e => e.ParametroPensionId, "IX_AfiliacionesPension_ParametroPensionId");

            entity.Property(e => e.Cuspp).HasMaxLength(12);

            entity.HasOne(d => d.Empleado).WithOne(p => p.AfiliacionPension)
                .HasForeignKey<AfiliacionPension>(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ParametroPension).WithMany(p => p.AfiliacionesPension)
                .HasForeignKey(d => d.ParametroPensionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

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

        modelBuilder.Entity<AsignacionTurno>(entity =>
        {
            entity.HasIndex(e => new { e.EmpleadoId, e.FechaInicio }, "IX_AsignacionesTurno_EmpleadoId_FechaInicio").IsUnique();

            entity.HasIndex(e => e.TurnoId, "IX_AsignacionesTurno_TurnoId");

            entity.HasOne(d => d.Empleado).WithMany(p => p.AsignacionesTurno)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Turno).WithMany(p => p.AsignacionesTurno)
                .HasForeignKey(d => d.TurnoId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<Ausencia>(entity =>
        {
            entity.HasIndex(e => new { e.EmpleadoId, e.FechaInicio }, "IX_Ausencias_EmpleadoId_FechaInicio");

            entity.HasIndex(e => e.Numero, "IX_Ausencias_Numero").IsUnique();

            entity.Property(e => e.DecididoPor).HasMaxLength(100);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Motivo).HasMaxLength(300);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(300);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Tipo).HasMaxLength(30);

            entity.HasOne(d => d.Empleado).WithMany(p => p.Ausencias)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BonoPlanilla>(entity =>
        {
            entity.HasIndex(e => new { e.Anio, e.Mes }, "IX_BonosPlanilla_Anio_Mes");

            entity.HasIndex(e => e.EmpleadoId, "IX_BonosPlanilla_EmpleadoId");

            entity.HasIndex(e => e.PlanillaId, "IX_BonosPlanilla_PlanillaId");

            entity.Property(e => e.Concepto).HasMaxLength(100);
            entity.Property(e => e.Monto).HasPrecision(12, 2);
            entity.Property(e => e.TipoPlanilla).HasMaxLength(20);

            entity.HasOne(d => d.Empleado).WithMany(p => p.BonosPlanilla)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Planilla).WithMany(p => p.BonosPlanilla)
                .HasForeignKey(d => d.PlanillaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CalificacionEvaluacion>(entity =>
        {
            entity.HasIndex(e => e.CompetenciaEvaluacionId, "IX_CalificacionesEvaluacion_CompetenciaEvaluacionId");

            entity.HasIndex(e => new { e.EvaluacionId, e.CompetenciaEvaluacionId }, "IX_CalificacionesEvaluacion_EvaluacionId_CompetenciaEvaluacionI").IsUnique();

            entity.HasOne(d => d.CompetenciaEvaluacion).WithMany(p => p.CalificacionesEvaluacion)
                .HasForeignKey(d => d.CompetenciaEvaluacionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_CalificacionesEvaluacion_CompetenciasEvaluacion_CompetenciaE");

            entity.HasOne(d => d.Evaluacion).WithMany(p => p.CalificacionesEvaluacion)
                .HasForeignKey(d => d.EvaluacionId)
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

        modelBuilder.Entity<CertificacionCurso>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_CertificacionesCurso_Codigo").IsUnique();

            entity.HasIndex(e => e.InscripcionCursoId, "IX_CertificacionesCurso_InscripcionCursoId").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(40);

            entity.HasOne(d => d.InscripcionCurso).WithOne(p => p.CertificacionCurso)
                .HasForeignKey<CertificacionCurso>(d => d.InscripcionCursoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CicloEvaluacion>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_CiclosEvaluacion_Codigo").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Periodo).HasMaxLength(20);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasIndex(e => e.Ruc, "IX_Clientes_Ruc").IsUnique();
        });

        modelBuilder.Entity<CompetenciaEvaluacion>(entity =>
        {
            entity.HasIndex(e => new { e.CicloEvaluacionId, e.Nombre }, "IX_CompetenciasEvaluacion_CicloEvaluacionId_Nombre").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(300);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Peso).HasPrecision(5, 2);

            entity.HasOne(d => d.CicloEvaluacion).WithMany(p => p.CompetenciasEvaluacion)
                .HasForeignKey(d => d.CicloEvaluacionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Conductor>(entity =>
        {
            entity.HasIndex(e => e.DocumentoLicencia, "IX_Conductores_DocumentoLicencia").IsUnique();

            entity.Property(e => e.DocumentoLicencia).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
        });

        modelBuilder.Entity<ConsolidacionCarga>(entity =>
        {
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Estado)
                .HasMaxLength(15)
                .HasDefaultValueSql("'Borrador'::character varying");
            entity.Property(e => e.MotivoCancelacion).HasMaxLength(500);
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

        modelBuilder.Entity<ContratoLaboral>(entity =>
        {
            entity.HasIndex(e => e.ContratoAnteriorId, "IX_ContratosLaborales_ContratoAnteriorId");

            entity.HasIndex(e => e.EmpleadoId, "IX_ContratosLaborales_EmpleadoId");

            entity.HasIndex(e => new { e.EmpleadoId, e.Estado }, "IX_ContratosLaborales_EmpleadoId_Vigente")
                .IsUnique()
                .HasFilter("((\"Estado\")::text = 'Vigente'::text)");

            entity.HasIndex(e => e.Numero, "IX_ContratosLaborales_Numero").IsUnique();

            entity.Property(e => e.Cargo).HasMaxLength(100);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Modalidad).HasMaxLength(20);
            entity.Property(e => e.MotivoCierre).HasMaxLength(300);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.SueldoBasico).HasPrecision(12, 2);

            entity.HasOne(d => d.ContratoAnterior).WithMany(p => p.InverseContratoAnterior)
                .HasForeignKey(d => d.ContratoAnteriorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Empleado).WithMany(p => p.ContratosLaborales)
                .HasForeignKey(d => d.EmpleadoId)
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

        modelBuilder.Entity<CuotaPrestamo>(entity =>
        {
            entity.HasIndex(e => new { e.Anio, e.Mes }, "IX_CuotasPrestamo_Anio_Mes");

            entity.HasIndex(e => new { e.PrestamoId, e.NumeroCuota }, "IX_CuotasPrestamo_PrestamoId_NumeroCuota").IsUnique();

            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Monto).HasPrecision(12, 2);
            entity.Property(e => e.MontoDescontado).HasPrecision(12, 2);

            entity.HasOne(d => d.Prestamo).WithMany(p => p.CuotasPrestamo)
                .HasForeignKey(d => d.PrestamoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Curso>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_Cursos_Codigo").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Horas).HasPrecision(5, 1);
            entity.Property(e => e.Nombre).HasMaxLength(150);
            entity.Property(e => e.Proveedor).HasMaxLength(150);
            entity.Property(e => e.Tipo).HasMaxLength(20);
        });

        modelBuilder.Entity<DescuentoCuotaPrestamo>(entity =>
        {
            entity.HasIndex(e => e.CuotaPrestamoId, "IX_DescuentosCuotaPrestamo_CuotaPrestamoId");

            entity.HasIndex(e => e.PlanillaId, "IX_DescuentosCuotaPrestamo_PlanillaId");

            entity.Property(e => e.Monto).HasPrecision(12, 2);

            entity.HasOne(d => d.CuotaPrestamo).WithMany(p => p.DescuentosCuotaPrestamo)
                .HasForeignKey(d => d.CuotaPrestamoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Planilla).WithMany(p => p.DescuentosCuotaPrestamo)
                .HasForeignKey(d => d.PlanillaId)
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

        modelBuilder.Entity<DetalleListaMaterial>(entity =>
        {
            entity.HasIndex(e => e.ComponenteProductoId, "IX_DetallesListaMaterial_ComponenteProductoId");

            entity.HasIndex(e => new { e.ListaMaterialId, e.ComponenteProductoId }, "IX_DetallesListaMaterial_ListaMaterialId_ComponenteProductoId").IsUnique();

            entity.Property(e => e.Cantidad).HasPrecision(18, 6);
            entity.Property(e => e.UnidadMedida).HasMaxLength(20);

            entity.HasOne(d => d.ComponenteProducto).WithMany(p => p.DetallesListaMaterial)
                .HasForeignKey(d => d.ComponenteProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ListaMaterial).WithMany(p => p.DetallesListaMaterial).HasForeignKey(d => d.ListaMaterialId);
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

        modelBuilder.Entity<EntregaEpp>(entity =>
        {
            entity.HasIndex(e => e.EmpleadoId, "IX_EntregasEpp_EmpleadoId");

            entity.HasIndex(e => e.EquipoProteccionId, "IX_EntregasEpp_EquipoProteccionId");

            entity.HasOne(d => d.Empleado).WithMany(p => p.EntregasEpp)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.EquipoProteccion).WithMany(p => p.EntregasEpp)
                .HasForeignKey(d => d.EquipoProteccionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EquipoProteccion>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_EquiposProteccion_Codigo").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(100);
        });

        modelBuilder.Entity<Evaluacion>(entity =>
        {
            entity.HasIndex(e => new { e.CicloEvaluacionId, e.EmpleadoId }, "IX_Evaluaciones_CicloEvaluacionId_EmpleadoId").IsUnique();

            entity.HasIndex(e => e.EmpleadoId, "IX_Evaluaciones_EmpleadoId");

            entity.HasIndex(e => e.EvaluadorId, "IX_Evaluaciones_EvaluadorId");

            entity.Property(e => e.Comentario).HasMaxLength(500);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.PuntajeFinal).HasPrecision(4, 2);

            entity.HasOne(d => d.CicloEvaluacion).WithMany(p => p.Evaluaciones)
                .HasForeignKey(d => d.CicloEvaluacionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Empleado).WithMany(p => p.EvaluacionesEmpleado)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Evaluador).WithMany(p => p.EvaluacionesEvaluador)
                .HasForeignKey(d => d.EvaluadorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvaluacionProveedor>(entity =>
        {
            entity.HasIndex(e => new { e.ProveedorId, e.Periodo }, "IX_EvaluacionesProveedor_ProveedorId_Periodo").IsUnique();

            entity.Property(e => e.Calificacion).HasMaxLength(20);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Periodo).HasMaxLength(7);
            entity.Property(e => e.PuntajeFinal).HasPrecision(5, 2);

            entity.HasOne(d => d.Proveedor).WithMany(p => p.EvaluacionesProveedor)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExamenMedico>(entity =>
        {
            entity.HasIndex(e => e.EmpleadoId, "IX_ExamenesMedicos_EmpleadoId");

            entity.Property(e => e.Clinica).HasMaxLength(150);
            entity.Property(e => e.Restricciones).HasMaxLength(300);
            entity.Property(e => e.Resultado).HasMaxLength(30);
            entity.Property(e => e.Tipo).HasMaxLength(20);

            entity.HasOne(d => d.Empleado).WithMany(p => p.ExamenesMedicos)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FleteTercero>(entity =>
        {
            entity.HasIndex(e => new { e.Fecha, e.Id }, "IX_FletesTerceros_FechaId").IsDescending();

            entity.HasIndex(e => e.TransportistaTerceroId, "IX_FletesTerceros_TransportistaTerceroId");

            entity.Property(e => e.Destino).HasMaxLength(120);
            entity.Property(e => e.Estado)
                .HasMaxLength(15)
                .HasDefaultValueSql("'Registrado'::character varying");
            entity.Property(e => e.Importe).HasPrecision(18, 2);
            entity.Property(e => e.Moneda).HasMaxLength(3);
            entity.Property(e => e.MotivoCancelacion).HasMaxLength(500);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Origen).HasMaxLength(120);
            entity.Property(e => e.Referencia).HasMaxLength(30);

            entity.HasOne(d => d.TransportistaTercero).WithMany(p => p.Fletes)
                .HasForeignKey(d => d.TransportistaTerceroId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_FletesTerceros_TransportistasTerceros");
        });

        modelBuilder.Entity<GuiaRemision>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("GuiasRemision_pkey");

            entity.HasIndex(e => new { e.Serie, e.Numero }, "IX_GuiasRemision_Serie_Numero").IsUnique();

            entity.Property(e => e.Conductor).HasMaxLength(100);
            entity.Property(e => e.DestinatarioDocumento).HasMaxLength(20);
            entity.Property(e => e.DestinatarioNombre).HasMaxLength(200);
            entity.Property(e => e.DireccionDestino).HasMaxLength(300);
            entity.Property(e => e.DireccionOrigen).HasMaxLength(300);
            entity.Property(e => e.Estado)
                .HasMaxLength(10)
                .HasDefaultValueSql("'Borrador'::character varying");
            entity.Property(e => e.MotivoAnulacion).HasMaxLength(500);
            entity.Property(e => e.MotivoTraslado).HasMaxLength(200);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.PesoBrutoKg).HasPrecision(12, 2);
            entity.Property(e => e.Placa).HasMaxLength(20);
            entity.Property(e => e.RemitenteDocumento).HasMaxLength(20);
            entity.Property(e => e.RemitenteNombre).HasMaxLength(200);
            entity.Property(e => e.Serie).HasMaxLength(8);
            entity.Property(e => e.Transportista).HasMaxLength(200);
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

        modelBuilder.Entity<IncidenteSst>(entity =>
        {
            entity.HasIndex(e => e.EmpleadoId, "IX_IncidentesSst_EmpleadoId");

            entity.HasIndex(e => e.Fecha, "IX_IncidentesSst_Fecha");

            entity.HasIndex(e => e.Numero, "IX_IncidentesSst_Numero").IsUnique();

            entity.Property(e => e.Descripcion).HasMaxLength(1000);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Lugar).HasMaxLength(150);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Severidad).HasMaxLength(20);
            entity.Property(e => e.Tipo).HasMaxLength(30);

            entity.HasOne(d => d.Empleado).WithMany(p => p.IncidentesSst)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InscripcionCurso>(entity =>
        {
            entity.HasIndex(e => e.EmpleadoId, "IX_InscripcionesCurso_EmpleadoId");

            entity.HasIndex(e => new { e.ProgramacionCursoId, e.EmpleadoId }, "IX_InscripcionesCurso_ProgramacionCursoId_EmpleadoId").IsUnique();

            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Nota).HasPrecision(4, 1);

            entity.HasOne(d => d.Empleado).WithMany(p => p.InscripcionesCurso)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.ProgramacionCurso).WithMany(p => p.InscripcionesCurso)
                .HasForeignKey(d => d.ProgramacionCursoId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<JornadaAsistencia>(entity =>
        {
            entity.HasIndex(e => new { e.EmpleadoId, e.Fecha }, "IX_JornadasAsistencia_EmpleadoId_Fecha").IsUnique();

            entity.HasIndex(e => e.TurnoId, "IX_JornadasAsistencia_TurnoId");

            entity.Property(e => e.DecididoPor).HasMaxLength(100);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.EstadoHorasExtra).HasMaxLength(20);
            entity.Property(e => e.MotivoRechazo).HasMaxLength(300);

            entity.HasOne(d => d.Empleado).WithMany(p => p.JornadasAsistencia)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Turno).WithMany(p => p.JornadasAsistencia)
                .HasForeignKey(d => d.TurnoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LineaConsolidacionCarga>(entity =>
        {
            entity.HasIndex(e => e.ConsolidacionCargaId, "IX_LineasConsolidacionCarga_ConsolidacionCargaId");

            entity.Property(e => e.AltoCm).HasPrecision(6, 2);
            entity.Property(e => e.AnchoCm).HasPrecision(6, 2);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.Property(e => e.LargoCm).HasPrecision(6, 2);
            entity.Property(e => e.PesoBultoKg).HasPrecision(8, 2);
            entity.Property(e => e.Sku).HasMaxLength(40);

            entity.HasOne(d => d.ConsolidacionCarga).WithMany(p => p.Lineas)
                .HasForeignKey(d => d.ConsolidacionCargaId)
                .HasConstraintName("FK_LineasConsolidacionCarga_Consolidacion");
        });

        modelBuilder.Entity<LineaGuiaRemision>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("LineasGuiaRemision_pkey");

            entity.HasIndex(e => e.GuiaRemisionId, "IX_LineasGuiaRemision_GuiaRemisionId");

            entity.Property(e => e.Cantidad).HasPrecision(12, 3);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.Property(e => e.Sku).HasMaxLength(40);
            entity.Property(e => e.UnidadMedida).HasMaxLength(10);

            entity.HasOne(d => d.GuiaRemision).WithMany(p => p.Lineas)
                .HasForeignKey(d => d.GuiaRemisionId)
                .HasConstraintName("LineasGuiaRemision_GuiaRemisionId_fkey");
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

        modelBuilder.Entity<ListaMateriales>(entity =>
        {
            entity.HasIndex(e => e.ProductoId, "IX_ListasMateriales_ProductoId_Activa")
                .IsUnique()
                .HasFilter("((\"Estado\")::text = 'Activa'::text)");

            entity.HasIndex(e => new { e.ProductoId, e.Version }, "IX_ListasMateriales_ProductoId_Version").IsUnique();

            entity.Property(e => e.Estado).HasMaxLength(10);

            entity.HasOne(d => d.Producto).WithOne(p => p.ListaMateriales)
                .HasForeignKey<ListaMateriales>(d => d.ProductoId)
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

        modelBuilder.Entity<Marcacion>(entity =>
        {
            entity.HasIndex(e => new { e.Dispositivo, e.NumeroRegistro }, "IX_Marcaciones_Dispositivo_NumeroRegistro").IsUnique();

            entity.HasIndex(e => new { e.EmpleadoId, e.Instante }, "IX_Marcaciones_EmpleadoId_Instante");

            entity.Property(e => e.Dispositivo).HasMaxLength(50);
            entity.Property(e => e.NumeroRegistro).HasMaxLength(50);
            entity.Property(e => e.Observacion).HasMaxLength(200);
            entity.Property(e => e.Origen).HasMaxLength(10);
            entity.Property(e => e.Tipo).HasMaxLength(10);

            entity.HasOne(d => d.Empleado).WithMany(p => p.Marcaciones)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OperacionRutaProduccion>(entity =>
        {
            entity.HasIndex(e => e.CentroTrabajoId, "IX_OperacionesRutaProduccion_CentroTrabajoId");

            entity.HasIndex(e => new { e.RutaProduccionId, e.Secuencia }, "IX_OperacionesRutaProduccion_RutaProduccionId_Secuencia").IsUnique();

            entity.Property(e => e.MinutosPorUnidad).HasPrecision(10, 2);
            entity.Property(e => e.MinutosPreparacion).HasPrecision(10, 2);
            entity.Property(e => e.Nombre).HasMaxLength(120);

            entity.HasOne(d => d.CentroTrabajo).WithMany(p => p.OperacionesRutaProduccion)
                .HasForeignKey(d => d.CentroTrabajoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.RutaProduccion).WithMany(p => p.OperacionesRutaProduccion).HasForeignKey(d => d.RutaProduccionId);
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

        modelBuilder.Entity<ParametroPension>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_ParametrosPension_Codigo").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.RemuneracionMaximaAsegurable).HasPrecision(12, 2);
            entity.Property(e => e.Sistema).HasMaxLength(10);
            entity.Property(e => e.TasaAporte).HasPrecision(6, 4);
            entity.Property(e => e.TasaComision).HasPrecision(6, 4);
            entity.Property(e => e.TasaPrimaSeguro).HasPrecision(6, 4);
        });

        modelBuilder.Entity<ParametroPlanilla>(entity =>
        {
            entity.HasIndex(e => e.Clave, "IX_ParametrosPlanilla_Clave").IsUnique();

            entity.Property(e => e.Clave).HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.Property(e => e.Valor).HasPrecision(14, 4);
        });

        modelBuilder.Entity<PermisoRol>(entity =>
        {
            entity.HasIndex(e => new { e.RolId, e.ModuloNumero }, "IX_PermisosRoles_RolId_ModuloNumero").IsUnique();

            entity.HasOne(d => d.Rol).WithMany(p => p.PermisosRoles).HasForeignKey(d => d.RolId);
        });

        modelBuilder.Entity<Planilla>(entity =>
        {
            entity.HasIndex(e => new { e.Anio, e.Mes, e.Tipo }, "IX_Planillas_Anio_Mes_Tipo").IsUnique();

            entity.HasIndex(e => e.Numero, "IX_Planillas_Numero").IsUnique();

            entity.Property(e => e.AprobadoPor).HasMaxLength(100);
            entity.Property(e => e.CalculadoPor).HasMaxLength(100);
            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Tipo).HasMaxLength(20);
            entity.Property(e => e.TotalAportesEmpleador).HasPrecision(12, 2);
            entity.Property(e => e.TotalDescuentos).HasPrecision(12, 2);
            entity.Property(e => e.TotalIngresos).HasPrecision(12, 2);
            entity.Property(e => e.TotalNeto).HasPrecision(12, 2);
        });

        modelBuilder.Entity<PlanillaDetalle>(entity =>
        {
            entity.HasIndex(e => e.ContratoLaboralId, "IX_PlanillaDetalles_ContratoLaboralId");

            entity.HasIndex(e => e.EmpleadoId, "IX_PlanillaDetalles_EmpleadoId");

            entity.HasIndex(e => e.NumeroBoleta, "IX_PlanillaDetalles_NumeroBoleta").IsUnique();

            entity.HasIndex(e => new { e.PlanillaId, e.EmpleadoId }, "IX_PlanillaDetalles_PlanillaId_EmpleadoId").IsUnique();

            entity.Property(e => e.AporteEssalud).HasPrecision(12, 2);
            entity.Property(e => e.Cargo).HasMaxLength(100);
            entity.Property(e => e.CodigoPension).HasMaxLength(20);
            entity.Property(e => e.DescuentoPension).HasPrecision(12, 2);
            entity.Property(e => e.DescuentoPrestamos).HasPrecision(12, 2);
            entity.Property(e => e.DescuentoQuinta).HasPrecision(12, 2);
            entity.Property(e => e.Incidencia).HasMaxLength(300);
            entity.Property(e => e.IngresoBasico).HasPrecision(12, 2);
            entity.Property(e => e.IngresoBonos).HasPrecision(12, 2);
            entity.Property(e => e.IngresoHorasExtra).HasPrecision(12, 2);
            entity.Property(e => e.Neto).HasPrecision(12, 2);
            entity.Property(e => e.NumeroBoleta).HasMaxLength(30);
            entity.Property(e => e.SueldoBasico).HasPrecision(12, 2);
            entity.Property(e => e.TotalDescuentos).HasPrecision(12, 2);
            entity.Property(e => e.TotalIngresos).HasPrecision(12, 2);

            entity.HasOne(d => d.ContratoLaboral).WithMany(p => p.PlanillaDetalles)
                .HasForeignKey(d => d.ContratoLaboralId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Empleado).WithMany(p => p.PlanillaDetalles)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Planilla).WithMany(p => p.Detalles)
                .HasForeignKey(d => d.PlanillaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Prestamo>(entity =>
        {
            entity.HasIndex(e => e.EmpleadoId, "IX_Prestamos_EmpleadoId");

            entity.HasIndex(e => e.Numero, "IX_Prestamos_Numero").IsUnique();

            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Monto).HasPrecision(12, 2);
            entity.Property(e => e.Motivo).HasMaxLength(300);
            entity.Property(e => e.MotivoAnulacion).HasMaxLength(300);
            entity.Property(e => e.Numero).HasMaxLength(20);
            entity.Property(e => e.Saldo).HasPrecision(12, 2);
            entity.Property(e => e.Tipo).HasMaxLength(20);

            entity.HasOne(d => d.Empleado).WithMany(p => p.Prestamos)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.Property(e => e.Precio).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProgramacionCurso>(entity =>
        {
            entity.HasIndex(e => e.Anio, "IX_ProgramacionesCurso_Anio");

            entity.HasIndex(e => e.CursoId, "IX_ProgramacionesCurso_CursoId");

            entity.Property(e => e.Estado).HasMaxLength(20);
            entity.Property(e => e.Lugar).HasMaxLength(150);

            entity.HasOne(d => d.Curso).WithMany(p => p.ProgramacionesCurso)
                .HasForeignKey(d => d.CursoId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<RutaDespacho>(entity =>
        {
            entity.HasIndex(e => e.Zona, "IX_RutasDespacho_Zona");

            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Destino).HasMaxLength(120);
            entity.Property(e => e.DistanciaKm).HasPrecision(10, 2);
            entity.Property(e => e.Moneda).HasMaxLength(3);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Origen).HasMaxLength(120);
            entity.Property(e => e.PeajeEstimado).HasPrecision(18, 2);
            entity.Property(e => e.Zona).HasMaxLength(100);
        });

        modelBuilder.Entity<RutaProduccion>(entity =>
        {
            entity.HasIndex(e => new { e.ProductoId, e.Estado }, "IX_RutasProduccion_ProductoId_Activa")
                .IsUnique()
                .HasFilter("((\"Estado\")::text = 'Activa'::text)");

            entity.HasIndex(e => new { e.ProductoId, e.Version }, "IX_RutasProduccion_ProductoId_Version").IsUnique();

            entity.Property(e => e.Estado).HasMaxLength(8);

            entity.HasOne(d => d.Producto).WithMany(p => p.RutasProduccion)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<TramoQuintaCategoria>(entity =>
        {
            entity.HasIndex(e => e.Orden, "IX_TramosQuintaCategoria_Orden").IsUnique();

            entity.Property(e => e.HastaUit).HasPrecision(8, 2);
            entity.Property(e => e.Tasa).HasPrecision(6, 4);
        });

        modelBuilder.Entity<TransportistaTercero>(entity =>
        {
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Contacto).HasMaxLength(100);
            entity.Property(e => e.DocumentoFiscal).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            entity.Property(e => e.Pais).HasMaxLength(100);
            entity.Property(e => e.RazonSocial).HasMaxLength(200);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.TipoServicio).HasMaxLength(10);
        });

        modelBuilder.Entity<Turno>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "IX_Turnos_Codigo").IsUnique();

            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.DiasLaborables).HasMaxLength(13);
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Tipo).HasMaxLength(20);
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
