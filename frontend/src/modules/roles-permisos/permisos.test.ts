import { describe, expect, it } from 'vitest';
import {
  agruparPorDominio,
  aplicarAtajoDominio,
  cambiarCasilla,
  casillaBloqueada,
  construirRequestPermisos,
  contarCambios,
  contarPermisos,
} from './permisos';
import type { PermisoModulo } from './tipos';

function modulo(numero: number, dominio: string, puedeLeer = false, puedeEscribir = false): PermisoModulo {
  return { moduloNumero: numero, moduloNombre: `Modulo ${numero}`, dominio, puedeLeer, puedeEscribir };
}

const base: PermisoModulo[] = [
  modulo(1, 'Comercial', true, true),
  modulo(2, 'Comercial', true, false),
  modulo(3, 'Comercial'),
  modulo(11, 'Compras', true, false),
  modulo(12, 'Compras'),
];

describe('lógica de permisos del editor (módulo 72)', () => {
  it('agrupa por dominio conservando el orden de llegada', () => {
    const grupos = agruparPorDominio(base);
    expect(grupos.map((grupo) => grupo.dominio)).toEqual(['Comercial', 'Compras']);
    expect(grupos[0]?.modulos.map((item) => item.moduloNumero)).toEqual([1, 2, 3]);
    expect(grupos[1]?.modulos.map((item) => item.moduloNumero)).toEqual([11, 12]);
  });

  it('marcar «Escribir» marca también «Leer»', () => {
    const siguiente = cambiarCasilla(base, 3, 'escribir', true);
    expect(siguiente.find((item) => item.moduloNumero === 3)).toMatchObject({ puedeLeer: true, puedeEscribir: true });
  });

  it('desmarcar «Leer» desmarca «Escribir»', () => {
    const siguiente = cambiarCasilla(base, 1, 'leer', false);
    expect(siguiente.find((item) => item.moduloNumero === 1)).toMatchObject({ puedeLeer: false, puedeEscribir: false });
  });

  it('desmarcar «Escribir» conserva «Leer» y marcar «Leer» no marca «Escribir»', () => {
    const sinEscritura = cambiarCasilla(base, 1, 'escribir', false);
    expect(sinEscritura.find((item) => item.moduloNumero === 1)).toMatchObject({ puedeLeer: true, puedeEscribir: false });
    const conLectura = cambiarCasilla(base, 3, 'leer', true);
    expect(conLectura.find((item) => item.moduloNumero === 3)).toMatchObject({ puedeLeer: true, puedeEscribir: false });
  });

  it('cambiar una casilla no modifica los demás módulos ni el arreglo original', () => {
    const copia = JSON.stringify(base);
    const siguiente = cambiarCasilla(base, 3, 'escribir', true);
    expect(JSON.stringify(base)).toBe(copia);
    expect(siguiente.filter((item) => item.moduloNumero !== 3)).toEqual(base.filter((item) => item.moduloNumero !== 3));
  });

  it('los atajos de dominio afectan solo a ese dominio', () => {
    const lectura = aplicarAtajoDominio(base, base, 'Comercial', 'lectura', false);
    expect(lectura.filter((item) => item.dominio === 'Comercial').every((item) => item.puedeLeer && !item.puedeEscribir)).toBe(true);
    expect(lectura.filter((item) => item.dominio === 'Compras')).toEqual(base.filter((item) => item.dominio === 'Compras'));

    const escritura = aplicarAtajoDominio(base, base, 'Compras', 'escritura', false);
    expect(escritura.filter((item) => item.dominio === 'Compras').every((item) => item.puedeLeer && item.puedeEscribir)).toBe(true);

    const ninguno = aplicarAtajoDominio(base, base, 'Comercial', 'ninguno', false);
    expect(ninguno.filter((item) => item.dominio === 'Comercial').every((item) => !item.puedeLeer && !item.puedeEscribir)).toBe(true);
  });

  it('en un rol de sistema los atajos no quitan permisos que ya tenía', () => {
    const lectura = aplicarAtajoDominio(base, base, 'Comercial', 'lectura', true);
    expect(lectura.find((item) => item.moduloNumero === 1)).toMatchObject({ puedeLeer: true, puedeEscribir: true });
    const ninguno = aplicarAtajoDominio(base, base, 'Comercial', 'ninguno', true);
    expect(ninguno.filter((item) => item.dominio === 'Comercial')).toEqual(base.filter((item) => item.dominio === 'Comercial'));
  });

  it('bloquea solo las casillas que quitarían permisos en un rol de sistema', () => {
    expect(casillaBloqueada(base, 1, 'leer', true)).toBe(true);
    expect(casillaBloqueada(base, 1, 'escribir', true)).toBe(true);
    expect(casillaBloqueada(base, 2, 'leer', true)).toBe(true);
    expect(casillaBloqueada(base, 2, 'escribir', true)).toBe(false);
    expect(casillaBloqueada(base, 3, 'leer', true)).toBe(false);
    expect(casillaBloqueada(base, 1, 'leer', false)).toBe(false);
  });

  it('cuenta los módulos cambiados respecto al último estado guardado', () => {
    expect(contarCambios(base, base)).toBe(0);
    const dos = cambiarCasilla(cambiarCasilla(base, 3, 'leer', true), 12, 'escribir', true);
    expect(contarCambios(base, dos)).toBe(2);
    const deVuelta = cambiarCasilla(dos, 3, 'leer', false);
    expect(contarCambios(base, deVuelta)).toBe(1);
  });

  it('construye el PUT con todos los módulos y solo los tres campos del contrato', () => {
    const request = construirRequestPermisos(base);
    expect(request.permisos).toHaveLength(5);
    expect(request.permisos[0]).toEqual({ moduloNumero: 1, puedeLeer: true, puedeEscribir: true });
    expect(Object.keys(request.permisos[4] ?? {})).toEqual(['moduloNumero', 'puedeLeer', 'puedeEscribir']);
  });

  it('cuenta los módulos con lectura y con escritura', () => {
    expect(contarPermisos(base)).toEqual({ lectura: 3, escritura: 1 });
  });
});
