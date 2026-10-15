import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import { listarClientesDevolucion, listarDevoluciones, listarReclamosDevolucion, obtenerDevolucion, obtenerResumenDevoluciones } from './api';
import type { ClienteOpcion, DevolucionDetalle, FiltrosDevolucion, PaginaDevoluciones, ReclamoOpcion, ResumenDevoluciones } from './tipos';

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function useDevolucionesData(filtros: FiltrosDevolucion, seleccionadoId: number | null) {
  const [lista, setLista] = useState<PaginaDevoluciones | null>(null);
  const [resumen, setResumen] = useState<ResumenDevoluciones | null>(null);
  const [clientes, setClientes] = useState<ClienteOpcion[]>([]);
  const [reclamos, setReclamos] = useState<ReclamoOpcion[]>([]);
  const [detalle, setDetalle] = useState<DevolucionDetalle | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorCatalogos, setErrorCatalogos] = useState('');
  const [errorDetalle, setErrorDetalle] = useState('');
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarDevoluciones(filtros).then((datos) => {
      if (vigente) { setLista(datos); setErrorLista(''); }
    }).catch((error: unknown) => {
      if (vigente) setErrorLista(mensaje(error, 'No se pudo cargar la lista de devoluciones.'));
    });
    return () => { vigente = false; };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenDevoluciones().then((datos) => {
      if (vigente) { setResumen(datos); setErrorResumen(''); }
    }).catch((error: unknown) => {
      if (vigente) setErrorResumen(mensaje(error, 'No se pudo cargar el resumen de devoluciones.'));
    });
    return () => { vigente = false; };
  }, [revision]);

  useEffect(() => {
    let vigente = true;
    void Promise.all([listarClientesDevolucion(), listarReclamosDevolucion()]).then(([listaClientes, listaReclamos]) => {
      if (vigente) { setClientes(listaClientes); setReclamos(listaReclamos); setErrorCatalogos(''); }
    }).catch((error: unknown) => {
      if (vigente) setErrorCatalogos(mensaje(error, 'No se pudieron cargar los catálogos.'));
    });
    return () => { vigente = false; };
  }, []);

  useEffect(() => {
    if (seleccionadoId === null) return;
    let vigente = true;
    void obtenerDevolucion(seleccionadoId).then((datos) => {
      if (vigente) { setDetalle(datos); setErrorDetalle(''); }
    }).catch((error: unknown) => {
      if (vigente) { setDetalle(null); setErrorDetalle(mensaje(error, 'No se pudo cargar el detalle de la devolución.')); }
    });
    return () => { vigente = false; };
  }, [seleccionadoId, revision]);

  const recargar = useCallback(() => setRevision((actual) => actual + 1), []);
  return { lista, resumen, clientes, reclamos, detalle: detalle?.id === seleccionadoId ? detalle : null, errorLista, errorResumen, errorCatalogos, errorDetalle, recargar };
}
