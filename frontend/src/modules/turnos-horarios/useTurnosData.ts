import { useCallback, useEffect, useState } from 'react';
import { listarTurnos, obtenerResumenTurnos, obtenerTurno } from './api';
import { mensajeError } from './presentacion';
import type { FiltrosTurnos, PaginaTurnos, ResumenTurnos, TurnoDetalle } from './tipos';

interface ErrorPorTurno {
  turnoId: number;
  mensaje: string;
}

// El detalle y su error se guardan con el id al que pertenecen, para no mostrar
// datos o fallos de una selección anterior tras cambiar de turno.
export function useTurnosData(filtros: FiltrosTurnos, seleccionadoId: number | null) {
  const [lista, setLista] = useState<PaginaTurnos | null>(null);
  const [resumen, setResumen] = useState<ResumenTurnos | null>(null);
  const [detalle, setDetalle] = useState<TurnoDetalle | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorDetalle, setErrorDetalle] = useState<ErrorPorTurno | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarTurnos(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de turnos.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenTurnos().then((datos) => {
      if (vigente) {
        setResumen(datos);
        setErrorResumen('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de turnos.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [revision]);

  useEffect(() => {
    if (seleccionadoId === null) {
      return;
    }
    const turnoId = seleccionadoId;
    let vigente = true;
    void obtenerTurno(turnoId).then((datos) => {
      if (vigente) {
        setDetalle(datos);
        setErrorDetalle(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setDetalle(null);
        setErrorDetalle({ turnoId, mensaje: mensajeError(error, 'No se pudo cargar el detalle del turno.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [seleccionadoId, revision]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  const turno = detalle !== null && detalle.id === seleccionadoId ? detalle : null;
  const mensajeDetalle = errorDetalle !== null && errorDetalle.turnoId === seleccionadoId ? errorDetalle.mensaje : '';
  return { lista, resumen, turno, errorLista, errorResumen, errorDetalle: mensajeDetalle, recargar };
}
