import { useEffect, useState } from 'react';
import { listarEntregas, listarEquipos } from './api';
import { MAXIMO_SELECTOR, mensajeError, useListado } from './presentacion';
import type { EquipoProteccion, FiltrosEntregas, FiltrosEquipos } from './tipos';

export function useEquiposData(filtros: FiltrosEquipos) {
  return useListado(listarEquipos, filtros, 'No se pudo cargar el catálogo de equipos.');
}

export function useEntregasData(filtros: FiltrosEntregas) {
  return useListado(listarEntregas, filtros, 'No se pudo cargar la lista de entregas.');
}

// Todos los equipos (activos e inactivos) para el filtro y el alta de entregas; se vuelve a pedir tras cambiar el catálogo.
export function useEquiposSelector(revision: number) {
  const [equipos, setEquipos] = useState<EquipoProteccion[] | null>(null);
  const [errorEquipos, setErrorEquipos] = useState('');

  useEffect(() => {
    let vigente = true;
    void listarEquipos({ busqueda: '', activo: 'Todos', pagina: 1, tamanoPagina: MAXIMO_SELECTOR }).then((datos) => {
      if (vigente) {
        setEquipos(datos.items);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorEquipos(mensajeError(error, 'No se pudo cargar la lista de equipos.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [revision]);

  return { equipos, errorEquipos };
}
