import type { ResumenGuias } from './tipos';
export function GuiasResumen({ datos }: { datos: ResumenGuias }) {
  return (
    <section className="guias-remision-summary" aria-label="Resumen de guías">
      {[
        ['Total', datos.total],
        ['Borradores', datos.borradores],
        ['Emitidas', datos.emitidas],
        ['Anuladas', datos.anuladas],
      ].map(([label, valor]) => (
        <article key={label}>
          <strong>{valor}</strong>
          <span>{label}</span>
        </article>
      ))}
    </section>
  );
}
