import { formateadorMoneda, formatearCantidad } from '../../lib/formato';
import { nombreMotivo } from './presentacion';
import type { ResumenDevoluciones } from './tipos';

interface Props { datos: ResumenDevoluciones | null; error: string; }

export function DevolucionesResumen({ datos, error }: Props) {
  if (error) return <p className="clientes-error" role="alert">{error}</p>;
  if (datos === null) return <p role="status">Cargando resumen de devoluciones…</p>;
  return (
    <section aria-label="Resumen de devoluciones comerciales" className="devoluciones-summary">
      <article>
        <span>Solicitadas</span>
        <strong>{datos.solicitadas}</strong>
      </article>
      <article>
        <span>Aprobadas</span>
        <strong>{datos.aprobadas}</strong>
      </article>
      <article>
        <span>Recibidas</span>
        <strong>{datos.recibidas}</strong>
      </article>
      <article>
        <span>Rechazadas</span>
        <strong>{datos.rechazadas}</strong>
      </article>
      <article>
        <span>Monto reembolsado</span>
        <strong>{formateadorMoneda.format(datos.montoReembolsado)}</strong>
      </article>
      <article>
        <span>Tasa de devolución</span>
        <strong>{datos.tasaDevolucion.toFixed(2)} % de las unidades vendidas</strong>
      </article>
      <section className="solicitudes-top-panel devoluciones-reason-summary">
        <h2>Devoluciones por motivo</h2>
        {datos.porMotivo.length === 0 ? (
          <p>Sin devoluciones registradas.</p>
        ) : (
          <ol>
            {datos.porMotivo.map((item) => (
              <li key={item.motivo}>
                {nombreMotivo(item.motivo)}
                <strong>
                  {formatearCantidad(item.devoluciones, 'devolución', 'devoluciones')}
                  {' · '}
                  {formatearCantidad(item.unidades, 'unidad', 'unidades')}
                </strong>
              </li>
            ))}
          </ol>
        )}
      </section>
    </section>
  );
}
