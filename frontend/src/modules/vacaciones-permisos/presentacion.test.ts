import { describe, expect, it } from 'vitest';
import { aEstadoFiltro, aTipoFiltro, calcularDias, esFechaIso, hoyUtc, puedeCancelar, textoTipo } from './presentacion';

describe('presentación de ausencias', () => {
  it('muestra los tipos con texto legible', () => {
    expect(textoTipo('PermisoConGoce')).toBe('Permiso con goce');
    expect(textoTipo('PermisoSinGoce')).toBe('Permiso sin goce');
    expect(textoTipo('LicenciaMaternidad')).toBe('Licencia de maternidad');
    expect(textoTipo('LicenciaPaternidad')).toBe('Licencia de paternidad');
    expect(textoTipo('DescansoMedico')).toBe('Descanso médico');
  });

  it('convierte los filtros desconocidos en «Todos»', () => {
    expect(aTipoFiltro('Vacaciones')).toBe('Vacaciones');
    expect(aTipoFiltro('Otro')).toBe('');
    expect(aEstadoFiltro('Cancelada')).toBe('Cancelada');
    expect(aEstadoFiltro('Otro')).toBe('');
  });

  it('cuenta los días con ambos extremos incluidos', () => {
    expect(calcularDias('2026-11-02', '2026-11-02')).toBe(1);
    expect(calcularDias('2026-11-02', '2026-11-13')).toBe(12);
  });

  it('cuenta los días con aritmética UTC a través del fin de mes, de año y del 29 de febrero', () => {
    expect(calcularDias('2026-12-30', '2027-01-02')).toBe(4);
    expect(calcularDias('2028-02-28', '2028-03-01')).toBe(3);
    expect(calcularDias('2026-03-28', '2026-03-30')).toBe(3);
    expect(calcularDias('2026-10-24', '2026-10-26')).toBe(3);
  });

  it('devuelve null con fechas vacías, inválidas o en orden inverso', () => {
    expect(calcularDias('', '2026-11-02')).toBeNull();
    expect(calcularDias('2026-11-02', '')).toBeNull();
    expect(calcularDias('2026-02-30', '2026-03-02')).toBeNull();
    expect(calcularDias('2026-11-03', '2026-11-02')).toBeNull();
  });

  it('valida el formato de fecha', () => {
    expect(esFechaIso('2026-11-22')).toBe(true);
    expect(esFechaIso('22/11/2026')).toBe(false);
    expect(esFechaIso('2026-13-01')).toBe(false);
  });

  it('permite cancelar solo pendientes o aprobadas que no han empezado', () => {
    expect(puedeCancelar('Pendiente', '2099-01-01')).toBe(true);
    expect(puedeCancelar('Aprobada', '2099-01-01')).toBe(true);
    expect(puedeCancelar('Aprobada', '2020-01-01')).toBe(false);
    expect(puedeCancelar('Aprobada', hoyUtc())).toBe(false);
    expect(puedeCancelar('Rechazada', '2099-01-01')).toBe(false);
    expect(puedeCancelar('Cancelada', '2099-01-01')).toBe(false);
  });
});
