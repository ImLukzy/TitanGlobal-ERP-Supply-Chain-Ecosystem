import { useCallback, useEffect, useState } from 'react';
import { listarEmpleados } from '../empleados/api';
import type { PaginaEmpleados } from '../empleados/tipos';
import { listarAsignacionesTurno, listarTurnos, obtenerProgramacion } from './api';
import { DIAS_PROGRAMACION, hoyUtc, mensajeError, sumarDias } from './presentacion';
import type { DiaProgramado, FiltrosAsignaciones, PaginaAsignacionesTurno, PaginaTurnos } from './tipos';

export const MAXIMO_EMPLEADOS_SELECTOR = 100;
const MAXIMO_TURNOS_SELECTOR = 100;

interface ProgramacionPorEmpleado {
  empleadoId: number;
  dias: DiaProgramado[];
}

interface ErrorPorEmpleado {
  empleadoId: number;
  mensaje: string;
}

// La programación y su error se guardan con el empleado al que pertenecen, para no mostrar
// datos de una selección anterior tras cambiar de asignación.
export function useAsignacionesTurnoData(filtros: FiltrosAsignaciones, programacionEmpleadoId: number | null) {
  const [lista, setLista] = useState<PaginaAsignacionesTurno | null>(null);
  const [empleados, setEmpleados] = useState<PaginaEmpleados | null>(null);
  const [turnos, setTurnos] = useState<PaginaTurnos | null>(null);
  const [programacion, setProgramacion] = useState<ProgramacionPorEmpleado | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorEmpleados, setErrorEmpleados] = useState('');
  const [errorTurnos, setErrorTurnos] = useState('');
  const [errorProgramacion, setErrorProgramacion] = useState<ErrorPorEmpleado | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarAsignacionesTurno(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de asignaciones.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  // Empleados activos y turnos alimentan los filtros y el alta; se piden al abrir la vista.
  useEffect(() => {
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
    void listarTurnos({ busqueda: '', tipo: '', activo: 'Todos', pagina: 1, tamanoPagina: MAXIMO_TURNOS_SELECTOR }).then((datos) => {
      if (vigente) {
        setTurnos(datos);
        setErrorTurnos('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorTurnos(mensajeError(error, 'No se pudo cargar la lista de turnos.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, []);

  useEffect(() => {
    if (programacionEmpleadoId === null) {
      return;
    }
    const empleadoId = programacionEmpleadoId;
    const desde = hoyUtc();
    let vigente = true;
    void obtenerProgramacion(empleadoId, desde, sumarDias(desde, DIAS_PROGRAMACION - 1)).then((dias) => {
      if (vigente) {
        setProgramacion({ empleadoId, dias });
        setErrorProgramacion(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setProgramacion(null);
        setErrorProgramacion({ empleadoId, mensaje: mensajeError(error, 'No se pudo cargar la programación del empleado.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [programacionEmpleadoId, revision]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  const dias = programacion !== null && programacion.empleadoId === programacionEmpleadoId ? programacion.dias : null;
  const mensajeProgramacion = errorProgramacion !== null && errorProgramacion.empleadoId === programacionEmpleadoId
    ? errorProgramacion.mensaje
    : '';
  return {
    lista,
    empleados,
    turnos,
    programacion: dias,
    errorLista,
    errorEmpleados,
    errorTurnos,
    errorProgramacion: mensajeProgramacion,
    recargar,
  };
}
