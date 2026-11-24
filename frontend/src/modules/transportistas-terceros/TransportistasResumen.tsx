import type { Resumen } from './tipos';
export function TransportistasResumen({ datos }: { datos: Resumen | null }) {
  if (!datos) return <p role="status">Cargando resumen…</p>;
  return (
    <section aria-label="Resumen de transportistas" className="transportistas-terceros-summary">
      {[
        ['Transportistas', datos.totalTransportistas],
        ['Activos', datos.activos],
        ['Inactivos', datos.inactivos],
      ].map(([etiqueta, valor]) => (
        <article className="transportistas-terceros-stat" key={etiqueta}>
          <span>{etiqueta}</span>
          <strong>{valor}</strong>
        </article>
      ))}
      <div className="transportistas-terceros-card">
        <h2>Por servicio</h2>
        {datos.porTipoServicio.length ? (
          datos.porTipoServicio.map((tipo) => (
            <p key={tipo.tipoServicio}>
              {tipo.tipoServicio}: <strong>{tipo.totalTransportistas}</strong> ({tipo.activos}{' '}
              activos)
            </p>
          ))
        ) : (
          <p>Sin transportistas registrados</p>
        )}
      </div>
    </section>
  );
}
