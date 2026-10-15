import { useCallback, useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import { listarAuditoria, listarEntidadesAuditoria, obtenerRegistroAuditoria, obtenerResumenAuditoria } from './api';
import { esRangoInvalido } from './presentacion';
import type { FiltrosAuditoria, PaginaAuditoria, RegistroDetalle, ResumenAuditoria } from './tipos';

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function useAuditoriaData(filtros: FiltrosAuditoria, seleccionadoId: number | null) {
  const [pagina, setPagina] = useState<PaginaAuditoria | null>(null);
  const [resumen, setResumen] = useState<ResumenAuditoria | null>(null);
  const [entidades, setEntidades] = useState<string[]>([]);
  const [detalle, setDetalle] = useState<RegistroDetalle | null>(null);
  const [errorPagina, setErrorPagina] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorEntidades, setErrorEntidades] = useState('');
  const [errorDetalle, setErrorDetalle] = useState('');
  const [revision, setRevision] = useState(0);
  const rangoInvalido = esRangoInvalido(filtros.fechaDesde, filtros.fechaHasta);

  useEffect(() => {
    if (rangoInvalido) {
      void Promise.resolve().then(() => {
        setPagina(null);
        setErrorPagina('');
      });
      return;
    }
    let vigente = true;
    void listarAuditoria(filtros)
      .then((resultado) => {
        if (vigente) {
          setPagina(resultado);
          setErrorPagina('');
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          setErrorPagina(mensaje(error, 'No se pudo cargar la auditoría.'));
        }
      });
    return () => {
      vigente = false;
    };
  }, [filtros, rangoInvalido, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenAuditoria()
      .then((resultado) => {
        if (vigente) {
          setResumen(resultado);
          setErrorResumen('');
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          setErrorResumen(mensaje(error, 'No se pudo cargar el resumen de auditoría.'));
        }
      });
    return () => {
      vigente = false;
    };
  }, [revision]);

  useEffect(() => {
    let vigente = true;
    void listarEntidadesAuditoria()
      .then((resultado) => {
        if (vigente) {
          setEntidades(resultado);
          setErrorEntidades('');
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          setErrorEntidades(mensaje(error, 'No se pudieron cargar las entidades.'));
        }
      });
    return () => {
      vigente = false;
    };
  }, [revision]);

  useEffect(() => {
    if (seleccionadoId === null) {
      void Promise.resolve().then(() => {
        setDetalle(null);
        setErrorDetalle('');
      });
      return;
    }
    let vigente = true;
    void obtenerRegistroAuditoria(seleccionadoId)
      .then((resultado) => {
        if (vigente) {
          setDetalle(resultado);
          setErrorDetalle('');
        }
      })
      .catch((error: unknown) => {
        if (vigente) {
          setErrorDetalle(mensaje(error, 'No se pudo cargar el detalle.'));
        }
      });
    return () => {
      vigente = false;
    };
  }, [seleccionadoId, revision]);

  const recargar = useCallback(() => {
    setRevision((valor) => valor + 1);
  }, []);

  return {
    pagina,
    resumen,
    entidades,
    detalle: detalle?.id === seleccionadoId ? detalle : null,
    errorPagina,
    errorResumen,
    errorEntidades,
    errorDetalle,
    rangoInvalido,
    recargar,
  };
}
