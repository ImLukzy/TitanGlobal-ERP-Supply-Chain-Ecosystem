import type { ReactNode } from 'react';
import { InicioSesion } from '../pages/InicioSesion';
import { useSesion } from './SesionContext';

export function AccesoProtegido({ children }: { children: ReactNode }) {
  const { sesion } = useSesion();
  if (sesion === null) {
    return <InicioSesion />;
  }
  return <>{children}</>;
}
