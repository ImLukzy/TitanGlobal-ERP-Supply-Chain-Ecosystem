import { describe, expect, it } from 'vitest';
import { construirRequest, validarAusencia, validarMotivoRechazo, type CamposAusencia } from './validacionAusencia';

const validos: CamposAusencia = {
  empleadoId: '2',
  tipo: 'PermisoConGoce',
  fechaInicio: '2027-01-04',
  fechaFin: '2027-01-05',
  motivo: 'Trámite',
};

describe('validación de ausencias', () => {
  it('acepta una solicitud válida', () => {
    expect(validarAusencia(validos)).toEqual({});
  });

  it('exige empleado, fechas y, según el tipo, motivo (mensajes 1, 3, 4 y 6)', () => {
    const errores = validarAusencia({ empleadoId: '', tipo: 'DescansoMedico', fechaInicio: '', fechaFin: '', motivo: '  ' });
    expect(errores.empleadoId).toBe('El empleado es obligatorio.');
    expect(errores.fechaInicio).toBe('La fecha de inicio es obligatoria.');
    expect(errores.fechaFin).toBe('La fecha de fin es obligatoria.');
    expect(errores.motivo).toBe('El motivo es obligatorio para este tipo de ausencia.');
  });

  it('rechaza un tipo que no es del catálogo (mensaje 2)', () => {
    expect(validarAusencia({ ...validos, tipo: 'Otro' }).tipo).toBe(
      'El tipo debe ser Vacaciones, PermisoConGoce, PermisoSinGoce, LicenciaMaternidad, LicenciaPaternidad o DescansoMedico.',
    );
  });

  it('no exige motivo en vacaciones y licencias', () => {
    for (const tipo of ['Vacaciones', 'LicenciaMaternidad', 'LicenciaPaternidad']) {
      expect(validarAusencia({ ...validos, tipo, motivo: '', fechaFin: '2027-01-05' }).motivo).toBeUndefined();
    }
  });

  it('rechaza un fin anterior al inicio (mensaje 5)', () => {
    expect(validarAusencia({ ...validos, fechaFin: '2027-01-03' }).fechaFin).toBe(
      'La fecha de fin no puede ser anterior a la fecha de inicio.',
    );
  });

  it('limita el motivo a 300 caracteres (mensaje 7)', () => {
    expect(validarAusencia({ ...validos, motivo: 'a'.repeat(300) }).motivo).toBeUndefined();
    expect(validarAusencia({ ...validos, motivo: 'a'.repeat(301) }).motivo).toBe('El motivo no puede superar 300 caracteres.');
  });

  it('aplica los topes de maternidad y paternidad (mensajes 8 y 9)', () => {
    expect(validarAusencia({ ...validos, tipo: 'LicenciaMaternidad', fechaInicio: '2027-01-01', fechaFin: '2027-04-08' }).fechaFin).toBeUndefined();
    expect(validarAusencia({ ...validos, tipo: 'LicenciaMaternidad', fechaInicio: '2027-01-01', fechaFin: '2027-04-09' }).fechaFin).toBe(
      'La licencia de maternidad no puede superar 98 días.',
    );
    expect(validarAusencia({ ...validos, tipo: 'LicenciaPaternidad', fechaInicio: '2027-01-01', fechaFin: '2027-01-10' }).fechaFin).toBeUndefined();
    expect(validarAusencia({ ...validos, tipo: 'LicenciaPaternidad', fechaInicio: '2027-01-01', fechaFin: '2027-01-11' }).fechaFin).toBe(
      'La licencia de paternidad no puede superar 10 días.',
    );
  });

  it('construye la petición con el motivo recortado o nulo', () => {
    expect(construirRequest({ ...validos, motivo: ' Trámite ' })).toEqual({
      empleadoId: 2,
      tipo: 'PermisoConGoce',
      fechaInicio: '2027-01-04',
      fechaFin: '2027-01-05',
      motivo: 'Trámite',
    });
    expect(construirRequest({ ...validos, tipo: 'Vacaciones', motivo: '  ' })?.motivo).toBeNull();
    expect(construirRequest({ ...validos, tipo: 'Otro' })).toBeNull();
  });

  it('valida el motivo del rechazo (mensajes 17 y 18)', () => {
    expect(validarMotivoRechazo('  ')).toBe('El motivo del rechazo es obligatorio.');
    expect(validarMotivoRechazo('a'.repeat(301))).toBe('El motivo del rechazo no puede superar 300 caracteres.');
    expect(validarMotivoRechazo('a'.repeat(300))).toBeUndefined();
  });
});
