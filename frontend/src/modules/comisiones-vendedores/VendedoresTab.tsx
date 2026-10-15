import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda, formatearCantidad } from '../../lib/formato';
import { actualizarVendedor, crearVendedor, eliminarVendedor, listarVendedores, obtenerVendedor } from './api';
import { VendedorFormulario } from './VendedorFormulario';
import type { FiltrosVendedor, GuardarVendedorRequest, Pagina, VendedorDetalle, VendedorListado } from './tipos';

const TAMANO_PAGINA = 10;

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

interface Props {
  activo: boolean;
}

export function VendedoresTab({ activo }: Props) {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [filtroActivo, setFiltroActivo] = useState<boolean | ''>('');
  const [pagina, setPagina] = useState(1);
  const [resultado, setResultado] = useState<Pagina<VendedorListado> | null>(null);
  const [errorLista, setErrorLista] = useState('');
  const [cargando, setCargando] = useState(true);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [detalle, setDetalle] = useState<VendedorDetalle | null>(null);
  const [errorDetalle, setErrorDetalle] = useState('');
  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [editar, setEditar] = useState<VendedorDetalle | null>(null);
  const [dialogoBaja, setDialogoBaja] = useState(false);
  const [errorBaja, setErrorBaja] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const [actualizacion, setActualizacion] = useState(0);
  const botonBajaRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    const temporizador = window.setTimeout(() => setBusquedaAplicada(busqueda), 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  useEffect(() => {
    if (!activo || busqueda !== busquedaAplicada) {
      return;
    }
    let vigente = true;
    setCargando(true);
    setErrorLista('');
    const filtros: FiltrosVendedor = { busqueda: busquedaAplicada, activo: filtroActivo === '' ? undefined : filtroActivo, pagina, tamanoPagina: TAMANO_PAGINA };
    void listarVendedores(filtros).then((datos) => {
      if (vigente) {
        setResultado(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorLista(mensaje(error, 'No se pudo cargar la lista de vendedores.'));
      }
    }).finally(() => {
      if (vigente) {
        setCargando(false);
      }
    });
    return () => {
      vigente = false;
    };
  }, [activo, busqueda, busquedaAplicada, filtroActivo, pagina, actualizacion]);

  useEffect(() => {
    if (!activo || seleccionadoId === null) {
      setDetalle(null);
      setErrorDetalle('');
      return;
    }
    let vigente = true;
    setErrorDetalle('');
    void obtenerVendedor(seleccionadoId).then((datos) => {
      if (vigente) {
        setDetalle(datos);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorDetalle(mensaje(error, 'No se pudo cargar el detalle del vendedor.'));
      }
    });
    return () => {
      vigente = false;
    };
  }, [activo, seleccionadoId, actualizacion]);

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setPagina(1);
  }

  function cambiarActivo(valor: string): void {
    setFiltroActivo(valor === '' ? '' : valor === 'true');
    setPagina(1);
  }

  async function guardar(request: GuardarVendedorRequest): Promise<void> {
    if (editar === null) {
      const respuesta = await crearVendedor(request);
      setSeleccionadoId(respuesta.id);
    } else {
      await actualizarVendedor(editar.id, request);
      setSeleccionadoId(editar.id);
    }
    setMostrarFormulario(false);
    setEditar(null);
    setActualizacion((actual) => actual + 1);
  }

  async function confirmarBaja(): Promise<void> {
    if (detalle === null) {
      return;
    }
    setEliminando(true);
    setErrorBaja('');
    try {
      await eliminarVendedor(detalle.id);
      setDialogoBaja(false);
      setSeleccionadoId(null);
      setActualizacion((actual) => actual + 1);
    } catch (error: unknown) {
      setErrorBaja(mensaje(error, 'No se pudo eliminar el vendedor.'));
    } finally {
      setEliminando(false);
    }
  }

  function cerrarBaja(): void {
    setDialogoBaja(false);
    setErrorBaja('');
    botonBajaRef.current?.focus();
  }

  const paginas = Math.max(1, Math.ceil((resultado?.total ?? 0) / TAMANO_PAGINA));
  return (
    <section aria-label="Vendedores" className="comisiones-work-area">
      <section className="panel-card comisiones-list-panel">
        <div className="clientes-panel-heading">
          <div><div className="page-eyebrow">MAESTRO DE VENDEDORES</div><h2>Vendedores</h2></div>
          <button className="primary-button" onClick={() => { setEditar(null); setMostrarFormulario(true); }} type="button">Nuevo vendedor</button>
        </div>
        <div className="solicitudes-filters comisiones-filtros">
          <div><label htmlFor="vendedor-busqueda">Buscar por código o nombre</label><input id="vendedor-busqueda" onChange={(event) => cambiarBusqueda(event.currentTarget.value)} placeholder="Código o nombre" type="search" value={busqueda} /></div>
          <div><label htmlFor="vendedor-activo">Estado</label><select id="vendedor-activo" onChange={(event) => cambiarActivo(event.currentTarget.value)} value={filtroActivo === '' ? '' : String(filtroActivo)}><option value="">Todos</option><option value="true">Activos</option><option value="false">Inactivos</option></select></div>
        </div>
        {errorLista !== '' && <p className="clientes-error" role="alert">{errorLista}</p>}
        {cargando && <p role="status">Cargando vendedores…</p>}
        {!cargando && !errorLista && (resultado?.items.length ?? 0) === 0 && <p role="status">No hay vendedores que coincidan con los filtros.</p>}
        {!cargando && !errorLista && resultado !== null && resultado.items.length > 0 && <TablaPaginada
          caption="Vendedores"
          className="solicitudes-table comisiones-table vendedores-table"
          classNameContenedor="solicitudes-table-scroll"
          cambiarPagina={setPagina}
          columnas={[{ id: 'codigo', etiqueta: 'Código' }, { id: 'nombre', etiqueta: 'Nombre' }, { id: 'porcentaje', etiqueta: 'Comisión' }, { id: 'ordenes', etiqueta: 'Órdenes' }, { id: 'estado', etiqueta: 'Estado' }]}
          elementos={resultado.items}
          etiquetaRegion="Tabla de vendedores"
          obtenerClave={(vendedor) => vendedor.id}
          pagina={pagina}
          renderizarFila={(vendedor) => <tr className={seleccionadoId === vendedor.id ? 'is-selected' : ''} key={vendedor.id}>
            <th scope="row"><button className="comision-row-link" onClick={() => setSeleccionadoId(vendedor.id)} type="button">{vendedor.codigo}</button></th>
            <td>{vendedor.nombre}</td>
            <td className="cantidad-sin-partir">{vendedor.porcentajeComision.toFixed(2)} %</td>
            <td>{formatearCantidad(vendedor.ordenesAsignadas, 'orden', 'órdenes')}</td>
            <td><span className={`proveedor-status${vendedor.activo ? ' is-active' : ' is-inactive'}`}>{vendedor.activo ? 'Activo' : 'Inactivo'}</span></td>
          </tr>}
          totalPaginas={paginas}
        />}
        <span className="comisiones-total-resultados">{resultado?.total ?? 0} vendedores</span>
      </section>
      <PanelDetalle ariaLabelledBy="vendedor-detalle-title" className="panel-card comisiones-detail-panel">
        {detalle === null ? <div className="comisiones-empty-detail"><p>{errorDetalle || 'Selecciona un vendedor para ver el detalle.'}</p></div> : <>
          <div className="clientes-panel-heading">
            <div><div className="page-eyebrow">DETALLE DEL VENDEDOR</div><h2 id="vendedor-detalle-title">{detalle.codigo}</h2></div>
            <span className={`proveedor-status${detalle.activo ? ' is-active' : ' is-inactive'}`}>{detalle.activo ? 'Activo' : 'Inactivo'}</span>
          </div>
          <dl className="comisiones-ficha">
            <div><dt>Nombre</dt><dd>{detalle.nombre}</dd></div>
            <div><dt>Email</dt><dd>{detalle.email ?? '—'}</dd></div>
            <div><dt>Comisión</dt><dd>{detalle.porcentajeComision.toFixed(2)} %</dd></div>
            <div><dt>Órdenes asignadas</dt><dd>{formatearCantidad(detalle.ordenesAsignadas, 'orden', 'órdenes')}</dd></div>
            <div><dt>Ventas totales</dt><dd className="cantidad-sin-partir">{formateadorMoneda.format(detalle.ventasTotales)}</dd></div>
          </dl>
          <div className="clientes-dialog-actions">
            <button className="secondary-button" onClick={() => { setEditar(detalle); setMostrarFormulario(true); }} type="button">Editar</button>
            <button className="danger-button" onClick={() => { setDialogoBaja(true); setErrorBaja(''); }} ref={botonBajaRef} type="button">Eliminar</button>
          </div>
        </>}
      </PanelDetalle>
      {mostrarFormulario && <VendedorFormulario cancelar={() => { setMostrarFormulario(false); setEditar(null); }} guardar={guardar} vendedor={editar} />}
      {dialogoBaja && detalle !== null && <DialogoBaja cerrar={cerrarBaja} confirmar={() => void confirmarBaja()} eliminando={eliminando} entidad="vendedor" error={errorBaja} nombre={detalle.codigo} />}
    </section>
  );
}
