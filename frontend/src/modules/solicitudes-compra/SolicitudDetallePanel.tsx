import { PanelDetalle } from '../../components/PanelDetalle';
import { formateadorMoneda, formatearCantidad, formatearFecha } from '../../lib/formato';
import type { SolicitudDetalle } from './tipos';

interface SolicitudDetallePanelProps {
  solicitud: SolicitudDetalle | null;
  cargando: boolean;
  editar: () => void;
  aprobar: (boton: HTMLButtonElement) => void;
  rechazar: (boton: HTMLButtonElement) => void;
  eliminar: (boton: HTMLButtonElement) => void;
}

export function SolicitudDetallePanel({
  solicitud,
  cargando,
  editar,
  aprobar,
  rechazar,
  eliminar,
}: SolicitudDetallePanelProps) {
  return (
    <PanelDetalle ariaLabelledBy="solicitud-detail-title" className="solicitudes-panel solicitudes-detail">
      {cargando && <p role="status">Cargando solicitud…</p>}
      {!cargando && solicitud === null && <p role="status">Selecciona una solicitud para ver el detalle.</p>}
      {!cargando && solicitud !== null && (
        <>
          <div className="solicitudes-panel-heading">
            <div>
              <span className="page-eyebrow">DETALLE DE SOLICITUD</span>
              <h2 id="solicitud-detail-title">{solicitud.numero}</h2>
            </div>
            <span className={`solicitud-status is-${solicitud.estado.toLowerCase()}`}>{solicitud.estado}</span>
          </div>
          <dl className="solicitud-detail-grid">
            <div><dt>Fecha</dt><dd>{formatearFecha(solicitud.fecha)}</dd></div>
            <div><dt>Solicitante</dt><dd>{solicitud.solicitante}</dd></div>
            <div><dt>Proveedor</dt><dd>{solicitud.proveedorNombre ?? 'Sin proveedor'}</dd></div>
            <div><dt>Total estimado</dt><dd>{formateadorMoneda.format(solicitud.montoEstimado)}</dd></div>
          </dl>
          <section aria-label="Líneas de la solicitud" className="solicitud-detail-lines">
            <h3>Productos solicitados</h3>
            <div className="solicitud-lines-table-scroll">
              <table className="solicitud-lines-table">
                <caption>Detalle de productos y subtotales</caption>
                <thead>
                  <tr>
                    <th scope="col">Producto</th>
                    <th scope="col">Cantidad</th>
                    <th scope="col">Precio</th>
                    <th scope="col">Subtotal</th>
                  </tr>
                </thead>
                <tbody>
                  {solicitud.lineas.map((linea) => (
                    <tr key={linea.productoId}>
                      <th scope="row">{linea.productoNombre}</th>
                      <td>{formatearCantidad(linea.cantidad, 'unidad', 'unidades')}</td>
                      <td>{formateadorMoneda.format(linea.precioEstimado)}</td>
                      <td>{formateadorMoneda.format(linea.subtotal)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
          {solicitud.observaciones && (
            <div className="solicitud-detail-note">
              <h3>Observaciones</h3>
              <p>{solicitud.observaciones}</p>
            </div>
          )}
          {solicitud.fechaDecision && (
            <dl className="solicitud-decision">
              <div><dt>Fecha de decisión</dt><dd>{formatearFecha(solicitud.fechaDecision)}</dd></div>
              {solicitud.estado === 'Rechazada' && solicitud.motivoRechazo && (
                <div><dt>Motivo de rechazo</dt><dd>{solicitud.motivoRechazo}</dd></div>
              )}
            </dl>
          )}
          {solicitud.estado === 'Pendiente' && (
            <div className="solicitud-actions">
              <button className="secondary-button" onClick={editar} type="button">Editar</button>
              <button className="primary-button" onClick={(event) => aprobar(event.currentTarget)} type="button">Aprobar</button>
              <button className="secondary-button" onClick={(event) => rechazar(event.currentTarget)} type="button">Rechazar</button>
              <button className="danger-button" onClick={(event) => eliminar(event.currentTarget)} type="button">Eliminar</button>
            </div>
          )}
        </>
      )}
    </PanelDetalle>
  );
}
