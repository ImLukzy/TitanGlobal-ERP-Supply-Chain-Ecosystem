import { useCallback, useEffect, useState } from 'react';
import { listarEmpleados } from '../empleados/api';
import type { PaginaEmpleados } from '../empleados/tipos';
import { listarBonos } from './api';
import { MAXIMO_EMPLEADOS_SELECTOR, mensajeError } from './presentacion';
import type { FiltrosBonos, PaginaBonos } from './tipos';

// Lista de empleados para los selectores de las vistas Bonos y Afiliaciones.
export function useEmpleadosSelector() {
  const [empleados, setEmpleados] = useState<PaginaEmpleados | null>(null);
  const [errorEmpleados, setErrorEmpleados] = useState('');

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

  return { empleados, errorEmpleados };
}

export function useBonosData(filtros: FiltrosBonos) {
  const [lista, setLista] = useState<PaginaBonos | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [revision, setRevision] = useState(0);
  const { empleados, errorEmpleados } = useEmpleadosSelector();

  useEffect(() => {
    let vigente = true;
    void listarBonos(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de bonos.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  return { lista, empleados, errorLista, errorEmpleados, recargar };
}
