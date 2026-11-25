import { describe, expect, it } from 'vitest';
import {
  construirRequestCertificacion,
  construirRequestCurso,
  construirRequestProgramacion,
  interpretarHoras,
  interpretarNota,
  validarCertificacion,
  validarCurso,
  validarProgramacion,
  type CamposCertificacion,
  type CamposCurso,
  type CamposProgramacion,
} from './validacionCapacitacion';

const cursoValido: CamposCurso = { codigo: 'SEG-101', nombre: 'Seguridad industrial', horas: '8', tipo: 'Interno', proveedor: '' };
const programacionValida: CamposProgramacion = { cursoId: '1', fechaInicio: '2026-12-10', fechaFin: '2026-12-11', cupo: '3', lugar: '' };
const certificacionValida: CamposCertificacion = { codigo: 'CERT-1', fechaEmision: '2026-12-11', fechaVencimiento: '' };

describe('horas y nota', () => {
  it('aceptan coma o punto con un decimal y respetan los límites', () => {
    expect(interpretarHoras('12,5')).toBe(12.5);
    expect(interpretarHoras('500')).toBe(500);
    expect(interpretarHoras('0')).toBeNull();
    expect(interpretarHoras('500,1')).toBeNull();
    expect(interpretarHoras('8.25')).toBeNull();
    expect(interpretarHoras('abc')).toBeNull();
    expect(interpretarNota('0')).toBe(0);
    expect(interpretarNota('20')).toBe(20);
    expect(interpretarNota('20,1')).toBeNull();
    expect(interpretarNota('-1')).toBeNull();
    expect(interpretarNota('15,55')).toBeNull();
  });
});

describe('validación de curso (mensajes 1 a 5)', () => {
  it('un curso válido no tiene errores y el request recorta y usa null sin proveedor', () => {
    expect(validarCurso(cursoValido)).toEqual({});
    expect(construirRequestCurso({ ...cursoValido, codigo: ' seg-101 ', horas: '12,5', proveedor: '  ' })).toEqual({
      codigo: 'seg-101', nombre: 'Seguridad industrial', horas: 12.5, tipo: 'Interno', proveedor: null,
    });
  });

  it('devuelve los mensajes literales en cada límite', () => {
    const errores = validarCurso({ codigo: '', nombre: 'x'.repeat(151), horas: '501', tipo: 'Otro', proveedor: 'p'.repeat(151) });
    expect(errores).toEqual({
      codigo: 'El código es obligatorio y no puede superar 20 caracteres.',
      nombre: 'El nombre es obligatorio y no puede superar 150 caracteres.',
      horas: 'Las horas deben ser mayores que 0 y como máximo 500, con un decimal.',
      tipo: 'El tipo debe ser Interno, Externo o Virtual.',
      proveedor: 'El proveedor no puede superar 150 caracteres.',
    });
    expect(validarCurso({ ...cursoValido, codigo: 'x'.repeat(21) }).codigo).toBeDefined();
    expect(validarCurso({ ...cursoValido, codigo: 'x'.repeat(20), nombre: 'x'.repeat(150), proveedor: 'p'.repeat(150) })).toEqual({});
  });
});

describe('validación de programación (mensajes 10 a 13)', () => {
  it('una programación válida no tiene errores y el request usa null sin lugar', () => {
    expect(validarProgramacion(programacionValida)).toEqual({});
    expect(construirRequestProgramacion(programacionValida)).toEqual({ cursoId: 1, fechaInicio: '2026-12-10', fechaFin: '2026-12-11', cupo: 3, lugar: null });
  });

  it('exige curso y fechas, ordena las fechas y limita el cupo y el lugar', () => {
    expect(validarProgramacion({ ...programacionValida, cursoId: '' }).cursoId).toBe('Selecciona un curso.');
    const sinFechas = validarProgramacion({ ...programacionValida, fechaInicio: '', fechaFin: '' });
    expect(sinFechas.fechaInicio).toBe('Las fechas de inicio y fin son obligatorias.');
    expect(sinFechas.fechaFin).toBe('Las fechas de inicio y fin son obligatorias.');
    expect(validarProgramacion({ ...programacionValida, fechaFin: '2026-12-09' }).fechaFin).toBe('La fecha de fin no puede ser anterior a la de inicio.');
    expect(validarProgramacion({ ...programacionValida, fechaFin: '2026-12-10' })).toEqual({});
    expect(validarProgramacion({ ...programacionValida, fechaInicio: '2026-02-31' }).fechaInicio).toBeDefined();
    for (const cupo of ['0', '501', '', '2,5', 'x']) {
      expect(validarProgramacion({ ...programacionValida, cupo }).cupo).toBe('El cupo debe estar entre 1 y 500.');
    }
    expect(validarProgramacion({ ...programacionValida, cupo: '500' })).toEqual({});
    expect(validarProgramacion({ ...programacionValida, lugar: 'l'.repeat(151) }).lugar).toBe('El lugar no puede superar 150 caracteres.');
  });
});

describe('validación de certificación (mensajes 27 a 29)', () => {
  it('una certificación válida no tiene errores y el vencimiento vacío es null', () => {
    expect(validarCertificacion(certificacionValida, '2026-12-11')).toEqual({});
    expect(construirRequestCertificacion({ ...certificacionValida, codigo: ' CERT-1 ' })).toEqual({ codigo: 'CERT-1', fechaEmision: '2026-12-11', fechaVencimiento: null });
  });

  it('exige código, emisión desde el fin del curso y vencimiento desde la emisión', () => {
    expect(validarCertificacion({ ...certificacionValida, codigo: ' ' }, '2026-12-11').codigo).toBe('El código de certificación es obligatorio y no puede superar 40 caracteres.');
    expect(validarCertificacion({ ...certificacionValida, codigo: 'c'.repeat(41) }, '2026-12-11').codigo).toBeDefined();
    expect(validarCertificacion({ ...certificacionValida, fechaEmision: '2026-12-10' }, '2026-12-11').fechaEmision).toBe('La fecha de emisión es obligatoria y no puede ser anterior al fin del curso.');
    expect(validarCertificacion({ ...certificacionValida, fechaEmision: '' }, '2026-12-11').fechaEmision).toBeDefined();
    expect(validarCertificacion({ ...certificacionValida, fechaVencimiento: '2026-12-10' }, '2026-12-11').fechaVencimiento).toBe('La fecha de vencimiento no puede ser anterior a la de emisión.');
    expect(validarCertificacion({ ...certificacionValida, fechaVencimiento: '2026-12-11' }, '2026-12-11')).toEqual({});
  });
});
