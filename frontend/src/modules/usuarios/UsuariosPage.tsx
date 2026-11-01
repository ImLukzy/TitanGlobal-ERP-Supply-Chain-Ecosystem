import { useEffect, useMemo, useRef, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { actualizarUsuario, crearUsuario, eliminarUsuario } from './api';
import { mensajeError } from './presentacion';
import { UsuarioDetallePanel } from './UsuarioDetallePanel';
import { UsuarioFormulario } from './UsuarioFormulario';
import { UsuariosFiltros } from './UsuariosFiltros';
import { UsuariosResumen } from './UsuariosResumen';
import { UsuariosTabla } from './UsuariosTabla';
import { useUsuariosData } from './useUsuariosData';
import type { FiltrosUsuarios, GuardarUsuarioRequest } from './tipos';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosUsuarios = { rolId: '', activo: '', pagina: 1, tamanoPagina: TAMANO_PAGINA };

type ModoFormulario = 'cerrado' | 'nuevo' | 'editar';

export function UsuariosPage() {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [filtros, setFiltros] = useState<FiltrosUsuarios>(INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [modoFormulario, setModoFormulario] = useState<ModoFormulario>('cerrado');
  const [dialogoAbierto, setDialogoAbierto] = useState(false);
  const [errorBaja, setErrorBaja] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);
  const filtrosConsulta = useMemo<FiltrosUsuarios>(() => {
    return { ...filtros, busqueda: busquedaAplicada };
  }, [filtros, busquedaAplicada]);
  const data = useUsuariosData(filtrosConsulta, seleccionadoId);

  useEffect(() => {
    const temporizador = window.setTimeout(() => setBusquedaAplicada(busqueda), 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setFiltros((actual) => (actual.pagina === 1 ? actual : { ...actual, pagina: 1 }));
  }

  function cambiarFiltro(cambios: Partial<FiltrosUsuarios>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: 1 }));
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actual) => ({ ...actual, pagina }));
  }

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setModoFormulario('cerrado');
  }

  async function guardarUsuario(request: GuardarUsuarioRequest): Promise<void> {
    const resultado = modoFormulario === 'editar' && data.usuario !== null
      ? await actualizarUsuario(data.usuario.id, request)
      : await crearUsuario(request);
    setSeleccionadoId(resultado.id);
    setModoFormulario('cerrado');
    data.recargar();
  }

  function cerrarBaja(): void {
    setDialogoAbierto(false);
    setErrorBaja('');
    window.requestAnimationFrame(() => botonEliminarRef.current?.focus());
  }

  async function confirmarBaja(): Promise<void> {
    if (data.usuario === null) {
      return;
    }
    setEliminando(true);
    setErrorBaja('');
    try {
      await eliminarUsuario(data.usuario.id);
      setSeleccionadoId(null);
      cerrarBaja();
      data.recargar();
    } catch (error: unknown) {
      setErrorBaja(mensajeError(error, 'No se pudo eliminar el usuario.'));
    } finally {
      setEliminando(false);
    }
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  const cargandoDetalle = seleccionadoId !== null && data.usuario === null && data.errorDetalle === '';

  return (
    <PaginaModulo className="usuarios-page" modulo="71-usuarios">
      <p className="module-intro">Administra los usuarios, sus roles y los permisos efectivos que de ellos resultan.</p>
      <UsuariosResumen datos={data.resumen} error={data.errorResumen} />
      {data.errorRoles !== '' && <p className="clientes-error" role="alert">{data.errorRoles}</p>}
      <div className="usuarios-work-area">
        <section aria-labelledby="usuarios-list-title" className="clientes-list-panel usuarios-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">DIRECTORIO</div>
              <h2 id="usuarios-list-title">Todos los usuarios</h2>
            </div>
            <button
              className="primary-button"
              onClick={() => setModoFormulario('nuevo')}
              type="button"
            >
              Nuevo usuario
            </button>
          </div>
          <UsuariosFiltros
            busqueda={busqueda}
            cambiarBusqueda={cambiarBusqueda}
            cambiarFiltro={cambiarFiltro}
            filtros={filtros}
            roles={data.roles}
          />
          <UsuariosTabla
            cambiarPagina={cambiarPagina}
            cargando={data.lista === null}
            error={data.errorLista}
            items={data.lista?.items ?? []}
            pagina={filtros.pagina ?? 1}
            seleccionadoId={seleccionadoId}
            seleccionar={seleccionar}
            totalPaginas={paginas}
          />
        </section>
        <UsuarioDetallePanel
          botonEliminarRef={botonEliminarRef}
          cargando={cargandoDetalle}
          editar={() => setModoFormulario('editar')}
          eliminar={() => {
            setErrorBaja('');
            setDialogoAbierto(true);
          }}
          error={data.errorDetalle}
          errorPermisos={data.errorPermisos}
          permisos={data.permisos}
          usuario={data.usuario}
        />
      </div>
      {modoFormulario !== 'cerrado' && (
        <UsuarioFormulario
          cancelar={() => setModoFormulario('cerrado')}
          guardar={guardarUsuario}
          key={modoFormulario === 'editar' && data.usuario !== null ? `editar-${data.usuario.id}` : 'nuevo'}
          roles={data.roles}
          usuario={modoFormulario === 'editar' ? data.usuario : null}
        />
      )}
      {dialogoAbierto && data.usuario !== null && (
        <DialogoBaja
          cerrar={cerrarBaja}
          confirmar={() => void confirmarBaja()}
          eliminando={eliminando}
          entidad="usuario"
          error={errorBaja}
          nombre={data.usuario.nombreCompleto}
        />
      )}
    </PaginaModulo>
  );
}
