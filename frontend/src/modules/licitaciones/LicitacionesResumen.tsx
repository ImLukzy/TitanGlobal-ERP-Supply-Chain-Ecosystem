import { formateadorMoneda } from '../../lib/formato';
import { estados } from './plazo';
import type { ResumenLicitaciones } from './tipos';

interface LicitacionesResumenProps {
  datos: ResumenLicitaciones | null;
  error: string;
}

export function LicitacionesResumen({ datos, error }: LicitacionesResumenProps) {
  if (error !== '') {
    return <p className="licitaciones-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p className="licitaciones-loading" role="status">Cargando resumen…</p>;
  }
  const tarjetas = [
    { etiqueta: 'Licitaciones', valor: String(datos.totalLicitaciones) },
    { etiqueta: 'Ofertas', valor: String(datos.totalOfertas) },
    { etiqueta: 'Presupuesto abierto', valor: formateadorMoneda.format(datos.presupuestoAbierto) },
    { etiqueta: 'Monto adjudicado', valor: formateadorMoneda.format(datos.montoAdjudicado) },
  ];
  const conteos = estados.map((estado) => ({
    estado,
    total: datos.porEstado.find((conteo) => conteo.estado === estado)?.total ?? 0,
  }));
  return (
    <section aria-label="Resumen de licitaciones" className="licitaciones-summary">
      {tarjetas.map((tarjeta) => (
        <article key={tarjeta.etiqueta}>
          <span>{tarjeta.etiqueta}</span>
          <strong>{tarjeta.valor}</strong>
        </article>
      ))}
      <ul aria-label="Licitaciones por estado" className="licitaciones-conteos">
        {conteos.map((conteo) => (
          <li key={conteo.estado}>
            <span className={`licitacion-estado is-${conteo.estado.toLowerCase()}`}>{conteo.estado}</span>
            <strong>{conteo.total}</strong>
          </li>
        ))}
      </ul>
    </section>
  );
}
