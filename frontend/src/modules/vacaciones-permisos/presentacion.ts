import { useEffect, type RefObject } from 'react';
import { ApiError } from '../../api/client';
import type { EstadoAusencia, TipoAusencia } from './tipos';

const textosTipo: Record<TipoAusencia, string> = {
  Vacaciones: 'Vacaciones',
  PermisoConGoce: 'Permiso con goce',
  PermisoSinGoce: 'Permiso sin goce',
  LicenciaMaternidad: 'Licencia de maternidad',
  LicenciaPaternidad: 'Licencia de paternidad',
  DescansoMedico: 'Descanso médico',
};

export const TIPOS: TipoAusencia[] = [
  'Vacaciones',
  'PermisoConGoce',
  'PermisoSinGoce',
  'LicenciaMaternidad',
  'LicenciaPaternidad',
  'DescansoMedico',
];

export const ESTADOS: EstadoAusencia[] = ['Pendiente', 'Aprobada', 'Rechazada', 'Cancelada'];

export const TIPOS_CON_MOTIVO_OBLIGATORIO: TipoAusencia[] = ['PermisoConGoce', 'PermisoSinGoce', 'DescansoMedico'];

export const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden aprobar o rechazar ausencias.';

export function textoTipo(tipo: TipoAusencia): string {
  return textosTipo[tipo];
}

export function textoEstado(estado: EstadoAusencia): string {
  return estado;
}

export function aTipo(valor: string): TipoAusencia | null {
  const encontrado = TIPOS.find((tipo) => tipo === valor);
  return encontrado ?? null;
}

export function aTipoFiltro(valor: string): '' | TipoAusencia {
  return aTipo(valor) ?? '';
}

export function aEstadoFiltro(valor: string): '' | EstadoAusencia {
  const encontrado = ESTADOS.find((estado) => estado === valor);
  return encontrado ?? '';
}

export function mensajeError(error: unknown, respaldo: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }
  return respaldo;
}

const formatoFechaIso = /^\d{4}-\d{2}-\d{2}$/;

function instanteUtc(fecha: string): number | null {
  if (!formatoFechaIso.test(fecha)) {
    return null;
  }
  const partes = fecha.split('-').map(Number);
  const anio = partes[0] ?? 0;
  const mes = partes[1] ?? 0;
  const dia = partes[2] ?? 0;
  const instante = Date.UTC(anio, mes - 1, dia);
  const comprobacion = new Date(instante);
  if (comprobacion.getUTCFullYear() !== anio || comprobacion.getUTCMonth() !== mes - 1 || comprobacion.getUTCDate() !== dia) {
    return null;
  }
  return instante;
}

export function esFechaIso(fecha: string): boolean {
  return instanteUtc(fecha) !== null;
}

// Días naturales de la solicitud, ambos extremos incluidos (como el backend). Aritmética UTC: nunca depende de la zona local.
export function calcularDias(inicio: string, fin: string): number | null {
  const desde = instanteUtc(inicio);
  const hasta = instanteUtc(fin);
  if (desde === null || hasta === null || hasta < desde) {
    return null;
  }
  return Math.round((hasta - desde) / 86_400_000) + 1;
}

export function hoyUtc(): string {
  return new Date().toISOString().slice(0, 10);
}

// Una ausencia solo se cancela si todavía no ha empezado (la fecha de inicio es posterior a hoy).
export function puedeCancelar(estado: EstadoAusencia, fechaInicio: string): boolean {
  return (estado === 'Pendiente' || estado === 'Aprobada') && fechaInicio > hoyUtc();
}

export function formatearDecimal(valor: number): string {
  return valor.toFixed(1);
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
