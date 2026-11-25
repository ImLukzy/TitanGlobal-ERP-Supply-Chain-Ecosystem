import { useCallback, useEffect, useState } from 'react';
import { listarEmpleados } from '../empleados/api';
import type { PaginaEmpleados } from '../empleados/tipos';
import { listarJornadas, listarMarcaciones, obtenerResumenAsistencia } from './api';
import { mensajeError, sumarDias } from './presentacion';
import type { FiltrosJornadas, Marcacion, PaginaJornadas, ResumenAsistencia } from './tipos';

export const MAXIMO_EMPLEADOS_SELECTOR = 100;
const MAXIMO_MARCACIONES_DIA = 100;

export interface JornadaSeleccionada {
  id: number;
  empleadoId: number;
  fecha: string;
}

interface MarcacionesDeJornada {
  jornadaId: number;
  items: Marcacion[];
}

interface ErrorPorJornada {
  jornadaId: number;
  mensaje: string;
}

// Las marcaciones y su error se guardan con la jornada a la que pertenecen, para no mostrar
// datos o fallos de una selección anterior tras cambiar de jornada.
export function useJornadasData(filtros: FiltrosJornadas, seleccion: JornadaSeleccionada | null) {
  const [lista, setLista] = useState<PaginaJornadas | null>(null);
  const [resumen, setResumen] = useState<ResumenAsistencia | null>(null);
  const [empleados, setEmpleados] = useState<PaginaEmpleados | null>(null);
  const [marcaciones, setMarcaciones] = useState<MarcacionesDeJornada | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorEmpleados, setErrorEmpleados] = useState('');
  const [errorMarcaciones, setErrorMarcaciones] = useState<ErrorPorJornada | null>(null);
  const [revision, setRevision] = useState(0);
  const jornadaId = seleccion === null ? null : seleccion.id;
  const empleadoId = seleccion === null ? null : seleccion.empleadoId;
  const fecha = seleccion === null ? null : seleccion.fecha;

  useEffect(() => {
    let vigente = true;
    void listarJornadas(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de jornadas.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  // El resumen depende solo del rango, no de la página ni de los demás filtros.
  useEffect(() => {
    let vigente = true;
    void obtenerResumenAsistencia(filtros.desde, filtros.hasta).then((datos) => {
      if (vigente) {
        setResumen(datos);
        setErrorResumen('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setResumen(null);
        setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de asistencia.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros.desde, filtros.hasta, revision]);

  useEffect(() => {
    let vigente = true;
    void listarEmpleados({ busqueda: '', estado: '', pagina: 1, tamanoPagina: MAXIMO_EMPLEADOS_SELECTOR }).then((datos) => {
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
  }, []);

  // Las marcaciones del día de la jornada y del siguiente (la salida de un turno nocturno cae al día siguiente).
  useEffect(() => {
    if (jornadaId === null || empleadoId === null || fecha === null) {
      return;
    }
    const idJornada = jornadaId;
    let vigente = true;
    void listarMarcaciones({
      empleadoId,
      desde: fecha,
      hasta: sumarDias(fecha, 1),
      tipo: '',
      origen: '',
      pagina: 1,
      tamanoPagina: MAXIMO_MARCACIONES_DIA,
    }).then((datos) => {
      if (vigente) {
        setMarcaciones({ jornadaId: idJornada, items: datos.items });
        setErrorMarcaciones(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setMarcaciones(null);
        setErrorMarcaciones({ jornadaId: idJornada, mensaje: mensajeError(error, 'No se pudieron cargar las marcaciones del día.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [jornadaId, empleadoId, fecha, revision]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  const marcacionesDelDia = marcaciones !== null && marcaciones.jornadaId === jornadaId ? marcaciones.items : null;
  const mensajeMarcaciones = errorMarcaciones !== null && errorMarcaciones.jornadaId === jornadaId ? errorMarcaciones.mensaje : '';
  return {
    lista,
    resumen,
    empleados,
    marcaciones: marcacionesDelDia,
    errorLista,
    errorResumen,
    errorEmpleados,
    errorMarcaciones: mensajeMarcaciones,
    recargar,
  };
}
