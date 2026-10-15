import { formatearCantidad } from '../../lib/formato';
import { nombreTipo } from './presentacion';
import type { ResumenAlertas } from './tipos';

interface Props {
  datos: ResumenAlertas | null;
  error: string;
}

export function AlertasResumen({ datos, error }: Props) {
  if (error !== '') {
    return <p className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p role="status">Cargando resumen de alertas…</p>;
  }
  return (
    <section aria-label="Resumen de alertas" className="alertas-summary">
      <article><span>Total pendientes</span><strong>{datos.total}</strong></article>
      <article className="alertas-count-high"><span>Altas</span><strong>{datos.altas}</strong></article>
      <article className="alertas-count-medium"><span>Medias</span><strong>{datos.medias}</strong></article>
      <article className="alertas-count-low"><span>Bajas</span><strong>{datos.bajas}</strong></article>
      <article><span>Descartadas</span><strong>{datos.descartadas}</strong></article>
      <section aria-label="Alertas por tipo" className="solicitudes-top-panel alertas-by-type">
        <h2>Alertas por tipo</h2>
        {datos.porTipo.length === 0 ? (
          <p>Sin alertas pendientes.</p>
        ) : (
          <ol>
            {datos.porTipo.map((item) => (
              <li key={item.tipo}>
                <span>{nombreTipo(item.tipo)}</span>
                <strong>{formatearCantidad(item.alertas, 'alerta', 'alertas')}</strong>
              </li>
            ))}
          </ol>
        )}
      </section>
    </section>
  );
}
