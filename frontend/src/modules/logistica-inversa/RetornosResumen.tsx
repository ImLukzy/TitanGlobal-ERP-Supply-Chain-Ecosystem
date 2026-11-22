import type { ResumenRetornos } from './tipos';
export function RetornosResumen({ datos }: { datos: ResumenRetornos }) {
  return (
    <section aria-label="Resumen de retornos" className="logistica-inversa-summary">
      {(
        [
          ['Total', datos.total],
          ['Programados', datos.programados],
          ['Recogidos', datos.recogidos],
          ['Recibidos', datos.recibidos],
          ['Cancelados', datos.cancelados],
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
