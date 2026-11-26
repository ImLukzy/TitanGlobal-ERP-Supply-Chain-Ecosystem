import { useState } from 'react';
import { useSesionOpcional } from '../layout/SesionContext';
import { mensajeApi } from './consultaPantalla';

export interface PaginaApi<T> {
  items: T[];
  pagina: number;
  tamanoPagina: number;
  totalRegistros: number;
  totalPaginas: number;
}

export function conFiltros(ruta: string, filtros: Record<string, string | number | boolean | null | undefined>) {
  const parametros = new URLSearchParams();
  for (const [clave, valor] of Object.entries(filtros)) {
    if (valor !== null && valor !== undefined && valor !== '') parametros.set(clave, String(valor));
  }
  return parametros.size === 0 ? ruta : ruta + '?' + parametros.toString();
}

export function textoFormulario(datos: FormData, campo: string): string {
  return String(datos.get(campo) ?? '').trim();
}

export function monedaFinanzas(valor: number, moneda: string): string {
  return new Intl.NumberFormat('es-PE', { style: 'currency', currency: moneda }).format(valor);
}

export function useEscrituraFinanzas(): boolean {
  const roles = useSesionOpcional()?.sesion?.roles ?? [];
  return roles.includes('FINANZAS') || roles.includes('ADMIN');
}

export function useOperacionFinanzas() {
  const [revision, revisar] = useState(0);
  const [ocupado, bloquear] = useState(false);
  const [aviso, informar] = useState('');
  const [error, indicarError] = useState('');

  function limpiar() {
    informar('');
    indicarError('');
  }

  async function ejecutar(operacion: () => Promise<unknown>, mensaje: string, continuar?: () => void) {
    bloquear(true);
    limpiar();
    try {
      await operacion();
      revisar(actual => actual + 1);
      informar(mensaje);
      continuar?.();
    } catch (causa) {
      indicarError(mensajeApi(causa));
    } finally {
      bloquear(false);
    }
  }

  return { revision, ocupado, aviso, error, limpiar, indicarError, ejecutar };
}

export async function todasLasPaginas<T>(cargar: (pagina: number) => Promise<PaginaApi<T>>) {
  const primera = await cargar(1);
  const resultado = [...primera.items];
  for (let pagina = 2; pagina <= primera.totalPaginas; pagina++) {
    resultado.push(...(await cargar(pagina)).items);
  }
  return resultado;
}
