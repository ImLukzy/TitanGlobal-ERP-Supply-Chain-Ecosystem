import { describe, expect, it } from 'vitest';
import {
  calcularPuntajeCentesimas,
  construirRequestCalificaciones,
  construirRequestCompetencia,
  etiquetaPuntaje,
  formatearCentesimas,
  formatearDecimal,
  interpretarPeso,
  validarCiclo,
  validarComentario,
  validarCompetencia,
  validarNuevaEvaluacion,
  type CamposCiclo,
} from './validacionEvaluacion';

const cicloValido: CamposCiclo = {
  codigo: '2026-S2',
  nombre: 'Segundo semestre 2026',
  periodo: 'Semestral',
  fechaInicioPeriodo: '2026-06-01',
  fechaFinPeriodo: '2026-11-30',
  fechaApertura: '2026-11-01',
  fechaCierre: '2026-12-31',
};

describe('puntaje ponderado (igual que el backend)', () => {
  it('pesos no redondos: 33,33 / 33,33 / 33,34 con 4 da 4,00', () => {
    const puntaje = calcularPuntajeCentesimas([{ peso: 33.33, calificacion: 4 }, { peso: 33.33, calificacion: 4 }, { peso: 33.34, calificacion: 4 }]);
    expect(formatearCentesimas(puntaje)).toBe('4,00');
  });

  it('redondea hacia arriba en el punto medio: 45,5 y 54,5 con 3 y 4 da 3,55', () => {
    const puntaje = calcularPuntajeCentesimas([{ peso: 45.5, calificacion: 3 }, { peso: 54.5, calificacion: 4 }]);
    expect(formatearCentesimas(puntaje)).toBe('3,55');
  });

  it('no sufre errores de coma flotante con pesos como 1,15 o 10,05', () => {
    expect(formatearCentesimas(calcularPuntajeCentesimas([{ peso: 1.15, calificacion: 5 }, { peso: 98.85, calificacion: 5 }]))).toBe('5,00');
    expect(formatearCentesimas(calcularPuntajeCentesimas([{ peso: 10.05, calificacion: 1 }, { peso: 89.95, calificacion: 2 }]))).toBe('1,90');
  });

  it('llega a los extremos 1,00 y 5,00 y formatea con coma', () => {
    expect(formatearCentesimas(calcularPuntajeCentesimas([{ peso: 100, calificacion: 1 }]))).toBe('1,00');
    expect(formatearCentesimas(calcularPuntajeCentesimas([{ peso: 100, calificacion: 5 }]))).toBe('5,00');
    expect(formatearDecimal(3.5)).toBe('3,50');
    expect(formatearCentesimas(7)).toBe('0,07');
  });

  it('asigna la etiqueta en los límites del servidor', () => {
    expect(etiquetaPuntaje(199)).toBe('Insuficiente');
    expect(etiquetaPuntaje(200)).toBe('En desarrollo');
    expect(etiquetaPuntaje(299)).toBe('En desarrollo');
    expect(etiquetaPuntaje(300)).toBe('Competente');
    expect(etiquetaPuntaje(399)).toBe('Competente');
    expect(etiquetaPuntaje(400)).toBe('Destacado');
    expect(etiquetaPuntaje(449)).toBe('Destacado');
    expect(etiquetaPuntaje(450)).toBe('Excepcional');
  });
});

describe('validación de ciclo', () => {
  it('acepta un ciclo válido', () => {
    expect(validarCiclo(cicloValido)).toEqual({});
  });

  it('mensajes 1 a 3: código, nombre y periodo', () => {
    const errores = validarCiclo({ ...cicloValido, codigo: ' ', nombre: 'x'.repeat(101), periodo: 'Mensual' });
    expect(errores.codigo).toBe('El código es obligatorio y no puede superar 20 caracteres.');
    expect(errores.nombre).toBe('El nombre es obligatorio y no puede superar 100 caracteres.');
    expect(errores.periodo).toBe('El periodo debe ser Semestral o Anual.');
    expect(validarCiclo({ ...cicloValido, codigo: 'x'.repeat(21) }).codigo).toBeDefined();
    expect(validarCiclo({ ...cicloValido, codigo: 'x'.repeat(20) }).codigo).toBeUndefined();
  });

  it('mensaje 4: fechas obligatorias y reales', () => {
    const errores = validarCiclo({ ...cicloValido, fechaInicioPeriodo: '', fechaCierre: '2026-02-31' });
    expect(errores.fechaInicioPeriodo).toBe('Las fechas del periodo, de apertura y de cierre son obligatorias.');
    expect(errores.fechaCierre).toBe('Las fechas del periodo, de apertura y de cierre son obligatorias.');
  });

  it('mensaje 5: duración según el periodo (170 a 190 semestral, 360 a 370 anual)', () => {
    expect(validarCiclo({ ...cicloValido, periodo: 'Anual' }).fechaFinPeriodo).toBe('La duración del periodo no corresponde a un ciclo Anual.');
    expect(validarCiclo({ ...cicloValido, fechaInicioPeriodo: '2026-06-14' }).fechaFinPeriodo).toBeUndefined();
    expect(validarCiclo({ ...cicloValido, fechaInicioPeriodo: '2026-06-15' }).fechaFinPeriodo).toBe('La duración del periodo no corresponde a un ciclo Semestral.');
    expect(validarCiclo({ ...cicloValido, fechaInicioPeriodo: '2026-05-25' }).fechaFinPeriodo).toBeUndefined();
    expect(validarCiclo({ ...cicloValido, fechaInicioPeriodo: '2026-05-24' }).fechaFinPeriodo).toBe('La duración del periodo no corresponde a un ciclo Semestral.');
    expect(validarCiclo({ ...cicloValido, fechaInicioPeriodo: '2026-01-01', fechaFinPeriodo: '2026-12-31', periodo: 'Anual', fechaApertura: '2026-12-01' })).toEqual({});
  });

  it('mensaje 6: la apertura no puede ser anterior a 30 días antes del fin del periodo', () => {
    expect(validarCiclo({ ...cicloValido, fechaApertura: '2026-10-30' }).fechaApertura).toBe('La apertura no puede ser anterior a 30 días antes del fin del periodo.');
    expect(validarCiclo({ ...cicloValido, fechaApertura: '2026-10-31' }).fechaApertura).toBeUndefined();
  });

  it('mensaje 7: el cierre debe ser posterior a la apertura', () => {
    expect(validarCiclo({ ...cicloValido, fechaCierre: '2026-11-01' }).fechaCierre).toBe('La fecha de cierre debe ser posterior a la de apertura.');
    expect(validarCiclo({ ...cicloValido, fechaCierre: '2026-11-02' }).fechaCierre).toBeUndefined();
  });
});

describe('validación de competencia', () => {
  it('interpreta el peso con coma o punto y hasta 2 decimales', () => {
    expect(interpretarPeso('45,5')).toBe(45.5);
    expect(interpretarPeso('100')).toBe(100);
    expect(interpretarPeso('0,01')).toBe(0.01);
    expect(interpretarPeso('0')).toBeNull();
    expect(interpretarPeso('100,01')).toBeNull();
    expect(interpretarPeso('10,123')).toBeNull();
    expect(interpretarPeso('abc')).toBeNull();
  });

  it('mensajes 2, 34 y 11', () => {
    const errores = validarCompetencia({ nombre: '', descripcion: 'x'.repeat(301), peso: '0' });
    expect(errores.nombre).toBe('El nombre es obligatorio y no puede superar 100 caracteres.');
    expect(errores.descripcion).toBe('La descripción no puede superar 300 caracteres.');
    expect(errores.peso).toBe('El peso debe ser mayor que 0 y como máximo 100, con 2 decimales.');
    expect(validarCompetencia({ nombre: 'Calidad', descripcion: '', peso: '25' })).toEqual({});
  });

  it('construye la solicitud con la descripción vacía como null', () => {
    expect(construirRequestCompetencia({ nombre: ' Calidad ', descripcion: ' ', peso: '25,5' })).toEqual({ nombre: 'Calidad', descripcion: null, peso: 25.5 });
  });
});

describe('validación de evaluación', () => {
  it('mensaje 17 cuando falta ciclo, empleado o evaluador', () => {
    const errores = validarNuevaEvaluacion({ cicloId: '', empleadoId: '', evaluadorId: '' });
    expect(errores.cicloId).toBe('El ciclo, el empleado y el evaluador son obligatorios.');
    expect(errores.empleadoId).toBe('El ciclo, el empleado y el evaluador son obligatorios.');
    expect(errores.evaluadorId).toBe('El ciclo, el empleado y el evaluador son obligatorios.');
  });

  it('mensaje 18 cuando el evaluador es el propio evaluado', () => {
    expect(validarNuevaEvaluacion({ cicloId: '2', empleadoId: '1', evaluadorId: '1' }).evaluadorId).toBe('El evaluador no puede ser el propio evaluado.');
    expect(validarNuevaEvaluacion({ cicloId: '2', empleadoId: '1', evaluadorId: '2' })).toEqual({});
  });

  it('mensaje 27: el comentario no supera 500 caracteres', () => {
    expect(validarComentario('x'.repeat(500))).toBe('');
    expect(validarComentario('x'.repeat(501))).toBe('El comentario no puede superar 500 caracteres.');
  });

  it('construye las calificaciones y el comentario nulo', () => {
    expect(construirRequestCalificaciones(new Map([[3, 4], [4, 5]]), '  ')).toEqual({
      calificaciones: [{ competenciaId: 3, calificacion: 4 }, { competenciaId: 4, calificacion: 5 }],
      comentario: null,
    });
  });
});
