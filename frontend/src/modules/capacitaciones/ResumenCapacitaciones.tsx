import { formatearHoras } from './presentacion';
import type { ResumenCapacitaciones as Resumen } from './tipos';

interface Props {
  datos: Resumen | null;
  error: string;
}

export function ResumenCapacitaciones({ datos, error }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen de capacitaciones…</p>;
  }
  return (
    <section aria-label={`Resumen de capacitaciones ${datos.anio}`} className="capacitaciones-summary">
      <article className="cliente-stat">
        <span>Cursos activos</span>
        <strong>{datos.cursosActivos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Programadas</span>
        <strong>{datos.programadas}</strong>
      </article>
      <article className="cliente-stat">
        <span>Realizadas</span>
        <strong>{datos.realizadas}</strong>
      </article>
      <article className="cliente-stat">
        <span>Inscritos</span>
        <strong>{datos.inscritos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Horas totales</span>
        <strong>{formatearHoras(datos.horasTotales)}</strong>
      </article>
      <article className="cliente-stat">
        <span>Certificaciones vencidas</span>
        <strong>{datos.certificacionesVencidas}</strong>
      </article>
    </section>
  );
}
