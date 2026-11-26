import { useEffect, useState } from 'react';
import { listarEmpleados } from '../empleados/api';
import type { EmpleadoListado } from '../empleados/tipos';
import { listarIncidentes, obtenerIncidente, obtenerResumen } from './api';
import { MAXIMO_SELECTOR, mensajeError, useListado, usePorId } from './presentacion';
import type { FiltrosIncidentes, ResumenSst } from './tipos';

export function useIncidentesData(filtros: FiltrosIncidentes) {
  return useListado(listarIncidentes, filtros, 'No se pudo cargar la lista de incidentes.');
}

export function useIncidenteDetalle(id: number | null, revision: number) {
  return usePorId(id, obtenerIncidente, revision, 'No se pudo cargar el incidente.');
}

// Empleados para filtros y formularios (todos los estados: el backend valida el cese donde importa).
export function useEmpleados() {
  const [empleados, setEmpleados] = useState<EmpleadoListado[] | null>(null);
  const [errorEmpleados, setErrorEmpleados] = useState('');

  useEffect(() => {
    let vigente = true;
    void listarEmpleados({ busqueda: '', estado: '', pagina: 1, tamanoPagina: MAXIMO_SELECTOR }).then((datos) => {
      if (vigente) {
        setEmpleados(datos.items);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorEmpleados(mensajeError(error, 'No se pudo cargar la lista de empleados.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, []);

  return { empleados, errorEmpleados };
}

// Resumen del año; se vuelve a pedir cuando cambia el año o la revisión (tras cualquier escritura).
export function useResumen(anio: number, revision: number) {
  const [resumen, setResumen] = useState<ResumenSst | null>(null);
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
        setError(mensajeError(falla, 'No se pudo cargar el resumen de seguridad y salud.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [anio, revision]);

  return { resumen: resumen !== null && resumen.anio === anio ? resumen : null, error };
}
