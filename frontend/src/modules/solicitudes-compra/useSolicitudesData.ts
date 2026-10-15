import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import {
  listarProductosSolicitud,
  listarProveedoresActivos,
  listarSolicitudes,
  obtenerResumenSolicitudes,
  obtenerSolicitud,
} from './api';
import type {
  FiltrosSolicitud,
  PaginaSolicitudes,
  ProductoOpcion,
  ProveedorOpcion,
  ResumenSolicitudes,
  SolicitudDetalle,
} from './tipos';

function obtenerMensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function useSolicitudesData(filtros: FiltrosSolicitud, seleccionadaId: number | null) {
  const [lista, setLista] = useState<PaginaSolicitudes | null>(null);
  const [resumen, setResumen] = useState<ResumenSolicitudes | null>(null);
  const [proveedores, setProveedores] = useState<ProveedorOpcion[]>([]);
  const [productos, setProductos] = useState<ProductoOpcion[]>([]);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorCatalogos, setErrorCatalogos] = useState('');
  const [solicitud, setSolicitud] = useState<SolicitudDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState<{ id: number; message: string } | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarSolicitudes(filtros).then((datos) => {
      if (vigente) {
        setErrorLista('');
        setLista(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(obtenerMensaje(error, 'No se pudieron cargar las solicitudes.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenSolicitudes().then((datos) => {
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
    void Promise.all([listarProveedoresActivos(), listarProductosSolicitud()]).then(([activos, catalogo]) => {
      if (vigente) {
        setProveedores(activos);
        setProductos(catalogo);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorCatalogos(obtenerMensaje(error, 'No se pudieron cargar proveedores y productos.'));
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
    void obtenerSolicitud(seleccionadaId).then((datos) => {
      if (vigente) {
        setErrorDetalle(null);
        setSolicitud(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorDetalle({
          id: seleccionadaId,
          message: obtenerMensaje(error, 'No se pudo cargar el detalle.'),
        });
      }
    });
    return () => {
      vigente = false;
    };
  }, [seleccionadaId, revision]);

  const recargar = useCallback(() => setRevision((valor) => valor + 1), []);
  const solicitudVisible = solicitud?.id === seleccionadaId ? solicitud : null;
  const mensajeErrorDetalle = errorDetalle?.id === seleccionadaId ? errorDetalle.message : '';
  return {
    lista,
    resumen,
    proveedores,
    productos,
    errorLista,
    errorResumen,
    errorCatalogos,
    solicitud: solicitudVisible,
    errorDetalle: mensajeErrorDetalle,
    cargandoDetalle: seleccionadaId !== null && solicitudVisible === null && mensajeErrorDetalle === '',
    recargar,
  };
}
