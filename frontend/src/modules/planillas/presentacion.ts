import { useEffect, type RefObject } from 'react';
import { ApiError } from '../../api/client';
import type { EstadoPlanilla, TipoPlanilla } from './tipos';

export const TIPOS_PLANILLA: TipoPlanilla[] = ['Mensual', 'Complementaria'];

export const ESTADOS_PLANILLA: EstadoPlanilla[] = ['Borrador', 'Calculada', 'Aprobada', 'Pagada'];

export const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden modificar planillas.';

export const MESES: string[] = [
  'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio',
  'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre',
];

export const MAXIMO_EMPLEADOS_SELECTOR = 100;

export function aTipoPlanilla(valor: string): TipoPlanilla | null {
  return TIPOS_PLANILLA.find((tipo) => tipo === valor) ?? null;
}

export function aTipoFiltro(valor: string): '' | TipoPlanilla {
  return aTipoPlanilla(valor) ?? '';
}

export function aEstadoFiltro(valor: string): '' | EstadoPlanilla {
  return ESTADOS_PLANILLA.find((estado) => estado === valor) ?? '';
}

export function textoPeriodo(mes: number, anio: number): string {
  return `${mes}/${anio}`;
}

// El resumen envía el periodo como AAAA-MM: se muestra como «11/2026».
export function textoPeriodoResumen(periodo: string | null): string {
  if (periodo === null) {
    return '—';
  }
  const partes = /^(\d{4})-(\d{2})$/.exec(periodo);
  if (partes === null) {
    return periodo;
  }
  return textoPeriodo(Number(partes[2]), Number(partes[1]));
}

export function textoMes(mes: number): string {
  return MESES[mes - 1] ?? String(mes);
}

export function formatearMinutosExtra(minutos: number): string {
  return `${minutos} min`;
}

export function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
}

export function esConflicto(error: unknown): boolean {
  return error instanceof ApiError && error.statusCode === 409;
}

export function hoyUtc(): string {
  return new Date().toISOString().slice(0, 10);
}

const formatoFechaIso = /^\d{4}-\d{2}-\d{2}$/;

// Fecha ISO real (rechaza «2026-02-31»): aritmética UTC, nunca depende de la zona local.
export function esFechaIso(fecha: string): boolean {
  if (!formatoFechaIso.test(fecha)) {
    return false;
  }
  const partes = fecha.split('-').map(Number);
  const anio = partes[0] ?? 0;
  const mes = partes[1] ?? 0;
  const dia = partes[2] ?? 0;
  const comprobacion = new Date(Date.UTC(anio, mes - 1, dia));
  return comprobacion.getUTCFullYear() === anio && comprobacion.getUTCMonth() === mes - 1 && comprobacion.getUTCDate() === dia;
}

// Año de un filtro escrito a mano: vacío = todos (null); un año válido de 2000 a 2100 se aplica; otra cosa (undefined) no cambia el filtro.
export function interpretarAnioFiltro(texto: string): number | null | undefined {
  const limpio = texto.trim();
  if (limpio === '') {
    return null;
  }
  if (!/^\d{4}$/.test(limpio)) {
    return undefined;
  }
  const anio = Number(limpio);
  return anio >= 2000 && anio <= 2100 ? anio : undefined;
}

const SELECTOR_ENFOCABLES = 'input:not(:disabled), select:not(:disabled), textarea:not(:disabled), button:not(:disabled)';

// Comportamiento común de los diálogos modales: Escape cierra (salvo mientras se envía) y Tab no sale del diálogo.
export function useDialogoModal(dialogoRef: RefObject<HTMLDivElement | null>, cerrar: () => void, enviando: boolean): void {
  useEffect(() => {
    function manejarTeclado(event: globalThis.KeyboardEvent): void {
      if (event.key === 'Escape') {
        event.preventDefault();
        if (!enviando) {
          cerrar();
        }
      }
      if (event.key === 'Tab' && dialogoRef.current !== null) {
        const elementos = Array.from(dialogoRef.current.querySelectorAll<HTMLElement>(SELECTOR_ENFOCABLES));
        const primero = elementos[0];
        const ultimo = elementos[elementos.length - 1];
        if (primero !== undefined && ultimo !== undefined) {
          if (event.shiftKey && document.activeElement === primero) {
            event.preventDefault();
            ultimo.focus();
          } else if (!event.shiftKey && document.activeElement === ultimo) {
            event.preventDefault();
            primero.focus();
          }
        }
      }
    }
    document.addEventListener('keydown', manejarTeclado);
    return () => document.removeEventListener('keydown', manejarTeclado);
  }, [dialogoRef, cerrar, enviando]);
}
