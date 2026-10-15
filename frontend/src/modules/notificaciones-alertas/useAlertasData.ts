import { useCallback, useEffect, useState } from 'react';
import { listarAlertas, obtenerResumenAlertas } from './api';
import type { FiltrosAlerta, PaginaAlertas, ResumenAlertas } from './tipos';

interface ResultadoLista {
  clave: string;
  lista?: PaginaAlertas;
  error?: string;
}

interface ResultadoResumen {
  recarga: number;
  resumen?: ResumenAlertas;
  error?: string;
}

function claveFiltros(filtros: FiltrosAlerta): string {
  return [filtros.tipo, filtros.severidad, filtros.incluirDescartadas, filtros.pagina, filtros.tamanoPagina].join('|');
}

export function useAlertasData(filtros: FiltrosAlerta) {
  const [resultadoLista, setResultadoLista] = useState<ResultadoLista | null>(null);
  const [resultadoResumen, setResultadoResumen] = useState<ResultadoResumen | null>(null);
  const [recarga, setRecarga] = useState(0);
  const filtrosClave = claveFiltros(filtros);

  const recargar = useCallback(() => {
    setRecarga((actual) => actual + 1);
  }, []);

  useEffect(() => {
    let activo = true;
    void listarAlertas(filtros).then((lista) => {
      if (activo) {
        setResultadoLista({ clave: filtrosClave, lista });
      }
    }).catch(() => {
      if (activo) {
        setResultadoLista({ clave: filtrosClave, error: 'No se pudo cargar el listado de alertas.' });
      }
    });
    return () => {
      activo = false;
    };
  }, [filtros, filtrosClave, recarga]);

  useEffect(() => {
    let activo = true;
    void obtenerResumenAlertas().then((resumen) => {
      if (activo) {
        setResultadoResumen({ recarga, resumen });
      }
    }).catch(() => {
      if (activo) {
        setResultadoResumen({ recarga, error: 'No se pudo cargar el resumen de alertas.' });
      }
    });
    return () => {
      activo = false;
    };
  }, [recarga]);

  const listaActiva = resultadoLista?.clave === filtrosClave ? resultadoLista.lista ?? null : null;
  const errorLista = resultadoLista?.clave === filtrosClave ? resultadoLista.error ?? '' : '';
  const resumenActivo = resultadoResumen?.recarga === recarga ? resultadoResumen.resumen ?? null : null;
  const errorResumen = resultadoResumen?.recarga === recarga ? resultadoResumen.error ?? '' : '';

  return { lista: listaActiva, resumen: resumenActivo, errorLista, errorResumen, recargar };
}
