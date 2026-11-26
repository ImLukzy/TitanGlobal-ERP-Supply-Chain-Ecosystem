import { useCallback, useEffect, useState } from 'react';
import { listarEmpleados } from '../empleados/api';
import type { PaginaEmpleados } from '../empleados/tipos';
import { listarCiclos, listarEvaluaciones, obtenerEvaluacion } from './api';
import { MAXIMO_SELECTOR, mensajeError } from './presentacion';
import type { EvaluacionDetalle, FiltrosEvaluaciones, PaginaCiclos, PaginaEvaluaciones } from './tipos';

interface PorEvaluacion<T> {
  evaluacionId: number;
  valor: T;
}

// Selectores de la vista: ciclos (para el filtro y el alta) y empleados activos (evaluado y evaluador).
export function useSelectoresEvaluacion() {
  const [ciclos, setCiclos] = useState<PaginaCiclos | null>(null);
  const [empleados, setEmpleados] = useState<PaginaEmpleados | null>(null);
  const [errorSelectores, setErrorSelectores] = useState('');

  useEffect(() => {
    let vigente = true;
    void listarCiclos({ busqueda: '', estado: '', pagina: 1, tamanoPagina: MAXIMO_SELECTOR }).then((datos) => {
      if (vigente) {
        setCiclos(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorSelectores(mensajeError(error, 'No se pudo cargar la lista de ciclos.'));
      }
    });
    void listarEmpleados({ busqueda: '', estado: 'Activo', pagina: 1, tamanoPagina: MAXIMO_SELECTOR }).then((datos) => {
      if (vigente) {
        setEmpleados(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorSelectores(mensajeError(error, 'No se pudo cargar la lista de empleados.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, []);

  return { ciclos, empleados, errorSelectores };
}

export function useEvaluacionesData(filtros: FiltrosEvaluaciones) {
  const [lista, setLista] = useState<PaginaEvaluaciones | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarEvaluaciones(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de evaluaciones.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  return { lista, errorLista, revision, recargar };
}

// El detalle y su error se guardan con la evaluación a la que pertenecen, para ignorar respuestas de una selección anterior.
export function useEvaluacionDetalle(evaluacionId: number | null, revision: number) {
  const [detalle, setDetalle] = useState<PorEvaluacion<EvaluacionDetalle> | null>(null);
  const [error, setError] = useState<PorEvaluacion<string> | null>(null);

  useEffect(() => {
    if (evaluacionId === null) {
      return;
    }
    let vigente = true;
    void obtenerEvaluacion(evaluacionId).then((datos) => {
      if (vigente) {
        setDetalle({ evaluacionId, valor: datos });
        setError(null);
      }
    }).catch((falla: unknown) => {
      if (vigente) {
        setError({ evaluacionId, valor: mensajeError(falla, 'No se pudo cargar la evaluación.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [evaluacionId, revision]);

  return {
    detalle: detalle !== null && detalle.evaluacionId === evaluacionId ? detalle.valor : null,
    error: error !== null && error.evaluacionId === evaluacionId ? error.valor : '',
  };
}
