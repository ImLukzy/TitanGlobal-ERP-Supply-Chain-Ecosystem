export type UserRole = 'Administrador' | 'Bloqueado' | 'SinPermiso';

const STORAGE_KEY = 'titanglobal-role';
let selectedRole: UserRole | null = null;
const listeners = new Set<() => void>();

export function isUserRole(value: string | null): value is UserRole {
  return value === 'Administrador' || value === 'Bloqueado' || value === 'SinPermiso';
}

try {
  const storedRole = window.localStorage.getItem(STORAGE_KEY);
  selectedRole = isUserRole(storedRole) ? storedRole : null;
} catch {
  selectedRole = null;
}

export function getSelectedRole(): UserRole | null {
  return selectedRole;
}

export function subscribeToRole(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function setSelectedRole(role: UserRole | null): void {
  selectedRole = role;
  try {
    if (role === null) {
      window.localStorage.removeItem(STORAGE_KEY);
    } else {
      window.localStorage.setItem(STORAGE_KEY, role);
    }
  } catch {
    // La selección sigue funcionando durante esta sesión si el almacenamiento está bloqueado.
  }
  listeners.forEach((listener) => listener());
}
