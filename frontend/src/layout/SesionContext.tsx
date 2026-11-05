import { createContext, useContext, useSyncExternalStore, type ReactNode } from 'react';
import { getSesion, limpiarSesion, setSesion, subscribeToSesion, type Sesion } from '../api/sesionStore';

interface SesionContextValue {
  sesion: Sesion | null;
  guardarSesion: (sesion: Sesion) => void;
  cerrarSesion: () => void;
}

const SesionContext = createContext<SesionContextValue | null>(null);

export function SesionProvider({ children }: { children: ReactNode }) {
  const sesion = useSyncExternalStore(subscribeToSesion, getSesion, () => null);
  return (
    <SesionContext.Provider value={{ sesion, guardarSesion: setSesion, cerrarSesion: limpiarSesion }}>
      {children}
    </SesionContext.Provider>
  );
}

export function useSesion(): SesionContextValue {
  const context = useContext(SesionContext);
  if (context === null) {
    throw new Error('useSesion debe usarse dentro de SesionProvider.');
  }
  return context;
}

/** Devuelve null fuera del proveedor, para componentes que deben funcionar sin sesión. */
export function useSesionOpcional(): SesionContextValue | null {
  return useContext(SesionContext);
}
