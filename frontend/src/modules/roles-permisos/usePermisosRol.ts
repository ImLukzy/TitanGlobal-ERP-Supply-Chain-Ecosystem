import { useEffect, useState } from 'react';
import { guardarPermisosRol, obtenerPermisosRol } from './api';
import { mensajeError } from './errores';
import {
  aplicarAtajoDominio,
  cambiarCasilla,
  construirRequestPermisos,
  contarCambios,
  type AtajoDominio,
  type CasillaPermiso,
} from './permisos';
import type { PermisoModulo } from './tipos';

interface EstadoPermisos {
  original: PermisoModulo[];
  actual: PermisoModulo[];
}

export interface PermisosRolData {
  /** Último estado guardado en el servidor. */
  original: PermisoModulo[];
  /** Estado que se está editando. */
  actual: PermisoModulo[];
  cargando: boolean;
  error: string;
  cambios: number;
  guardando: boolean;
  errorGuardar: string;
  cambiarCasilla: (moduloNumero: number, casilla: CasillaPermiso, marcada: boolean) => void;
  aplicarAtajo: (dominio: string, atajo: AtajoDominio) => void;
  descartar: () => void;
  guardar: () => Promise<void>;
}

/**
 * Carga los permisos de un rol y gestiona su edición. Se usa con `key={rolId}` para empezar de cero
 * al cambiar de rol.
 */
export function usePermisosRol(rolId: number, esSistema: boolean, alGuardar: () => void): PermisosRolData {
  const [estado, setEstado] = useState<EstadoPermisos | null>(null);
  const [error, setError] = useState('');
  const [guardando, setGuardando] = useState(false);
  const [errorGuardar, setErrorGuardar] = useState('');

  useEffect(() => {
    let activo = true;
    void obtenerPermisosRol(rolId).then((permisos) => {
      if (activo) {
        setEstado({ original: permisos, actual: permisos });
      }
    }).catch((falla: unknown) => {
      if (activo) {
        setError(mensajeError(falla, 'No se pudieron cargar los permisos del rol.'));
      }
    });
    return () => {
      activo = false;
    };
  }, [rolId]);

  function cambiarCasillaModulo(moduloNumero: number, casilla: CasillaPermiso, marcada: boolean): void {
    setEstado((anterior) => {
      if (anterior === null) {
        return anterior;
      }
      return { ...anterior, actual: cambiarCasilla(anterior.actual, moduloNumero, casilla, marcada) };
    });
  }

  function aplicarAtajo(dominio: string, atajo: AtajoDominio): void {
    setEstado((anterior) => {
      if (anterior === null) {
        return anterior;
      }
      return { ...anterior, actual: aplicarAtajoDominio(anterior.actual, anterior.original, dominio, atajo, esSistema) };
    });
  }

  function descartar(): void {
    setErrorGuardar('');
    setEstado((anterior) => {
      if (anterior === null) {
        return anterior;
      }
      return { ...anterior, actual: anterior.original };
    });
  }

  async function guardar(): Promise<void> {
    if (estado === null) {
      return;
    }
    setGuardando(true);
    setErrorGuardar('');
    try {
      const guardados = await guardarPermisosRol(rolId, construirRequestPermisos(estado.actual));
      setEstado({ original: guardados, actual: guardados });
      alGuardar();
    } catch (falla: unknown) {
      setErrorGuardar(mensajeError(falla, 'No se pudieron guardar los permisos.'));
    } finally {
      setGuardando(false);
    }
  }

  const original = estado?.original ?? [];
  const actual = estado?.actual ?? [];

  return {
    original,
    actual,
    cargando: estado === null && error === '',
    error,
    cambios: contarCambios(original, actual),
    guardando,
    errorGuardar,
    cambiarCasilla: cambiarCasillaModulo,
    aplicarAtajo,
    descartar,
    guardar,
  };
}
