import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { OrdenesResumen } from './OrdenesResumen';
import type { ResumenOrdenesAbastecimiento } from './tipos';

const resumen: ResumenOrdenesAbastecimiento = {
  total: 3,
  emitidas: 2,
  recibidas: 1,
  anuladas: 0,
  atrasadas: 0,
  montoPorRecibir: 1000,
  montoRecibido: 2500,
  topProveedores: [
    { proveedorId: 1, razonSocial: 'Andes Suministros S.A.C.', ordenes: 1, monto: 1000 },
    { proveedorId: 2, razonSocial: 'Cobre Austral SpA', ordenes: 2, monto: 3400 },
  ],
};

describe('resumen de órdenes de abastecimiento', () => {
  it('pluraliza la cantidad de órdenes por proveedor', () => {
    render(<OrdenesResumen datos={resumen} error="" />);
    expect(screen.getByText(/1 orden ·/)).toBeInTheDocument();
    expect(screen.getByText(/2 órdenes ·/)).toBeInTheDocument();
  });
});
