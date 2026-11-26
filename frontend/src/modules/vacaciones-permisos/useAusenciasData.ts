import { useCallback, useEffect, useState } from 'react';
import { listarEmpleados } from '../empleados/api';
import type { PaginaEmpleados } from '../empleados/tipos';
import { listarAusencias, obtenerAusencia, obtenerResumenAusencias, obtenerSaldoVacacional } from './api';
import { mensajeError } from './presentacion';
import type { AusenciaDetalle, FiltrosAusencias, PaginaAusencias, ResumenAusencias, SaldoVacacional } from './tipos';

export const MAXIMO_EMPLEADOS_SELECTOR = 100;

interface ErrorPorAusencia {
  ausenciaId: number;
  mensaje: string;
}

interface ResultadoSaldo {
  clave: string;
  saldo: SaldoVacacional | null;
  error: string;
}

// El detalle y su error se guardan con el id al que pertenecen, para no mostrar
// datos o fallos de una selección anterior tras cambiar de ausencia.
export function useAusenciasData(filtros: FiltrosAusencias, seleccionadoId: number | null, necesitaEmpleados: boolean) {
  const [lista, setLista] = useState<PaginaAusencias | null>(null);
  const [resumen, setResumen] = useState<ResumenAusencias | null>(null);
  const [detalle, setDetalle] = useState<AusenciaDetalle | null>(null);
  const [empleados, setEmpleados] = useState<PaginaEmpleados | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorEmpleados, setErrorEmpleados] = useState('');
  const [errorDetalle, setErrorDetalle] = useState<ErrorPorAusencia | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarAusencias(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de ausencias.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenAusencias().then((datos) => {
      if (vigente) {
        setResumen(datos);
        setErrorResumen('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de ausencias.'));
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
    const ausenciaId = seleccionadoId;
    let vigente = true;
    void obtenerAusencia(ausenciaId).then((datos) => {
      if (vigente) {
        setDetalle(datos);
        setErrorDetalle(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setDetalle(null);
        setErrorDetalle({ ausenciaId, mensaje: mensajeError(error, 'No se pudo cargar el detalle de la ausencia.') });
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

  const ausencia = detalle !== null && detalle.id === seleccionadoId ? detalle : null;
  const mensajeDetalle = errorDetalle !== null && errorDetalle.ausenciaId === seleccionadoId ? errorDetalle.mensaje : '';
  return {
    lista,
    resumen,
    ausencia,
    empleados,
    errorLista,
    errorResumen,
    errorEmpleados,
    errorDetalle: mensajeDetalle,
    revision,
    recargar,
  };
}

// El saldo se guarda con su clave (empleado, fecha y revisión): una respuesta tardía de otra consulta no se muestra.
export function useSaldoVacacional(empleadoId: number | null, fecha: string, revision: number) {
  const clave = empleadoId === null ? '' : `${empleadoId}|${fecha}|${revision}`;
  const [resultado, setResultado] = useState<ResultadoSaldo | null>(null);

  useEffect(() => {
    if (empleadoId === null) {
      return;
    }
    let vigente = true;
    void obtenerSaldoVacacional(empleadoId, fecha).then((saldo) => {
      if (vigente) {
        setResultado({ clave, saldo, error: '' });
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setResultado({ clave, saldo: null, error: mensajeError(error, 'No se pudo cargar el saldo vacacional.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [empleadoId, fecha, clave]);

  const actual = resultado !== null && resultado.clave === clave ? resultado : null;
  return {
    saldo: actual?.saldo ?? null,
    error: actual?.error ?? '',
    cargando: empleadoId !== null && actual === null,
  };
}
