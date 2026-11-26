import { useEffect, type RefObject } from 'react';
import { ApiError } from '../../api/client';
import type { EstadoPrestamo, PrestamoDetalle, TipoPrestamo } from './tipos';

const textosTipo: Record<TipoPrestamo, string> = {
  Adelanto: 'Adelanto',
  Prestamo: 'Préstamo',
};

export const TIPOS: TipoPrestamo[] = ['Adelanto', 'Prestamo'];

export const ESTADOS: EstadoPrestamo[] = ['Activo', 'Cancelado', 'Anulado'];

export const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden registrar o anular préstamos.';

export function textoTipo(tipo: TipoPrestamo): string {
  return textosTipo[tipo];
}

export function textoEstado(estado: EstadoPrestamo): string {
  return estado;
}

export function aTipo(valor: string): TipoPrestamo | null {
  const encontrado = TIPOS.find((tipo) => tipo === valor);
  return encontrado ?? null;
}

export function aTipoFiltro(valor: string): '' | TipoPrestamo {
  return aTipo(valor) ?? '';
}

export function aEstadoFiltro(valor: string): '' | EstadoPrestamo {
  const encontrado = ESTADOS.find((estado) => estado === valor);
  return encontrado ?? '';
}

export function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
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

// Motivo por el que un préstamo no se puede anular (mensajes 18 y 19 de la spec backend) o null si se puede.
export function motivoNoAnulable(prestamo: PrestamoDetalle): string | null {
  if (prestamo.estado !== 'Activo') {
    return `El préstamo ${prestamo.numero} no está activo y no se puede anular.`;
  }
  const conDescuentos = prestamo.cuotas.some((cuota) => cuota.montoDescontado > 0 || cuota.estado === 'Descontada');
  if (conDescuentos) {
    return `El préstamo ${prestamo.numero} ya tiene descuentos y no se puede anular.`;
  }
  return null;
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
