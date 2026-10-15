import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { ZonasResumen } from './ZonasResumen';

afterEach(cleanup);

describe('ZonasResumen', () => {
  it('expone todos los atributos accesibles del medidor y su texto', () => {
    render(
      <ZonasResumen
        error=""
        resumen={{
          totalZonas: 2,
          activas: 2,
          porTipo: [],
          ocupacion: [{ almacenId: 1, almacenCodigo: 'ALM-LIM-01', capacidadAlmacenM3: 500, capacidadAsignadaM3: 450, porcentajeAsignado: 90 }],
        }}
      />,
    );
    const medidor = screen.getByRole('meter', { name: 'Ocupación de ALM-LIM-01' });
    expect(medidor).toHaveAttribute('aria-valuemin', '0');
    expect(medidor).toHaveAttribute('aria-valuemax', '100');
    expect(medidor).toHaveAttribute('aria-valuenow', '90');
    expect(medidor).toHaveAttribute('aria-valuetext', '450.00 de 500.00 m³ · 90.00%');
    expect(screen.getByText('450.00 de 500.00 m³ · 90.00%')).toBeInTheDocument();
  });

  it('marca Casi lleno al 90 % y no al 89.99 %', () => {
    render(
      <ZonasResumen
        error=""
        resumen={{
          totalZonas: 2,
          activas: 2,
          porTipo: [],
          ocupacion: [
            { almacenId: 1, almacenCodigo: 'ALM-LIM-01', capacidadAlmacenM3: 500, capacidadAsignadaM3: 450, porcentajeAsignado: 90 },
            { almacenId: 3, almacenCodigo: 'ALM-ARE-01', capacidadAlmacenM3: 150, capacidadAsignadaM3: 134.985, porcentajeAsignado: 89.99 },
          ],
        }}
      />,
    );
    expect(screen.getByText('Casi lleno')).toBeInTheDocument();
    expect(screen.getByLabelText('Ocupación de ALM-ARE-01')).toHaveAttribute('aria-valuenow', '89.99');
    expect(screen.getAllByText('Casi lleno')).toHaveLength(1);
  });
});
