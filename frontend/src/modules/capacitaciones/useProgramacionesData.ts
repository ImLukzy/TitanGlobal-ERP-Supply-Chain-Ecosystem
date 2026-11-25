import { useEffect, useState } from 'react';
import { listarEmpleados } from '../empleados/api';
import type { PaginaEmpleados } from '../empleados/tipos';
import { listarCursos, listarProgramaciones, obtenerProgramacion, obtenerResumen } from './api';
import { MAXIMO_SELECTOR, mensajeError, useListado, usePorId } from './presentacion';
import type { FiltrosProgramaciones, PaginaCursos, ResumenCapacitaciones } from './tipos';

export function useProgramacionesData(filtros: FiltrosProgramaciones) {
  return useListado(listarProgramaciones, filtros, 'No se pudo cargar la lista de programaciones.');
}

export function useProgramacionDetalle(id: number | null, revision: number) {
  return usePorId(id, obtenerProgramacion, revision, 'No se pudo cargar la programación.');
}

// Selectores de la vista: cursos (filtro y alta) y empleados activos (inscripción).
export function useSelectoresProgramacion() {
  const [cursos, setCursos] = useState<PaginaCursos | null>(null);
  const [empleados, setEmpleados] = useState<PaginaEmpleados | null>(null);
  const [errorSelectores, setErrorSelectores] = useState('');

  useEffect(() => {
    let vigente = true;
    void listarCursos({ busqueda: '', tipo: '', activo: 'Todos', pagina: 1, tamanoPagina: MAXIMO_SELECTOR }).then((datos) => {
      if (vigente) {
        setCursos(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorSelectores(mensajeError(error, 'No se pudo cargar la lista de cursos.'));
      }
    });
    void listarEmpleados({ busqueda: '', estado: 'Activo', pagina: 1, tamanoPagina: MAXIMO_SELECTOR }).then((datos) => {
      if (vigente) {
        setEmpleados(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorSelectores(mensajeError(error, 'No se pudo cargar la lista de empleados.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, []);

  return { cursos, empleados, errorSelectores };
}

// Resumen del año; se vuelve a pedir cuando cambia el año o la revisión (tras cualquier escritura).
export function useResumen(anio: number, revision: number) {
  const [resumen, setResumen] = useState<ResumenCapacitaciones | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    let vigente = true;
    void obtenerResumen(anio).then((datos) => {
      if (vigente) {
        setResumen(datos);
        setError('');
      }
    }).catch((falla: unknown) => {
      if (vigente) {
        setError(mensajeError(falla, 'No se pudo cargar el resumen de capacitaciones.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [anio, revision]);

  return { resumen: resumen !== null && resumen.anio === anio ? resumen : null, error };
}
