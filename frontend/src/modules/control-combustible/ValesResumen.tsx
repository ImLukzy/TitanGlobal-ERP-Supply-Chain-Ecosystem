import type { ResumenVales } from './tipos';
export function ValesResumen({ datos }: { datos: ResumenVales }) {
  return (
    <section aria-label="Resumen de vales" className="control-combustible-summary">
      {[
        ['Total', datos.total],
        ['Borradores', datos.borradores],
        ['Confirmados', datos.confirmados],
        ['Anulados', datos.anulados],
      ].map(([l, v]) => (
        <article key={l}>
          <strong>{v}</strong>
          <span>{l}</span>
        </article>
      ))}
    </section>
  );
}
