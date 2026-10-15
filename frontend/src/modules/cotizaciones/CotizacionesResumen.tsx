import { formateadorMoneda, formatearCantidad } from '../../lib/formato';
import type { ResumenCotizaciones } from './tipos';

export function CotizacionesResumen({
  datos,
  error,
}: {
  datos: ResumenCotizaciones | null;
  error: string;
}) {
  if (error) {
    return (
      <p className="solicitudes-error" role="alert">
        {error}
      </p>
    );
  }
  if (datos === null) {
    return <p role="status">Cargando resumen…</p>;
  }
  return (
    <section
      aria-label="Resumen de cotizaciones"
      className="solicitudes-summary cotizaciones-summary"
    >
      <article>
        <span>Emitidas</span>
        <strong>{datos.emitidas}</strong>
      </article>
      <article>
        <span>Vencidas</span>
        <strong>{datos.vencidas}</strong>
      </article>
      <article>
        <span>Aceptadas</span>
        <strong>{datos.aceptadas}</strong>
      </article>
      <article>
        <span>Rechazadas</span>
        <strong>{datos.rechazadas}</strong>
      </article>
      <article>
        <span>Monto vigente</span>
        <strong>{formateadorMoneda.format(datos.montoVigente)}</strong>
      </article>
      <article>
        <span>Monto aceptado</span>
        <strong>{formateadorMoneda.format(datos.montoAceptado)}</strong>
      </article>
      <article>
        <span>Tasa de aceptación</span>
        <strong>{datos.tasaAceptacion.toFixed(2)} %</strong>
      </article>
      <section
        aria-label="Clientes con mayor monto aceptado"
        className="solicitudes-top-panel"
      >
        <h2>Principales clientes</h2>
        {datos.topClientes.length === 0 ? (
          <p role="status">Sin clientes aceptados</p>
        ) : (
          <ol>
            {datos.topClientes.map((cliente) => (
              <li key={cliente.clienteId}>
                {cliente.nombre}
                <strong>
                  {formatearCantidad(
                    cliente.cotizaciones,
                    'cotización',
                    'cotizaciones',
                  )}{' '}
                  · {formateadorMoneda.format(cliente.montoAceptado)}
                </strong>
              </li>
            ))}
          </ol>
        )}
      </section>
    </section>
  );
}
