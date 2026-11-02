import { useCallback, useEffect, useState } from 'react';
import { listarEmpleados, obtenerEmpleado } from './api';
import { mensajeError } from './presentacion';
import type { EmpleadoDetalle, FiltrosEmpleados, PaginaEmpleados, ResumenEmpleados } from './tipos';

interface ErrorPorEmpleado {
  empleadoId: number;
  mensaje: string;
}

// El resumen sale de dos conteos: un listado de 1 fila por estado. El total de la API se usa como conteo.
async function cargarResumen(): Promise<ResumenEmpleados> {
  const [activos, cesados] = await Promise.all([
    listarEmpleados({ busqueda: '', estado: 'Activo', pagina: 1, tamanoPagina: 1 }),
    listarEmpleados({ busqueda: '', estado: 'Cesado', pagina: 1, tamanoPagina: 1 }),
  ]);
  return {
    activos: activos.total,
    cesados: cesados.total,
    total: activos.total + cesados.total,
  };
}

// El detalle y su error se guardan con el id al que pertenecen, para no mostrar
// datos o fallos de una selección anterior tras cambiar de empleado.
export function useEmpleadosData(filtros: FiltrosEmpleados, seleccionadoId: number | null) {
  const [lista, setLista] = useState<PaginaEmpleados | null>(null);
  const [resumen, setResumen] = useState<ResumenEmpleados | null>(null);
  const [detalle, setDetalle] = useState<EmpleadoDetalle | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorDetalle, setErrorDetalle] = useState<ErrorPorEmpleado | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarEmpleados(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de empleados.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void cargarResumen().then((datos) => {
      if (vigente) {
        setResumen(datos);
        setErrorResumen('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de empleados.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [revision]);

  useEffect(() => {
    if (seleccionadoId === null) {
      return;
    }
    const empleadoId = seleccionadoId;
    let vigente = true;
    void obtenerEmpleado(empleadoId).then((datos) => {
      if (vigente) {
        setDetalle(datos);
        setErrorDetalle(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setDetalle(null);
        setErrorDetalle({ empleadoId, mensaje: mensajeError(error, 'No se pudo cargar el detalle del empleado.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [seleccionadoId, revision]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  const empleado = detalle !== null && detalle.id === seleccionadoId ? detalle : null;
  const mensajeDetalle = errorDetalle !== null && errorDetalle.empleadoId === seleccionadoId ? errorDetalle.mensaje : '';
  return {
    lista,
    resumen,
    empleado,
    errorLista,
    errorResumen,
    errorDetalle: mensajeDetalle,
    recargar,
  };
}
