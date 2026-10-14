import type { ComponentType } from 'react';
import { OrdenesCompraPage } from './ordenes-compra/OrdenesCompraPage';

export const pantallas: Record<string, ComponentType> = {
  '04-ordenes-compra': OrdenesCompraPage,
};
