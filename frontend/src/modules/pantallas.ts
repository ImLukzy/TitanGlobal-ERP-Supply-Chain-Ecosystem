import type { ComponentType } from 'react';
import { ClientesPage } from './clientes/ClientesPage';
import { OrdenesCompraPage } from './ordenes-compra/OrdenesCompraPage';
import { ProductosPage } from './productos/ProductosPage';
import { ProveedoresPage } from './proveedores/ProveedoresPage';

export const pantallas: Record<string, ComponentType> = {
  '01-clientes': ClientesPage,
  '04-ordenes-compra': OrdenesCompraPage,
  '11-proveedores': ProveedoresPage,
  '21-productos': ProductosPage,
};
