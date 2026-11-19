export interface Modulo {
  numero: number;
  nombre: string;
  titulo: string;
  slug: string;
  dominio: string;
}

export interface Dominio {
  nombre: string;
  modulos: Modulo[];
}

const nombresPorDominio: { nombre: string; modulos: string[] }[] = [
  {
    nombre: 'Comercial, Ventas y CRM',
    modulos: ['Clientes', 'SegmentacionClientes', 'Cotizaciones', 'OrdenesCompra', 'ContratosComerciales', 'ListasPrecios', 'PromocionesDescuentos', 'ComisionesVendedores', 'ReclamosPostventa', 'DevolucionesComerciales'],
  },
  {
    nombre: 'Compras y Abastecimiento Global',
    modulos: ['Proveedores', 'EvaluacionProveedores', 'SolicitudesCompra', 'Licitaciones', 'OrdenesAbastecimiento', 'Importaciones', 'AgentesAduaneros', 'CosteoImportacion', 'RecepcionMercaderia', 'AcuerdosAbastecimiento'],
  },
  {
    nombre: 'Inventarios y Almacenes — WMS',
    modulos: ['Productos', 'CategoriasProducto', 'Almacenes', 'ZonasAlmacenaje', 'LotesSeries', 'KardexValorizado', 'TransferenciasAlmacen', 'TomaInventario', 'AjustesInventario', 'ReabastecimientoAutomatico'],
  },
  {
    nombre: 'Manufactura y Producción — MRP',
    modulos: ['ListasMateriales', 'RutasProduccion', 'CentrosTrabajo', 'OrdenesProduccion', 'ConsumoMateriales', 'ControlCalidadInsumos', 'ControlCalidadProductoTerminado', 'MermasProduccion', 'MantenimientoPreventivo', 'MantenimientoCorrectivo'],
  },
  {
    nombre: 'Logística, Flota y Distribución — TMS',
    modulos: ['VehiculosFlota', 'Conductores', 'RutasDespacho', 'ConsolidacionCarga', 'GuiasRemision', 'MonitoreoEntregas', 'ControlCombustible', 'TransportistasTerceros', 'LogisticaInversa', 'SiniestrosFlota'],
  },
  {
    nombre: 'Finanzas, Contabilidad y Tesorería',
    modulos: ['FacturacionElectronica', 'NotasCreditoDebito', 'CuentasPorCobrar', 'CuentasPorPagar', 'TesoreriaBancos', 'ConciliacionBancaria', 'CajaChica', 'Presupuestos', 'CentrosCosto', 'ActivosFijos'],
  },
  {
    nombre: 'Capital Humano y Nóminas — HCM',
    modulos: ['Empleados', 'ContratosLaborales', 'AsistenciaBiometrica', 'TurnosHorarios', 'VacacionesPermisos', 'Planillas', 'AdelantosPrestamos', 'EvaluacionDesempeno', 'Capacitaciones', 'SeguridadSaludOcupacional'],
  },
  {
    nombre: 'Gobierno Corporativo, Seguridad y Auditoría',
    modulos: ['Usuarios', 'RolesPermisos', 'Sucursales', 'MonedasTiposCambio', 'ImpuestosTasas', 'SeriesNumeracion', 'AuditoriaTransaccional', 'NotificacionesAlertas', 'ParametrosSistema', 'CierresPeriodo'],
  },
];

// Nombre para mostrar en el menú, la miga de pan y el título de cada pantalla.
const titulos: Record<string, string> = {
  Clientes: 'Clientes',
  SegmentacionClientes: 'Segmentación de clientes',
  Cotizaciones: 'Cotizaciones',
  OrdenesCompra: 'Órdenes de compra',
  ContratosComerciales: 'Contratos comerciales',
  ListasPrecios: 'Listas de precios',
  PromocionesDescuentos: 'Promociones y descuentos',
  ComisionesVendedores: 'Comisiones de vendedores',
  ReclamosPostventa: 'Reclamos postventa',
  DevolucionesComerciales: 'Devoluciones comerciales',
  Proveedores: 'Proveedores',
  EvaluacionProveedores: 'Evaluación de proveedores',
  SolicitudesCompra: 'Solicitudes de compra',
  Licitaciones: 'Licitaciones',
  OrdenesAbastecimiento: 'Órdenes de abastecimiento',
  Importaciones: 'Importaciones',
  AgentesAduaneros: 'Agentes aduaneros',
  CosteoImportacion: 'Costeo de importación',
  RecepcionMercaderia: 'Recepción de mercadería',
  AcuerdosAbastecimiento: 'Acuerdos de abastecimiento',
  Productos: 'Productos',
  CategoriasProducto: 'Categorías de producto',
  Almacenes: 'Almacenes',
  ZonasAlmacenaje: 'Zonas de almacenaje',
  LotesSeries: 'Lotes y series',
  KardexValorizado: 'Kárdex valorizado',
  TransferenciasAlmacen: 'Transferencias entre almacenes',
  TomaInventario: 'Toma de inventario',
  AjustesInventario: 'Ajustes de inventario',
  ReabastecimientoAutomatico: 'Reabastecimiento automático',
  ListasMateriales: 'Listas de materiales',
  RutasProduccion: 'Rutas de producción',
  CentrosTrabajo: 'Centros de trabajo',
  OrdenesProduccion: 'Órdenes de producción',
  ConsumoMateriales: 'Consumo de materiales',
  ControlCalidadInsumos: 'Control de calidad de insumos',
  ControlCalidadProductoTerminado: 'Control de calidad de producto terminado',
  MermasProduccion: 'Mermas de producción',
  MantenimientoPreventivo: 'Mantenimiento preventivo',
  MantenimientoCorrectivo: 'Mantenimiento correctivo',
  VehiculosFlota: 'Vehículos de flota',
  Conductores: 'Conductores',
  RutasDespacho: 'Rutas de despacho',
  ConsolidacionCarga: 'Consolidación de carga',
  GuiasRemision: 'Guías de remisión',
  MonitoreoEntregas: 'Monitoreo de entregas',
  ControlCombustible: 'Control de combustible',
  TransportistasTerceros: 'Transportistas terceros',
  LogisticaInversa: 'Logística inversa',
  SiniestrosFlota: 'Siniestros de flota',
  FacturacionElectronica: 'Facturación electrónica',
  NotasCreditoDebito: 'Notas de crédito y débito',
  CuentasPorCobrar: 'Cuentas por cobrar',
  CuentasPorPagar: 'Cuentas por pagar',
  TesoreriaBancos: 'Tesorería y bancos',
  ConciliacionBancaria: 'Conciliación bancaria',
  CajaChica: 'Caja chica',
  Presupuestos: 'Presupuestos',
  CentrosCosto: 'Centros de costo',
  ActivosFijos: 'Activos fijos',
  Empleados: 'Empleados',
  ContratosLaborales: 'Contratos laborales',
  AsistenciaBiometrica: 'Asistencia biométrica',
  TurnosHorarios: 'Turnos y horarios',
  VacacionesPermisos: 'Vacaciones y permisos',
  Planillas: 'Planillas',
  AdelantosPrestamos: 'Adelantos y préstamos',
  EvaluacionDesempeno: 'Evaluación de desempeño',
  Capacitaciones: 'Capacitaciones',
  SeguridadSaludOcupacional: 'Seguridad y salud ocupacional',
  Usuarios: 'Usuarios',
  RolesPermisos: 'Roles y permisos',
  Sucursales: 'Sucursales',
  MonedasTiposCambio: 'Monedas y tipos de cambio',
  ImpuestosTasas: 'Impuestos y tasas',
  SeriesNumeracion: 'Series de numeración',
  AuditoriaTransaccional: 'Auditoría transaccional',
  NotificacionesAlertas: 'Notificaciones y alertas',
  ParametrosSistema: 'Parámetros del sistema',
  CierresPeriodo: 'Cierres de periodo',
};

function aKebab(texto: string): string {
  return texto
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .toLowerCase();
}

export const modulos: Modulo[] = nombresPorDominio.flatMap((dominio, indiceDominio) =>
  dominio.modulos.map((nombre, indiceModulo) => {
    const numero = indiceDominio * 10 + indiceModulo + 1;
    const numeroConCero = String(numero).padStart(2, '0');
    return {
      numero,
      nombre,
      titulo: titulos[nombre] ?? nombre,
      slug: `${numeroConCero}-${aKebab(nombre)}`,
      dominio: dominio.nombre,
    };
  }),
);

export const dominios: Dominio[] = nombresPorDominio.map((dominio) => ({
  nombre: dominio.nombre,
  modulos: modulos.filter((modulo) => modulo.dominio === dominio.nombre),
}));
