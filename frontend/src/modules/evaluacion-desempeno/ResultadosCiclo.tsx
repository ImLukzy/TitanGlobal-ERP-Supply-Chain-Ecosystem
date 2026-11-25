import type { ResultadosCiclo as Resultados } from './tipos';
import { formatearDecimal } from './validacionEvaluacion';

interface Props {
  resultados: Resultados | null;
  error: string;
}

interface BarraProps {
  etiqueta: string;
  valor: number;
  maximo: number;
  texto: string;
}

// Barra horizontal hecha con CSS: el ancho es proporcional y el número va escrito al lado, nunca solo color.
function Barra({ etiqueta, valor, maximo, texto }: BarraProps) {
  const porcentaje = maximo > 0 ? Math.min(100, Math.round((valor / maximo) * 100)) : 0;
  return (
    <li className="evaluacion-desempeno-barra">
      <span className="evaluacion-desempeno-barra-etiqueta">{etiqueta}</span>
      <span aria-hidden="true" className="evaluacion-desempeno-barra-pista">
        <span className="evaluacion-desempeno-barra-relleno" style={{ width: `${porcentaje}%` }} />
      </span>
      <span className="evaluacion-desempeno-barra-valor">{texto}</span>
    </li>
  );
}

export function ResultadosCiclo({ resultados, error }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (resultados === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resultados…</p>;
  }
  const maximoCantidad = Math.max(1, ...resultados.distribucion.map((item) => item.cantidad));
  return (
    <section aria-labelledby="ciclo-resultados-title" className="evaluacion-desempeno-section">
      <h3 id="ciclo-resultados-title">Resultados</h3>
      <dl className="evaluacion-desempeno-resumen">
        <div><dt>Evaluaciones enviadas o cerradas</dt><dd>{resultados.evaluadas}</dd></div>
        <div><dt>Puntaje promedio</dt><dd>{formatearDecimal(resultados.puntajePromedio)}</dd></div>
      </dl>
      {resultados.evaluadas === 0 && <p className="clientes-status">Todavía no hay evaluaciones enviadas en este ciclo.</p>}
      <h4 id="ciclo-distribucion-title">Distribución por etiqueta</h4>
      <ul aria-labelledby="ciclo-distribucion-title" className="evaluacion-desempeno-barras">
        {resultados.distribucion.map((item) => (
          <Barra etiqueta={item.etiqueta} key={item.etiqueta} maximo={maximoCantidad} texto={String(item.cantidad)} valor={item.cantidad} />
        ))}
      </ul>
      <h4 id="ciclo-competencias-promedio-title">Promedio por competencia (de 1 a 5)</h4>
      <ul aria-labelledby="ciclo-competencias-promedio-title" className="evaluacion-desempeno-barras">
        {resultados.porCompetencia.map((item) => (
          <Barra etiqueta={item.competencia} key={item.competencia} maximo={5} texto={formatearDecimal(item.promedio)} valor={item.promedio} />
        ))}
      </ul>
    </section>
  );
}
