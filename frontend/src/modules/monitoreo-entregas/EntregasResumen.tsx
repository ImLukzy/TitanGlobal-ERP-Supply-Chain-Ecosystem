import type { ResumenEntregas } from './tipos';
export function EntregasResumen({ datos }: { datos: ResumenEntregas }) {
  return (
    <section className="monitoreo-entregas-summary" aria-label="Resumen de entregas">
      {[
        ['Total', datos.total],
        ['Programadas', datos.programadas],
        ['En ruta', datos.enRuta],
        ['Entregadas', datos.entregadas],
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
