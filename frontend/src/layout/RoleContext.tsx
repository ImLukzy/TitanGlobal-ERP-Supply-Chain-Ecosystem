import { createContext, useContext, useSyncExternalStore, type ReactNode } from 'react';
import { getSelectedRole, setSelectedRole, subscribeToRole, type UserRole } from '../api/roleStore';

interface RoleContextValue {
  role: UserRole | null;
  setRole: (role: UserRole | null) => void;
}

const RoleContext = createContext<RoleContextValue | null>(null);

export function RoleProvider({ children }: { children: ReactNode }) {
  const role = useSyncExternalStore(subscribeToRole, getSelectedRole, () => null);
  return <RoleContext.Provider value={{ role, setRole: setSelectedRole }}>{children}</RoleContext.Provider>;
}

export function useRole(): RoleContextValue {
  const context = useContext(RoleContext);
  if (context === null) {
    throw new Error('useRole debe usarse dentro de RoleProvider.');
  }
  return context;
}
