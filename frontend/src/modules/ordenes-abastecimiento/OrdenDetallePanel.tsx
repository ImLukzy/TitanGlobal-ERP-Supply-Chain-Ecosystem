import { PanelDetalle } from '../../components/PanelDetalle';
import { formateadorMoneda, formatearCantidad, formatearFecha } from '../../lib/formato';
import type { OrdenAbastecimientoDetalle } from './tipos';

interface OrdenDetallePanelProps {
  orden: OrdenAbastecimientoDetalle | null;
  cargando: boolean;
  editar: () => void;
  recibir: (boton: HTMLButtonElement) => void;
  anular: (boton: HTMLButtonElement) => void;
}

export function OrdenDetallePanel({ orden, cargando, editar, recibir, anular }: OrdenDetallePanelProps) {
  return (
    <PanelDetalle ariaLabelledBy="orden-detail-title" className="ordenes-panel ordenes-detail">
      {cargando && <p role="status">Cargando orden…</p>}
      {!cargando && orden === null && <p role="status">Selecciona una orden para ver el detalle.</p>}
      {!cargando && orden !== null && (
        <>
          <div className="ordenes-panel-heading">
            <div>
              <span className="page-eyebrow">DETALLE DE ORDEN</span>
              <h2 id="orden-detail-title">{orden.numero}</h2>
            </div>
            <span className={`orden-status is-${orden.estado.toLowerCase()}`}>{orden.estado}</span>
          </div>
          {orden.atrasada && orden.estado === 'Emitida' && <p className="orden-late-banner">Atrasada</p>}
          <dl className="orden-detail-grid">
            <div><dt>Fecha</dt><dd>{formatearFecha(orden.fecha)}</dd></div>
            <div><dt>Proveedor</dt><dd>{orden.proveedorNombre}</dd></div>
            <div><dt>Solicitud de origen</dt><dd>{orden.solicitudNumero ?? 'Manual'}</dd></div>
            <div><dt>Entrega estimada</dt><dd>{formatearFecha(orden.fechaEntregaEstimada)}</dd></div>
            {orden.fechaRecepcion && <div><dt>Fecha de recepción</dt><dd>{formatearFecha(orden.fechaRecepcion)}</dd></div>}
            <div><dt>Monto total</dt><dd>{formateadorMoneda.format(orden.montoTotal)}</dd></div>
          </dl>
          <section aria-label="Líneas de la orden" className="orden-detail-lines">
            <h3>Productos pedidos</h3>
            <div className="orden-lines-table-scroll">
              <table className="orden-lines-table">
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
                  {orden.lineas.map((linea) => (
                    <tr key={linea.productoId}>
                      <th scope="row">{linea.productoNombre}</th>
                      <td>{formatearCantidad(linea.cantidad, 'unidad', 'unidades')}</td>
                      <td>{formateadorMoneda.format(linea.precioUnitario)}</td>
                      <td>{formateadorMoneda.format(linea.subtotal)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
          {orden.observaciones && <section className="orden-detail-note"><h3>Observaciones</h3><p>{orden.observaciones}</p></section>}
          {orden.estado === 'Anulada' && orden.motivoAnulacion && <section className="orden-detail-note"><h3>Motivo de anulación</h3><p>{orden.motivoAnulacion}</p></section>}
          {orden.estado === 'Emitida' && (
            <div className="orden-actions">
              <button className="secondary-button" onClick={editar} type="button">Editar</button>
              <button className="primary-button" onClick={(event) => recibir(event.currentTarget)} type="button">Marcar recibida</button>
              <button className="danger-button" onClick={(event) => anular(event.currentTarget)} type="button">Anular</button>
            </div>
          )}
        </>
      )}
    </PanelDetalle>
  );
}
