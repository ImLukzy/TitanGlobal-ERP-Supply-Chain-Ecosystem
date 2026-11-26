import type { ResumenCargas } from './tipos';
export function ConsolidacionesResumen({ datos }: { datos: ResumenCargas }) {
  return (
    <section className="consolidacion-carga-summary" aria-label="Resumen de consolidaciones">
      {[
        ['Total', datos.total],
        ['Borradores', datos.borradores],
        ['Confirmadas', datos.confirmadas],
        ['Canceladas', datos.canceladas],
      ].map(([label, valor]) => (
        <article key={label}>
          <strong>{valor}</strong>
          <span>{label}</span>
        </article>
      ))}
    </section>
  );
}
