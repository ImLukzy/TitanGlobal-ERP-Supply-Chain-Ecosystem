export interface Modulo {
  numero: number;
  nombre: string;
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
      slug: `${numeroConCero}-${aKebab(nombre)}`,
      dominio: dominio.nombre,
    };
  }),
);

export const dominios: Dominio[] = nombresPorDominio.map((dominio) => ({
  nombre: dominio.nombre,
  modulos: modulos.filter((modulo) => modulo.dominio === dominio.nombre),
}));
