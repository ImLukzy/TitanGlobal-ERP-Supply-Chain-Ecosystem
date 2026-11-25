import { useCallback, useEffect, useState } from 'react';
import { listarCiclos, obtenerCiclo, obtenerResultadosCiclo } from './api';
import { mensajeError } from './presentacion';
import type { CicloDetalle, FiltrosCiclos, PaginaCiclos, ResultadosCiclo } from './tipos';

interface PorCiclo<T> {
  cicloId: number;
  valor: T;
}

export function useCiclosData(filtros: FiltrosCiclos) {
  const [lista, setLista] = useState<PaginaCiclos | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarCiclos(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de ciclos.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  return { lista, errorLista, revision, recargar };
}

// El detalle, los resultados y sus errores se guardan con el ciclo al que pertenecen,
// para no mostrar datos o fallos de una selección anterior tras cambiar de ciclo.
// Los resultados solo se piden cuando el ciclo ya no está en configuración.
export function useCicloDetalle(cicloId: number | null, revision: number) {
  const [detalle, setDetalle] = useState<PorCiclo<CicloDetalle> | null>(null);
  const [resultados, setResultados] = useState<PorCiclo<ResultadosCiclo> | null>(null);
  const [errorDetalle, setErrorDetalle] = useState<PorCiclo<string> | null>(null);
  const [errorResultados, setErrorResultados] = useState<PorCiclo<string> | null>(null);

  useEffect(() => {
    if (cicloId === null) {
      return;
    }
    let vigente = true;
    void obtenerCiclo(cicloId).then((datos) => {
      if (vigente) {
        setDetalle({ cicloId, valor: datos });
        setErrorDetalle(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorDetalle({ cicloId, valor: mensajeError(error, 'No se pudo cargar el ciclo.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [cicloId, revision]);

  const actual = detalle !== null && detalle.cicloId === cicloId ? detalle.valor : null;
  const pedirResultados = actual !== null && actual.estado !== 'Configuracion';

  useEffect(() => {
    if (cicloId === null || !pedirResultados) {
      return;
    }
    let vigente = true;
    void obtenerResultadosCiclo(cicloId).then((datos) => {
      if (vigente) {
        setResultados({ cicloId, valor: datos });
        setErrorResultados(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorResultados({ cicloId, valor: mensajeError(error, 'No se pudieron cargar los resultados del ciclo.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [cicloId, pedirResultados, revision]);

  return {
    detalle: actual,
    resultados: pedirResultados && resultados !== null && resultados.cicloId === cicloId ? resultados.valor : null,
    errorDetalle: errorDetalle !== null && errorDetalle.cicloId === cicloId ? errorDetalle.valor : '',
    errorResultados: errorResultados !== null && errorResultados.cicloId === cicloId ? errorResultados.valor : '',
  };
}
