import { Link } from 'react-router';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import type { CotizacionDetalle } from './tipos';

interface Props {
  cotizacion: CotizacionDetalle | null;
  cargando: boolean;
  editar: () => void;
  aceptar: (boton: HTMLButtonElement) => void;
  rechazar: (boton: HTMLButtonElement) => void;
  eliminar: (boton: HTMLButtonElement) => void;
}

export function CotizacionDetallePanel({
  cotizacion,
  cargando,
  editar,
  aceptar,
  rechazar,
  eliminar,
}: Props) {
  const emitida = cotizacion?.estado === 'Emitida';
  return (
    <PanelDetalle
      ariaLabelledBy="cotizacion-detail-title"
      className="solicitudes-panel solicitudes-detail"
    >
      {cargando && <p role="status">Cargando cotización…</p>}
      {!cargando && cotizacion === null && (
        <p role="status">Selecciona una cotización para ver el detalle.</p>
      )}
      {!cargando && cotizacion !== null && (
        <>
          <div className="solicitudes-panel-heading">
            <div>
              <span className="page-eyebrow">DETALLE DE COTIZACIÓN</span>
              <h2 id="cotizacion-detail-title">{cotizacion.numero}</h2>
            </div>
            <span
              className={`solicitud-status is-${cotizacion.estado.toLowerCase()}`}
            >
              {cotizacion.estado}
            </span>
          </div>
          <dl className="solicitud-detail-grid">
            <div>
              <dt>Cliente</dt>
              <dd>{cotizacion.clienteNombre}</dd>
            </div>
            <div>
              <dt>Fecha</dt>
              <dd>{formatearFecha(cotizacion.fecha)}</dd>
            </div>
            <div>
              <dt>Validez</dt>
              <dd>
                {cotizacion.vencida
                  ? 'Vencida'
                  : formatearFecha(cotizacion.fechaValidez)}
              </dd>
            </div>
            <div>
              <dt>Estado</dt>
              <dd>{cotizacion.estado}</dd>
            </div>
          </dl>
          <section
            aria-label="Líneas de la cotización"
            className="solicitud-detail-lines"
          >
            <h3>Productos cotizados</h3>
            <div className="solicitud-lines-table-scroll cotizacion-lines-scroll">
              <table className="solicitud-lines-table cotizacion-lines-table">
                <caption>Precios, descuentos e importes por línea</caption>
                <thead>
                  <tr>
                    <th scope="col">Producto</th>
                    <th scope="col">Cantidad</th>
                    <th scope="col">Precio</th>
                    <th scope="col">Descuento</th>
                    <th scope="col">Importe</th>
                  </tr>
                </thead>
                <tbody>
                  {cotizacion.lineas.map((linea) => (
                    <tr key={linea.productoId}>
                      <th scope="row">{linea.productoNombre}</th>
                      <td>{linea.cantidad}</td>
                      <td>{formateadorMoneda.format(linea.precioUnitario)}</td>
                      <td>
                        {linea.descuentoPorcentaje.toFixed(2)} %<br />
                        {formateadorMoneda.format(linea.descuento)}
                      </td>
                      <td>{formateadorMoneda.format(linea.importe)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
          <dl className="cotizacion-totals">
            <div>
              <dt>Subtotal</dt>
              <dd>{formateadorMoneda.format(cotizacion.subtotal)}</dd>
            </div>
            <div>
              <dt>Descuento</dt>
              <dd>{formateadorMoneda.format(cotizacion.descuento)}</dd>
            </div>
            <div>
              <dt>Total</dt>
              <dd>{formateadorMoneda.format(cotizacion.total)}</dd>
            </div>
          </dl>
          {cotizacion.observaciones && (
            <section className="solicitud-detail-note">
              <h3>Observaciones</h3>
              <p>{cotizacion.observaciones}</p>
            </section>
          )}
          {cotizacion.estado === 'Rechazada' && cotizacion.motivoRechazo && (
            <p className="solicitudes-error">
              Motivo: {cotizacion.motivoRechazo}
            </p>
          )}
          {cotizacion.estado === 'Aceptada' && cotizacion.ordenCompraNumero && (
            <p>
              <Link to="/modulos/04-ordenes-compra">
                Ver orden {cotizacion.ordenCompraNumero}
              </Link>
            </p>
          )}
          {emitida && (
            <div className="solicitud-actions">
              <button
                className="secondary-button"
                onClick={editar}
                type="button"
              >
                Editar
              </button>
              {!cotizacion.vencida && (
                <button
                  className="primary-button"
                  onClick={(event) => aceptar(event.currentTarget)}
                  type="button"
                >
                  Aceptar
                </button>
              )}
              <button
                className="secondary-button"
                onClick={(event) => rechazar(event.currentTarget)}
                type="button"
              >
                Rechazar
              </button>
              <button
                className="danger-button"
                onClick={(event) => eliminar(event.currentTarget)}
                type="button"
              >
                Eliminar
              </button>
            </div>
          )}
        </>
      )}
    </PanelDetalle>
  );
}
