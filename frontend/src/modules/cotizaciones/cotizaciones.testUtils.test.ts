import { describe, expect, it } from 'vitest';
import { cotizacionesDetalleMock } from './cotizaciones.testUtils';

describe('mocks de detalle de cotizaciones', () => {
  it('mantiene coherentes subtotales, descuentos, importes y totales', () => {
    for (const cotizacion of cotizacionesDetalleMock) {
      const sumaImportes = cotizacion.lineas.reduce(
        (suma, linea) => suma + linea.importe,
        0,
      );
      expect(cotizacion.subtotal - cotizacion.descuento).toBeCloseTo(
        cotizacion.total,
        2,
      );
      expect(sumaImportes).toBeCloseTo(cotizacion.total, 2);
    }
  });
});
