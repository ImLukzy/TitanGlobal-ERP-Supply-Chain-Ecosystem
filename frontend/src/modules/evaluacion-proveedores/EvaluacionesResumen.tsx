import { calificaciones, formateadorPuntaje } from './puntaje';
import type { ResumenEvaluaciones } from './tipos';

interface EvaluacionesResumenProps {
  datos: ResumenEvaluaciones | null;
  error: string;
}

export function EvaluacionesResumen({ datos, error }: EvaluacionesResumenProps) {
  if (error !== '') {
    return <p className="evaluaciones-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p className="evaluaciones-loading" role="status">Cargando resumen…</p>;
  }
  const generales = [
    { etiqueta: 'Evaluaciones', valor: String(datos.totalEvaluaciones) },
    { etiqueta: 'Proveedores evaluados', valor: String(datos.proveedoresEvaluados) },
    { etiqueta: 'Promedio general', valor: formateadorPuntaje.format(datos.promedioGeneral) },
  ];
  const conteos = calificaciones.map((calificacion) => ({
    calificacion,
    total: datos.porCalificacion.find((conteo) => conteo.calificacion === calificacion)?.total ?? 0,
  }));
  return (
    <section aria-label="Resumen de evaluaciones de proveedores" className="evaluaciones-summary">
      {generales.map((tarjeta) => (
        <article key={tarjeta.etiqueta}>
          <span>{tarjeta.etiqueta}</span>
          <strong>{tarjeta.valor}</strong>
        </article>
      ))}
      <ul aria-label="Evaluaciones por calificación" className="evaluaciones-conteos">
        {conteos.map((conteo) => (
          <li key={conteo.calificacion}>
            <span className={`evaluacion-calificacion is-${conteo.calificacion.toLowerCase()}`}>{conteo.calificacion}</span>
            <strong>{conteo.total}</strong>
          </li>
        ))}
      </ul>
      <section aria-labelledby="evaluaciones-ranking-title" className="evaluaciones-ranking">
        <h2 id="evaluaciones-ranking-title">Ranking de proveedores</h2>
        {datos.ranking.length === 0 ? (
          <p>No hay evaluaciones para resumir.</p>
        ) : (
          <div className="evaluaciones-ranking-scroll">
            <table>
              <caption>Proveedores ordenados por promedio</caption>
              <thead>
                <tr>
                  <th scope="col">Posición</th>
                  <th scope="col">Proveedor</th>
                  <th scope="col">Evaluaciones</th>
                  <th scope="col">Promedio</th>
                  <th scope="col">Última calificación</th>
                </tr>
              </thead>
              <tbody>
                {datos.ranking.map((fila, indice) => (
                  <tr key={fila.proveedorId}>
                    <td>{indice + 1}</td>
                    <th scope="row">{fila.proveedorNombre}</th>
                    <td>{fila.evaluaciones}</td>
                    <td>{formateadorPuntaje.format(fila.promedio)}</td>
                    <td><span className={`evaluacion-calificacion is-${fila.ultimaCalificacion.toLowerCase()}`}>{fila.ultimaCalificacion}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </section>
  );
}
