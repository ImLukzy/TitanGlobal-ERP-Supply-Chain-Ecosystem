import type { ResumenSiniestros } from './tipos';
export function SiniestrosResumen({ datos: d }: { datos: ResumenSiniestros }) {
  return (
    <section aria-label="Resumen de siniestros" className="siniestros-flota-summary">
      {(
        [
          ['Total', d.total],
          ['Registrados', d.registrados],
          ['En reclamo', d.enReclamo],
          ['Cerrados', d.cerrados],
          ['Anulados', d.anulados],
        ] as const
      ).map(([l, v]) => (
        <article key={l}>
          <strong>{v}</strong>
          <span>{l}</span>
        </article>
      ))}
    </section>
  );
}
