import { describe, expect, it } from 'vitest';
import { sumarDias } from './presentacion';
import {
  construirRequestExamen,
  construirRequestIncidente,
  validarAccion,
  validarEntrega,
  validarEquipo,
  validarExamen,
  validarIncidente,
} from './validacionSst';

const HOY = '2026-11-22';

describe('validación de equipos y entregas', () => {
  it('equipo: código, nombre y vida útil con los mensajes literales', () => {
    expect(validarEquipo({ codigo: '', nombre: '', vidaUtilDias: '' })).toEqual({
      codigo: 'El código es obligatorio y no puede superar 20 caracteres.',
      nombre: 'El nombre es obligatorio y no puede superar 100 caracteres.',
      vidaUtilDias: 'La vida útil debe estar entre 1 y 3650 días.',
    });
    expect(validarEquipo({ codigo: 'C'.repeat(21), nombre: 'N'.repeat(101), vidaUtilDias: '3651' })).toHaveProperty('vidaUtilDias');
    expect(validarEquipo({ codigo: 'C'.repeat(20), nombre: 'N'.repeat(100), vidaUtilDias: '3650' })).toEqual({});
    expect(validarEquipo({ codigo: 'A', nombre: 'B', vidaUtilDias: '0' })).toHaveProperty('vidaUtilDias');
    expect(validarEquipo({ codigo: 'A', nombre: 'B', vidaUtilDias: '1,5' })).toHaveProperty('vidaUtilDias');
  });

  it('entrega: empleado, fecha no futura y cantidad de 1 a 100', () => {
    const errores = validarEntrega({ equipoId: '', empleadoId: '', fechaEntrega: '2026-11-23', cantidad: '101' }, HOY);
    expect(errores.empleadoId).toBe('El empleado es obligatorio.');
    expect(errores.equipoId).toBe('Selecciona un equipo.');
    expect(errores.fechaEntrega).toBe('La fecha es obligatoria y no puede ser posterior a hoy.');
    expect(errores.cantidad).toBe('La cantidad debe estar entre 1 y 100.');
    expect(validarEntrega({ equipoId: '1', empleadoId: '2', fechaEntrega: HOY, cantidad: '100' }, HOY)).toEqual({});
    expect(validarEntrega({ equipoId: '1', empleadoId: '2', fechaEntrega: HOY, cantidad: '0' }, HOY)).toHaveProperty('cantidad');
  });

  it('suma días con aritmética UTC y rechaza fechas imposibles', () => {
    expect(sumarDias('2026-11-22', 365)).toBe('2027-11-22');
    expect(sumarDias('2026-12-31', 1)).toBe('2027-01-01');
    expect(sumarDias('2026-02-31', 10)).toBeNull();
  });
});

describe('validación de exámenes', () => {
  const base = { empleadoId: '1', tipo: 'Periodico', fecha: '2026-11-01', resultado: 'Apto', restricciones: '', fechaVencimiento: '2027-11-01', clinica: '' };

  it('acepta un examen periódico válido y construye el cuerpo', () => {
    expect(validarExamen(base, HOY)).toEqual({});
    expect(construirRequestExamen(base)).toEqual({ empleadoId: 1, tipo: 'Periodico', fecha: '2026-11-01', resultado: 'Apto', restricciones: null, fechaVencimiento: '2027-11-01', clinica: null });
  });

  it('exige restricciones solo con AptoConRestricciones y las omite con otro resultado', () => {
    expect(validarExamen({ ...base, resultado: 'AptoConRestricciones' }, HOY).restricciones).toBe('Las restricciones son obligatorias si el resultado es AptoConRestricciones y no pueden superar 300 caracteres.');
    expect(validarExamen({ ...base, resultado: 'AptoConRestricciones', restricciones: 'x'.repeat(301) }, HOY)).toHaveProperty('restricciones');
    expect(validarExamen({ ...base, resultado: 'AptoConRestricciones', restricciones: 'Sin altura' }, HOY)).toEqual({});
    expect(construirRequestExamen({ ...base, resultado: 'NoApto', restricciones: 'olvidado' }).restricciones).toBeNull();
  });

  it('el vencimiento es obligatorio y posterior en ingreso y periódico, y no existe en retiro', () => {
    const mensaje = 'La fecha de vencimiento es obligatoria y posterior al examen en los exámenes de ingreso y periódicos.';
    expect(validarExamen({ ...base, fechaVencimiento: '' }, HOY).fechaVencimiento).toBe(mensaje);
    expect(validarExamen({ ...base, fechaVencimiento: '2026-11-01' }, HOY).fechaVencimiento).toBe(mensaje);
    expect(validarExamen({ ...base, tipo: 'Retiro', fechaVencimiento: '' }, HOY)).toEqual({});
    expect(construirRequestExamen({ ...base, tipo: 'Retiro' }).fechaVencimiento).toBeNull();
  });

  it('valida empleado, tipo, fecha, resultado y clínica', () => {
    const errores = validarExamen({ empleadoId: '', tipo: '', fecha: '2026-12-01', resultado: '', restricciones: '', fechaVencimiento: '', clinica: 'c'.repeat(151) }, HOY);
    expect(errores.empleadoId).toBe('El empleado es obligatorio.');
    expect(errores.tipo).toBe('El tipo de examen debe ser Ingreso, Periodico o Retiro.');
    expect(errores.fecha).toBe('La fecha es obligatoria y no puede ser posterior a hoy.');
    expect(errores.resultado).toBe('El resultado debe ser Apto, AptoConRestricciones o NoApto.');
    expect(errores.clinica).toBe('La clínica no puede superar 150 caracteres.');
  });
});

describe('validación de incidentes y acciones', () => {
  const base = { empleadoId: '1', tipo: 'AccidenteSinBaja', severidad: 'Leve', fecha: '2026-11-20', diasBaja: '', lugar: '', descripcion: 'Golpe' };

  it('acepta un incidente válido y los días de baja solo cuentan con AccidenteConBaja', () => {
    expect(validarIncidente(base, HOY)).toEqual({});
    expect(construirRequestIncidente({ ...base, diasBaja: '9' }).diasBaja).toBe(0);
    expect(construirRequestIncidente({ ...base, tipo: 'AccidenteConBaja', diasBaja: '3' }).diasBaja).toBe(3);
    expect(validarIncidente({ ...base, tipo: 'AccidenteConBaja' }, HOY).diasBaja).toBe('Un accidente con baja exige al menos 1 día de baja.');
    expect(validarIncidente({ ...base, tipo: 'AccidenteConBaja', diasBaja: '0' }, HOY)).toHaveProperty('diasBaja');
  });

  it('usa los mensajes 8, 11 y 20 a 25', () => {
    const errores = validarIncidente({ empleadoId: '', tipo: '', severidad: '', fecha: '2026-11-23', diasBaja: '', lugar: 'l'.repeat(151), descripcion: ' ' }, HOY);
    expect(errores.empleadoId).toBe('El empleado es obligatorio.');
    expect(errores.tipo).toBe('El tipo de incidente debe ser AccidenteSinBaja, AccidenteConBaja, IncidenteSinLesion o EnfermedadOcupacional.');
    expect(errores.severidad).toBe('La severidad debe ser Leve, Moderada, Grave o Mortal.');
    expect(errores.fecha).toBe('La fecha es obligatoria y no puede ser posterior a hoy.');
    expect(errores.descripcion).toBe('La descripción es obligatoria y no puede superar 1000 caracteres.');
    expect(errores.lugar).toBe('El lugar no puede superar 150 caracteres.');
    expect(validarIncidente({ ...base, descripcion: 'd'.repeat(1001) }, HOY)).toHaveProperty('descripcion');
  });

  it('acción: descripción de hasta 500, responsable de hasta 150 y fecha de compromiso', () => {
    const mensaje = 'La acción correctiva necesita descripción (máximo 500), responsable (máximo 150) y fecha de compromiso.';
    expect(validarAccion({ descripcion: '', responsable: '', fechaCompromiso: '' })).toEqual({ descripcion: mensaje, responsable: mensaje, fechaCompromiso: mensaje });
    expect(validarAccion({ descripcion: 'd'.repeat(501), responsable: 'r'.repeat(151), fechaCompromiso: '2026-12-01' })).toEqual({ descripcion: mensaje, responsable: mensaje });
    expect(validarAccion({ descripcion: 'd'.repeat(500), responsable: 'r'.repeat(150), fechaCompromiso: '2026-12-01' })).toEqual({});
  });
});
