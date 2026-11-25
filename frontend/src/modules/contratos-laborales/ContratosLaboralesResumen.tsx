import type { ResumenContratosLaborales } from './tipos';

interface Props {
  datos: ResumenContratosLaborales | null;
  error: string;
}

export function ContratosLaboralesResumen({ datos, error }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen de contratos…</p>;
  }
  return (
    <section aria-label="Resumen de contratos" className="contratos-laborales-summary">
      <article className="cliente-stat">
        <span>Vigentes</span>
        <strong>{datos.vigentes}</strong>
      </article>
      <article className="cliente-stat">
        <span>Vencidos</span>
        <strong>{datos.vencidos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Total</span>
        <strong>{datos.total}</strong>
      </article>
    </section>
  );
}
