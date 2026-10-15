import { useEffect, useState, type Dispatch, type MutableRefObject, type SetStateAction } from 'react';
import { ApiError } from '../../api/client';
import { listarSegmentos } from './api';
import type { FiltrosSegmentos, Pagina, SegmentoListado } from './tipos';

interface Props {
  busqueda: string;
  busquedaAplicada: string;
  filtros: { activo: boolean | ''; pagina: number };
  actualizacion: number;
  seleccionadoRef: MutableRefObject<number | null>;
  setSeleccionadoId: Dispatch<SetStateAction<number | null>>;
}

function mensajeDeError(error: unknown): string {
  return error instanceof ApiError ? error.message : 'No se pudo cargar la lista de segmentos.';
}

export function useListadoSegmentos({
  busqueda,
  busquedaAplicada,
  filtros,
  actualizacion,
  seleccionadoRef,
  setSeleccionadoId,
}: Props) {
  const [segmentos, setSegmentos] = useState<Pagina<SegmentoListado> | null>(null);
  const [errorSegmentos, setErrorSegmentos] = useState('');

  useEffect(() => {
    if (busqueda !== busquedaAplicada) {
      return;
    }
    let vigente = true;
    const filtrosConsulta: FiltrosSegmentos = {
      busqueda: busquedaAplicada,
      activo: filtros.activo,
      pagina: filtros.pagina,
      tamanoPagina: 10,
    };
    void listarSegmentos(filtrosConsulta).then((datos) => {
      if (!vigente) {
        return;
      }
      setErrorSegmentos('');
      setSegmentos(datos);
      const idActual = seleccionadoRef.current;
      if (datos.items.length > 0 && (idActual === null || !datos.items.some((item) => item.id === idActual))) {
        setSeleccionadoId(datos.items[0]?.id ?? null);
      }
      if (datos.items.length === 0) {
        setSeleccionadoId(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorSegmentos(mensajeDeError(error));
      }
    });
    return () => {
      vigente = false;
    };
  }, [busqueda, busquedaAplicada, filtros, actualizacion, seleccionadoRef, setSeleccionadoId]);

  return { segmentos, errorSegmentos };
}
