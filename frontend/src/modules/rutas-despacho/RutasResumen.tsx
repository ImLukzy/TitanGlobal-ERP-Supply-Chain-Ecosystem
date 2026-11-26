import type { ResumenRutas } from './tipos';
export function RutasResumen({ datos }: { datos: ResumenRutas }) {
  return (
    <section aria-label="Resumen de rutas" className="rutas-despacho-summary">
      {[
        ['Rutas registradas', datos.totalRutas],
        ['Activas', datos.activas],
        ['Inactivas', datos.inactivas],
      ].map(([etiqueta, valor]) => (
        <article className="rutas-despacho-stat" key={etiqueta}>
          <strong>{valor}</strong>
          <span>{etiqueta}</span>
        </article>
      ))}
      <section className="rutas-despacho-zonas" aria-label="Distribución por zona">
        <h2>Rutas por zona</h2>
        {datos.porZona.length ? (
          <ul>
            {datos.porZona.map((z) => (
              <li key={z.zona}>
                <span>{z.zona}</span>
                <strong>{z.totalRutas}</strong>
                <small>{z.activas} activas</small>
              </li>
            ))}
          </ul>
        ) : (
          <p>Sin zonas registradas</p>
        )}
      </section>
    </section>
  );
}
