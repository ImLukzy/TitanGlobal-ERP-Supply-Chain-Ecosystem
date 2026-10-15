import { Link } from 'react-router';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import { claseEstado, nombreMotivo } from './presentacion';
import type { DevolucionDetalle } from './tipos';

type Accion = 'aprobar' | 'recibir' | 'rechazar' | 'eliminar';
interface Props {
  devolucion: DevolucionDetalle | null;
  error: string;
  cargando: boolean;
  editar: () => void;
  accion: (tipo: Accion, boton: HTMLButtonElement) => void;
}

export function DevolucionDetallePanel({ devolucion, error, cargando, editar, accion }: Props) {
  return (
    <PanelDetalle ariaLabelledBy="devolucion-detail-title" className="solicitudes-panel devoluciones-detail">
      {error && <p className="clientes-error" role="alert">{error}</p>}
      {!error && cargando && <p role="status">Cargando devolución…</p>}
      {!error && !cargando && devolucion === null && <p role="status">Selecciona una devolución para ver el detalle.</p>}
      {!error && !cargando && devolucion !== null && (
        <>
        <div className="solicitudes-panel-heading">
          <div>
            <span className="page-eyebrow">DETALLE DE DEVOLUCIÓN</span>
            <h2 id="devolucion-detail-title">{devolucion.numero}</h2>
          </div>
          <span className={claseEstado(devolucion.estado)}>{devolucion.estado}</span>
        </div>
        <dl className="devolucion-detail-grid">
          <div>
            <dt>Cliente</dt>
            <dd>{devolucion.clienteNombre}</dd>
          </div>
          <div>
            <dt>Orden de compra</dt>
            <dd><Link to="/modulos/04-ordenes-compra">{devolucion.numeroOrden}</Link></dd>
          </div>
          <div>
            <dt>Reclamo</dt>
            <dd>
              {devolucion.reclamoNumero ? (
                <Link to="/modulos/09-reclamos-postventa">{devolucion.reclamoNumero}</Link>
              ) : '—'}
            </dd>
          </div>
          <div>
            <dt>Motivo</dt>
            <dd>{nombreMotivo(devolucion.motivo)}</dd>
          </div>
          <div>
            <dt>Fecha</dt>
            <dd>{formatearFecha(devolucion.fecha)}</dd>
          </div>
          <div>
            <dt>Unidades</dt>
            <dd>{devolucion.unidades}</dd>
          </div>
          <div>
            <dt>Total</dt>
            <dd>{formateadorMoneda.format(devolucion.total)}</dd>
          </div>
          <div>
            <dt>Fecha de aprobación</dt>
            <dd>{devolucion.fechaAprobacion ? formatearFecha(devolucion.fechaAprobacion) : '—'}</dd>
          </div>
          <div>
            <dt>Fecha de recepción</dt>
            <dd>{devolucion.fechaRecepcion ? formatearFecha(devolucion.fechaRecepcion) : '—'}</dd>
          </div>
          <div>
            <dt>Fecha de rechazo</dt>
            <dd>{devolucion.fechaRechazo ? formatearFecha(devolucion.fechaRechazo) : '—'}</dd>
          </div>
        </dl>
        <div className="devolucion-lines-scroll">
          <table className="tabla-paginada devolucion-lines-table">
            <caption>Productos de la devolución</caption>
            <thead>
              <tr>
                <th scope="col">Producto</th>
                <th scope="col">Cantidad</th>
                <th scope="col">Precio</th>
                <th scope="col">Importe</th>
              </tr>
            </thead>
            <tbody>
              {devolucion.lineas.map((linea) => (
                <tr key={linea.productoId}>
                  <th scope="row">{linea.productoNombre}</th>
                  <td>{linea.cantidad}</td>
                  <td>{formateadorMoneda.format(linea.precioUnitario)}</td>
                  <td>{formateadorMoneda.format(linea.importe)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {devolucion.observaciones && (
          <section className="solicitud-detail-note">
            <h3>Observaciones</h3>
            <p>{devolucion.observaciones}</p>
          </section>
        )}
        {devolucion.motivoRechazo && (
          <p className="solicitudes-error">Motivo de rechazo: {devolucion.motivoRechazo}</p>
        )}
        <div className="solicitud-actions">
          {devolucion.estado === 'Solicitada' && (
            <>
              <button className="secondary-button" onClick={editar} type="button">Editar</button>
              <button className="primary-button" onClick={(event) => accion('aprobar', event.currentTarget)} type="button">Aprobar</button>
              <button className="danger-button" onClick={(event) => accion('rechazar', event.currentTarget)} type="button">Rechazar</button>
              <button className="secondary-button" onClick={(event) => accion('eliminar', event.currentTarget)} type="button">Eliminar</button>
            </>
          )}
          {devolucion.estado === 'Aprobada' && (
            <>
              <button className="primary-button" onClick={(event) => accion('recibir', event.currentTarget)} type="button">Marcar recibida</button>
              <button className="danger-button" onClick={(event) => accion('rechazar', event.currentTarget)} type="button">Rechazar</button>
            </>
          )}
        </div>
        </>
      )}
    </PanelDetalle>
  );
}
