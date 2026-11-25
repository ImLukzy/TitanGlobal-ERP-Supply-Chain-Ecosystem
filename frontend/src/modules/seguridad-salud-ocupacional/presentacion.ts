import { useCallback, useEffect, useState, type RefObject } from 'react';
import { ApiError } from '../../api/client';
import type { FiltroActivo, ResultadoExamen, SeveridadIncidente, TipoExamen, TipoIncidente } from './tipos';

export const TEXTO_SOLO_RRHH = 'Solo RRHH o ADMIN pueden modificar seguridad y salud.';

export const MAXIMO_SELECTOR = 100;

export const TAMANO_PAGINA = 10;

export const TIPOS_INCIDENTE: TipoIncidente[] = ['AccidenteSinBaja', 'AccidenteConBaja', 'IncidenteSinLesion', 'EnfermedadOcupacional'];

export const SEVERIDADES: SeveridadIncidente[] = ['Leve', 'Moderada', 'Grave', 'Mortal'];

export const TIPOS_EXAMEN: TipoExamen[] = ['Ingreso', 'Periodico', 'Retiro'];

export const RESULTADOS_EXAMEN: ResultadoExamen[] = ['Apto', 'AptoConRestricciones', 'NoApto'];

export const FILTROS_ACTIVO: FiltroActivo[] = ['Activos', 'Inactivos', 'Todos'];

const TEXTOS: Record<string, string> = {
  AccidenteSinBaja: 'Accidente sin baja',
  AccidenteConBaja: 'Accidente con baja',
  IncidenteSinLesion: 'Incidente sin lesión',
  EnfermedadOcupacional: 'Enfermedad ocupacional',
  AptoConRestricciones: 'Apto con restricciones',
  NoApto: 'No apto',
  Periodico: 'Periódico',
};

// Los valores del backend se muestran con tilde y espacios; los desconocidos pasan tal cual.
export function textoValor(valor: string): string {
  return TEXTOS[valor] ?? valor;
}

export function aTipoIncidente(valor: string): '' | TipoIncidente {
  return TIPOS_INCIDENTE.find((tipo) => tipo === valor) ?? '';
}

export function aSeveridad(valor: string): '' | SeveridadIncidente {
  return SEVERIDADES.find((severidad) => severidad === valor) ?? '';
}

export function aTipoExamen(valor: string): '' | TipoExamen {
  return TIPOS_EXAMEN.find((tipo) => tipo === valor) ?? '';
}

export function aResultado(valor: string): '' | ResultadoExamen {
  return RESULTADOS_EXAMEN.find((resultado) => resultado === valor) ?? '';
}

export function aFiltroActivo(valor: string): FiltroActivo {
  return FILTROS_ACTIVO.find((filtro) => filtro === valor) ?? 'Todos';
}

export function aNumeroOnulo(valor: string): number | null {
  return valor === '' ? null : Number(valor);
}

// La fecha de hoy se calcula en UTC, como el backend.
export function hoyIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export function anioActual(): number {
  return Number(hoyIso().slice(0, 4));
}

export function aniosDisponibles(actual: number): number[] {
  return Array.from({ length: 8 }, (_, indice) => actual + 2 - indice);
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

// Suma días a una fecha ISO con aritmética UTC; devuelve null si la fecha no es válida.
export function sumarDias(fecha: string, dias: number): string | null {
  if (!esFechaIso(fecha)) {
    return null;
  }
  const partes = fecha.split('-').map(Number);
  const resultado = new Date(Date.UTC(partes[0] ?? 0, (partes[1] ?? 1) - 1, (partes[2] ?? 1) + dias));
  return resultado.toISOString().slice(0, 10);
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

interface PorId<T> {
  id: number;
  valor: T;
}

// Listado paginado con recarga: se descartan las respuestas de filtros anteriores.
export function useListado<T, F>(cargar: (filtros: F) => Promise<T>, filtros: F, respaldo: string) {
  const [lista, setLista] = useState<T | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void cargar(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, respaldo));
      }
    });
    return () => {
      vigente = false;
    };
  }, [cargar, filtros, revision, respaldo]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  return { lista, errorLista, revision, recargar };
}

// El detalle y su error se guardan con el id al que pertenecen, para no mostrar datos de una selección anterior.
export function usePorId<T>(id: number | null, cargar: (id: number) => Promise<T>, revision: number, respaldo: string) {
  const [dato, setDato] = useState<PorId<T> | null>(null);
  const [error, setError] = useState<PorId<string> | null>(null);

  useEffect(() => {
    if (id === null) {
      return;
    }
    let vigente = true;
    void cargar(id).then((datos) => {
      if (vigente) {
        setDato({ id, valor: datos });
        setError(null);
      }
    }).catch((falla: unknown) => {
      if (vigente) {
        setError({ id, valor: mensajeError(falla, respaldo) });
      }
    });
    return () => {
      vigente = false;
    };
  }, [id, cargar, revision, respaldo]);

  return {
    detalle: dato !== null && dato.id === id ? dato.valor : null,
    error: error !== null && error.id === id ? error.valor : '',
  };
}
