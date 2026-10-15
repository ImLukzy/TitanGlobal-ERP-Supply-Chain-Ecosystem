import type { ComponentType } from 'react';
import { ClientesPage } from './clientes/ClientesPage';
import { OrdenesCompraPage } from './ordenes-compra/OrdenesCompraPage';
import { ProductosPage } from './productos/ProductosPage';
import { ProveedoresPage } from './proveedores/ProveedoresPage';
import { SolicitudesCompraPage } from './solicitudes-compra/SolicitudesCompraPage';

export const pantallas: Record<string, ComponentType> = {
  '01-clientes': ClientesPage,
  '04-ordenes-compra': OrdenesCompraPage,
  '11-proveedores': ProveedoresPage,
  '13-solicitudes-compra': SolicitudesCompraPage,
  '21-productos': ProductosPage,
};
