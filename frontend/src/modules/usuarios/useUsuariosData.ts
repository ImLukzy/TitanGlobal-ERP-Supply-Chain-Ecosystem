import { useCallback, useEffect, useState } from 'react';
import {
  listarRolesUsuarios,
  listarUsuarios,
  obtenerPermisosUsuario,
  obtenerResumenUsuarios,
  obtenerUsuario,
} from './api';
import { mensajeError } from './presentacion';
import type {
  FiltrosUsuarios,
  PaginaUsuarios,
  PermisoEfectivo,
  ResumenUsuarios,
  RolOpcion,
  UsuarioDetalle,
} from './tipos';

interface PermisosCargados {
  usuarioId: number;
  items: PermisoEfectivo[];
}

interface ErrorPorUsuario {
  usuarioId: number;
  mensaje: string;
}

// Los errores de detalle y permisos se guardan con su usuario para no mostrar
// el fallo de una selección anterior tras cambiar de usuario.
export function useUsuariosData(filtros: FiltrosUsuarios, seleccionadoId: number | null) {
  const [lista, setLista] = useState<PaginaUsuarios | null>(null);
  const [resumen, setResumen] = useState<ResumenUsuarios | null>(null);
  const [roles, setRoles] = useState<RolOpcion[]>([]);
  const [detalle, setDetalle] = useState<UsuarioDetalle | null>(null);
  const [permisos, setPermisos] = useState<PermisosCargados | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorRoles, setErrorRoles] = useState('');
  const [errorDetalle, setErrorDetalle] = useState<ErrorPorUsuario | null>(null);
  const [errorPermisos, setErrorPermisos] = useState<ErrorPorUsuario | null>(null);
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let vigente = true;
    void listarUsuarios(filtros).then((datos) => {
      if (vigente) {
        setLista(datos);
        setErrorLista('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensajeError(error, 'No se pudo cargar la lista de usuarios.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenUsuarios().then((datos) => {
      if (vigente) {
        setResumen(datos);
        setErrorResumen('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de usuarios.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [revision]);

  useEffect(() => {
    let vigente = true;
    void listarRolesUsuarios().then((datos) => {
      if (vigente) {
        setRoles(datos);
        setErrorRoles('');
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorRoles(mensajeError(error, 'No se pudieron cargar los roles.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, []);

  useEffect(() => {
    if (seleccionadoId === null) {
      return;
    }
    const usuarioId = seleccionadoId;
    let vigente = true;
    void obtenerUsuario(usuarioId).then((datos) => {
      if (vigente) {
        setDetalle(datos);
        setErrorDetalle(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setDetalle(null);
        setErrorDetalle({ usuarioId, mensaje: mensajeError(error, 'No se pudo cargar el detalle del usuario.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [seleccionadoId, revision]);

  useEffect(() => {
    if (seleccionadoId === null) {
      return;
    }
    const usuarioId = seleccionadoId;
    let vigente = true;
    void obtenerPermisosUsuario(usuarioId).then((items) => {
      if (vigente) {
        setPermisos({ usuarioId, items });
        setErrorPermisos(null);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setPermisos(null);
        setErrorPermisos({ usuarioId, mensaje: mensajeError(error, 'No se pudieron cargar los permisos efectivos.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [seleccionadoId, revision]);

  const recargar = useCallback(() => {
    setRevision((actual) => actual + 1);
  }, []);

  const usuario = detalle !== null && detalle.id === seleccionadoId ? detalle : null;
  const permisosActuales = permisos !== null && permisos.usuarioId === seleccionadoId ? permisos.items : null;
  const mensajeDetalle = errorDetalle !== null && errorDetalle.usuarioId === seleccionadoId ? errorDetalle.mensaje : '';
  const mensajePermisos = errorPermisos !== null && errorPermisos.usuarioId === seleccionadoId ? errorPermisos.mensaje : '';
  return {
    lista,
    resumen,
    roles,
    usuario,
    permisos: permisosActuales,
    errorLista,
    errorResumen,
    errorRoles,
    errorDetalle: mensajeDetalle,
    errorPermisos: mensajePermisos,
    recargar,
  };
}
