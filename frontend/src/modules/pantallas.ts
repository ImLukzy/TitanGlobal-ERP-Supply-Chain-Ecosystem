import type { ComponentType } from 'react';
import { ClientesPage } from './clientes/ClientesPage';
import { OrdenesCompraPage } from './ordenes-compra/OrdenesCompraPage';
import { ProductosPage } from './productos/ProductosPage';
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

export const pantallas: Record<string, ComponentType> = {
  '01-clientes': ClientesPage,
  '02-segmentacion-clientes': SegmentacionClientesPage,
  '03-cotizaciones': CotizacionesPage,
  '05-contratos-comerciales': ContratosComercialesPage,
  '06-listas-precios': ListasPreciosPage,
  '07-promociones-descuentos': PromocionesPage,
  '08-comisiones-vendedores': ComisionesVendedoresPage,
  '04-ordenes-compra': OrdenesCompraPage,
  '11-proveedores': ProveedoresPage,
  '13-solicitudes-compra': SolicitudesCompraPage,
  '15-ordenes-abastecimiento': OrdenesAbastecimientoPage,
  '21-productos': ProductosPage,
  '23-almacenes': AlmacenesPage,
  '24-zonas-almacenaje': ZonasAlmacenajePage,
  '09-reclamos-postventa': ReclamosPostventaPage,
};
