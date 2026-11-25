import { describe, expect, it } from 'vitest';
import {
  construirRequestAsignacion,
  construirRequestTurno,
  validarAsignacion,
  validarFinalizacion,
  validarTurno,
  type CamposTurno,
} from './validacionTurno';

const FIJO_VALIDO: CamposTurno = {
  codigo: 'TARDE',
  nombre: 'Turno tarde',
  tipo: 'Fijo',
  horaEntrada: '14:00',
  horaSalida: '22:00',
  cruzaMedianoche: false,
  toleranciaMinutos: '10',
  diasLaborables: [1, 2, 3],
  diasTrabajo: '',
  diasDescanso: '',
  fechaInicioCiclo: '',
};

const ROTATIVO_VALIDO: CamposTurno = {
  ...FIJO_VALIDO,
  tipo: 'Rotativo',
  diasLaborables: [],
  diasTrabajo: '4',
  diasDescanso: '2',
  fechaInicioCiclo: '2026-01-05',
};

describe('validación de turnos (mensajes 1 a 19)', () => {
  it('acepta un turno fijo, un rotativo y uno nocturno válidos', () => {
    expect(validarTurno(FIJO_VALIDO)).toEqual({});
    expect(validarTurno(ROTATIVO_VALIDO)).toEqual({});
    expect(validarTurno({ ...FIJO_VALIDO, horaEntrada: '22:00', horaSalida: '06:00', cruzaMedianoche: true })).toEqual({});
  });

  const casos: [string, Partial<CamposTurno>, keyof CamposTurno, string][] = [
    ['1', { codigo: '  ' }, 'codigo', 'El código es obligatorio.'],
    ['2', { codigo: 'A'.repeat(21) }, 'codigo', 'El código no puede superar 20 caracteres.'],
    ['3', { nombre: '' }, 'nombre', 'El nombre es obligatorio.'],
    ['4', { nombre: 'N'.repeat(101) }, 'nombre', 'El nombre no puede superar 100 caracteres.'],
    ['5', { tipo: 'Mixto' }, 'tipo', 'El tipo de turno debe ser Fijo o Rotativo.'],
    ['6', { horaEntrada: '' }, 'horaEntrada', 'La hora de entrada es obligatoria.'],
    ['7', { horaSalida: '' }, 'horaSalida', 'La hora de salida es obligatoria.'],
    ['8', { horaSalida: '14:00' }, 'horaSalida', 'La hora de salida no puede ser igual a la hora de entrada.'],
    ['9', { horaSalida: '06:00', cruzaMedianoche: false }, 'cruzaMedianoche', 'Si la hora de salida es anterior a la de entrada, el turno debe cruzar medianoche.'],
    ['10', { cruzaMedianoche: true }, 'cruzaMedianoche', 'El turno solo cruza medianoche si la hora de salida es anterior a la de entrada.'],
    ['11', { toleranciaMinutos: '' }, 'toleranciaMinutos', 'La tolerancia es obligatoria.'],
    ['12', { toleranciaMinutos: '31' }, 'toleranciaMinutos', 'La tolerancia debe estar entre 0 y 30 minutos.'],
    ['12 negativa', { toleranciaMinutos: '-1' }, 'toleranciaMinutos', 'La tolerancia debe estar entre 0 y 30 minutos.'],
    ['13', { diasLaborables: [] }, 'diasLaborables', 'El turno fijo debe tener al menos un día laborable.'],
    ['14', { diasLaborables: [1, 1] }, 'diasLaborables', 'Los días laborables deben ser números del 1 (lunes) al 7 (domingo) sin repetir.'],
    ['19', { diasTrabajo: '4' }, 'diasTrabajo', 'El turno fijo no usa ciclo de trabajo y descanso.'],
  ];

  it.each(casos)('mensaje %s', (_numero, cambios, campo, mensaje) => {
    expect(validarTurno({ ...FIJO_VALIDO, ...cambios })[campo]).toBe(mensaje);
  });

  const casosRotativo: [string, Partial<CamposTurno>, keyof CamposTurno, string][] = [
    ['15', { diasLaborables: [1] }, 'diasLaborables', 'El turno rotativo no usa días laborables de la semana.'],
    ['16 trabajo', { diasTrabajo: '' }, 'diasTrabajo', 'El turno rotativo necesita días de trabajo y días de descanso.'],
    ['16 descanso', { diasDescanso: '' }, 'diasDescanso', 'El turno rotativo necesita días de trabajo y días de descanso.'],
    ['17 cero', { diasTrabajo: '0' }, 'diasTrabajo', 'Los días de trabajo y de descanso deben estar entre 1 y 30.'],
    ['17 mayor', { diasDescanso: '31' }, 'diasDescanso', 'Los días de trabajo y de descanso deben estar entre 1 y 30.'],
    ['18', { fechaInicioCiclo: '' }, 'fechaInicioCiclo', 'La fecha de inicio del ciclo es obligatoria en el turno rotativo.'],
  ];

  it.each(casosRotativo)('mensaje %s', (_numero, cambios, campo, mensaje) => {
    expect(validarTurno({ ...ROTATIVO_VALIDO, ...cambios })[campo]).toBe(mensaje);
  });

  it('gana el primer fallo de cada campo según el orden del backend', () => {
    const errores = validarTurno({ ...FIJO_VALIDO, codigo: '', nombre: '', horaEntrada: '', diasLaborables: [] });
    expect(errores.codigo).toBe('El código es obligatorio.');
    expect(errores.nombre).toBe('El nombre es obligatorio.');
    expect(errores.horaEntrada).toBe('La hora de entrada es obligatoria.');
    expect(errores.diasLaborables).toBe('El turno fijo debe tener al menos un día laborable.');
  });
});

describe('cuerpo de la petición del turno', () => {
  it('envía un fijo con días ordenados, horas HH:mm:ss y sin campos de ciclo', () => {
    expect(construirRequestTurno({ ...FIJO_VALIDO, codigo: ' TARDE ', diasLaborables: [3, 1, 2] }, 'Fijo')).toEqual({
      codigo: 'TARDE',
      nombre: 'Turno tarde',
      tipo: 'Fijo',
      horaEntrada: '14:00:00',
      horaSalida: '22:00:00',
      cruzaMedianoche: false,
      toleranciaMinutos: 10,
      diasLaborables: [1, 2, 3],
    });
  });

  it('envía un rotativo con ciclo y sin días laborables', () => {
    expect(construirRequestTurno(ROTATIVO_VALIDO, 'Rotativo')).toEqual({
      codigo: 'TARDE',
      nombre: 'Turno tarde',
      tipo: 'Rotativo',
      horaEntrada: '14:00:00',
      horaSalida: '22:00:00',
      cruzaMedianoche: false,
      toleranciaMinutos: 10,
      diasTrabajo: 4,
      diasDescanso: 2,
      fechaInicioCiclo: '2026-01-05',
    });
  });
});

describe('validación de asignaciones (mensajes 25 a 28 y 36)', () => {
  const valida = { empleadoId: '1', turnoId: '5', fechaInicio: '2026-12-01', fechaFin: '' };

  it('acepta una asignación sin fin y construye el cuerpo sin fechaFin', () => {
    expect(validarAsignacion(valida)).toEqual({});
    expect(construirRequestAsignacion(valida)).toEqual({ empleadoId: 1, turnoId: 5, fechaInicio: '2026-12-01' });
    expect(construirRequestAsignacion({ ...valida, fechaFin: '2027-01-31' })).toMatchObject({ fechaFin: '2027-01-31' });
  });

  it('devuelve los mensajes 25, 26, 27 y 28 literales', () => {
    expect(validarAsignacion({ ...valida, empleadoId: '' }).empleadoId).toBe('El empleado es obligatorio.');
    expect(validarAsignacion({ ...valida, turnoId: '' }).turnoId).toBe('El turno es obligatorio.');
    expect(validarAsignacion({ ...valida, fechaInicio: '' }).fechaInicio).toBe('La fecha de inicio es obligatoria.');
    expect(validarAsignacion({ ...valida, fechaFin: '2026-11-30' }).fechaFin).toBe('La fecha de fin no puede ser anterior a la fecha de inicio.');
  });

  it('valida la finalización con los mensajes 36 y 28', () => {
    expect(validarFinalizacion('', '2026-01-01')).toBe('La fecha de fin es obligatoria.');
    expect(validarFinalizacion('2025-12-31', '2026-01-01')).toBe('La fecha de fin no puede ser anterior a la fecha de inicio.');
    expect(validarFinalizacion('2026-01-01', '2026-01-01')).toBeUndefined();
  });
});
