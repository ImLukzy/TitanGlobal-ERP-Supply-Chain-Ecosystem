import { useEffect, useState } from 'react';
import { listarRoles, obtenerRol } from './api';
import { mensajeError } from './errores';
import type { PaginaRoles, RolDetalle } from './tipos';

export const TAMANO_PAGINA = 10;

export interface RolesData {
  busqueda: string;
  cambiarBusqueda: (valor: string) => void;
  pagina: number;
  cambiarPagina: (pagina: number) => void;
  lista: PaginaRoles | null;
  errorLista: string;
  seleccionado: number | null;
  seleccionar: (id: number) => void;
  detalle: RolDetalle | null;
  errorDetalle: string;
  refrescar: () => void;
  limpiarSeleccion: () => void;
}

export function useRolesData(): RolesData {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [pagina, setPagina] = useState(1);
  const [lista, setLista] = useState<PaginaRoles | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [seleccionado, setSeleccionado] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<RolDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [actualizacion, setActualizacion] = useState(0);

  useEffect(() => {
    const temporizador = window.setTimeout(() => setBusquedaAplicada(busqueda), 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  useEffect(() => {
    if (busqueda !== busquedaAplicada) {
      return;
    }
    let activo = true;
    void listarRoles({ busqueda: busquedaAplicada, pagina, tamanoPagina: TAMANO_PAGINA }).then((datos) => {
      if (!activo) {
        return;
      }
      if (datos.items.length === 0 && pagina > 1) {
        setPagina(pagina - 1);
        return;
      }
      setErrorLista('');
      setLista(datos);
      if (datos.items.length === 0) {
        setSeleccionado(null);
        setDetalle(null);
        return;
      }
      setSeleccionado((rolActual) => {
        if (rolActual !== null && datos.items.some((item) => item.id === rolActual)) {
          return rolActual;
        }
        return datos.items[0]?.id ?? null;
      });
    }).catch((error: unknown) => {
      if (activo) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de roles.'));
      }
    });
    return () => {
      activo = false;
    };
  }, [busqueda, busquedaAplicada, pagina, actualizacion]);

  useEffect(() => {
    if (seleccionado === null) {
      return;
    }
    let activo = true;
    void obtenerRol(seleccionado).then((datos) => {
      if (activo) {
        setErrorDetalle('');
        setDetalle(datos);
      }
    }).catch((error: unknown) => {
      if (activo) {
        setErrorDetalle(mensajeError(error, 'No se pudo cargar el detalle del rol.'));
        setDetalle(null);
      }
    });
    return () => {
      activo = false;
    };
  }, [seleccionado, actualizacion]);

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setPagina(1);
  }

  function refrescar(): void {
    setActualizacion((valor) => valor + 1);
  }

  function limpiarSeleccion(): void {
    setSeleccionado(null);
    setDetalle(null);
  }

  return {
    busqueda,
    cambiarBusqueda,
    pagina,
    cambiarPagina: setPagina,
    lista,
    errorLista,
    seleccionado,
    seleccionar: setSeleccionado,
    detalle: seleccionado === null ? null : detalle,
    errorDetalle: seleccionado === null ? '' : errorDetalle,
    refrescar,
    limpiarSeleccion,
  };
}
