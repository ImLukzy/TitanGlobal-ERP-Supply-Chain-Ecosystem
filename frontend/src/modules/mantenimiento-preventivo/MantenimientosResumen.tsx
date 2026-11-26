import { formatearHoras } from './presentacion';
import { etiquetasEstado, type ResumenMantenimientos } from './tipos';

interface ResumenProps {
  datos?: ResumenMantenimientos;
  error?: string;
  reintentar: () => void;
}

export function MantenimientosResumen({ datos, error, reintentar }: ResumenProps) {
  if (error !== undefined) {
    return (
      <section aria-label="Resumen de mantenimientos" className="mantenimiento-preventivo-error">
        <p className="field-error" role="alert">{error}</p>
        <button className="secondary-button" onClick={reintentar} type="button">Reintentar resumen</button>
      </section>
    );
  }
  if (datos === undefined) {
    return <p role="status">Cargando resumen…</p>;
  }

  return (
    <section aria-labelledby="mantenimiento-preventivo-resumen" className="mantenimiento-preventivo-resumen">
      <h2 className="sr-only" id="mantenimiento-preventivo-resumen">Resumen de mantenimientos</h2>
      <div className="mantenimiento-preventivo-metricas">
        <article className="cliente-stat">
          <span>Mantenimientos</span>
          <strong>{datos.totalMantenimientos}</strong>
        </article>
        {datos.porEstado.map((fila) => (
          <article className="cliente-stat" key={fila.estado}>
            <span>{etiquetasEstado[fila.estado]}</span>
            <strong>{fila.total}</strong>
          </article>
        ))}
        <article className="cliente-stat">
          <span>Vencidos</span>
          <strong>{datos.vencidos}</strong>
        </article>
        <article className="cliente-stat">
          <span>Próximos 7 días</span>
          <strong>{datos.proximos7Dias}</strong>
        </article>
        <article className="cliente-stat">
          <span>Horas pendientes</span>
          <strong className="mantenimiento-preventivo-cifra">{formatearHoras(datos.horasPlanificadasPendientes)}</strong>
        </article>
      </div>
    </section>
  );
}
