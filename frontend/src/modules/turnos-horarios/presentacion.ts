import { useEffect, type RefObject } from 'react';
import { ApiError } from '../../api/client';
import type { DiaProgramado, EstadoAsignacion, FiltroActivo, TipoTurno, TurnoListado } from './tipos';

export const TIPOS: TipoTurno[] = ['Fijo', 'Rotativo'];

export const ESTADOS_ASIGNACION: EstadoAsignacion[] = ['Vigente', 'Futura', 'Finalizada'];

export const FILTROS_ACTIVO: FiltroActivo[] = ['Activos', 'Inactivos', 'Todos'];

export const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden modificar turnos.';

export const TEXTO_TURNO_CON_ASIGNACIONES = 'El turno tiene asignaciones: crea uno nuevo.';

export const TEXTO_SOLO_FUTURAS = 'Solo se puede eliminar una asignación que todavía no empezó.';

export const TEXTO_SIN_FIN = 'Sin fin';

export const DIAS_PROGRAMACION = 14;

const DIAS_CORTOS: Record<number, string> = { 1: 'L', 2: 'M', 3: 'X', 4: 'J', 5: 'V', 6: 'S', 7: 'D' };

export const DIAS_SEMANA: { numero: number; nombre: string }[] = [
  { numero: 1, nombre: 'Lunes' },
  { numero: 2, nombre: 'Martes' },
  { numero: 3, nombre: 'Miércoles' },
  { numero: 4, nombre: 'Jueves' },
  { numero: 5, nombre: 'Viernes' },
  { numero: 6, nombre: 'Sábado' },
  { numero: 7, nombre: 'Domingo' },
];

export function aTipoFiltro(valor: string): '' | TipoTurno {
  if (valor === 'Fijo' || valor === 'Rotativo') {
    return valor;
  }
  return '';
}

export function aTipoTurno(valor: string): TipoTurno {
  if (valor === 'Rotativo') {
    return 'Rotativo';
  }
  return 'Fijo';
}

export function aFiltroActivo(valor: string): FiltroActivo {
  if (valor === 'Activos' || valor === 'Inactivos') {
    return valor;
  }
  return 'Todos';
}

export function aEstadoAsignacion(valor: string): '' | EstadoAsignacion {
  if (valor === 'Vigente' || valor === 'Futura' || valor === 'Finalizada') {
    return valor;
  }
  return '';
}

// La API envía las horas como HH:mm:ss; se muestran como HH:mm.
export function formatearHora(hora: string): string {
  return hora.slice(0, 5);
}

// Los campos type="time" dan HH:mm; la API recibe HH:mm:ss.
export function horaParaApi(hora: string): string {
  if (hora.length === 5) {
    return `${hora}:00`;
  }
  return hora;
}

export function horarioTurno(horaEntrada: string, horaSalida: string, cruzaMedianoche: boolean): string {
  const sufijo = cruzaMedianoche ? ' (+1)' : '';
  return `${formatearHora(horaEntrada)} – ${formatearHora(horaSalida)}${sufijo}`;
}

export function diasOCiclo(turno: TurnoListado): string {
  if (turno.tipo === 'Rotativo') {
    return `${turno.diasTrabajo ?? 0} x ${turno.diasDescanso ?? 0}`;
  }
  return turno.diasLaborables.map((dia) => DIAS_CORTOS[dia] ?? '').join(' ');
}

export function nombresDias(dias: number[]): string {
  return dias
    .map((dia) => DIAS_SEMANA.find((item) => item.numero === dia)?.nombre ?? '')
    .join(', ');
}

// Las fechas viajan como yyyy-MM-dd y se calculan siempre en UTC, igual que el «hoy» del backend.
export function hoyUtc(): string {
  return new Date().toISOString().slice(0, 10);
}

export function sumarDias(fecha: string, dias: number): string {
  const base = new Date(`${fecha}T00:00:00Z`);
  base.setUTCDate(base.getUTCDate() + dias);
  return base.toISOString().slice(0, 10);
}

export function diaDeLaSemana(fecha: string): string {
  const indice = new Date(`${fecha}T00:00:00Z`).getUTCDay();
  const numero = indice === 0 ? 7 : indice;
  return DIAS_SEMANA.find((dia) => dia.numero === numero)?.nombre ?? '';
}

// El estado del día se muestra siempre con texto.
export function textoDia(dia: DiaProgramado): string {
  if (dia.laborable) {
    return 'Laborable';
  }
  if (dia.observacion === 'Cesado') {
    return 'Cesado';
  }
  if (dia.observacion === 'SinTurno') {
    return 'Sin turno';
  }
  return 'Descanso';
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
