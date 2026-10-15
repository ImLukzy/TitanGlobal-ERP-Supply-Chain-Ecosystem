import { useCallback, useEffect, useState } from 'react';
import { ApiError, apiGet } from '../../api/client';
import { listarReclamos, obtenerReclamo, obtenerResumenReclamos } from './api';
import type { ClienteOpcion, FiltrosReclamos, OrdenOpcion, PaginaReclamos, ReclamoDetalle, ResumenReclamos } from './tipos';

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function useReclamosData(filtros: FiltrosReclamos, seleccionadoId: number | null) {
  const [lista, setLista] = useState<PaginaReclamos | null>(null);
  const [resumen, setResumen] = useState<ResumenReclamos | null>(null);
  const [clientes, setClientes] = useState<ClienteOpcion[]>([]);
  const [reclamo, setReclamo] = useState<ReclamoDetalle | null>(null);
  const [ordenes, setOrdenes] = useState<OrdenOpcion[]>([]);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorCatalogos, setErrorCatalogos] = useState('');
  const [errorDetalle, setErrorDetalle] = useState('');
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarReclamos(filtros).then((datos) => {
      if (vigente) { setLista(datos); setErrorLista(''); }
    }).catch((error: unknown) => { if (vigente) setErrorLista(mensaje(error, 'No se pudieron cargar los reclamos.')); });
    return () => { vigente = false; };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenReclamos().then((datos) => { if (vigente) { setResumen(datos); setErrorResumen(''); } })
      .catch((error: unknown) => { if (vigente) setErrorResumen(mensaje(error, 'No se pudo cargar el resumen.')); });
    return () => { vigente = false; };
  }, [revision]);

  useEffect(() => {
    let vigente = true;
    void apiGet<{ items: ClienteOpcion[] }>('/clientes').then((datos) => { if (vigente) setClientes(datos.items ?? []); })
      .catch((error: unknown) => { if (vigente) setErrorCatalogos(mensaje(error, 'No se pudo cargar el catálogo de clientes.')); });
    return () => { vigente = false; };
  }, []);

  useEffect(() => {
    if (seleccionadoId === null) return;
    let vigente = true;
    void obtenerReclamo(seleccionadoId).then((datos) => { if (vigente) { setReclamo(datos); setErrorDetalle(''); } })
      .catch((error: unknown) => { if (vigente) setErrorDetalle(mensaje(error, 'No se pudo cargar el detalle.')); });
    return () => { vigente = false; };
  }, [seleccionadoId, revision]);

  const cargarOrdenes = useCallback(async (clienteId: number): Promise<OrdenOpcion[]> => {
    const resultado = await apiGet<Array<{ ordenId: number; numeroOrden: string; detalles: Array<{ productoId: number; nombreProducto: string }> }>>(`/ordenes-compra/clientes/${clienteId}/jerarquia`);
    const opciones = resultado.map((orden) => ({ id: orden.ordenId, numero: orden.numeroOrden, productos: orden.detalles.map((producto) => ({ id: producto.productoId, nombre: producto.nombreProducto })) }));
    setOrdenes(opciones);
    return opciones;
  }, []);

  return { lista, resumen, clientes, ordenes, reclamo: reclamo?.id === seleccionadoId ? reclamo : null, errorLista, errorResumen, errorCatalogos, errorDetalle, recargar: () => setRevision((actual) => actual + 1), cargarOrdenes };
}
