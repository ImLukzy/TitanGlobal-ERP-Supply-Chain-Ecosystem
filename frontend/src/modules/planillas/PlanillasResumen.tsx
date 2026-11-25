import { formateadorMoneda } from '../../lib/formato';
import { textoPeriodoResumen } from './presentacion';
import type { ResumenPlanillas } from './tipos';

interface Props {
  datos: ResumenPlanillas | null;
  error: string;
}

export function PlanillasResumen({ datos, error }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen de planillas…</p>;
  }
  return (
    <section aria-label="Resumen de planillas" className="planillas-summary">
      <article className="cliente-stat">
        <span>Último periodo</span>
        <strong>{textoPeriodoResumen(datos.ultimoPeriodo)}</strong>
      </article>
      <article className="cliente-stat">
        <span>Estado</span>
        <strong>{datos.estadoUltimo ?? '—'}</strong>
      </article>
      <article className="cliente-stat">
        <span>Neto del último periodo</span>
        <strong>{formateadorMoneda.format(datos.netoUltimo)}</strong>
      </article>
      <article className="cliente-stat">
        <span>Empleados</span>
        <strong>{datos.empleadosUltimo}</strong>
      </article>
      <article className="cliente-stat">
        <span>Incidencias</span>
        <strong>{datos.incidenciasUltimo}</strong>
      </article>
      <article className="cliente-stat">
        <span>Pendientes de aprobar</span>
        <strong>{datos.borradoresYCalculadas}</strong>
      </article>
    </section>
  );
}
