import { useEffect, type RefObject } from 'react';
import { ApiError } from '../../api/client';
import type { EstadoContrato, ModalidadContrato } from './tipos';

const textosEstado: Record<EstadoContrato, string> = {
  Vigente: 'Vigente',
  Renovado: 'Renovado',
  Vencido: 'Vencido',
  Cerrado: 'Cerrado',
  Anulado: 'Anulado',
};

const textosModalidad: Record<ModalidadContrato, string> = {
  Indeterminado: 'Indeterminado',
  PlazoFijo: 'Plazo fijo',
  Formativo: 'Formativo',
};

export const ESTADOS: EstadoContrato[] = ['Vigente', 'Renovado', 'Vencido', 'Cerrado', 'Anulado'];

export const MODALIDADES: ModalidadContrato[] = ['Indeterminado', 'PlazoFijo', 'Formativo'];

export const TEXTO_SIN_FECHA_FIN = 'Sin fecha de fin';

export const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden modificar contratos.';

export const TEXTO_INDETERMINADO = 'Un contrato indeterminado no se renueva.';

export function textoEstado(estado: EstadoContrato): string {
  return textosEstado[estado];
}

export function textoModalidad(modalidad: ModalidadContrato): string {
  return textosModalidad[modalidad];
}

export function aModalidad(valor: string): ModalidadContrato {
  if (valor === 'Indeterminado' || valor === 'PlazoFijo' || valor === 'Formativo') {
    return valor;
  }
  return 'Indeterminado';
}

export function aEstado(valor: string): '' | EstadoContrato {
  if (valor === 'Vigente' || valor === 'Renovado' || valor === 'Vencido' || valor === 'Cerrado' || valor === 'Anulado') {
    return valor;
  }
  return '';
}

export function aModalidadFiltro(valor: string): '' | ModalidadContrato {
  if (valor === 'Indeterminado' || valor === 'PlazoFijo' || valor === 'Formativo') {
    return valor;
  }
  return '';
}

export function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
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
