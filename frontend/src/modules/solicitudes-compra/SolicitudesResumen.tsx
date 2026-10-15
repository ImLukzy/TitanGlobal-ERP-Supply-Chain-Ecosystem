import { formateadorMoneda, formatearCantidad } from '../../lib/formato';
import type { ResumenSolicitudes } from './tipos';

interface SolicitudesResumenProps {
  datos: ResumenSolicitudes | null;
  error: string;
}

export function SolicitudesResumen({ datos, error }: SolicitudesResumenProps) {
  if (error !== '') {
    return <p className="solicitudes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p role="status">Cargando resumen…</p>;
  }
  return (
    <section aria-label="Resumen de solicitudes de compra" className="solicitudes-summary">
      <article>
        <span>Pendientes</span>
        <strong>{datos.pendientes}</strong>
      </article>
      <article>
        <span>Aprobadas</span>
        <strong>{datos.aprobadas}</strong>
      </article>
      <article>
        <span>Rechazadas</span>
        <strong>{datos.rechazadas}</strong>
      </article>
      <article>
        <span>Monto pendiente</span>
        <strong>{formateadorMoneda.format(datos.montoPendiente)}</strong>
      </article>
      <article>
        <span>Monto aprobado</span>
        <strong>{formateadorMoneda.format(datos.montoAprobado)}</strong>
      </article>
      <section aria-labelledby="solicitudes-top-title" className="solicitudes-top-panel">
        <h2 id="solicitudes-top-title">Productos más solicitados</h2>
        {datos.topProductosSolicitados.length === 0 ? (
          <p role="status">Sin productos solicitados</p>
        ) : (
          <ol>
            {datos.topProductosSolicitados.map((producto) => (
              <li key={producto.productoId}>
                <span>{producto.nombre}</span>
                <strong>{formatearCantidad(producto.unidades, 'unidad', 'unidades')}</strong>
              </li>
            ))}
          </ol>
        )}
      </section>
    </section>
  );
}
