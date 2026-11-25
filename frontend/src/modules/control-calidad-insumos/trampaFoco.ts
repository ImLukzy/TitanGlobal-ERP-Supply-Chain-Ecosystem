import type { KeyboardEvent, RefObject } from 'react';

// Escape cierra el diálogo sin enviar nada; Tab y Shift+Tab recorren solo sus controles habilitados.
export function manejarTecladoDialogo(
  event: KeyboardEvent<HTMLElement>,
  dialogo: RefObject<HTMLElement | null>,
  cerrar: () => void,
  bloqueado: boolean,
): void {
  if (event.key === 'Escape' && !bloqueado) {
    event.preventDefault();
    cerrar();
    return;
  }
  if (event.key !== 'Tab') {
    return;
  }
  const controles = dialogo.current?.querySelectorAll<HTMLElement>(
    'button:enabled, input:enabled, textarea:enabled, select:enabled',
  );
  const primero = controles?.[0];
  const ultimo = controles?.[controles.length - 1];
  if (primero === undefined || document.activeElement === dialogo.current) {
    event.preventDefault();
    (event.shiftKey ? ultimo : primero)?.focus();
  } else if (event.shiftKey && document.activeElement === primero) {
    event.preventDefault();
    ultimo?.focus();
  } else if (!event.shiftKey && document.activeElement === ultimo) {
    event.preventDefault();
    primero.focus();
  }
}
