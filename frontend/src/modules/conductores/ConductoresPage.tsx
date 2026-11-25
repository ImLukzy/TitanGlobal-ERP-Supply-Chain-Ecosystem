import { useCallback, useEffect, useRef, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { formatearCantidad } from '../../lib/formato';
import { ConductorDetallePanel } from './ConductorDetallePanel';
import { ConductorFormulario } from './ConductorFormulario';
import { ConductoresFiltros } from './ConductoresFiltros';
import { ConductoresTabla } from './ConductoresTabla';
import { eliminarConductor, listarConductores, obtenerConductor } from './api';
import { mensajeError } from './presentacion';
import type { ConductorDetalle, PaginaConductores } from './tipos';

export function ConductoresPage() {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [filtros, setFiltros] = useState<{ habilitado: boolean | ''; pagina: number }>({ habilitado: '', pagina: 1 });
  const [lista, setLista] = useState<PaginaConductores | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [seleccionado, setSeleccionado] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<ConductorDetalle | null>(null);
  const [cargandoDetalle, setCargandoDetalle] = useState(false);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [formulario, setFormulario] = useState<{ conductor: ConductorDetalle | null } | null>(null);
  const [baja, setBaja] = useState<ConductorDetalle | null>(null);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const [actualizacion, setActualizacion] = useState(0);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);
  const botonEditarRef = useRef<HTMLButtonElement>(null);
  const botonNuevoRef = useRef<HTMLButtonElement>(null);
  const seleccionadoRef = useRef(seleccionado);
  seleccionadoRef.current = seleccionado;

  const cerrarDialogo = useCallback(() => {
    if (eliminando) { return; }
    setBaja(null);
    setErrorEliminar('');
    window.requestAnimationFrame(() => botonEliminarRef.current?.focus());
  }, [eliminando]);

  useEffect(() => {
    const timer = window.setTimeout(() => setBusquedaAplicada(busqueda), 300);
    return () => window.clearTimeout(timer);
  }, [busqueda]);

  useEffect(() => {
    if (busqueda !== busquedaAplicada) { return; }
    let vigente = true;
    setLista(null);
    setErrorLista('');
    void listarConductores({ busqueda: busquedaAplicada, ...filtros, tamanoPagina: 10 }).then((datos) => {
      if (!vigente) { return; }
      const ultimaPagina = Math.max(1, Math.ceil(datos.total / 10));
      if (filtros.pagina > ultimaPagina) {
        setFiltros((actual) => ({ ...actual, pagina: ultimaPagina }));
        return;
      }
      setLista(datos);
      if (!datos.items.some((item) => item.id === seleccionadoRef.current)) {
        setSeleccionado(datos.items[0]?.id ?? null);
      }
    }).catch((error: unknown) => {
      if (vigente) { setErrorLista(mensajeError(error, 'No se pudo cargar la lista de conductores.')); }
    });
    return () => { vigente = false; };
  }, [busqueda, busquedaAplicada, filtros, actualizacion]);

  useEffect(() => {
    setDetalle(null);
    setErrorDetalle('');
    if (seleccionado === null) { setCargandoDetalle(false); return; }
    let vigente = true;
    setCargandoDetalle(true);
    void obtenerConductor(seleccionado).then((datos) => {
      if (vigente) { setDetalle(datos); }
    }).catch((error: unknown) => {
      if (vigente) { setErrorDetalle(mensajeError(error, 'No se pudo cargar el detalle del conductor.')); }
    }).finally(() => { if (vigente) { setCargandoDetalle(false); } });
    return () => { vigente = false; };
  }, [seleccionado, actualizacion]);

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setFiltros((actual) => actual.pagina === 1 ? actual : { ...actual, pagina: 1 });
  }

  function cerrarFormulario(): void {
    setFormulario(null);
    window.requestAnimationFrame(() => ((formulario?.conductor ? botonEditarRef.current : null) ?? botonNuevoRef.current)?.focus());
  }

  function guardado(id: number): void {
    cerrarFormulario();
    setSeleccionado(id);
    setActualizacion((valor) => valor + 1);
  }

  async function confirmarEliminacion(): Promise<void> {
    if (baja === null || eliminando) { return; }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await eliminarConductor(baja.id);
      setBaja(null);
      setSeleccionado(null);
      setDetalle(null);
      setActualizacion((valor) => valor + 1);
      window.requestAnimationFrame(() => botonNuevoRef.current?.focus());
    } catch (error: unknown) {
      setErrorEliminar(mensajeError(error, 'No se pudo eliminar el conductor.'));
    } finally { setEliminando(false); }
  }

  return (
    <PaginaModulo modulo="42-conductores" className="conductores-page">
      <p className="module-intro">Administra los conductores, sus licencias y su habilitación administrativa.</p>
      <div className="conductores-work-area">
        <section className="clientes-list-panel conductores-list-panel" aria-labelledby="conductores-list-title">
          <div className="clientes-panel-heading"><div><div className="page-eyebrow">DIRECTORIO</div>
            <h2 id="conductores-list-title">Todos los conductores</h2></div>
            <button className="primary-button" ref={botonNuevoRef} type="button" onClick={() => setFormulario({ conductor: null })}>Nuevo conductor</button>
          </div>
          <ConductoresFiltros busqueda={busqueda} habilitado={filtros.habilitado} onBusqueda={cambiarBusqueda}
            onHabilitado={(habilitado) => setFiltros({ habilitado, pagina: 1 })} />
          <p className="conductores-help">La habilitación administrativa y la vigencia de la licencia se muestran por separado.</p>
          {errorLista !== '' ? <p className="clientes-error" role="alert">{errorLista}<button className="secondary-button" type="button"
            onClick={() => setActualizacion((valor) => valor + 1)}>Reintentar</button></p>
            : lista === null ? <p className="clientes-status" role="status">Cargando conductores…</p>
              : lista.items.length === 0 ? <p className="clientes-status" role="status">No hay conductores que coincidan con los filtros.</p>
                : <>
                  <p className="conductores-help" role="status">{formatearCantidad(lista.total, 'conductor', 'conductores')} en el resultado</p>
                  <ConductoresTabla elementos={lista.items} pagina={lista.pagina} totalPaginas={Math.max(1, Math.ceil(lista.total / 10))}
                    seleccionado={seleccionado} seleccionar={setSeleccionado}
                    cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))} />
                </>}
        </section>
        <ConductorDetallePanel conductor={detalle} cargando={cargandoDetalle} error={errorDetalle}
          botonEliminarRef={botonEliminarRef} botonEditarRef={botonEditarRef}
          editar={() => { if (detalle !== null) { setFormulario({ conductor: detalle }); } }}
          eliminar={() => { setErrorEliminar(''); setBaja(detalle); }} />
      </div>
      {formulario !== null && <ConductorFormulario key={formulario.conductor?.id ?? 'nuevo'} conductor={formulario.conductor} cancelar={cerrarFormulario} guardado={guardado} />}
      {baja !== null && <DialogoBaja entidad="conductor" nombre={baja.nombre} cerrar={cerrarDialogo}
        confirmar={() => void confirmarEliminacion()} eliminando={eliminando} error={errorEliminar} />}
    </PaginaModulo>
  );
}
