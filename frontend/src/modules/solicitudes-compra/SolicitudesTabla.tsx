import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import type { PaginaSolicitudes, SolicitudListado } from './tipos';

interface SolicitudesTablaProps {
  lista: PaginaSolicitudes | null;
  error: string;
  seleccionadaId: number | null;
  seleccionar: (solicitud: SolicitudListado) => void;
  pagina: number;
  cambiarPagina: (pagina: number) => void;
}

export function SolicitudesTabla({
  lista,
  error,
  seleccionadaId,
  seleccionar,
  pagina,
  cambiarPagina,
}: SolicitudesTablaProps) {
  if (error !== '') {
    return <p className="solicitudes-error" role="alert">{error}</p>;
  }
  if (lista === null) {
    return <p role="status">Cargando solicitudes…</p>;
  }
  if (lista.items.length === 0) {
    return <p role="status">No hay solicitudes que coincidan con los filtros.</p>;
  }
  const totalPaginas = Math.max(1, Math.ceil(lista.total / 10));
  return (
    <TablaPaginada
      caption="Solicitudes de compra"
      etiquetaRegion="Tabla de solicitudes de compra"
      columnas={[
        { id: 'numero', etiqueta: 'Número' },
        { id: 'fecha', etiqueta: 'Fecha' },
        { id: 'solicitante', etiqueta: 'Solicitante' },
        { id: 'proveedor', etiqueta: 'Proveedor' },
        { id: 'estado', etiqueta: 'Estado' },
        { id: 'monto', etiqueta: 'Monto estimado' },
      ]}
      elementos={lista.items}
      obtenerClave={(solicitud) => solicitud.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="solicitudes-table"
      classNameContenedor="solicitudes-table-scroll"
      classNamePaginacion="solicitudes-pagination"
      renderizarFila={(solicitud) => (
        <tr className={seleccionadaId === solicitud.id ? 'is-selected' : ''}>
          <td>
            <button className="solicitud-row-select" onClick={() => seleccionar(solicitud)} type="button">
              {solicitud.numero}
            </button>
          </td>
          <td>{formatearFecha(solicitud.fecha)}</td>
          <td>{solicitud.solicitante}</td>
          <td>{solicitud.proveedorNombre ?? 'Sin proveedor'}</td>
          <td>
            <span className={`solicitud-status is-${solicitud.estado.toLowerCase()}`}>
              {solicitud.estado}
            </span>
          </td>
          <td>{formateadorMoneda.format(solicitud.montoEstimado)}</td>
        </tr>
      )}
    />
  );
}
