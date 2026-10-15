import { useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import { asignarVendedor, listarOrdenes, listarVendedores, quitarVendedor } from './api';
import type { FiltrosOrden, OrdenVendedor, Pagina, VendedorListado } from './tipos';

const TAMANO_PAGINA = 10;
const MESES = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];

interface Props {
  activo: boolean;
}

function mensaje(error: unknown): string {
  return error instanceof ApiError ? error.message : 'No se pudo actualizar la asignación de la orden.';
}

export function AsignacionesTab({ activo }: Props) {
  const [anio, setAnio] = useState(2026);
  const [mes, setMes] = useState(1);
  const [vendedorId, setVendedorId] = useState<number | ''>('');
  const [sinAsignar, setSinAsignar] = useState(false);
  const [vendedores, setVendedores] = useState<VendedorListado[]>([]);
  const [paginaOrdenes, setPaginaOrdenes] = useState<Pagina<OrdenVendedor> | null>(null);
  const [pagina, setPagina] = useState(1);
  const [error, setError] = useState('');
  const [cargando, setCargando] = useState(true);
  const [erroresFila, setErroresFila] = useState<Record<number, string>>({});
  const [actualizacion, setActualizacion] = useState(0);

  useEffect(() => {
    if (!activo) {
      return;
    }
    let vigente = true;
    void listarVendedores({ activo: true, pagina: 1, tamanoPagina: 100 }).then((datos) => {
      if (vigente) {
        setVendedores(datos.items.filter((vendedor) => vendedor.activo));
      }
    }).catch(() => {
      if (vigente) {
        setError('No se pudo cargar la lista de vendedores activos.');
      }
    });
    return () => {
      vigente = false;
    };
  }, [activo]);

  useEffect(() => {
    if (!activo) {
      return;
    }
    let vigente = true;
    setCargando(true);
    setError('');
    const filtros: FiltrosOrden = { anio, mes, pagina, tamanoPagina: TAMANO_PAGINA };
    if (vendedorId !== '') {
      filtros.vendedorId = vendedorId;
    }
    if (sinAsignar) {
      filtros.sinAsignar = true;
    }
    void listarOrdenes(filtros).then((datos) => {
      if (vigente) {
        setPaginaOrdenes(datos);
      }
    }).catch((errorListado: unknown) => {
      if (vigente) {
        setError(errorListado instanceof ApiError ? errorListado.message : 'No se pudo cargar las órdenes de compra.');
      }
    }).finally(() => {
      if (vigente) {
        setCargando(false);
      }
    });
    return () => {
      vigente = false;
    };
  }, [activo, anio, mes, vendedorId, sinAsignar, pagina, actualizacion]);

  async function asignar(orden: OrdenVendedor, valor: string): Promise<void> {
    if (valor === '') {
      return;
    }
    setErroresFila((actuales) => ({ ...actuales, [orden.ordenCompraId]: '' }));
    try {
      await asignarVendedor(orden.ordenCompraId, { vendedorId: Number(valor) });
      setActualizacion((actual) => actual + 1);
    } catch (errorAsignacion: unknown) {
      setErroresFila((actuales) => ({ ...actuales, [orden.ordenCompraId]: mensaje(errorAsignacion) }));
    }
  }

  async function quitar(orden: OrdenVendedor): Promise<void> {
    setErroresFila((actuales) => ({ ...actuales, [orden.ordenCompraId]: '' }));
    try {
      await quitarVendedor(orden.ordenCompraId);
      setActualizacion((actual) => actual + 1);
    } catch (errorQuitar: unknown) {
      setErroresFila((actuales) => ({ ...actuales, [orden.ordenCompraId]: mensaje(errorQuitar) }));
    }
  }

  const totalPaginas = Math.max(1, Math.ceil((paginaOrdenes?.total ?? 0) / TAMANO_PAGINA));
  return (
    <section aria-label="Asignación de ventas" className="panel-card comisiones-list-panel">
      <div className="clientes-panel-heading">
        <div><div className="page-eyebrow">ASIGNACIÓN DE VENTAS</div><h2>Órdenes de compra por vendedor</h2></div>
      </div>
      <div className="solicitudes-filters comisiones-filtros asignacion-filtros">
        <div><label htmlFor="asignacion-anio">Año de la orden</label><select id="asignacion-anio" onChange={(event) => { setAnio(Number(event.currentTarget.value)); setPagina(1); }} value={anio}>{Array.from({ length: 11 }, (_, indice) => 2020 + indice).map((valor) => <option key={valor} value={valor}>{valor}</option>)}</select></div>
        <div><label htmlFor="asignacion-mes">Mes de la orden</label><select id="asignacion-mes" onChange={(event) => { setMes(Number(event.currentTarget.value)); setPagina(1); }} value={mes}>{MESES.map((nombre, indice) => <option key={nombre} value={indice + 1}>{nombre}</option>)}</select></div>
        <div><label htmlFor="asignacion-vendedor">Vendedor</label><select id="asignacion-vendedor" onChange={(event) => { setVendedorId(event.currentTarget.value ? Number(event.currentTarget.value) : ''); setPagina(1); }} value={vendedorId}><option value="">Todos</option>{vendedores.map((vendedor) => <option key={vendedor.id} value={vendedor.id}>{vendedor.codigo} · {vendedor.nombre}</option>)}</select></div>
        <label className="comisiones-checkbox"><input checked={sinAsignar} onChange={(event) => { setSinAsignar(event.currentTarget.checked); setPagina(1); }} type="checkbox" />Solo sin asignar</label>
      </div>
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {cargando && <p role="status">Cargando órdenes de compra…</p>}
      {!cargando && !error && (paginaOrdenes?.items.length ?? 0) === 0 && <p role="status">No hay órdenes que coincidan con los filtros del periodo.</p>}
      {!cargando && !error && paginaOrdenes !== null && paginaOrdenes.items.length > 0 && <TablaPaginada
        caption="Órdenes de compra asignadas a vendedores"
        className="solicitudes-table comisiones-table ordenes-vendedor-table"
        classNameContenedor="solicitudes-table-scroll"
        cambiarPagina={setPagina}
        columnas={[{ id: 'orden', etiqueta: 'Orden' }, { id: 'fecha', etiqueta: 'Fecha' }, { id: 'cliente', etiqueta: 'Cliente' }, { id: 'total', etiqueta: 'Total' }, { id: 'vendedor', etiqueta: 'Vendedor' }, { id: 'acciones', etiqueta: 'Asignar o reasignar' }]}
        elementos={paginaOrdenes.items}
        etiquetaRegion="Tabla de asignaciones de ventas"
        obtenerClave={(orden) => orden.ordenCompraId}
        pagina={pagina}
        renderizarFila={(orden) => <tr key={orden.ordenCompraId}>
          <th scope="row">{orden.numeroOrden}</th>
          <td className="cantidad-sin-partir">{formatearFecha(orden.fecha)}</td>
          <td>{orden.clienteNombre}</td>
          <td className="cantidad-sin-partir">{formateadorMoneda.format(orden.total)}</td>
          <td>{orden.vendedorNombre ?? <span className="comision-unassigned">Sin asignar</span>}</td>
          <td><div className="asignacion-control">
            <label className="sr-only" htmlFor={`asignar-${orden.ordenCompraId}`}>Vendedor para {orden.numeroOrden}</label>
            <select id={`asignar-${orden.ordenCompraId}`} onChange={(event) => void asignar(orden, event.currentTarget.value)} value={orden.vendedorId ?? ''}>
              <option value="">Selecciona vendedor activo</option>
              {vendedores.map((vendedor) => <option key={vendedor.id} value={vendedor.id}>{vendedor.codigo} · {vendedor.nombre}</option>)}
            </select>
            {orden.vendedorId !== null && <button className="secondary-button" onClick={() => void quitar(orden)} type="button">Quitar</button>}
            {erroresFila[orden.ordenCompraId] && <span className="clientes-error" role="alert">{erroresFila[orden.ordenCompraId]}</span>}
          </div></td>
        </tr>}
        totalPaginas={totalPaginas}
      />}
      <span className="comisiones-total-resultados">{paginaOrdenes?.total ?? 0} órdenes</span>
    </section>
  );
}
