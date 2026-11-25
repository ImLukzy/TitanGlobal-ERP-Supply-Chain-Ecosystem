import { useCallback, useEffect, useState } from 'react';
import { listarEmpleados } from '../empleados/api';
import type { PaginaEmpleados } from '../empleados/tipos';
import { listarContratosLaborales, obtenerContratoLaboral } from './api';
import { mensajeError } from './presentacion';
import type {
  ContratoLaboralDetalle,
  FiltrosContratosLaborales,
  PaginaContratosLaborales,
  ResumenContratosLaborales,
} from './tipos';

export const MAXIMO_EMPLEADOS_SELECTOR = 100;

interface ErrorPorContrato {
  contratoId: number;
  mensaje: string;
}

const SIN_FILTROS = { busqueda: '', modalidad: '', empleadoId: null, pagina: 1, tamanoPagina: 1 } as const;

// El resumen sale de tres conteos: un listado de 1 fila por estado y otro sin estado. El total de la API se usa como conteo.
async function cargarResumen(): Promise<ResumenContratosLaborales> {
  const [vigentes, vencidos, total] = await Promise.all([
    listarContratosLaborales({ ...SIN_FILTROS, estado: 'Vigente' }),
    listarContratosLaborales({ ...SIN_FILTROS, estado: 'Vencido' }),
    listarContratosLaborales({ ...SIN_FILTROS, estado: '' }),
  ]);
  return { vigentes: vigentes.total, vencidos: vencidos.total, total: total.total };
}

// El detalle y su error se guardan con el id al que pertenecen, para no mostrar
// datos o fallos de una selección anterior tras cambiar de contrato.
export function useContratosLaboralesData(
  filtros: FiltrosContratosLaborales,
  seleccionadoId: number | null,
  necesitaEmpleados: boolean,
) {
  const [lista, setLista] = useState<PaginaContratosLaborales | null>(null);
  const [resumen, setResumen] = useState<ResumenContratosLaborales | null>(null);
  const [detalle, setDetalle] = useState<ContratoLaboralDetalle | null>(null);
  const [empleados, setEmpleados] = useState<PaginaEmpleados | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorEmpleados, setErrorEmpleados] = useState('');
  const [errorDetalle, setErrorDetalle] = useState<ErrorPorContrato | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarContratosLaborales(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de contratos.'));
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
        setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de contratos.'));
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
    const contratoId = seleccionadoId;
    let vigente = true;
    void obtenerContratoLaboral(contratoId).then((datos) => {
      if (vigente) {
        setDetalle(datos);
        setErrorDetalle(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setDetalle(null);
        setErrorDetalle({ contratoId, mensaje: mensajeError(error, 'No se pudo cargar el detalle del contrato.') });
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

  const contrato = detalle !== null && detalle.id === seleccionadoId ? detalle : null;
  const mensajeDetalle = errorDetalle !== null && errorDetalle.contratoId === seleccionadoId ? errorDetalle.mensaje : '';
  return {
    lista,
    resumen,
    contrato,
    empleados,
    errorLista,
    errorResumen,
    errorEmpleados,
    errorDetalle: mensajeDetalle,
    recargar,
  };
}
