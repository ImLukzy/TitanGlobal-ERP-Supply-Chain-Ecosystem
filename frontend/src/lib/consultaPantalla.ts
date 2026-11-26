import { useEffect, useState } from 'react';
import { ApiError } from '../api/client';

type Cargar<T> = () => Promise<T>;

interface Respuesta<T> {
  cargar: Cargar<T>;
  revision: number;
  datos: T | null;
  error: string;
}

export function mensajeApi(error: unknown): string {
  return error instanceof ApiError ? error.message : 'No se pudo completar la operación.';
}

export function useConsultaPantalla<T>(cargar: Cargar<T> | null, revision = 0) {
  const [respuesta, setRespuesta] = useState<Respuesta<T> | null>(null);
  useEffect(() => {
    if (cargar === null) return;
    let vigente = true;
    void cargar().then(
      datos => {
        if (vigente) setRespuesta({ cargar, revision, datos, error: '' });
      },
      error => {
        if (vigente) setRespuesta({ cargar, revision, datos: null, error: mensajeApi(error) });
      },
    );
    return () => { vigente = false; };
  }, [cargar, revision]);

  const actual = respuesta?.cargar === cargar && respuesta.revision === revision ? respuesta : null;
  return { datos: actual?.datos ?? null, error: actual?.error ?? '', cargando: cargar !== null && actual === null };
}
