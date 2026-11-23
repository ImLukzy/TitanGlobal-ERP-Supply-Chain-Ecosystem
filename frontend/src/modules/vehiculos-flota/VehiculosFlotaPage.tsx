import { useCallback, useEffect, useRef, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { VehiculoFlotaDetallePanel } from './VehiculoFlotaDetallePanel';
import { VehiculoFlotaFormulario } from './VehiculoFlotaFormulario';
import { VehiculosFlotaFiltros } from './VehiculosFlotaFiltros';
import { VehiculosFlotaResumen } from './VehiculosFlotaResumen';
import { VehiculosFlotaTabla } from './VehiculosFlotaTabla';
import { eliminarVehiculoFlota, listarVehiculosFlota, listarTiposVehiculoFlota, obtenerVehiculoFlota, obtenerResumenVehiculosFlota } from './api';
import { mensajeError } from './presentacion';
import type { VehiculoFlotaDetalle, PaginaVehiculosFlota, ResumenVehiculosFlota } from './tipos';

export function VehiculosFlotaPage() {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [filtros, setFiltros] = useState<{ tipo: string; activo: boolean | ''; pagina: number }>({
    tipo: '', activo: '', pagina: 1,
  });
  const [lista, setLista] = useState<PaginaVehiculosFlota | null>(null);
  const [resumen, setResumen] = useState<ResumenVehiculosFlota | null>(null);
  const [tipos, setTipos] = useState<string[]>([]);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorTipos, setErrorTipos] = useState('');
  const [seleccionado, setSeleccionado] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<VehiculoFlotaDetalle | null>(null);
  const [cargandoDetalle, setCargandoDetalle] = useState(false);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [formulario, setFormulario] = useState<{ vehiculo: VehiculoFlotaDetalle | null } | null>(null);
  const [baja, setBaja] = useState<VehiculoFlotaDetalle | null>(null);
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
    let vigente = true;
    void obtenerResumenVehiculosFlota().then((datos) => {
      if (vigente) { setResumen(datos); setErrorResumen(''); }
    }).catch((error: unknown) => {
      if (vigente) { setErrorResumen(mensajeError(error, 'No se pudo cargar el resumen de vehículos de flota.')); }
    });
    void listarTiposVehiculoFlota().then((datos) => {
      if (vigente) { setTipos(datos); setErrorTipos(''); }
    }).catch((error: unknown) => {
      if (vigente) { setErrorTipos(mensajeError(error, 'No se pudieron cargar los tipos.')); }
    });
    return () => { vigente = false; };
  }, [actualizacion]);

  useEffect(() => {
    if (busqueda !== busquedaAplicada) { return; }
    let vigente = true;
    setLista(null);
    setErrorLista('');
    void listarVehiculosFlota({ busqueda: busquedaAplicada, ...filtros, tamanoPagina: 10 }).then((datos) => {
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
      if (vigente) { setErrorLista(mensajeError(error, 'No se pudo cargar la lista de vehículos de flota.')); }
    });
    return () => { vigente = false; };
  }, [busqueda, busquedaAplicada, filtros, actualizacion]);

  useEffect(() => {
    setDetalle(null);
    setErrorDetalle('');
    if (seleccionado === null) { setCargandoDetalle(false); return; }
    let vigente = true;
    setCargandoDetalle(true);
    void obtenerVehiculoFlota(seleccionado).then((datos) => {
      if (vigente) { setDetalle(datos); }
    }).catch((error: unknown) => {
      if (vigente) { setErrorDetalle(mensajeError(error, 'No se pudo cargar el detalle del vehículo.')); }
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
    window.requestAnimationFrame(() => (formulario?.vehiculo ? botonEditarRef.current : botonNuevoRef.current)?.focus());
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
      await eliminarVehiculoFlota(baja.id);
      setBaja(null);
      setSeleccionado(null);
      setDetalle(null);
      setActualizacion((valor) => valor + 1);
      window.requestAnimationFrame(() => botonNuevoRef.current?.focus());
    } catch (error: unknown) {
      setErrorEliminar(mensajeError(error, 'No se pudo eliminar el vehículo.'));
    } finally {
      setEliminando(false);
    }
  }

  return (
    <PaginaModulo modulo="41-vehiculos-flota" className="vehiculos-flota-page">
      <p className="module-intro">Administra los vehículos, su capacidad de carga y disponibilidad.</p>
      <VehiculosFlotaResumen datos={resumen} error={errorResumen} />
      {errorTipos !== '' && <p className="clientes-error" role="alert">{errorTipos}</p>}
      <div className="vehiculos-flota-work-area">
        <section className="clientes-list-panel vehiculos-flota-list-panel" aria-labelledby="vehiculos-list-title">
          <div className="clientes-panel-heading"><div><div className="page-eyebrow">DIRECTORIO</div>
            <h2 id="vehiculos-list-title">Todos los vehículos</h2></div>
            <button className="primary-button" ref={botonNuevoRef} type="button" onClick={() => setFormulario({ vehiculo: null })}>Nuevo vehículo</button>
          </div>
          <VehiculosFlotaFiltros busqueda={busqueda} tipos={tipos} tipo={filtros.tipo} activo={filtros.activo}
            onBusqueda={cambiarBusqueda}
            onTipo={(tipo) => cambiarFiltro({ tipo })} onActivo={(activo) => cambiarFiltro({ activo })} />
          {errorLista !== '' ? <p className="clientes-error" role="alert">{errorLista}<button className="secondary-button" type="button" onClick={() => setActualizacion((valor) => valor + 1)}>Reintentar</button></p>
            : lista === null ? <p className="clientes-status" role="status">Cargando vehículos de flota…</p>
              : lista.items.length === 0 ? <p className="clientes-status" role="status">No hay vehículos de flota que coincidan con los filtros.</p>
                : <VehiculosFlotaTabla elementos={lista.items} pagina={lista.pagina} totalPaginas={Math.max(1, Math.ceil(lista.total / 10))}
                  seleccionado={seleccionado} seleccionar={setSeleccionado}
                  cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))} />}
        </section>
        <VehiculoFlotaDetallePanel vehiculo={detalle} cargando={cargandoDetalle} error={errorDetalle} botonEliminarRef={botonEliminarRef} botonEditarRef={botonEditarRef}
          editar={() => { if (detalle !== null) { setFormulario({ vehiculo: detalle }); } }}
          eliminar={() => { setErrorEliminar(''); setBaja(detalle); }} />
      </div>
      {formulario !== null && <VehiculoFlotaFormulario key={formulario.vehiculo?.id ?? 'nuevo'} vehiculo={formulario.vehiculo}
        cancelar={cerrarFormulario} guardado={guardado} />}
      {baja !== null && <DialogoBaja entidad="vehículo" nombre={baja.placa} cerrar={cerrarDialogo}
        confirmar={() => void confirmarEliminacion()} eliminando={eliminando} error={errorEliminar} />}
    </PaginaModulo>
  );
}
