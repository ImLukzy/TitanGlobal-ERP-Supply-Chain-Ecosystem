import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { AlmacenDetallePanel } from './AlmacenDetallePanel';
import { AlmacenFormulario } from './AlmacenFormulario';
import { AlmacenesFiltros } from './AlmacenesFiltros';
import { AlmacenesResumen } from './AlmacenesResumen';
import { AlmacenesTabla } from './AlmacenesTabla';
import {
  eliminarAlmacen,
  listarAlmacenes,
  listarCiudadesAlmacen,
  obtenerAlmacen,
  obtenerResumenAlmacenes,
} from './api';
import type {
  AlmacenDetalle,
  AlmacenListado,
  FiltrosAlmacen,
  PaginaAlmacenes as PaginaAlmacenesDto,
  ResumenAlmacenes as ResumenAlmacenesDto,
  TipoAlmacen,
} from './tipos';

const TAMANO_PAGINA = 10;

function errorApi(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function AlmacenesPage() {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [filtros, setFiltros] = useState<{ ciudad: string; tipo: TipoAlmacen | ''; activo: boolean | ''; pagina: number }>({
    ciudad: '', tipo: '', activo: '', pagina: 1,
  });
  const [lista, setLista] = useState<PaginaAlmacenesDto | null>(null);
  const [resumen, setResumen] = useState<ResumenAlmacenesDto | null>(null);
  const [ciudades, setCiudades] = useState<string[]>([]);
  const [errorLista, setErrorLista] = useState('');
  const [errorResumen, setErrorResumen] = useState('');
  const [errorCiudades, setErrorCiudades] = useState('');
  const [seleccionado, setSeleccionado] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<AlmacenDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [almacenEdicion, setAlmacenEdicion] = useState<AlmacenDetalle | null>(null);
  const [dialogoAbierto, setDialogoAbierto] = useState(false);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const [actualizacion, setActualizacion] = useState(0);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);
  const seleccionadoRef = useRef(seleccionado);
  seleccionadoRef.current = seleccionado;

  const cerrarDialogo = useCallback(() => {
    setDialogoAbierto(false);
    setErrorEliminar('');
    window.requestAnimationFrame(() => botonEliminarRef.current?.focus());
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setBusquedaAplicada((actual) => actual === busqueda ? actual : busqueda);
    }, 300);
    return () => window.clearTimeout(timer);
  }, [busqueda]);

  useEffect(() => {
    let vigente = true;
    void obtenerResumenAlmacenes().then((datos) => {
      if (vigente) { setResumen(datos); setErrorResumen(''); }
    }).catch((error: unknown) => {
      if (vigente) { setErrorResumen(errorApi(error, 'No se pudo cargar el resumen de almacenes.')); }
    });
    return () => { vigente = false; };
  }, [actualizacion]);

  useEffect(() => {
    let vigente = true;
    void listarCiudadesAlmacen().then((datos) => {
      if (vigente) { setCiudades(datos); setErrorCiudades(''); }
    }).catch((error: unknown) => {
      if (vigente) { setErrorCiudades(errorApi(error, 'No se pudieron cargar las ciudades.')); }
    });
    return () => { vigente = false; };
  }, []);

  useEffect(() => {
    if (busqueda !== busquedaAplicada) { return; }
    let vigente = true;
    setErrorLista('');
    const consulta: FiltrosAlmacen = {
      busqueda: busquedaAplicada,
      ciudad: filtros.ciudad,
      tipo: filtros.tipo,
      activo: filtros.activo,
      pagina: filtros.pagina,
      tamanoPagina: TAMANO_PAGINA,
    };
    void listarAlmacenes(consulta).then((datos) => {
      if (!vigente) { return; }
      setLista(datos);
      const idActual = seleccionadoRef.current;
      if (datos.items.length > 0 && (idActual === null || !datos.items.some((item) => item.id === idActual))) {
        setSeleccionado(datos.items[0]?.id ?? null);
      }
      if (datos.items.length === 0) { setSeleccionado(null); setDetalle(null); }
    }).catch((error: unknown) => {
      if (vigente) { setErrorLista(errorApi(error, 'No se pudo cargar la lista de almacenes.')); }
    });
    return () => { vigente = false; };
  }, [busqueda, busquedaAplicada, filtros, actualizacion]);

  useEffect(() => {
    if (seleccionado === null) { setDetalle(null); setErrorDetalle(''); return; }
    let vigente = true;
    setErrorDetalle('');
    void obtenerAlmacen(seleccionado).then((datos) => {
      if (vigente) { setDetalle(datos); }
    }).catch((error: unknown) => {
      if (vigente) { setErrorDetalle(errorApi(error, 'No se pudo cargar el detalle del almacén.')); setDetalle(null); }
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

  function seleccionar(almacen: AlmacenListado): void {
    setSeleccionado(almacen.id);
    setErrorDetalle('');
  }

  async function guardar(): Promise<void> {
    setMostrarFormulario(false);
    setAlmacenEdicion(null);
    setActualizacion((valor) => valor + 1);
  }

  async function confirmarEliminacion(): Promise<void> {
    if (detalle === null) { return; }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await eliminarAlmacen(detalle.id);
      cerrarDialogo();
      setSeleccionado(null);
      setDetalle(null);
      setActualizacion((valor) => valor + 1);
    } catch (error: unknown) {
      setErrorEliminar(errorApi(error, 'No se pudo eliminar el almacén.'));
    } finally {
      setEliminando(false);
    }
  }

  function abrirEdicion(): void {
    if (detalle !== null) { setAlmacenEdicion(detalle); setMostrarFormulario(true); }
  }

  const totalPaginas = Math.max(1, Math.ceil((lista?.total ?? 0) / TAMANO_PAGINA));

  return (
    <PaginaModulo className="almacenes-page" modulo="23-almacenes">
      <p className="module-intro">Administra los almacenes, sus capacidades y responsables.</p>
      <AlmacenesResumen datos={resumen} error={errorResumen} />
      {errorCiudades !== '' && <p className="clientes-error" role="alert">{errorCiudades}</p>}
      <div className="almacenes-work-area">
        <section aria-labelledby="almacenes-list-title" className="clientes-list-panel almacenes-list-panel">
          <div className="clientes-panel-heading"><div><div className="page-eyebrow">DIRECTORIO</div>
            <h2 id="almacenes-list-title">Todos los almacenes</h2></div>
            <button className="primary-button" onClick={() => { setAlmacenEdicion(null); setMostrarFormulario(true); }} type="button">
              Nuevo almacén
            </button></div>
          <AlmacenesFiltros busqueda={busqueda} ciudades={ciudades} ciudad={filtros.ciudad} tipo={filtros.tipo} activo={filtros.activo}
            onBusqueda={cambiarBusqueda} onCiudad={(ciudad) => cambiarFiltro({ ciudad })}
            onTipo={(tipo) => cambiarFiltro({ tipo })} onActivo={(activo) => cambiarFiltro({ activo })} />
          {errorLista !== '' ? <p className="clientes-error" role="alert">{errorLista}</p>
            : lista === null ? <p className="clientes-status" role="status">Cargando almacenes…</p>
              : lista.items.length === 0 ? <p className="clientes-status" role="status">No hay almacenes que coincidan con los filtros.</p>
                : <AlmacenesTabla cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))}
                  elementos={lista.items} pagina={filtros.pagina} seleccionar={seleccionar} seleccionado={seleccionado} totalPaginas={totalPaginas} />}
        </section>
        <AlmacenDetallePanel almacen={detalle} botonEliminarRef={botonEliminarRef} editar={abrirEdicion}
          eliminar={() => { setErrorEliminar(''); setDialogoAbierto(true); }} error={errorDetalle} />
      </div>
      {mostrarFormulario && <AlmacenFormulario almacen={almacenEdicion} cancelar={() => setMostrarFormulario(false)} ciudades={ciudades} guardado={guardar} />}
      {dialogoAbierto && detalle !== null && <DialogoBaja cerrar={cerrarDialogo} confirmar={() => void confirmarEliminacion()}
        eliminando={eliminando} entidad="almacén" error={errorEliminar} nombre={detalle.nombre} />}
    </PaginaModulo>
  );
}
