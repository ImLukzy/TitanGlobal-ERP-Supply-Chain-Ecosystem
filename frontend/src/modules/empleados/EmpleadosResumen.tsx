import type { ResumenEmpleados } from './tipos';

interface Props {
  datos: ResumenEmpleados | null;
  error: string;
}

export function EmpleadosResumen({ datos, error }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen de empleados…</p>;
  }
  return (
    <section aria-label="Resumen de empleados" className="empleados-summary">
      <article className="cliente-stat">
        <span>Activos</span>
        <strong>{datos.activos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Cesados</span>
        <strong>{datos.cesados}</strong>
      </article>
      <article className="cliente-stat">
        <span>Total</span>
        <strong>{datos.total}</strong>
      </article>
    </section>
  );
}
