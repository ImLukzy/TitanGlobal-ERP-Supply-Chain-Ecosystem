import { useEffect, type RefObject } from 'react';
import { ApiError } from '../../api/client';
import type { EstadoHorasExtra, EstadoJornada, OrigenMarcacion, TipoMarcacion } from './tipos';

export const TIPOS_MARCACION: TipoMarcacion[] = ['Entrada', 'Salida'];

export const ORIGENES_MARCACION: OrigenMarcacion[] = ['Reloj', 'Manual'];

export const ESTADOS_JORNADA: EstadoJornada[] = ['Asistencia', 'Tardanza', 'SinMarcacion', 'Falta', 'DescansoTrabajado'];

export const ESTADOS_HORAS_EXTRA: EstadoHorasExtra[] = ['NoAplica', 'Pendiente', 'Aprobada', 'Rechazada'];

export const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden modificar la asistencia.';

const TEXTOS_ESTADO: Record<EstadoJornada, string> = {
  Asistencia: 'Asistencia',
  Tardanza: 'Tardanza',
  SinMarcacion: 'Sin marcación',
  Falta: 'Falta',
  DescansoTrabajado: 'Descanso trabajado',
};

const TEXTOS_HORAS_EXTRA: Record<EstadoHorasExtra, string> = {
  NoAplica: 'No aplica',
  Pendiente: 'Pendiente',
  Aprobada: 'Aprobada',
  Rechazada: 'Rechazada',
};

export function textoEstado(estado: EstadoJornada): string {
  return TEXTOS_ESTADO[estado];
}

export function textoHorasExtra(estado: EstadoHorasExtra): string {
  return TEXTOS_HORAS_EXTRA[estado];
}

export function aTipoFiltro(valor: string): '' | TipoMarcacion {
  if (valor === 'Entrada' || valor === 'Salida') {
    return valor;
  }
  return '';
}

export function aOrigenFiltro(valor: string): '' | OrigenMarcacion {
  if (valor === 'Reloj' || valor === 'Manual') {
    return valor;
  }
  return '';
}

export function aEstadoJornada(valor: string): '' | EstadoJornada {
  const encontrado = ESTADOS_JORNADA.find((estado) => estado === valor);
  return encontrado ?? '';
}

export function aEstadoHorasExtra(valor: string): '' | EstadoHorasExtra {
  const encontrado = ESTADOS_HORAS_EXTRA.find((estado) => estado === valor);
  return encontrado ?? '';
}

// Los turnos están en hora de Perú, un desfase fijo de UTC−5 (sin horario de verano). Todo se calcula con aritmética explícita,
// nunca con la zona horaria del navegador.
const DESFASE_PERU_MS = 5 * 60 * 60 * 1000;

const MS_POR_DIA = 24 * 60 * 60 * 1000;

// Un instante sin zona se interpreta como UTC, igual que el backend.
function instanteEnMilisegundos(instante: string): number {
  const tieneZona = /(Z|[+-]\d{2}:?\d{2})$/i.test(instante);
  return Date.parse(tieneZona ? instante : `${instante}Z`);
}

function aTextoLocal(instante: string): string {
  return new Date(instanteEnMilisegundos(instante) - DESFASE_PERU_MS).toISOString();
}

export function aHoraLocal(instante: string): string {
  return aTextoLocal(instante).slice(11, 16);
}

export function aFechaLocal(instante: string): string {
  return aTextoLocal(instante).slice(0, 10);
}

export function aFechaHoraLocal(instante: string): string {
  const texto = aTextoLocal(instante);
  return `${texto.slice(8, 10)}/${texto.slice(5, 7)}/${texto.slice(0, 4)} ${texto.slice(11, 16)}`;
}

// Convierte una fecha y una hora locales (yyyy-MM-dd y HH:mm) al instante UTC que se envía a la API.
export function localAUtc(fecha: string, hora: string): string {
  return new Date(Date.parse(`${fecha}T${hora}:00Z`) + DESFASE_PERU_MS).toISOString();
}

export function hoyPeru(): string {
  return new Date(Date.now() - DESFASE_PERU_MS).toISOString().slice(0, 10);
}

export function sumarDias(fecha: string, dias: number): string {
  return new Date(Date.parse(`${fecha}T00:00:00Z`) + dias * MS_POR_DIA).toISOString().slice(0, 10);
}

export function diasEntre(desde: string, hasta: string): number {
  return Math.round((Date.parse(`${hasta}T00:00:00Z`) - Date.parse(`${desde}T00:00:00Z`)) / MS_POR_DIA);
}

// Rango por defecto: de lunes a hoy, ambos en hora de Perú.
export function semanaActual(): { desde: string; hasta: string } {
  const hoy = hoyPeru();
  const diaSemana = new Date(`${hoy}T00:00:00Z`).getUTCDay();
  const retroceso = diaSemana === 0 ? 6 : diaSemana - 1;
  return { desde: sumarDias(hoy, -retroceso), hasta: hoy };
}

// La API envía las horas programadas como HH:mm:ss; se muestran como HH:mm.
export function formatearHoraProgramada(hora: string | null): string {
  return hora === null ? '—' : hora.slice(0, 5);
}

// 85 minutos se muestran como «1 h 25 min».
export function formatearMinutos(minutos: number): string {
  const horas = Math.floor(minutos / 60);
  const resto = minutos % 60;
  if (horas === 0) {
    return `${resto} min`;
  }
  if (resto === 0) {
    return `${horas} h`;
  }
  return `${horas} h ${resto} min`;
}

// Un instante de otro día local que la jornada (salida de un turno nocturno) se marca con «(+1)».
export function horaDeJornada(instante: string | null, fechaJornada: string): string {
  if (instante === null) {
    return '—';
  }
  const sufijo = aFechaLocal(instante) === fechaJornada ? '' : ' (+1)';
  return `${aHoraLocal(instante)}${sufijo}`;
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
