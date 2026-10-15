import type { ReactNode } from 'react';

interface CampoConErrorProps {
  etiqueta: string;
  htmlFor: string;
  error?: string;
  idError: string;
  children: ReactNode;
}

export function CampoConError({ etiqueta, htmlFor, error, idError, children }: CampoConErrorProps) {
  return (
    <div className="form-field">
      <label htmlFor={htmlFor}>{etiqueta}</label>
      {children}
      {error && (
        <span className="field-error" id={idError}>
          {error}
        </span>
      )}
    </div>
  );
}
