import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import { TablaPaginada } from '../../components/TablaPaginada';
import { Link } from 'react-router';
import { claseEstado, nombreMotivo } from './presentacion';
import type { DevolucionListado } from './tipos';

interface Props {
  items: DevolucionListado[];
  total: number;
  pagina: number;
  error: string;
  cargando: boolean;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

export function DevolucionesTabla({ items, total, pagina, error, cargando, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  const paginas = Math.max(1, Math.ceil(total / 10));
  return (
    <>
      {error && <p className="clientes-error" role="alert">{error}</p>}
      {!error && cargando && <p role="status">Cargando devoluciones…</p>}
      {!error && !cargando && items.length === 0 && <p role="status">No hay devoluciones que coincidan con los filtros.</p>}
      {items.length > 0 && <TablaPaginada
        caption="Devoluciones comerciales"
        etiquetaRegion="Tabla de devoluciones comerciales"
        columnas={[
          { id: 'numero', etiqueta: 'Número' },
          { id: 'cliente', etiqueta: 'Cliente' },
          { id: 'orden', etiqueta: 'Orden' },
          { id: 'reclamo', etiqueta: 'Reclamo' },
          { id: 'motivo', etiqueta: 'Motivo' },
          { id: 'unidades', etiqueta: 'Unidades' },
          { id: 'total', etiqueta: 'Total' },
          { id: 'estado', etiqueta: 'Estado' },
        ]}
        elementos={items}
        obtenerClave={(item) => item.id}
        pagina={pagina}
        totalPaginas={paginas}
        cambiarPagina={cambiarPagina}
        className="solicitudes-table devoluciones-table"
        classNameContenedor="solicitudes-table-scroll devoluciones-table-scroll"
        classNamePaginacion="solicitudes-pagination"
        renderizarFila={(item) => (
          <tr className={item.id === seleccionadoId ? 'is-selected' : ''}>
            <th scope="row">
              <button className="solicitud-row-select" onClick={() => seleccionar(item.id)} type="button">
                {item.numero}
              </button>
              <small>{formatearFecha(item.fecha)}</small>
            </th>
            <td>{item.clienteNombre}</td>
            <td><Link to="/modulos/04-ordenes-compra">{item.numeroOrden}</Link></td>
            <td>{item.reclamoNumero ?? '—'}</td>
            <td>{nombreMotivo(item.motivo)}</td>
            <td>{item.unidades}</td>
            <td>{formateadorMoneda.format(item.total)}</td>
            <td><span className={claseEstado(item.estado)}>{item.estado}</span></td>
          </tr>
        )}
      />}
    </>
  );
}
