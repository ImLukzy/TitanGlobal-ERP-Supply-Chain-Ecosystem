# Asignaciones de módulos

Reparto equitativo de los 80 módulos: **20 por persona**, contando los que ya están hechos. Se asignan por bloques de un mismo dominio para que las dependencias entre módulos queden dentro de la misma persona y nadie toque las tablas de otro. Decidido por Lukas el 2026-10-15; si dos personas quieren intercambiar módulos, lo acuerdan con Lukas y se cambia aquí.

Primer paso de cada módulo: escribir su spec (ver `docs/guia-companeros.md` §4) y pedir a Lukas que la revise antes de programar. Un módulo lo lleva una sola persona de principio a fin (backend y pantalla).

## Resumen por persona
| Persona | Rol | Módulos | Bloque |
|---|---|---|---|
| Lukas Melgar | integrador: revisa specs y PR, integra en `ImLukzy/Claudio` y despliega la demo (con su equipo de agentes) | 20 | 8 módulos ya hechos o en curso por el equipo Claude (01, 04, 11, 13, 15, 21, 23, 24) + el resto de Comercial (02, 03, 05–10) + seguridad y auditoría (71 Usuarios, 72 RolesPermisos, 77 AuditoriaTransaccional, 78 NotificacionesAlertas), que tocan el middleware de roles |
| Leonel Portillo | desarrollo | 20 | Compras (12, 14, 16, 17, 18, 20) + Finanzas completo (51–60) + maestros financieros de Gobierno (73 Sucursales, 74 MonedasTiposCambio, 75 ImpuestosTasas, 76 SeriesNumeracion) |
| Antony Huayhua | desarrollo | 20 | Inventarios (22, 25–30) + Manufactura completo (31–40) + 70 SeguridadSaludOcupacional (planta) + 79 ParametrosSistema y 80 CierresPeriodo |
| Kelvin Pinto | desarrollo | 20 | 19 RecepcionMercaderia + Logística completo (41–50) + Capital Humano (61–69) |

## Orden sugerido (por dependencias)
- **Leonel:** 12 → 14 → 20 → 16 → 17 → 18; después 73–76 (maestros que usa Finanzas) y 51–60.
- **Antony:** 22 → 25 → 26 → 27 → 28 → 29 → 30; después 33 → 31 → 32 → 34 → 35… (Manufactura); 70, 79 y 80 al final (80 CierresPeriodo depende de Finanzas).
- **Kelvin:** 19 (depende de 15 y 23, ya hechos); 41 → 42 → 48 → 43 → 44 → 45 → 46 → 47 → 49 → 50; 61 → 62 → 64 → 63 → 65 → 66 → 67 → 68 → 69.
- **Lukas:** termina 23 y 24; después 02, 03, 05–10, y 71 → 72 → 77 → 78.

## Todos los módulos
| Módulo | Dominio | Responsable | Estado | Specs |
|---|---|---|---|---|
| 01 Clientes | Comercial, Ventas y CRM | Lukas Melgar (equipo Claude) | terminado | 07, 08 |
| 02 SegmentacionClientes | Comercial, Ventas y CRM | Lukas Melgar (equipo Claude) | terminado | 23, 24 |
| 03 Cotizaciones | Comercial, Ventas y CRM | Lukas Melgar (equipo Claude) | backend terminado, pantalla pendiente | 25, 26 |
| 04 OrdenesCompra | Comercial, Ventas y CRM | Lukas Melgar (equipo Claude) | terminado | 01–06 |
| 05 ContratosComerciales | Comercial, Ventas y CRM | Lukas Melgar (equipo Claude) | backend terminado, pantalla pendiente | 27, 28 |
| 06 ListasPrecios | Comercial, Ventas y CRM | Lukas Melgar (equipo Claude) | en curso | 29, 30 |
| 07 PromocionesDescuentos | Comercial, Ventas y CRM | Lukas Melgar (equipo Claude) | en curso | 31, 32 |
| 08 ComisionesVendedores | Comercial, Ventas y CRM | Lukas Melgar | pendiente | — |
| 09 ReclamosPostventa | Comercial, Ventas y CRM | Lukas Melgar | pendiente | — |
| 10 DevolucionesComerciales | Comercial, Ventas y CRM | Lukas Melgar | pendiente | — |
| 11 Proveedores | Compras y Abastecimiento Global | Lukas Melgar (equipo Claude) | terminado | 12, 13 |
| 12 EvaluacionProveedores | Compras y Abastecimiento Global | Leonel Portillo | pendiente | — |
| 13 SolicitudesCompra | Compras y Abastecimiento Global | Lukas Melgar (equipo Claude) | terminado | 14, 15 |
| 14 Licitaciones | Compras y Abastecimiento Global | Leonel Portillo | pendiente | — |
| 15 OrdenesAbastecimiento | Compras y Abastecimiento Global | Lukas Melgar (equipo Claude) | terminado | 16, 17, 18 |
| 16 Importaciones | Compras y Abastecimiento Global | Leonel Portillo | pendiente | — |
| 17 AgentesAduaneros | Compras y Abastecimiento Global | Leonel Portillo | pendiente | — |
| 18 CosteoImportacion | Compras y Abastecimiento Global | Leonel Portillo | pendiente | — |
| 19 RecepcionMercaderia | Compras y Abastecimiento Global | Kelvin Pinto | pendiente | — |
| 20 AcuerdosAbastecimiento | Compras y Abastecimiento Global | Leonel Portillo | pendiente | — |
| 21 Productos | Inventarios y Almacenes (WMS) | Lukas Melgar (equipo Claude) | terminado | 09, 10 |
| 22 CategoriasProducto | Inventarios y Almacenes (WMS) | Antony Huayhua | pendiente | — |
| 23 Almacenes | Inventarios y Almacenes (WMS) | Lukas Melgar (equipo Claude) | terminado | 19, 20 |
| 24 ZonasAlmacenaje | Inventarios y Almacenes (WMS) | Lukas Melgar (equipo Claude) | terminado | 21, 22 |
| 25 LotesSeries | Inventarios y Almacenes (WMS) | Antony Huayhua | pendiente | — |
| 26 KardexValorizado | Inventarios y Almacenes (WMS) | Antony Huayhua | pendiente | — |
| 27 TransferenciasAlmacen | Inventarios y Almacenes (WMS) | Antony Huayhua | pendiente | — |
| 28 TomaInventario | Inventarios y Almacenes (WMS) | Antony Huayhua | pendiente | — |
| 29 AjustesInventario | Inventarios y Almacenes (WMS) | Antony Huayhua | pendiente | — |
| 30 ReabastecimientoAutomatico | Inventarios y Almacenes (WMS) | Antony Huayhua | pendiente | — |
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
| 61 Empleados | Capital Humano y Nóminas (HCM) | Kelvin Pinto | pendiente | — |
| 62 ContratosLaborales | Capital Humano y Nóminas (HCM) | Kelvin Pinto | pendiente | — |
| 63 AsistenciaBiometrica | Capital Humano y Nóminas (HCM) | Kelvin Pinto | pendiente | — |
| 64 TurnosHorarios | Capital Humano y Nóminas (HCM) | Kelvin Pinto | pendiente | — |
| 65 VacacionesPermisos | Capital Humano y Nóminas (HCM) | Kelvin Pinto | pendiente | — |
| 66 Planillas | Capital Humano y Nóminas (HCM) | Kelvin Pinto | pendiente | — |
| 67 AdelantosPrestamos | Capital Humano y Nóminas (HCM) | Kelvin Pinto | pendiente | — |
| 68 EvaluacionDesempeno | Capital Humano y Nóminas (HCM) | Kelvin Pinto | pendiente | — |
| 69 Capacitaciones | Capital Humano y Nóminas (HCM) | Kelvin Pinto | pendiente | — |
| 70 SeguridadSaludOcupacional | Capital Humano y Nóminas (HCM) | Antony Huayhua | pendiente | — |
| 71 Usuarios | Gobierno Corporativo, Seguridad y Auditoría | Lukas Melgar | pendiente | — |
| 72 RolesPermisos | Gobierno Corporativo, Seguridad y Auditoría | Lukas Melgar | pendiente | — |
| 73 Sucursales | Gobierno Corporativo, Seguridad y Auditoría | Leonel Portillo | pendiente | — |
| 74 MonedasTiposCambio | Gobierno Corporativo, Seguridad y Auditoría | Leonel Portillo | pendiente | — |
| 75 ImpuestosTasas | Gobierno Corporativo, Seguridad y Auditoría | Leonel Portillo | pendiente | — |
| 76 SeriesNumeracion | Gobierno Corporativo, Seguridad y Auditoría | Leonel Portillo | pendiente | — |
| 77 AuditoriaTransaccional | Gobierno Corporativo, Seguridad y Auditoría | Lukas Melgar | pendiente | — |
| 78 NotificacionesAlertas | Gobierno Corporativo, Seguridad y Auditoría | Lukas Melgar | pendiente | — |
| 79 ParametrosSistema | Gobierno Corporativo, Seguridad y Auditoría | Antony Huayhua | pendiente | — |
| 80 CierresPeriodo | Gobierno Corporativo, Seguridad y Auditoría | Antony Huayhua | pendiente | — |
