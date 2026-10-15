import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import type { CotizacionListado, PaginaCotizaciones } from './tipos';

interface Props {
  lista: PaginaCotizaciones | null;
  error: string;
  seleccionadaId: number | null;
  seleccionar: (item: CotizacionListado) => void;
  pagina: number;
  cambiarPagina: (pagina: number) => void;
}

export function CotizacionesTabla({
  lista,
  error,
  seleccionadaId,
  seleccionar,
  pagina,
  cambiarPagina,
}: Props) {
  if (error) {
    return (
      <p className="solicitudes-error" role="alert">
        {error}
      </p>
    );
  }
  if (lista === null) {
    return <p role="status">Cargando cotizaciones…</p>;
  }
  if (lista.items.length === 0) {
    return (
      <p role="status">No hay cotizaciones que coincidan con los filtros.</p>
    );
  }
  return (
    <TablaPaginada
      caption="Cotizaciones"
      etiquetaRegion="Tabla de cotizaciones"
      columnas={[
        { id: 'numero', etiqueta: 'Número' },
        { id: 'fecha', etiqueta: 'Fecha' },
        { id: 'cliente', etiqueta: 'Cliente' },
        { id: 'validez', etiqueta: 'Validez' },
        { id: 'estado', etiqueta: 'Estado' },
        { id: 'total', etiqueta: 'Total' },
        { id: 'orden', etiqueta: 'Orden generada' },
      ]}
      elementos={lista.items}
      obtenerClave={(item) => item.id}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(lista.total / 10))}
      cambiarPagina={cambiarPagina}
      className="solicitudes-table cotizaciones-table"
      classNameContenedor="solicitudes-table-scroll"
      classNamePaginacion="solicitudes-pagination"
      renderizarFila={(item) => (
        <tr className={seleccionadaId === item.id ? 'is-selected' : ''}>
          <td>
            <button
              className="solicitud-row-select"
              onClick={() => seleccionar(item)}
              type="button"
            >
              {item.numero}
            </button>
          </td>
          <td>{formatearFecha(item.fecha)}</td>
          <td>{item.clienteNombre}</td>
          <td>
            {item.vencida ? (
              <span className="cotizacion-expired-label">Vencida</span>
            ) : (
              formatearFecha(item.fechaValidez)
            )}
          </td>
          <td>
            <span
              className={`solicitud-status is-${item.estado.toLowerCase()}`}
            >
              {item.estado}
            </span>
          </td>
          <td>{formateadorMoneda.format(item.total)}</td>
          <td>{item.ordenCompraNumero ?? '—'}</td>
        </tr>
      )}
    />
  );
}
