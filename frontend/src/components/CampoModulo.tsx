import { useId, type ReactNode } from 'react';
import { CampoConError } from './CampoConError';

interface CampoModuloProps {
  etiqueta: string;
  error?: string;
  children: (id: string, idError: string) => ReactNode;
}

export function CampoModulo({ etiqueta, error, children }: CampoModuloProps) {
  const id = useId();
  const idError = id + '-error';
  return <CampoConError etiqueta={etiqueta} htmlFor={id} idError={idError} error={error}>
    {children(id, idError)}
  </CampoConError>;
}
