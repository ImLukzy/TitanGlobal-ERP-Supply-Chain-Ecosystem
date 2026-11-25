import { useCallback, useEffect, useState } from 'react';
import { listarEmpleados } from '../empleados/api';
import type { PaginaEmpleados } from '../empleados/tipos';
import { listarMarcaciones } from './api';
import { mensajeError } from './presentacion';
import type { FiltrosMarcaciones, PaginaMarcaciones } from './tipos';
import { MAXIMO_EMPLEADOS_SELECTOR } from './useJornadasData';

export function useMarcacionesData(filtros: FiltrosMarcaciones) {
  const [lista, setLista] = useState<PaginaMarcaciones | null>(null);
  const [empleados, setEmpleados] = useState<PaginaEmpleados | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorEmpleados, setErrorEmpleados] = useState('');
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarMarcaciones(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de marcaciones.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

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

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  return { lista, empleados, errorLista, errorEmpleados, recargar };
}
