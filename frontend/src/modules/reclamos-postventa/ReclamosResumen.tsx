import type { ResumenReclamos } from './tipos';

export function ReclamosResumen({ datos, error }: { datos: ResumenReclamos | null; error: string }) {
  if (error) return <p className="clientes-error" role="alert">{error}</p>;
  if (datos === null) return <p role="status">Cargando resumen…</p>;
  return (
    <section aria-label="Resumen de reclamos postventa" className="reclamos-summary">
      <article><span>Abiertos</span><strong>{datos.abiertos}</strong></article>
      <article><span>En proceso</span><strong>{datos.enProceso}</strong></article>
      <article><span>Resueltos</span><strong>{datos.resueltos}</strong></article>
      <article><span>Cerrados</span><strong>{datos.cerrados}</strong></article>
      <article><span>Rechazados</span><strong>{datos.rechazados}</strong></article>
      <article className={datos.vencidos > 0 ? 'reclamos-overdue has-overdue' : 'reclamos-overdue'}><span>Vencidos</span><strong>{datos.vencidos}</strong></article>
      <article><span>Promedio de resolución</span><strong>{datos.horasPromedioResolucion.toFixed(2)} h</strong></article>
      <article><span>Cumplimiento de plazo</span><strong>{datos.cumplimientoPlazo.toFixed(2)} %</strong><small>atendidos a tiempo</small></article>
      <div className="reclamos-types"><h2>Reclamos por tipo</h2><ul>{datos.porTipo.map((item) => <li key={item.tipo}>{item.tipo === 'Facturacion' ? 'Facturación' : item.tipo}<strong>{item.reclamos}</strong></li>)}</ul></div>
    </section>
  );
}
