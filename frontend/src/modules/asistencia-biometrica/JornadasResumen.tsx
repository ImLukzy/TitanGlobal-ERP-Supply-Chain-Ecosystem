import { formatearMinutos } from './presentacion';
import type { ResumenAsistencia } from './tipos';

interface Props {
  datos: ResumenAsistencia | null;
  error: string;
}

export function JornadasResumen({ datos, error }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen de asistencia…</p>;
  }
  return (
    <section aria-label="Resumen de asistencia del rango" className="asistencia-summary">
      <article className="cliente-stat">
        <span>Asistencias</span>
        <strong>{datos.asistencias}</strong>
      </article>
      <article className="cliente-stat">
        <span>Tardanzas</span>
        <strong>{datos.tardanzas}</strong>
      </article>
      <article className="cliente-stat">
        <span>Sin marcación</span>
        <strong>{datos.sinMarcacion}</strong>
      </article>
      <article className="cliente-stat">
        <span>Faltas</span>
        <strong>{datos.faltas}</strong>
      </article>
      <article className="cliente-stat">
        <span>Extra pendiente</span>
        <strong>{formatearMinutos(datos.minutosExtraPendientes)}</strong>
      </article>
      <article className="cliente-stat">
        <span>Extra aprobada</span>
        <strong>{formatearMinutos(datos.minutosExtraAprobados)}</strong>
      </article>
    </section>
  );
}
