import { describe, expect, it } from 'vitest';
import { validarCategoriaProducto } from './validacion';

describe('validarCategoriaProducto', () => {
  it('exige nombre obligatorio y respeta longitudes máximas', () => {
    expect(validarCategoriaProducto({ nombre: '   ' }).nombre).toBeDefined();
    expect(validarCategoriaProducto({ nombre: 'a'.repeat(101) }).nombre).toBeDefined();
    expect(validarCategoriaProducto({ nombre: 'Válida', descripcion: 'b'.repeat(251) }).descripcion).toBeDefined();
    expect(validarCategoriaProducto({ nombre: 'Hardware', descripcion: 'Equipos físicos' })).toEqual({});
  });
});
