import { describe, expect, it } from 'vitest';
import { dominios, modulos } from './catalogo';

describe('catálogo de módulos', () => {
  it('contiene los 80 módulos consecutivos repartidos en 8 dominios de 10', () => {
    expect(modulos).toHaveLength(80);
    expect(modulos.map((modulo) => modulo.numero)).toEqual(Array.from({ length: 80 }, (_, indice) => indice + 1));
    expect(new Set(modulos.map((modulo) => modulo.numero)).size).toBe(80);
    expect(dominios).toHaveLength(8);
    expect(dominios.every((dominio) => dominio.modulos.length === 10)).toBe(true);
  });

  it('mantiene el slug y dominio de OrdenesCompra', () => {
    expect(modulos[3]).toMatchObject({
      numero: 4,
      nombre: 'OrdenesCompra',
      slug: '04-ordenes-compra',
      dominio: 'Comercial, Ventas y CRM',
    });
  });
});
