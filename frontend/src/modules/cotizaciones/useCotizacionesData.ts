import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import {
  listarClientesCotizaciones,
  listarProductosCotizaciones,
  listarCotizaciones,
  obtenerCotizacion,
  obtenerResumenCotizaciones,
} from './api';
import type {
  CotizacionDetalle,
  FiltrosCotizacion,
  PaginaCotizaciones,
  ResumenCotizaciones,
} from './tipos';

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function useCotizacionesData(
  filtros: FiltrosCotizacion,
  seleccionadaId: number | null,
) {
  const [lista, setLista] = useState<PaginaCotizaciones | null>(null);
  const [resumen, setResumen] = useState<ResumenCotizaciones | null>(null);
  const [clientes, setClientes] = useState<{ id: number; nombre: string }[]>(
    [],
  );
  const [productos, setProductos] = useState<
    { id: number; nombre: string; precio: number }[]
  >([]);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorCatalogos, setErrorCatalogos] = useState('');
  const [detalle, setDetalle] = useState<CotizacionDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState<{
    id: number;
    message: string;
  } | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarCotizaciones(filtros)
      .then((datos) => {
        if (vigente) {
          setLista(datos);
          setErrorLista('');
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          setErrorLista(
            mensaje(error, 'No se pudieron cargar las cotizaciones.'),
          );
        }
      });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenCotizaciones()
      .then((datos) => {
        if (vigente) {
          setResumen(datos);
          setErrorResumen('');
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          setErrorResumen(mensaje(error, 'No se pudo cargar el resumen.'));
        }
      });
    return () => {
      vigente = false;
    };
  }, [revision]);

  useEffect(() => {
    let vigente = true;
    void Promise.all([
      listarClientesCotizaciones(),
      listarProductosCotizaciones(),
    ])
      .then(([listaClientes, listaProductos]) => {
        if (vigente) {
          setClientes(listaClientes);
          setProductos(listaProductos);
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          setErrorCatalogos(
            mensaje(error, 'No se pudieron cargar clientes y productos.'),
          );
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
    void obtenerCotizacion(seleccionadaId)
      .then((datos) => {
        if (vigente) {
          setDetalle(datos);
          setErrorDetalle(null);
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          setErrorDetalle({
            id: seleccionadaId,
            message: mensaje(error, 'No se pudo cargar el detalle.'),
          });
        }
      });
    return () => {
      vigente = false;
    };
  }, [seleccionadaId, revision]);

  const recargar = useCallback(() => setRevision((valor) => valor + 1), []);
  const detalleVisible = detalle?.id === seleccionadaId ? detalle : null;
  return {
    lista,
    resumen,
    clientes,
    productos,
    errorLista,
    errorResumen,
    errorCatalogos,
    detalle: detalleVisible,
    errorDetalle:
      errorDetalle?.id === seleccionadaId ? errorDetalle.message : '',
    cargandoDetalle: seleccionadaId !== null && detalleVisible === null,
    recargar,
  };
}
