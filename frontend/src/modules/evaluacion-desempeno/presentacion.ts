import { useEffect, type RefObject } from 'react';
import { ApiError } from '../../api/client';
import type { EstadoCiclo, EstadoEvaluacion, PeriodoCiclo } from './tipos';

export const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden modificar evaluaciones.';

export const MAXIMO_SELECTOR = 100;

export const PERIODOS: PeriodoCiclo[] = ['Semestral', 'Anual'];

export const ESTADOS_CICLO: EstadoCiclo[] = ['Configuracion', 'Abierto', 'Cerrado'];

export const ESTADOS_EVALUACION: EstadoEvaluacion[] = ['Borrador', 'Enviada', 'Cerrada'];

export const ETIQUETAS_CALIFICACION: string[] = ['1 · Deficiente', '2 · Regular', '3 · Bueno', '4 · Muy bueno', '5 · Sobresaliente'];

export function textoEstadoCiclo(estado: EstadoCiclo): string {
  return estado === 'Configuracion' ? 'Configuración' : estado;
}

export function aEstadoCicloFiltro(valor: string): '' | EstadoCiclo {
  return ESTADOS_CICLO.find((estado) => estado === valor) ?? '';
}

export function aEstadoEvaluacionFiltro(valor: string): '' | EstadoEvaluacion {
  return ESTADOS_EVALUACION.find((estado) => estado === valor) ?? '';
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

// Las fechas del backend llegan como AAAA-MM-DD: se muestran como DD/MM/AAAA sin pasar por Date.
export function textoFecha(fecha: string | null): string {
  if (fecha === null) {
    return 'Sin dato';
  }
  const partes = /^(\d{4})-(\d{2})-(\d{2})/.exec(fecha);
  if (partes === null) {
    return fecha;
  }
  return `${partes[3]}/${partes[2]}/${partes[1]}`;
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
