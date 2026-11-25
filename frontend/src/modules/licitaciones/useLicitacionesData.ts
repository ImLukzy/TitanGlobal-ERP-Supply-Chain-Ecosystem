import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import {
  listarLicitaciones,
  listarProveedoresActivosLicitacion,
  obtenerLicitacion,
  obtenerResumenLicitaciones,
} from './api';
import type {
  FiltrosLicitacion,
  LicitacionDetalle,
  PaginaLicitaciones,
  ProveedorOpcion,
  ResumenLicitaciones,
} from './tipos';

function obtenerMensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function useLicitacionesData(filtros: FiltrosLicitacion, seleccionadaId: number | null) {
  const [lista, setLista] = useState<PaginaLicitaciones | null>(null);
  const [resumen, setResumen] = useState<ResumenLicitaciones | null>(null);
  const [proveedores, setProveedores] = useState<ProveedorOpcion[]>([]);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorProveedores, setErrorProveedores] = useState('');
  const [licitacion, setLicitacion] = useState<LicitacionDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState<{ id: number; message: string } | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarLicitaciones(filtros).then((datos) => {
      if (vigente) {
        setErrorLista('');
        setLista(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(obtenerMensaje(error, 'No se pudieron cargar las licitaciones.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenLicitaciones().then((datos) => {
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
    void listarProveedoresActivosLicitacion().then((datos) => {
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
    void obtenerLicitacion(seleccionadaId).then((datos) => {
      if (vigente) {
        setErrorDetalle(null);
        setLicitacion(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorDetalle({ id: seleccionadaId, message: obtenerMensaje(error, 'No se pudo cargar la licitación.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [seleccionadaId, revision]);

  const recargar = useCallback(() => setRevision((valor) => valor + 1), []);
  const licitacionVisible = licitacion?.id === seleccionadaId ? licitacion : null;
  const mensajeErrorDetalle = errorDetalle?.id === seleccionadaId ? errorDetalle.message : '';
  return {
    lista,
    resumen,
    proveedores,
    errorLista,
    errorResumen,
    errorProveedores,
    licitacion: licitacionVisible,
    errorDetalle: mensajeErrorDetalle,
    cargandoDetalle: seleccionadaId !== null && licitacionVisible === null && mensajeErrorDetalle === '',
    recargar,
  };
}
