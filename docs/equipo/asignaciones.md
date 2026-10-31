# Asignaciones de módulos

Reparto de los 80 módulos entre **7 personas**: 13 para Lukas (todos terminados, más la integración) y 11 o 12 para cada una de las otras seis. Sustituye al reparto de 4 personas del 2026-10-15. Se asignan por bloques de un mismo dominio para que las dependencias entre módulos queden dentro de la misma persona y nadie toque las tablas de otro. Si dos personas quieren intercambiar módulos, lo acuerdan con Lukas y se cambia aquí.

Primer paso de cada módulo: escribir su spec (ver `docs/guia-companeros.md` §4) y pedir a Lukas que la revise antes de programar. Un módulo lo lleva una sola persona de principio a fin (backend y pantalla).

## Resumen por persona
| Persona | Rol | Módulos | Bloque |
|---|---|---|---|
| Lukas Melgar | integración: revisa PR, integra en `main` y despliega la demo | 13 | Comercial (01, 02, 03, 04, 05, 06, 07), Compras (11, 13, 15), Inventarios (21, 23, 24). Todos terminados |
| Leonel Portillo | desarrollo | 11 | Finanzas completo (51–60) + 75 ImpuestosTasas |
| Antony Huayhua | desarrollo | 11 | Manufactura completo (31–40) + 79 ParametrosSistema |
| Kelvin Pinto | desarrollo | 11 | Logística completo (41–50) + 73 Sucursales |
| Alexander | desarrollo | 11 | Capital Humano completo (61–70) + 71 Usuarios |
| Sergio | desarrollo | 11 | Compras (12, 14, 16, 17, 18, 19, 20) + 72 RolesPermisos, 74 MonedasTiposCambio, 76 SeriesNumeracion, 80 CierresPeriodo |
| Maxi | desarrollo | 12 | Inventarios WMS (22, 25–30) + 08 ComisionesVendedores, 09 ReclamosPostventa, 10 DevolucionesComerciales, 77 AuditoriaTransaccional, 78 NotificacionesAlertas |

## Orden sugerido (por dependencias)
- **Leonel:** 75 antes de 51 (maestro de impuestos que usa la facturación); después 51 → 52 → … → 60.
- **Antony:** 33 → 31 → 32 → 34 → 35 → 36 → 37 → 38 → 39 → 40 (Manufactura); 79 ParametrosSistema al final.
- **Kelvin:** 41 → 42 → 48 → 43 → 44 → 45 → 46 → 47 → 49 → 50; 73 Sucursales al final.
- **Alexander:** 61 → 62 → 64 → 63 → 65 → 66 → 67 → 68 → 69 → 70; 71 Usuarios después de 72 (usa los roles de Sergio).
- **Sergio:** 12 → 14 → 20 → 16 → 17 → 18 → 19; después 72 → 74 → 76 → 80 (80 CierresPeriodo depende de Finanzas).
- **Maxi:** primero las pantallas pendientes de 09, 10, 08, 77 y 78 (ramas `pendiente/…`, ver [`relevo.md`](relevo.md)); después 22 → 25 → 26 → 27 → 28 → 29 → 30.
- **Lukas:** sus 13 módulos están terminados; ahora hace la integración.

## Todos los módulos
| Módulo | Dominio | Responsable | Estado | Specs |
|---|---|---|---|---|
| 01 Clientes | Comercial, Ventas y CRM | Lukas Melgar | terminado | 07, 08 |
| 02 SegmentacionClientes | Comercial, Ventas y CRM | Lukas Melgar | terminado | 23, 24 |
| 03 Cotizaciones | Comercial, Ventas y CRM | Lukas Melgar | terminado | 25, 26 |
| 04 OrdenesCompra | Comercial, Ventas y CRM | Lukas Melgar | terminado | 01–06 |
| 05 ContratosComerciales | Comercial, Ventas y CRM | Lukas Melgar | terminado | 27, 28 |
| 06 ListasPrecios | Comercial, Ventas y CRM | Lukas Melgar | terminado | 29, 30 |
| 07 PromocionesDescuentos | Comercial, Ventas y CRM | Lukas Melgar | terminado | 31, 32 |
| 08 ComisionesVendedores | Comercial, Ventas y CRM | Maxi | backend terminado; pantalla en rama `pendiente/08-comisiones-vendedores` (falta QA) | 39, 40 |
| 09 ReclamosPostventa | Comercial, Ventas y CRM | Maxi | backend terminado; pantalla en rama `pendiente/09-reclamos-postventa` (falta QA) | 33, 34 |
| 10 DevolucionesComerciales | Comercial, Ventas y CRM | Maxi | backend terminado; pantalla en rama `pendiente/10-devoluciones-comerciales` (falta QA) | 35, 36 |
| 11 Proveedores | Compras y Abastecimiento Global | Lukas Melgar | terminado | 12, 13 |
| 12 EvaluacionProveedores | Compras y Abastecimiento Global | Sergio | pendiente | — |
| 13 SolicitudesCompra | Compras y Abastecimiento Global | Lukas Melgar | terminado | 14, 15 |
| 14 Licitaciones | Compras y Abastecimiento Global | Sergio | pendiente | — |
| 15 OrdenesAbastecimiento | Compras y Abastecimiento Global | Lukas Melgar | terminado | 16, 17, 18 |
| 16 Importaciones | Compras y Abastecimiento Global | Sergio | pendiente | — |
| 17 AgentesAduaneros | Compras y Abastecimiento Global | Sergio | pendiente | — |
| 18 CosteoImportacion | Compras y Abastecimiento Global | Sergio | pendiente | — |
| 19 RecepcionMercaderia | Compras y Abastecimiento Global | Sergio | pendiente | — |
| 20 AcuerdosAbastecimiento | Compras y Abastecimiento Global | Sergio | pendiente | — |
| 21 Productos | Inventarios y Almacenes (WMS) | Lukas Melgar | terminado | 09, 10 |
| 22 CategoriasProducto | Inventarios y Almacenes (WMS) | Maxi | pendiente | — |
| 23 Almacenes | Inventarios y Almacenes (WMS) | Lukas Melgar | terminado | 19, 20 |
| 24 ZonasAlmacenaje | Inventarios y Almacenes (WMS) | Lukas Melgar | terminado | 21, 22 |
| 25 LotesSeries | Inventarios y Almacenes (WMS) | Maxi | pendiente | — |
| 26 KardexValorizado | Inventarios y Almacenes (WMS) | Maxi | pendiente | — |
| 27 TransferenciasAlmacen | Inventarios y Almacenes (WMS) | Maxi | pendiente | — |
| 28 TomaInventario | Inventarios y Almacenes (WMS) | Maxi | pendiente | — |
| 29 AjustesInventario | Inventarios y Almacenes (WMS) | Maxi | pendiente | — |
| 30 ReabastecimientoAutomatico | Inventarios y Almacenes (WMS) | Maxi | pendiente | — |
| 31 ListasMateriales | Manufactura y Producción (MRP) | Antony Huayhua | pendiente | — |
| 32 RutasProduccion | Manufactura y Producción (MRP) | Antony Huayhua | pendiente | — |
| 33 CentrosTrabajo | Manufactura y Producción (MRP) | Antony Huayhua | pendiente | — |
| 34 OrdenesProduccion | Manufactura y Producción (MRP) | Antony Huayhua | pendiente | — |
| 35 ConsumoMateriales | Manufactura y Producción (MRP) | Antony Huayhua | pendiente | — |
| 36 ControlCalidadInsumos | Manufactura y Producción (MRP) | Antony Huayhua | pendiente | — |
| 37 ControlCalidadProductoTerminado | Manufactura y Producción (MRP) | Antony Huayhua | pendiente | — |
| 38 MermasProduccion | Manufactura y Producción (MRP) | Antony Huayhua | pendiente | — |
| 39 MantenimientoPreventivo | Manufactura y Producción (MRP) | Antony Huayhua | pendiente | — |
| 40 MantenimientoCorrectivo | Manufactura y Producción (MRP) | Antony Huayhua | pendiente | — |
| 41 VehiculosFlota | Logística, Flota y Distribución (TMS) | Kelvin Pinto | pendiente | — |
| 42 Conductores | Logística, Flota y Distribución (TMS) | Kelvin Pinto | pendiente | — |
| 43 RutasDespacho | Logística, Flota y Distribución (TMS) | Kelvin Pinto | pendiente | — |
| 44 ConsolidacionCarga | Logística, Flota y Distribución (TMS) | Kelvin Pinto | pendiente | — |
| 45 GuiasRemision | Logística, Flota y Distribución (TMS) | Kelvin Pinto | pendiente | — |
| 46 MonitoreoEntregas | Logística, Flota y Distribución (TMS) | Kelvin Pinto | pendiente | — |
| 47 ControlCombustible | Logística, Flota y Distribución (TMS) | Kelvin Pinto | pendiente | — |
| 48 TransportistasTerceros | Logística, Flota y Distribución (TMS) | Kelvin Pinto | pendiente | — |
| 49 LogisticaInversa | Logística, Flota y Distribución (TMS) | Kelvin Pinto | pendiente | — |
| 50 SiniestrosFlota | Logística, Flota y Distribución (TMS) | Kelvin Pinto | pendiente | — |
| 51 FacturacionElectronica | Finanzas, Contabilidad y Tesorería | Leonel Portillo | pendiente | — |
| 52 NotasCreditoDebito | Finanzas, Contabilidad y Tesorería | Leonel Portillo | pendiente | — |
| 53 CuentasPorCobrar | Finanzas, Contabilidad y Tesorería | Leonel Portillo | pendiente | — |
| 54 CuentasPorPagar | Finanzas, Contabilidad y Tesorería | Leonel Portillo | pendiente | — |
| 55 TesoreriaBancos | Finanzas, Contabilidad y Tesorería | Leonel Portillo | pendiente | — |
| 56 ConciliacionBancaria | Finanzas, Contabilidad y Tesorería | Leonel Portillo | pendiente | — |
| 57 CajaChica | Finanzas, Contabilidad y Tesorería | Leonel Portillo | pendiente | — |
| 58 Presupuestos | Finanzas, Contabilidad y Tesorería | Leonel Portillo | pendiente | — |
| 59 CentrosCosto | Finanzas, Contabilidad y Tesorería | Leonel Portillo | pendiente | — |
| 60 ActivosFijos | Finanzas, Contabilidad y Tesorería | Leonel Portillo | pendiente | — |
| 61 Empleados | Capital Humano y Nóminas (HCM) | Alexander | pendiente | — |
| 62 ContratosLaborales | Capital Humano y Nóminas (HCM) | Alexander | pendiente | — |
| 63 AsistenciaBiometrica | Capital Humano y Nóminas (HCM) | Alexander | pendiente | — |
| 64 TurnosHorarios | Capital Humano y Nóminas (HCM) | Alexander | pendiente | — |
| 65 VacacionesPermisos | Capital Humano y Nóminas (HCM) | Alexander | pendiente | — |
| 66 Planillas | Capital Humano y Nóminas (HCM) | Alexander | pendiente | — |
| 67 AdelantosPrestamos | Capital Humano y Nóminas (HCM) | Alexander | pendiente | — |
| 68 EvaluacionDesempeno | Capital Humano y Nóminas (HCM) | Alexander | pendiente | — |
| 69 Capacitaciones | Capital Humano y Nóminas (HCM) | Alexander | pendiente | — |
| 70 SeguridadSaludOcupacional | Capital Humano y Nóminas (HCM) | Alexander | pendiente | — |
| 71 Usuarios | Gobierno Corporativo, Seguridad y Auditoría | Alexander | backend terminado, pantalla pendiente | 43, 44 |
| 72 RolesPermisos | Gobierno Corporativo, Seguridad y Auditoría | Sergio | backend terminado, pantalla pendiente | 41, 42 |
| 73 Sucursales | Gobierno Corporativo, Seguridad y Auditoría | Kelvin Pinto | pendiente | — |
| 74 MonedasTiposCambio | Gobierno Corporativo, Seguridad y Auditoría | Sergio | pendiente | — |
| 75 ImpuestosTasas | Gobierno Corporativo, Seguridad y Auditoría | Leonel Portillo | pendiente | — |
| 76 SeriesNumeracion | Gobierno Corporativo, Seguridad y Auditoría | Sergio | pendiente | — |
| 77 AuditoriaTransaccional | Gobierno Corporativo, Seguridad y Auditoría | Maxi | backend terminado; pantalla en rama `pendiente/77-auditoria-transaccional` (falta QA) | 45, 46 |
| 78 NotificacionesAlertas | Gobierno Corporativo, Seguridad y Auditoría | Maxi | backend terminado; pantalla en rama `pendiente/78-notificaciones-alertas` (falta QA) | 37, 38 |
| 79 ParametrosSistema | Gobierno Corporativo, Seguridad y Auditoría | Antony Huayhua | pendiente | — |
| 80 CierresPeriodo | Gobierno Corporativo, Seguridad y Auditoría | Sergio | pendiente | — |
