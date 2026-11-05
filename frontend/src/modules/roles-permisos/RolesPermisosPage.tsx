import { useCallback, useRef, useState, type KeyboardEvent } from 'react';
import { useSearchParams } from 'react-router';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { eliminarRol } from './api';
import { mensajeError } from './errores';
import { EditorPermisos } from './EditorPermisos';
import { MatrizPermisos } from './MatrizPermisos';
import { RolDetallePanel } from './RolDetallePanel';
import { RolFormulario } from './RolFormulario';
import { RolesLista } from './RolesLista';
import type { RolDetalle } from './tipos';
import { useRolesData } from './useRolesData';

type Vista = 'roles' | 'matriz';

const VISTAS: Vista[] = ['roles', 'matriz'];

function resolverVista(valor: string | null): Vista {
  return valor === 'matriz' ? 'matriz' : 'roles';
}

export function RolesPermisosPage() {
  const datos = useRolesData();
  const [searchParams, setSearchParams] = useSearchParams();
  const vista = resolverVista(searchParams.get('vista'));
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [rolEdicion, setRolEdicion] = useState<RolDetalle | null>(null);
  const [dialogoAbierto, setDialogoAbierto] = useState(false);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const [editandoPermisos, setEditandoPermisos] = useState(false);
  const [seleccionPrevia, setSeleccionPrevia] = useState<number | null>(datos.seleccionado);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);
  const botonPermisosRef = useRef<HTMLButtonElement>(null);

  if (seleccionPrevia !== datos.seleccionado) {
    setSeleccionPrevia(datos.seleccionado);
    // La primera selección automática (de nada a un rol) no cierra un «Nuevo rol» recién abierto.
    if (seleccionPrevia !== null) {
      setMostrarFormulario(false);
      setRolEdicion(null);
      setEditandoPermisos(false);
    }
  }

  const cerrarDialogo = useCallback(() => {
    setDialogoAbierto(false);
    setErrorEliminar('');
    window.requestAnimationFrame(() => botonEliminarRef.current?.focus());
  }, []);

  function cambiarVista(siguiente: Vista): void {
    setSearchParams(siguiente === 'roles' ? {} : { vista: siguiente });
  }

  function manejarTeclado(evento: KeyboardEvent<HTMLDivElement>): void {
    if (evento.key !== 'ArrowLeft' && evento.key !== 'ArrowRight') {
      return;
    }
    evento.preventDefault();
    const posicion = VISTAS.indexOf(vista);
    const desplazamiento = evento.key === 'ArrowRight' ? 1 : VISTAS.length - 1;
    const proxima = VISTAS[(posicion + desplazamiento) % VISTAS.length];
    if (proxima !== undefined) {
      cambiarVista(proxima);
      window.requestAnimationFrame(() => document.getElementById(`tab-roles-${proxima}`)?.focus());
    }
  }

  function abrirNuevo(): void {
    setEditandoPermisos(false);
    setRolEdicion(null);
    setMostrarFormulario(true);
  }

  function abrirEdicion(): void {
    if (datos.detalle !== null) {
      setEditandoPermisos(false);
      setRolEdicion(datos.detalle);
      setMostrarFormulario(true);
    }
  }

  function abrirPermisos(): void {
    setMostrarFormulario(false);
    setRolEdicion(null);
    setEditandoPermisos(true);
  }

  function cerrarPermisos(): void {
    setEditandoPermisos(false);
    window.requestAnimationFrame(() => botonPermisosRef.current?.focus());
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
      <div aria-label="Vistas de roles y permisos" className="segment-tabs roles-tabs" onKeyDown={manejarTeclado} role="tablist">
        <button
          aria-controls="panel-roles-roles"
          aria-selected={vista === 'roles'}
          className="segment-tab"
          id="tab-roles-roles"
          onClick={() => cambiarVista('roles')}
          role="tab"
          tabIndex={vista === 'roles' ? 0 : -1}
          type="button"
        >
          Roles
        </button>
        <button
          aria-controls="panel-roles-matriz"
          aria-selected={vista === 'matriz'}
          className="segment-tab"
          id="tab-roles-matriz"
          onClick={() => cambiarVista('matriz')}
          role="tab"
          tabIndex={vista === 'matriz' ? 0 : -1}
          type="button"
        >
          Matriz
        </button>
      </div>
      {/* La vista de roles se oculta, no se desmonta, para no perder la selección ni los cambios sin guardar. */}
      <div aria-labelledby="tab-roles-roles" hidden={vista !== 'roles'} id="panel-roles-roles" role="tabpanel">
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
            botonPermisosRef={botonPermisosRef}
            cargando={datos.cargandoDetalle}
            detalle={datos.detalle}
            editar={abrirEdicion}
            editarPermisos={abrirPermisos}
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
        {editandoPermisos && datos.detalle !== null && (
          <EditorPermisos
            cerrar={cerrarPermisos}
            guardado={datos.refrescar}
            key={datos.detalle.id}
            rol={datos.detalle}
          />
        )}
      </div>
      {vista === 'matriz' && (
        <div aria-labelledby="tab-roles-matriz" id="panel-roles-matriz" role="tabpanel">
          <MatrizPermisos />
        </div>
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
