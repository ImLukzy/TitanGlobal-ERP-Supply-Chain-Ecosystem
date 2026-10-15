import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearFecha } from '../../lib/formato';
import type { PaginaReclamos, ReclamoListado } from './tipos';

export function ReclamosTabla({ lista, error, seleccionadoId, seleccionar, pagina, cambiarPagina }: {
  lista: PaginaReclamos | null;
  error: string;
  seleccionadoId: number | null;
  seleccionar: (item: ReclamoListado) => void;
  pagina: number;
  cambiarPagina: (pagina: number) => void;
}) {
  if (error) return <p className="clientes-error" role="alert">{error}</p>;
  if (lista === null) return <p role="status">Cargando reclamos…</p>;
  if (lista.items.length === 0) return <p role="status">No hay reclamos que coincidan con los filtros.</p>;
  const columnas = [{ id: 'numero', etiqueta: 'Número' }, { id: 'cliente', etiqueta: 'Cliente' }, { id: 'orden', etiqueta: 'Orden' }, { id: 'tipo', etiqueta: 'Tipo' }, { id: 'prioridad', etiqueta: 'Prioridad' }, { id: 'estado', etiqueta: 'Estado' }, { id: 'plazo', etiqueta: 'Plazo' }];
  return (
    <TablaPaginada caption="Bandeja de reclamos postventa" etiquetaRegion="Listado de reclamos" columnas={columnas} elementos={lista.items} obtenerClave={(item) => item.id} pagina={pagina} totalPaginas={Math.max(1, Math.ceil(lista.total / lista.tamanoPagina))} cambiarPagina={cambiarPagina} className="solicitudes-table reclamos-table" classNameContenedor="solicitudes-table-scroll reclamos-table-scroll" classNamePaginacion="solicitudes-pagination"
      renderizarFila={(item) => (
        <tr aria-selected={seleccionadoId === item.id} className={seleccionadoId === item.id ? 'is-selected' : ''} key={item.id}>
          <th scope="row"><button className="solicitud-row-select" onClick={() => seleccionar(item)} type="button">{item.numero}</button></th>
          <td>{item.clienteNombre}</td>
          <td><a className="reclamo-order-link" href={`/modulos/04-ordenes-compra?orden=${item.ordenCompraId}`}>{item.numeroOrden}</a></td>
          <td>{item.tipo === 'Facturacion' ? 'Facturación' : item.tipo}</td>
          <td><span className={`reclamo-priority is-${item.prioridad.toLowerCase()}`}>{item.prioridad}</span></td>
          <td><span className={`reclamo-status is-${item.estado.toLowerCase()}`}>{item.estado === 'EnProceso' ? 'En proceso' : item.estado}</span></td>
          <td className="reclamo-deadline">{item.vencido ? <strong className="reclamo-overdue-label">Vencido</strong> : <>Vence el {formatearFecha(item.fechaLimite)}</>}</td>
        </tr>
      )}
    />
  );
}
