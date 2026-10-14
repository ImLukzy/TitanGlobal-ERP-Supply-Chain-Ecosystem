import type { ReactNode } from 'react';
import { modulos } from '../modules/catalogo';

interface PaginaModuloProps {
  modulo: string | number;
  className?: string;
  children: ReactNode;
}

function resolverModulo(referencia: string | number) {
  return modulos.find((modulo) => typeof referencia === 'number'
    ? modulo.numero === referencia
    : modulo.slug === referencia);
}

export function PaginaModulo({ modulo: referencia, className = '', children }: PaginaModuloProps) {
  const modulo = resolverModulo(referencia);

  if (modulo === undefined) {
    return null;
  }

  const numero = String(modulo.numero).padStart(2, '0');

  return (
    <div className={`page-content module-page ${className}`.trim()}>
      <nav aria-label="Miga de pan" className="breadcrumbs">
        <span>Inicio</span>
        <span aria-hidden="true">/</span>
        <span>{modulo.dominio}</span>
        <span aria-hidden="true">/</span>
        <span>{numero}</span>
      </nav>
      <header className="module-page-heading">
        <div className="page-eyebrow">MÓDULO {numero} · {modulo.dominio}</div>
        <h1>{numero} · {modulo.nombre}</h1>
      </header>
      {children}
    </div>
  );
}
