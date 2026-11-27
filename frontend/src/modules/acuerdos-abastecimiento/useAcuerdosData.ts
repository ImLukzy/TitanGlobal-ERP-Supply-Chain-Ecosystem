import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import {
  listarAcuerdos,
  listarProductosAcuerdo,
  listarProveedoresActivosAcuerdo,
  obtenerAcuerdo,
  obtenerResumenAcuerdos,
} from './api';
import type { AcuerdoDetalle, FiltrosAcuerdo, PaginaAcuerdos, ProductoOpcion, ProveedorOpcion, ResumenAcuerdos } from './tipos';

function obtenerMensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function useAcuerdosData(filtros: FiltrosAcuerdo, seleccionadoId: number | null) {
  const [lista, setLista] = useState<PaginaAcuerdos | null>(null);
  const [resumen, setResumen] = useState<ResumenAcuerdos | null>(null);
  const [proveedores, setProveedores] = useState<ProveedorOpcion[]>([]);
  const [productos, setProductos] = useState<ProductoOpcion[]>([]);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorOpciones, setErrorOpciones] = useState('');
  const [acuerdo, setAcuerdo] = useState<AcuerdoDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState<{ id: number; message: string } | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarAcuerdos(filtros).then((datos) => {
      if (vigente) {
        setErrorLista('');
        setLista(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(obtenerMensaje(error, 'No se pudieron cargar los acuerdos.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenAcuerdos().then((datos) => {
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
    void Promise.all([listarProveedoresActivosAcuerdo(), listarProductosAcuerdo()]).then(([proveedoresActivos, productosLista]) => {
      if (vigente) {
        setProveedores(proveedoresActivos);
        setProductos(productosLista);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorOpciones(obtenerMensaje(error, 'No se pudieron cargar los proveedores y productos.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, []);

  useEffect(() => {
    if (seleccionadoId === null) {
      return;
    }
    let vigente = true;
    void obtenerAcuerdo(seleccionadoId).then((datos) => {
      if (vigente) {
        setErrorDetalle(null);
        setAcuerdo(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorDetalle({ id: seleccionadoId, message: obtenerMensaje(error, 'No se pudo cargar el acuerdo.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [seleccionadoId, revision]);

  const recargar = useCallback(() => setRevision((valor) => valor + 1), []);
  const acuerdoVisible = acuerdo?.id === seleccionadoId ? acuerdo : null;
  const mensajeErrorDetalle = errorDetalle?.id === seleccionadoId ? errorDetalle.message : '';
  return {
    lista,
    resumen,
    proveedores,
    productos,
    errorLista,
    errorResumen,
    errorOpciones,
    acuerdo: acuerdoVisible,
    errorDetalle: mensajeErrorDetalle,
    cargandoDetalle: seleccionadoId !== null && acuerdoVisible === null && mensajeErrorDetalle === '',
    recargar,
  };
}
