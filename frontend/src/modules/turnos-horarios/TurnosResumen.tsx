import type { ResumenTurnos } from './tipos';

interface Props {
  datos: ResumenTurnos | null;
  error: string;
}

export function TurnosResumen({ datos, error }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen de turnos…</p>;
  }
  return (
    <section aria-label="Resumen de turnos" className="turnos-horarios-summary">
      <article className="cliente-stat">
        <span>Turnos activos</span>
        <strong>{datos.activos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Fijos</span>
        <strong>{datos.fijos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Rotativos</span>
        <strong>{datos.rotativos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Empleados sin turno hoy</span>
        <strong>{datos.empleadosActivosSinTurnoHoy}</strong>
      </article>
    </section>
  );
}
