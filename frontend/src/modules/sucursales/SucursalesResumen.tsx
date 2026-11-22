import type { ResumenSucursales } from './tipos';
export function SucursalesResumen({ datos: d }: { datos: ResumenSucursales }) {
  return (
    <section className="sucursales-summary" aria-label="Resumen de sucursales">
      {(
        [
          ['Total', d.total],
          ['Activas', d.activas],
          ['Inactivas', d.inactivas],
          ['Filiales', d.filiales],
          ['Plantas', d.plantas],
          ['Oficinas', d.oficinas],
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
