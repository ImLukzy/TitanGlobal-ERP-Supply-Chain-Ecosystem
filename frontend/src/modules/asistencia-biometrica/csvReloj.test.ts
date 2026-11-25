import { describe, expect, it } from 'vitest';
import { analizarCsvReloj, CABECERA_CSV, MENSAJE_CABECERA, MENSAJE_CANTIDAD } from './csvReloj';

function filas(cantidad: number): string {
  return Array.from({ length: cantidad }, (_, indice) => `R-${indice + 1},EMP-000001,Entrada,2026-11-03T13:00:00Z`).join('\n');
}

describe('analizarCsvReloj', () => {
  it('lee una cabecera válida y sus filas', () => {
    const resultado = analizarCsvReloj(`${CABECERA_CSV}\nR-1,EMP-000001,Entrada,2026-11-03T13:00:00Z\nR-2,EMP-000002,Salida,2026-11-03T22:00:00-05:00\n`);
    expect(resultado).toEqual({
      valido: true,
      registros: [
        { numeroRegistro: 'R-1', legajo: 'EMP-000001', tipo: 'Entrada', instante: '2026-11-03T13:00:00Z' },
        { numeroRegistro: 'R-2', legajo: 'EMP-000002', tipo: 'Salida', instante: '2026-11-03T22:00:00-05:00' },
      ],
    });
  });

  it('acepta BOM, saltos CRLF, espacios, comillas y filas vacías', () => {
    const texto = `${String.fromCharCode(0xfeff)}${CABECERA_CSV}\r\n\r\n"R-1", EMP-000001 ,Entrada,2026-11-03T13:00:00Z\r\n   \r\n`;
    const resultado = analizarCsvReloj(texto);
    expect(resultado).toEqual({
      valido: true,
      registros: [{ numeroRegistro: 'R-1', legajo: 'EMP-000001', tipo: 'Entrada', instante: '2026-11-03T13:00:00Z' }],
    });
  });

  it('rechaza una cabecera distinta o un archivo vacío', () => {
    expect(analizarCsvReloj('legajo,tipo,instante\nEMP-000001,Entrada,2026-11-03T13:00:00Z')).toEqual({ valido: false, error: MENSAJE_CABECERA });
    expect(analizarCsvReloj('numeroRegistro,legajo,instante,tipo\nR-1,EMP-000001,2026-11-03T13:00:00Z,Entrada')).toEqual({ valido: false, error: MENSAJE_CABECERA });
    expect(analizarCsvReloj('')).toEqual({ valido: false, error: MENSAJE_CABECERA });
    expect(MENSAJE_CABECERA).toBe('El archivo debe tener la cabecera numeroRegistro,legajo,tipo,instante.');
  });

  it('rechaza un archivo con solo la cabecera', () => {
    expect(analizarCsvReloj(`${CABECERA_CSV}\n\n`)).toEqual({ valido: false, error: MENSAJE_CANTIDAD });
  });

  it('acepta exactamente 1000 registros y rechaza 1001', () => {
    const maximo = analizarCsvReloj(`${CABECERA_CSV}\n${filas(1000)}`);
    expect(maximo.valido && maximo.registros.length).toBe(1000);
    expect(analizarCsvReloj(`${CABECERA_CSV}\n${filas(1001)}`)).toEqual({ valido: false, error: 'La importación debe tener entre 1 y 1000 registros.' });
  });

  it('deja vacíos los campos que faltan y un instante ilegible para que el servidor rechace solo ese registro', () => {
    const resultado = analizarCsvReloj(`${CABECERA_CSV}\nR-1,EMP-000001\n,EMP-000002,Salida,no-es-fecha`);
    expect(resultado).toEqual({
      valido: true,
      registros: [
        { numeroRegistro: 'R-1', legajo: 'EMP-000001', tipo: '', instante: null },
        { numeroRegistro: '', legajo: 'EMP-000002', tipo: 'Salida', instante: null },
      ],
    });
  });
});
