import type { ComponentType } from 'react';
import { ClientesPage } from './clientes/ClientesPage';
import { OrdenesCompraPage } from './ordenes-compra/OrdenesCompraPage';
import { ProductosPage } from './productos/ProductosPage';
import { CategoriasProductoPage } from './categorias-producto/CategoriasProductoPage';
import { ProveedoresPage } from './proveedores/ProveedoresPage';
import { SolicitudesCompraPage } from './solicitudes-compra/SolicitudesCompraPage';
import { OrdenesAbastecimientoPage } from './ordenes-abastecimiento/OrdenesAbastecimientoPage';
import { AlmacenesPage } from './almacenes/AlmacenesPage';
import { ZonasAlmacenajePage } from './zonas-almacenaje/ZonasAlmacenajePage';
import { SegmentacionClientesPage } from './segmentacion-clientes/SegmentacionClientesPage';
import { CotizacionesPage } from './cotizaciones/CotizacionesPage';
import { ContratosComercialesPage } from './contratos-comerciales/ContratosComercialesPage';
import { ListasPreciosPage } from './listas-precios/ListasPreciosPage';
import { PromocionesPage } from './promociones-descuentos/PromocionesPage';
import { ComisionesVendedoresPage } from './comisiones-vendedores/ComisionesVendedoresPage';
import { ReclamosPostventaPage } from './reclamos-postventa/ReclamosPostventaPage';
import { DevolucionesComercialesPage } from './devoluciones-comerciales/DevolucionesComercialesPage';
import { AuditoriaPage } from './auditoria-transaccional/AuditoriaPage';
import { NotificacionesAlertasPage } from './notificaciones-alertas/NotificacionesAlertasPage';
import { RolesPermisosPage } from './roles-permisos/RolesPermisosPage';
import { UsuariosPage } from './usuarios/UsuariosPage';
import { EmpleadosPage } from './empleados/EmpleadosPage';
import { CentrosTrabajoPage } from './centros-trabajo/CentrosTrabajoPage';
import { VehiculosFlotaPage } from './vehiculos-flota/VehiculosFlotaPage';
import { ConductoresPage } from './conductores/ConductoresPage';
import { TransportistasTercerosPage } from './transportistas-terceros/TransportistasTercerosPage';
import { ContratosLaboralesPage } from './contratos-laborales/ContratosLaboralesPage';
import { TurnosHorariosPage } from './turnos-horarios/TurnosHorariosPage';
import { AsistenciaBiometricaPage } from './asistencia-biometrica/AsistenciaBiometricaPage';
import { VacacionesPermisosPage } from './vacaciones-permisos/VacacionesPermisosPage';
import { AdelantosPrestamosPage } from './adelantos-prestamos/AdelantosPrestamosPage';
import { PlanillasPage } from './planillas/PlanillasPage';
import { EvaluacionDesempenoPage } from './evaluacion-desempeno/EvaluacionDesempenoPage';
import { CapacitacionesPage } from './capacitaciones/CapacitacionesPage';
import { SeguridadSaludOcupacionalPage } from './seguridad-salud-ocupacional/SeguridadSaludOcupacionalPage';

export const pantallas: Record<string, ComponentType> = {
  '01-clientes': ClientesPage,
  '02-segmentacion-clientes': SegmentacionClientesPage,
  '03-cotizaciones': CotizacionesPage,
  '05-contratos-comerciales': ContratosComercialesPage,
  '06-listas-precios': ListasPreciosPage,
  '07-promociones-descuentos': PromocionesPage,
  '08-comisiones-vendedores': ComisionesVendedoresPage,
  '10-devoluciones-comerciales': DevolucionesComercialesPage,
  '77-auditoria-transaccional': AuditoriaPage,
  '04-ordenes-compra': OrdenesCompraPage,
  '11-proveedores': ProveedoresPage,
  '13-solicitudes-compra': SolicitudesCompraPage,
  '15-ordenes-abastecimiento': OrdenesAbastecimientoPage,
  '21-productos': ProductosPage,
  '22-categorias-producto': CategoriasProductoPage,
  '23-almacenes': AlmacenesPage,
  '24-zonas-almacenaje': ZonasAlmacenajePage,
  '09-reclamos-postventa': ReclamosPostventaPage,
  '78-notificaciones-alertas': NotificacionesAlertasPage,
  '72-roles-permisos': RolesPermisosPage,
  '71-usuarios': UsuariosPage,
  '61-empleados': EmpleadosPage,
  '33-centros-trabajo': CentrosTrabajoPage,
  '41-vehiculos-flota': VehiculosFlotaPage,
  '42-conductores': ConductoresPage,
  '48-transportistas-terceros': TransportistasTercerosPage,
  '62-contratos-laborales': ContratosLaboralesPage,
  '64-turnos-horarios': TurnosHorariosPage,
  '63-asistencia-biometrica': AsistenciaBiometricaPage,
  '65-vacaciones-permisos': VacacionesPermisosPage,
  '67-adelantos-prestamos': AdelantosPrestamosPage,
  '66-planillas': PlanillasPage,
  '68-evaluacion-desempeno': EvaluacionDesempenoPage,
  '69-capacitaciones': CapacitacionesPage,
  '70-seguridad-salud-ocupacional': SeguridadSaludOcupacionalPage,
};
