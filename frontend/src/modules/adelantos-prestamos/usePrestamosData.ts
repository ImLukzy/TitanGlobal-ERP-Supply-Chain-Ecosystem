import { useCallback, useEffect, useState } from 'react';
import { listarEmpleados } from '../empleados/api';
import type { PaginaEmpleados } from '../empleados/tipos';
import { listarPrestamos, obtenerCapacidadPrestamo, obtenerPrestamo, obtenerResumenPrestamos } from './api';
import { mensajeError } from './presentacion';
import type { CapacidadPrestamo, FiltrosPrestamos, PaginaPrestamos, PrestamoDetalle, ResumenPrestamos } from './tipos';

export const MAXIMO_EMPLEADOS_SELECTOR = 100;

interface ErrorPorPrestamo {
  prestamoId: number;
  mensaje: string;
}

interface ResultadoCapacidad {
  clave: string;
  capacidad: CapacidadPrestamo | null;
  error: string;
}

// El detalle y su error se guardan con el id al que pertenecen, para no mostrar
// datos o fallos de una selección anterior tras cambiar de préstamo.
export function usePrestamosData(filtros: FiltrosPrestamos, seleccionadoId: number | null, necesitaEmpleados: boolean) {
  const [lista, setLista] = useState<PaginaPrestamos | null>(null);
  const [resumen, setResumen] = useState<ResumenPrestamos | null>(null);
  const [detalle, setDetalle] = useState<PrestamoDetalle | null>(null);
  const [empleados, setEmpleados] = useState<PaginaEmpleados | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorEmpleados, setErrorEmpleados] = useState('');
  const [errorDetalle, setErrorDetalle] = useState<ErrorPorPrestamo | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarPrestamos(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de préstamos.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenPrestamos().then((datos) => {
      if (vigente) {
        setResumen(datos);
        setErrorResumen('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de préstamos.'));
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
    const prestamoId = seleccionadoId;
    let vigente = true;
    void obtenerPrestamo(prestamoId).then((datos) => {
      if (vigente) {
        setDetalle(datos);
        setErrorDetalle(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setDetalle(null);
        setErrorDetalle({ prestamoId, mensaje: mensajeError(error, 'No se pudo cargar el detalle del préstamo.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [seleccionadoId, revision]);

  // Los empleados activos se piden cuando se abre el alta y se vuelven a pedir en cada apertura.
  useEffect(() => {
    if (!necesitaEmpleados) {
      return;
    }
    let vigente = true;
    void listarEmpleados({ busqueda: '', estado: 'Activo', pagina: 1, tamanoPagina: MAXIMO_EMPLEADOS_SELECTOR }).then((datos) => {
      if (vigente) {
        setEmpleados(datos);
        setErrorEmpleados('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorEmpleados(mensajeError(error, 'No se pudo cargar la lista de empleados.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [necesitaEmpleados]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  const prestamo = detalle !== null && detalle.id === seleccionadoId ? detalle : null;
  const mensajeDetalle = errorDetalle !== null && errorDetalle.prestamoId === seleccionadoId ? errorDetalle.mensaje : '';
  return {
    lista,
    resumen,
    prestamo,
    empleados,
    errorLista,
    errorResumen,
    errorEmpleados,
    errorDetalle: mensajeDetalle,
    revision,
    recargar,
  };
}

// La capacidad se guarda con su clave (empleado, fecha y revisión): una respuesta tardía de otra consulta no se muestra.
export function useCapacidadPrestamo(empleadoId: number | null, fecha: string, revision: number) {
  const clave = empleadoId === null || fecha === '' ? '' : `${empleadoId}|${fecha}|${revision}`;
  const [resultado, setResultado] = useState<ResultadoCapacidad | null>(null);

  useEffect(() => {
    if (empleadoId === null || fecha === '') {
      return;
    }
    let vigente = true;
    void obtenerCapacidadPrestamo(empleadoId, fecha).then((capacidad) => {
      if (vigente) {
        setResultado({ clave, capacidad, error: '' });
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setResultado({ clave, capacidad: null, error: mensajeError(error, 'No se pudo cargar la capacidad de descuento.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [empleadoId, fecha, clave]);

  const actual = clave !== '' && resultado !== null && resultado.clave === clave ? resultado : null;
  return {
    capacidad: actual?.capacidad ?? null,
    error: actual?.error ?? '',
    cargando: clave !== '' && actual === null,
  };
}
