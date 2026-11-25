import { useCallback, useEffect, useState } from 'react';
import {
  listarDetallesPlanilla,
  listarPlanillas,
  obtenerParametrosPlanilla,
  obtenerPlanilla,
  obtenerResumenPlanillas,
} from './api';
import { mensajeError } from './presentacion';
import type {
  FiltrosDetalles,
  FiltrosPlanillas,
  PaginaPlanillaEmpleados,
  PaginaPlanillas,
  ParametrosPlanilla,
  PlanillaDetalle,
  ResumenPlanillas,
} from './tipos';

interface PorPlanilla<T> {
  planillaId: number;
  valor: T;
}

export function usePlanillasData(filtros: FiltrosPlanillas) {
  const [lista, setLista] = useState<PaginaPlanillas | null>(null);
  const [resumen, setResumen] = useState<ResumenPlanillas | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarPlanillas(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de planillas.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenPlanillas().then((datos) => {
      if (vigente) {
        setResumen(datos);
        setErrorResumen('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de planillas.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [revision]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  return { lista, resumen, errorLista, errorResumen, revision, recargar };
}

// El detalle, la lista de empleados y sus errores se guardan con la planilla a la que pertenecen,
// para no mostrar datos o fallos de una selección anterior tras cambiar de planilla.
export function usePlanillaDetalle(planillaId: number | null, filtros: FiltrosDetalles, revision: number) {
  const [detalle, setDetalle] = useState<PorPlanilla<PlanillaDetalle> | null>(null);
  const [empleados, setEmpleados] = useState<PorPlanilla<PaginaPlanillaEmpleados> | null>(null);
  const [errorDetalle, setErrorDetalle] = useState<PorPlanilla<string> | null>(null);
  const [errorEmpleados, setErrorEmpleados] = useState<PorPlanilla<string> | null>(null);

  useEffect(() => {
    if (planillaId === null) {
      return;
    }
    let vigente = true;
    void obtenerPlanilla(planillaId).then((datos) => {
      if (vigente) {
        setDetalle({ planillaId, valor: datos });
        setErrorDetalle(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorDetalle({ planillaId, valor: mensajeError(error, 'No se pudo cargar la planilla.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [planillaId, revision]);

  useEffect(() => {
    if (planillaId === null) {
      return;
    }
    let vigente = true;
    void listarDetallesPlanilla(planillaId, filtros).then((datos) => {
      if (vigente) {
        setEmpleados({ planillaId, valor: datos });
        setErrorEmpleados(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorEmpleados({ planillaId, valor: mensajeError(error, 'No se pudo cargar el detalle por empleado.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [planillaId, filtros, revision]);

  return {
    detalle: detalle !== null && detalle.planillaId === planillaId ? detalle.valor : null,
    empleados: empleados !== null && empleados.planillaId === planillaId ? empleados.valor : null,
    errorDetalle: errorDetalle !== null && errorDetalle.planillaId === planillaId ? errorDetalle.valor : '',
    errorEmpleados: errorEmpleados !== null && errorEmpleados.planillaId === planillaId ? errorEmpleados.valor : '',
  };
}

// Parámetros de solo lectura (UIT, tasas, pensiones y tramos); se piden una vez al montar.
export function useParametros() {
  const [parametros, setParametros] = useState<ParametrosPlanilla | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    let vigente = true;
    void obtenerParametrosPlanilla().then((datos) => {
      if (vigente) {
        setParametros(datos);
        setError('');
      }
    }).catch((falla: unknown) => {
      if (vigente) {
        setError(mensajeError(falla, 'No se pudieron cargar los parámetros de planilla.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, []);

  return { parametros, error };
}
