import { formateadorMoneda, formatearCantidad } from '../../lib/formato';
import type { ResumenSegmentacion } from './tipos';

interface Props {
  datos: ResumenSegmentacion | null;
  error: string;
}

export function SegmentosResumen({ datos, error }: Props) {
  if (error !== '') {
    return <p className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p className="clientes-status" role="status">Cargando resumen de segmentación…</p>;
  }
  return (
    <section aria-label="Resumen de segmentación" className="segmentacion-summary">
      <div className="segmentacion-stats">
        <article className="cliente-stat"><span>Total de clientes</span><strong>{datos.totalClientes}</strong></article>
        <article className="cliente-stat"><span>Clientes segmentados</span><strong>{datos.clientesSegmentados}</strong></article>
        <article className="cliente-stat"><span>Sin segmento</span><strong>{datos.clientesSinSegmento}</strong></article>
      </div>
      <div aria-label="Clientes y monto por segmento" className="segmentacion-cards">
        {(datos.porSegmento ?? []).map((segmento) => (
          <article className="segmentacion-card" key={segmento.segmentoId}>
            <div><span>{segmento.codigo}</span><strong>{segmento.nombre}</strong></div>
            <p>{formatearCantidad(segmento.clientes, 'cliente', 'clientes')}</p>
            <strong>{formateadorMoneda.format(segmento.montoTotal)}</strong>
          </article>
        ))}
      </div>
    </section>
  );
}
