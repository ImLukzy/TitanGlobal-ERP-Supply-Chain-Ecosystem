import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import {
  listarOrdenesAbastecimiento,
  listarProductosOrden,
  listarProveedoresActivosOrden,
  obtenerOrdenAbastecimiento,
  obtenerResumenOrdenesAbastecimiento,
} from './api';
import type {
  FiltrosOrdenAbastecimiento,
  OrdenAbastecimientoDetalle,
  PaginaOrdenesAbastecimiento,
  ProductoOrdenOpcion,
  ProveedorOrdenOpcion,
  ResumenOrdenesAbastecimiento,
} from './tipos';

function obtenerMensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function useOrdenesData(filtros: FiltrosOrdenAbastecimiento, seleccionadaId: number | null) {
  const [lista, setLista] = useState<PaginaOrdenesAbastecimiento | null>(null);
  const [resumen, setResumen] = useState<ResumenOrdenesAbastecimiento | null>(null);
  const [proveedores, setProveedores] = useState<ProveedorOrdenOpcion[]>([]);
  const [productos, setProductos] = useState<ProductoOrdenOpcion[]>([]);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorCatalogos, setErrorCatalogos] = useState('');
  const [orden, setOrden] = useState<OrdenAbastecimientoDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState<{ id: number; message: string } | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarOrdenesAbastecimiento(filtros).then((datos) => {
      if (vigente) {
        setErrorLista('');
        setLista(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(obtenerMensaje(error, 'No se pudieron cargar las órdenes.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenOrdenesAbastecimiento().then((datos) => {
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
    void Promise.all([listarProveedoresActivosOrden(), listarProductosOrden()]).then(([activos, catalogo]) => {
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
    void obtenerOrdenAbastecimiento(seleccionadaId).then((datos) => {
      if (vigente) {
        setErrorDetalle(null);
        setOrden(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorDetalle({ id: seleccionadaId, message: obtenerMensaje(error, 'No se pudo cargar el detalle.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [seleccionadaId, revision]);

  const recargar = useCallback(() => setRevision((valor) => valor + 1), []);
  const ordenVisible = orden?.id === seleccionadaId ? orden : null;
  const mensajeErrorDetalle = errorDetalle?.id === seleccionadaId ? errorDetalle.message : '';
  return {
    lista,
    resumen,
    proveedores,
    productos,
    errorLista,
    errorResumen,
    errorCatalogos,
    orden: ordenVisible,
    errorDetalle: mensajeErrorDetalle,
    cargandoDetalle: seleccionadaId !== null && ordenVisible === null && mensajeErrorDetalle === '',
    recargar,
  };
}
