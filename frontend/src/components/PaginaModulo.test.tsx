import { cleanup, render, screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { PaginaModulo } from './PaginaModulo';

afterEach(cleanup);

describe('PaginaModulo', () => {
  it.each([
    [1, 'Comercial, Ventas y CRM', '01 · Clientes'],
    ['04-ordenes-compra', 'Comercial, Ventas y CRM', '04 · OrdenesCompra'],
  ] as const)('obtiene nombre y dominio del catálogo para %s', (modulo, dominio, titulo) => {
    render(<PaginaModulo modulo={modulo}><p>Contenido del módulo</p></PaginaModulo>);
    const migas = screen.getByRole('navigation', { name: 'Miga de pan' });

    expect(within(migas).getByText(dominio)).toBeInTheDocument();
    expect(screen.getByText(`MÓDULO ${titulo.slice(0, 2)} · ${dominio}`)).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: titulo })).toBeInTheDocument();
    expect(screen.getByText('Contenido del módulo')).toBeInTheDocument();
  });
});
