import { useEffect, useState } from 'react';
import { listarHoras, obtenerCapacitacionEmpleado } from './api';
import { mensajeError, useListado } from './presentacion';
import type { CapacitacionEmpleado, FiltrosHoras } from './tipos';

export function useHorasData(filtros: FiltrosHoras) {
  return useListado(listarHoras, filtros, 'No se pudieron cargar las horas por empleado.');
}

interface FichaCargada {
  empleadoId: number;
  anio: number;
  valor: CapacitacionEmpleado;
}

interface FichaError {
  empleadoId: number;
  anio: number;
  texto: string;
}

// La ficha y su error se guardan con el empleado y el año a los que pertenecen.
export function useFichaEmpleado(empleadoId: number | null, anio: number) {
  const [ficha, setFicha] = useState<FichaCargada | null>(null);
  const [error, setError] = useState<FichaError | null>(null);

  useEffect(() => {
    if (empleadoId === null) {
      return;
    }
    let vigente = true;
    void obtenerCapacitacionEmpleado(empleadoId, anio).then((datos) => {
      if (vigente) {
        setFicha({ empleadoId, anio, valor: datos });
        setError(null);
      }
    }).catch((falla: unknown) => {
      if (vigente) {
        setError({ empleadoId, anio, texto: mensajeError(falla, 'No se pudo cargar la ficha del empleado.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [empleadoId, anio]);

  const vigenteFicha = ficha !== null && ficha.empleadoId === empleadoId && ficha.anio === anio;
  const vigenteError = error !== null && error.empleadoId === empleadoId && error.anio === anio;
  return { ficha: vigenteFicha ? ficha.valor : null, error: vigenteError ? error.texto : '' };
}
