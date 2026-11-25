import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import {
  listarEvaluaciones,
  listarProveedoresEvaluables,
  obtenerEvaluacion,
  obtenerResumenEvaluaciones,
} from './api';
import type {
  EvaluacionDetalle,
  FiltrosEvaluacion,
  PaginaEvaluaciones,
  ProveedorOpcion,
  ResumenEvaluaciones,
} from './tipos';

function obtenerMensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function useEvaluacionesData(filtros: FiltrosEvaluacion, seleccionadaId: number | null) {
  const [lista, setLista] = useState<PaginaEvaluaciones | null>(null);
  const [resumen, setResumen] = useState<ResumenEvaluaciones | null>(null);
  const [proveedores, setProveedores] = useState<ProveedorOpcion[]>([]);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorProveedores, setErrorProveedores] = useState('');
  const [evaluacion, setEvaluacion] = useState<EvaluacionDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState<{ id: number; message: string } | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarEvaluaciones(filtros).then((datos) => {
      if (vigente) {
        setErrorLista('');
        setLista(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(obtenerMensaje(error, 'No se pudieron cargar las evaluaciones.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenEvaluaciones().then((datos) => {
      if (vigente) {
        setErrorResumen('');
        setResumen(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorResumen(obtenerMensaje(error, 'No se pudo cargar el resumen.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [revision]);

  useEffect(() => {
    let vigente = true;
    void listarProveedoresEvaluables().then((datos) => {
      if (vigente) {
        setProveedores(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorProveedores(obtenerMensaje(error, 'No se pudieron cargar los proveedores.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, []);

  useEffect(() => {
    if (seleccionadaId === null) {
      return;
    }
    let vigente = true;
    void obtenerEvaluacion(seleccionadaId).then((datos) => {
      if (vigente) {
        setErrorDetalle(null);
        setEvaluacion(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorDetalle({ id: seleccionadaId, message: obtenerMensaje(error, 'No se pudo cargar la evaluación.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [seleccionadaId, revision]);

  const recargar = useCallback(() => setRevision((valor) => valor + 1), []);
  const evaluacionVisible = evaluacion?.id === seleccionadaId ? evaluacion : null;
  const mensajeErrorDetalle = errorDetalle?.id === seleccionadaId ? errorDetalle.message : '';
  return {
    lista,
    resumen,
    proveedores,
    errorLista,
    errorResumen,
    errorProveedores,
    evaluacion: evaluacionVisible,
    errorDetalle: mensajeErrorDetalle,
    cargandoDetalle: seleccionadaId !== null && evaluacionVisible === null && mensajeErrorDetalle === '',
    recargar,
  };
}
