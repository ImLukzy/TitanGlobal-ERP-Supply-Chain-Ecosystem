import { formatearFecha } from '../../lib/formato';
import type { ResumenAuditoria } from './tipos';

interface Props {
  datos: ResumenAuditoria | null;
  error: string;
}

export function AuditoriaResumen({ datos, error }: Props) {
  if (error !== '') {
    return <p className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p role="status">Cargando resumen de auditoría…</p>;
  }

  const mayorCantidad = Math.max(1, ...datos.ultimos7Dias.map((dia) => dia.registros));
  return (
    <section aria-label="Resumen de auditoría" className="auditoria-summary">
      <article><span>Total de registros</span><strong>{datos.total}</strong></article>
      <article><span>Altas</span><strong>{datos.altas}</strong></article>
      <article><span>Modificaciones</span><strong>{datos.modificaciones}</strong></article>
      <article><span>Bajas</span><strong>{datos.bajas}</strong></article>
      <section aria-label="Entidades con más actividad" className="auditoria-top panel-card">
        <h2>Entidades con más actividad</h2>
        {datos.porEntidad.length === 0 ? <p>Sin actividad registrada.</p> : <ol>{datos.porEntidad.map((item) => <li key={item.entidad}><span>{item.entidad}</span><strong>{item.registros}</strong></li>)}</ol>}
      </section>
      <section aria-label="Actividad de los últimos 7 días" className="auditoria-actividad panel-card">
        <h2>Actividad de los últimos 7 días</h2>
        <ol>{datos.ultimos7Dias.map((dia) => {
          const alto = Math.max(dia.registros === 0 ? 0 : 8, Math.round((dia.registros / mayorCantidad) * 100));
          return <li key={dia.fecha}>
            <span>{formatearFecha(dia.fecha)}</span>
            <div aria-label={`${dia.registros} registros el ${formatearFecha(dia.fecha)}`} aria-valuemax={mayorCantidad} aria-valuemin={0} aria-valuenow={dia.registros} className="auditoria-barra" role="meter"><span style={{ height: `${alto}%` }} /></div>
            <strong>{dia.registros}</strong>
          </li>;
        })}</ol>
      </section>
    </section>
  );
}
