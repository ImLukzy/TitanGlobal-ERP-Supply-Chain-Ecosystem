import type { ComponentType } from 'react';
import { ClientesPage } from './clientes/ClientesPage';
import { OrdenesCompraPage } from './ordenes-compra/OrdenesCompraPage';
import { ProductosPage } from './productos/ProductosPage';

export const pantallas: Record<string, ComponentType> = {
  '01-clientes': ClientesPage,
  '04-ordenes-compra': OrdenesCompraPage,
  '21-productos': ProductosPage,
};
