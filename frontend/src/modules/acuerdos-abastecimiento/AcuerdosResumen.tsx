import { formateadorMoneda } from '../../lib/formato';
import type { ResumenAcuerdos } from './tipos';
import { estados } from './vigencia';

interface AcuerdosResumenProps {
  datos: ResumenAcuerdos | null;
  error: string;
}

export function AcuerdosResumen({ datos, error }: AcuerdosResumenProps) {
  if (error !== '') {
    return <p className="acuerdos-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p className="acuerdos-loading" role="status">Cargando resumen…</p>;
  }
  const tarjetas = [
    { etiqueta: 'Acuerdos', valor: String(datos.totalAcuerdos) },
    { etiqueta: 'Monto máximo vigente', valor: formateadorMoneda.format(datos.montoMaximoVigente) },
    { etiqueta: 'Monto consumido', valor: formateadorMoneda.format(datos.montoConsumido) },
  ];
  const conteos = estados.map((estado) => ({
    estado,
    total: datos.porEstado.find((conteo) => conteo.estado === estado)?.total ?? 0,
  }));
  return (
    <section aria-label="Resumen de acuerdos" className="acuerdos-summary">
      {tarjetas.map((tarjeta) => (
        <article key={tarjeta.etiqueta}>
          <span>{tarjeta.etiqueta}</span>
          <strong>{tarjeta.valor}</strong>
        </article>
      ))}
      <ul aria-label="Acuerdos por estado" className="acuerdos-conteos">
        {conteos.map((conteo) => (
          <li key={conteo.estado}>
            <span className={`acuerdo-estado is-${conteo.estado.toLowerCase()}`}>{conteo.estado}</span>
            <strong>{conteo.total}</strong>
          </li>
        ))}
      </ul>
    </section>
  );
}
