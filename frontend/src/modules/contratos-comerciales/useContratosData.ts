import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import { listarClientesContrato, listarContratos, obtenerContrato, obtenerResumenContratos } from './api';
import type { ClienteOpcion, ContratoDetalle, FiltrosContrato, PaginaContratos, ContratosResumen } from './tipos';

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function useContratosData(filtros: FiltrosContrato, seleccionadoId: number | null) {
  const [lista, setLista] = useState<PaginaContratos | null>(null);
  const [resumen, setResumen] = useState<ContratosResumen | null>(null);
  const [clientes, setClientes] = useState<ClienteOpcion[]>([]);
  const [detalle, setDetalle] = useState<ContratoDetalle | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorClientes, setErrorClientes] = useState('');
  const [errorDetalle, setErrorDetalle] = useState('');
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarContratos(filtros).then((datos) => {
      if (vigente) { setLista(datos); setErrorLista(''); }
    }).catch((error: unknown) => {
      if (vigente) setErrorLista(mensaje(error, 'No se pudo cargar la lista de contratos.'));
    });
    return () => { vigente = false; };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenContratos().then((datos) => {
      if (vigente) { setResumen(datos); setErrorResumen(''); }
    }).catch((error: unknown) => {
      if (vigente) setErrorResumen(mensaje(error, 'No se pudo cargar el resumen de contratos.'));
    });
    return () => { vigente = false; };
  }, [revision]);

  useEffect(() => {
    let vigente = true;
    void listarClientesContrato().then((datos) => {
      if (vigente) { setClientes(datos); setErrorClientes(''); }
    }).catch((error: unknown) => {
      if (vigente) setErrorClientes(mensaje(error, 'No se pudieron cargar los clientes.'));
    });
    return () => { vigente = false; };
  }, []);

  useEffect(() => {
    if (seleccionadoId === null) return;
    let vigente = true;
    void obtenerContrato(seleccionadoId).then((datos) => {
      if (vigente) setDetalle(datos);
    }).catch((error: unknown) => {
      if (vigente) { setDetalle(null); setErrorDetalle(mensaje(error, 'No se pudo cargar el detalle del contrato.')); }
    });
    return () => { vigente = false; };
  }, [seleccionadoId, revision]);

  const recargar = useCallback(() => setRevision((actual) => actual + 1), []);
  const contrato = detalle?.id === seleccionadoId ? detalle : null;
  return { lista, resumen, clientes, contrato, errorLista, errorResumen, errorClientes, errorDetalle, recargar };
}
