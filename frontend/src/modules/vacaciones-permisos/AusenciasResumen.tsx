import type { ResumenAusencias } from './tipos';

interface Props {
  datos: ResumenAusencias | null;
  error: string;
}

export function AusenciasResumen({ datos, error }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen de ausencias…</p>;
  }
  return (
    <section aria-label="Resumen de ausencias" className="vacaciones-permisos-summary">
      <article className="cliente-stat">
        <span>Pendientes</span>
        <strong>{datos.pendientes}</strong>
      </article>
      <article className="cliente-stat">
        <span>Ausentes hoy</span>
        <strong>{datos.ausentesHoy}</strong>
      </article>
      <article className="cliente-stat">
        <span>De vacaciones hoy</span>
        <strong>{datos.vacacionesHoy}</strong>
      </article>
      <article className="cliente-stat">
        <span>Descanso médico hoy</span>
        <strong>{datos.descansosMedicosHoy}</strong>
      </article>
      <article className="cliente-stat">
        <span>Aprobadas este mes</span>
        <strong>{datos.aprobadasMes}</strong>
      </article>
    </section>
  );
}
