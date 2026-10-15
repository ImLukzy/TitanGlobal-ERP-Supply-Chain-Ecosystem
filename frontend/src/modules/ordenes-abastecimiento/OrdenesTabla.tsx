import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import type { OrdenAbastecimientoListado, PaginaOrdenesAbastecimiento } from './tipos';

interface OrdenesTablaProps {
  lista: PaginaOrdenesAbastecimiento | null;
  error: string;
  seleccionadaId: number | null;
  seleccionar: (orden: OrdenAbastecimientoListado) => void;
  pagina: number;
  cambiarPagina: (pagina: number) => void;
}

const columnas = [
  { id: 'numero', etiqueta: 'Número' },
  { id: 'fecha', etiqueta: 'Fecha' },
  { id: 'proveedor', etiqueta: 'Proveedor' },
  { id: 'solicitud', etiqueta: 'Solicitud' },
  { id: 'entrega', etiqueta: 'Entrega estimada' },
  { id: 'estado', etiqueta: 'Estado' },
  { id: 'monto', etiqueta: 'Monto' },
];

export function OrdenesTabla({ lista, error, seleccionadaId, seleccionar, pagina, cambiarPagina }: OrdenesTablaProps) {
  if (error !== '') {
    return <p className="ordenes-error" role="alert">{error}</p>;
  }
  if (lista === null) {
    return <p className="ordenes-loading" role="status">Cargando órdenes…</p>;
  }
  if (lista.items.length === 0) {
    return <p className="ordenes-empty" role="status">No hay órdenes de abastecimiento para mostrar.</p>;
  }
  return (
    <TablaPaginada
      caption="Órdenes de abastecimiento"
      className="ordenes-table"
      classNameContenedor="ordenes-table-scroll"
      classNamePaginacion="ordenes-pagination"
      columnas={columnas}
      elementos={lista.items}
      etiquetaRegion="Listado de órdenes de abastecimiento"
      obtenerClave={(orden) => orden.id}
      renderizarFila={(orden) => (
        <tr aria-selected={seleccionadaId === orden.id} className={seleccionadaId === orden.id ? 'is-selected' : ''} key={orden.id}>
          <th scope="row">
            <button className="orden-row-select" onClick={() => seleccionar(orden)} type="button">{orden.numero}</button>
          </th>
          <td>{formatearFecha(orden.fecha)}</td>
          <td>{orden.proveedorNombre}</td>
          <td>{orden.solicitudNumero ?? 'Manual'}</td>
          <td>
            {formatearFecha(orden.fechaEntregaEstimada)}
            {orden.atrasada && <span className="orden-late-label">Atrasada</span>}
          </td>
          <td><span className={`orden-status is-${orden.estado.toLowerCase()}`}>{orden.estado}</span></td>
          <td>{formateadorMoneda.format(orden.montoTotal)}</td>
        </tr>
      )}
      cambiarPagina={cambiarPagina}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(lista.total / lista.tamanoPagina))}
    />
  );
}
