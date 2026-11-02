import { useCallback, useRef, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { eliminarRol } from './api';
import { mensajeError } from './errores';
import { RolDetallePanel } from './RolDetallePanel';
import { RolFormulario } from './RolFormulario';
import { RolesLista } from './RolesLista';
import type { RolDetalle } from './tipos';
import { useRolesData } from './useRolesData';

export function RolesPermisosPage() {
  const datos = useRolesData();
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [rolEdicion, setRolEdicion] = useState<RolDetalle | null>(null);
  const [dialogoAbierto, setDialogoAbierto] = useState(false);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);

  const cerrarDialogo = useCallback(() => {
    setDialogoAbierto(false);
    setErrorEliminar('');
    window.requestAnimationFrame(() => botonEliminarRef.current?.focus());
  }, []);

  function abrirNuevo(): void {
    setRolEdicion(null);
    setMostrarFormulario(true);
  }

  function abrirEdicion(): void {
    if (datos.detalle !== null) {
      setRolEdicion(datos.detalle);
      setMostrarFormulario(true);
    }
  }

  function cerrarFormulario(): void {
    setMostrarFormulario(false);
    setRolEdicion(null);
  }

  function rolGuardado(): void {
    cerrarFormulario();
    datos.refrescar();
  }

  async function confirmarEliminacion(): Promise<void> {
    if (datos.detalle === null) {
      return;
    }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await eliminarRol(datos.detalle.id);
      cerrarDialogo();
      datos.limpiarSeleccion();
      datos.refrescar();
    } catch (error: unknown) {
      setErrorEliminar(mensajeError(error, 'No se pudo eliminar el rol.'));
    } finally {
      setEliminando(false);
    }
  }

  return (
    <PaginaModulo className="roles-page" modulo="72-roles-permisos">
      <p className="module-intro">Define qué puede hacer cada perfil de usuario en los módulos del ERP.</p>
      <div className="roles-work-area">
        <RolesLista
          busqueda={datos.busqueda}
          cambiarBusqueda={datos.cambiarBusqueda}
          cambiarPagina={datos.cambiarPagina}
          errorLista={datos.errorLista}
          lista={datos.lista}
          nuevo={abrirNuevo}
          pagina={datos.pagina}
          seleccionado={datos.seleccionado}
          seleccionar={datos.seleccionar}
        />
        <RolDetallePanel
          botonEliminarRef={botonEliminarRef}
          detalle={datos.detalle}
          editar={abrirEdicion}
          eliminar={() => {
            setErrorEliminar('');
            setDialogoAbierto(true);
          }}
          error={datos.errorDetalle}
        />
      </div>
      {mostrarFormulario && (
        <RolFormulario
          cancelar={cerrarFormulario}
          guardado={rolGuardado}
          key={rolEdicion?.id ?? 'nuevo'}
          rol={rolEdicion}
        />
      )}
      {dialogoAbierto && datos.detalle !== null && (
        <DialogoBaja
          cerrar={cerrarDialogo}
          confirmar={() => void confirmarEliminacion()}
          eliminando={eliminando}
          entidad="rol"
          error={errorEliminar}
          nombre={datos.detalle.nombre}
        />
      )}
    </PaginaModulo>
  );
}
