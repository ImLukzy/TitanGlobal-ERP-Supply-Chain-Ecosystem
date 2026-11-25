import { useCallback, useEffect, useState } from 'react';
import { listarAfiliaciones } from './api';
import { mensajeError } from './presentacion';
import type { FiltrosAfiliaciones, PaginaAfiliaciones } from './tipos';
import { useEmpleadosSelector } from './useBonosData';
import { useParametros } from './usePlanillasData';

export function useAfiliacionesData(filtros: FiltrosAfiliaciones) {
  const [lista, setLista] = useState<PaginaAfiliaciones | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [revision, setRevision] = useState(0);
  const { empleados, errorEmpleados } = useEmpleadosSelector();
  const { parametros, error: errorParametros } = useParametros();

  useEffect(() => {
    let vigente = true;
    void listarAfiliaciones(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de afiliaciones.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  return { lista, empleados, parametros, errorLista, errorEmpleados, errorParametros, recargar };
}
