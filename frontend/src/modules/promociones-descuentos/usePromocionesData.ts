import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import { listarClientesPromocion, listarProductosPromocion, listarPromociones, listarSegmentosPromocion, obtenerPromocion, obtenerResumenPromociones } from './api';
import type { FiltrosPromociones, OpcionCatalogo, OpcionSegmento, PaginaPromociones, PromocionDetalle, ResumenPromociones } from './tipos';

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function usePromocionesData(filtros: FiltrosPromociones, seleccionadoId: number | null) {
  const [lista, setLista] = useState<PaginaPromociones | null>(null);
  const [resumen, setResumen] = useState<ResumenPromociones | null>(null);
  const [clientes, setClientes] = useState<OpcionCatalogo[]>([]);
  const [productos, setProductos] = useState<(OpcionCatalogo & { precio: number })[]>([]);
  const [segmentos, setSegmentos] = useState<OpcionSegmento[]>([]);
  const [detalle, setDetalle] = useState<PromocionDetalle | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorCatalogos, setErrorCatalogos] = useState('');
  const [errorDetalle, setErrorDetalle] = useState<{ id: number; mensaje: string } | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarPromociones(filtros).then((datos) => {
      if (vigente) { setLista(datos); setErrorLista(''); }
    }).catch((error: unknown) => {
      if (vigente) setErrorLista(mensaje(error, 'No se pudo cargar la lista de promociones.'));
    });
    return () => { vigente = false; };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenPromociones().then((datos) => {
      if (vigente) { setResumen(datos); setErrorResumen(''); }
    }).catch((error: unknown) => {
      if (vigente) setErrorResumen(mensaje(error, 'No se pudo cargar el resumen de promociones.'));
    });
    return () => { vigente = false; };
  }, [revision]);

  useEffect(() => {
    let vigente = true;
    void Promise.all([listarClientesPromocion(), listarProductosPromocion(), listarSegmentosPromocion()]).then(([listaClientes, listaProductos, listaSegmentos]) => {
      if (vigente) { setClientes(listaClientes); setProductos(listaProductos); setSegmentos(listaSegmentos); setErrorCatalogos(''); }
    }).catch((error: unknown) => {
      if (vigente) setErrorCatalogos(mensaje(error, 'No se pudieron cargar los catálogos.'));
    });
    return () => { vigente = false; };
  }, []);

  useEffect(() => {
    if (seleccionadoId === null) return;
    let vigente = true;
    void obtenerPromocion(seleccionadoId).then((datos) => {
      if (vigente) { setDetalle(datos); setErrorDetalle(null); }
    }).catch((error: unknown) => {
      if (vigente) { setDetalle(null); setErrorDetalle({ id: seleccionadoId, mensaje: mensaje(error, 'No se pudo cargar el detalle de la promoción.') }); }
    });
    return () => { vigente = false; };
  }, [seleccionadoId, revision]);

  const recargar = useCallback(() => setRevision((actual) => actual + 1), []);
  return { lista, resumen, clientes, productos, segmentos, promocion: detalle?.id === seleccionadoId ? detalle : null, errorLista, errorResumen, errorCatalogos, errorDetalle: errorDetalle?.id === seleccionadoId ? errorDetalle.mensaje : '', recargar };
}
