import { useCallback, useEffect, useRef, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { CentroTrabajoDetallePanel } from './CentroTrabajoDetallePanel';
import { CentroTrabajoFormulario } from './CentroTrabajoFormulario';
import { CentrosTrabajoFiltros } from './CentrosTrabajoFiltros';
import { CentrosTrabajoResumen } from './CentrosTrabajoResumen';
import { CentrosTrabajoTabla } from './CentrosTrabajoTabla';
import { eliminarCentroTrabajo, listarCentrosTrabajo, listarPlantasCentroTrabajo, obtenerCentroTrabajo, obtenerResumenCentrosTrabajo } from './api';
import { mensajeError } from './presentacion';
import type { CentroTrabajoDetalle, PaginaCentrosTrabajo, ResumenCentrosTrabajo, TipoCentroTrabajo } from './tipos';

export function CentrosTrabajoPage() {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [filtros, setFiltros] = useState<{ planta: string; tipo: TipoCentroTrabajo | ''; activo: boolean | ''; pagina: number }>({
    planta: '', tipo: '', activo: '', pagina: 1,
  });
  const [lista, setLista] = useState<PaginaCentrosTrabajo | null>(null);
  const [resumen, setResumen] = useState<ResumenCentrosTrabajo | null>(null);
  const [plantas, setPlantas] = useState<string[]>([]);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorPlantas, setErrorPlantas] = useState('');
  const [seleccionado, setSeleccionado] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<CentroTrabajoDetalle | null>(null);
  const [cargandoDetalle, setCargandoDetalle] = useState(false);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [formulario, setFormulario] = useState<{ centro: CentroTrabajoDetalle | null } | null>(null);
  const [baja, setBaja] = useState<CentroTrabajoDetalle | null>(null);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const [actualizacion, setActualizacion] = useState(0);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);
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
    let vigente = true;
    void obtenerResumenCentrosTrabajo().then((datos) => {
      if (vigente) { setResumen(datos); setErrorResumen(''); }
    }).catch((error: unknown) => {
      if (vigente) { setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de centros de trabajo.')); }
    });
    void listarPlantasCentroTrabajo().then((datos) => {
      if (vigente) { setPlantas(datos); setErrorPlantas(''); }
    }).catch((error: unknown) => {
      if (vigente) { setErrorPlantas(mensajeError(error, 'No se pudieron cargar las plantas.')); }
    });
    return () => { vigente = false; };
  }, [actualizacion]);

  useEffect(() => {
    if (busqueda !== busquedaAplicada) { return; }
    let vigente = true;
    setLista(null);
    setErrorLista('');
    void listarCentrosTrabajo({ busqueda: busquedaAplicada, ...filtros, tamanoPagina: 10 }).then((datos) => {
      if (!vigente) { return; }
      const ultimaPagina = Math.max(1, Math.ceil(datos.total / 10));
      if (filtros.pagina > ultimaPagina) {
        setFiltros((actual) => ({ ...actual, pagina: ultimaPagina }));
        return;
      }
      setLista(datos);
      const idActual = seleccionadoRef.current;
      if (!datos.items.some((item) => item.id === idActual)) {
        setSeleccionado(datos.items[0]?.id ?? null);
      }
    }).catch((error: unknown) => {
      if (vigente) { setErrorLista(mensajeError(error, 'No se pudo cargar la lista de centros de trabajo.')); }
    });
    return () => { vigente = false; };
  }, [busqueda, busquedaAplicada, filtros, actualizacion]);

  useEffect(() => {
    setDetalle(null);
    setErrorDetalle('');
    if (seleccionado === null) { setCargandoDetalle(false); return; }
    let vigente = true;
    setCargandoDetalle(true);
    void obtenerCentroTrabajo(seleccionado).then((datos) => {
      if (vigente) { setDetalle(datos); }
    }).catch((error: unknown) => {
      if (vigente) { setErrorDetalle(mensajeError(error, 'No se pudo cargar el detalle del centro de trabajo.')); }
    }).finally(() => {
      if (vigente) { setCargandoDetalle(false); }
    });
    return () => { vigente = false; };
  }, [seleccionado, actualizacion]);

  function cambiarFiltro(cambios: Partial<typeof filtros>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: 1 }));
  }

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setFiltros((actual) => actual.pagina === 1 ? actual : { ...actual, pagina: 1 });
  }

  function cerrarFormulario(): void {
    setFormulario(null);
    window.requestAnimationFrame(() => botonNuevoRef.current?.focus());
  }

  function guardado(): void {
    cerrarFormulario();
    setActualizacion((valor) => valor + 1);
  }

  async function confirmarEliminacion(): Promise<void> {
    if (baja === null || eliminando) { return; }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await eliminarCentroTrabajo(baja.id);
      setBaja(null);
      setSeleccionado(null);
      setDetalle(null);
      setActualizacion((valor) => valor + 1);
      window.requestAnimationFrame(() => botonNuevoRef.current?.focus());
    } catch (error: unknown) {
      setErrorEliminar(mensajeError(error, 'No se pudo eliminar el centro de trabajo.'));
    } finally {
      setEliminando(false);
    }
  }

  return (
    <PaginaModulo modulo="33-centros-trabajo" className="centros-trabajo-page">
      <p className="module-intro">Administra las líneas y celdas de producción, su capacidad y costo por hora.</p>
      <CentrosTrabajoResumen datos={resumen} error={errorResumen} />
      {errorPlantas !== '' && <p className="clientes-error" role="alert">{errorPlantas}</p>}
      <div className="centros-trabajo-work-area">
        <section className="clientes-list-panel centros-trabajo-list-panel" aria-labelledby="centros-list-title">
          <div className="clientes-panel-heading"><div><div className="page-eyebrow">DIRECTORIO</div>
            <h2 id="centros-list-title">Todos los centros</h2></div>
            <button className="primary-button" ref={botonNuevoRef} type="button" onClick={() => setFormulario({ centro: null })}>Nuevo centro de trabajo</button>
          </div>
          <CentrosTrabajoFiltros busqueda={busqueda} plantas={plantas} planta={filtros.planta} tipo={filtros.tipo} activo={filtros.activo}
            onBusqueda={cambiarBusqueda} onPlanta={(planta) => cambiarFiltro({ planta })}
            onTipo={(tipo) => cambiarFiltro({ tipo })} onActivo={(activo) => cambiarFiltro({ activo })} />
          {errorLista !== '' ? <p className="clientes-error" role="alert">{errorLista}</p>
            : lista === null ? <p className="clientes-status" role="status">Cargando centros de trabajo…</p>
              : lista.items.length === 0 ? <p className="clientes-status" role="status">No hay centros de trabajo que coincidan con los filtros.</p>
                : <CentrosTrabajoTabla elementos={lista.items} pagina={lista.pagina} totalPaginas={Math.max(1, Math.ceil(lista.total / 10))}
                  seleccionado={seleccionado} seleccionar={setSeleccionado}
                  cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))} />}
        </section>
        <CentroTrabajoDetallePanel centro={detalle} cargando={cargandoDetalle} error={errorDetalle} botonEliminarRef={botonEliminarRef}
          editar={() => { if (detalle !== null) { setFormulario({ centro: detalle }); } }}
          eliminar={() => { setErrorEliminar(''); setBaja(detalle); }} />
      </div>
      {formulario !== null && <CentroTrabajoFormulario key={formulario.centro?.id ?? 'nuevo'} centro={formulario.centro}
        plantas={plantas} cancelar={cerrarFormulario} guardado={guardado} />}
      {baja !== null && <DialogoBaja entidad="centro de trabajo" nombre={baja.nombre} cerrar={cerrarDialogo}
        confirmar={() => void confirmarEliminacion()} eliminando={eliminando} error={errorEliminar} />}
    </PaginaModulo>
  );
}
