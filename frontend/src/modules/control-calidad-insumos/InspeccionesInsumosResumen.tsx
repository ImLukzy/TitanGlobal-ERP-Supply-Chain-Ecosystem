import { formatearPorcentaje } from './presentacion';
import { estadosInspeccion, type ResumenInspeccionesInsumos } from './tipos';

interface ResumenProps {
  datos?: ResumenInspeccionesInsumos;
  error?: string;
  reintentar: () => void;
}

export function InspeccionesInsumosResumen({ datos, error, reintentar }: ResumenProps) {
  if (error !== undefined) {
    return (
      <section aria-label="Resumen de inspecciones" className="control-calidad-insumos-error">
        <p className="field-error" role="alert">{error}</p>
        <button className="secondary-button" onClick={reintentar} type="button">Reintentar resumen</button>
      </section>
    );
  }
  if (datos === undefined) {
    return <p role="status">Cargando resumen…</p>;
  }

  return (
    <section aria-labelledby="control-calidad-insumos-resumen" className="control-calidad-insumos-resumen">
      <h2 className="sr-only" id="control-calidad-insumos-resumen">Resumen de inspecciones</h2>
      <div className="control-calidad-insumos-metricas">
        <article className="cliente-stat">
          <span>Inspecciones</span>
          <strong>{datos.totalInspecciones}</strong>
        </article>
        {estadosInspeccion.map((estado) => (
          <article className="cliente-stat" key={estado}>
            <span>{`${estado}s`}</span>
            <strong>{datos.porEstado.find((fila) => fila.estado === estado)?.total ?? 0}</strong>
          </article>
        ))}
        <article className="cliente-stat">
          <span>Tasa de rechazo</span>
          <strong>{formatearPorcentaje(datos.tasaRechazoPorcentaje)}</strong>
        </article>
      </div>
      <div aria-label="Tasa de rechazo por proveedor" className="control-calidad-insumos-tabla-scroll" role="region" tabIndex={0}>
        <table className="control-calidad-insumos-tabla">
          <caption>Tasa de rechazo por proveedor</caption>
          <thead>
            <tr>
              <th scope="col">Proveedor</th>
              <th scope="col">Aprobadas</th>
              <th scope="col">Rechazadas</th>
              <th scope="col">Tasa de rechazo</th>
            </tr>
          </thead>
          <tbody>
            {datos.porProveedor.length === 0 && (
              <tr>
                <td colSpan={4}>Todavía no hay inspecciones dictaminadas.</td>
              </tr>
            )}
            {datos.porProveedor.map((fila) => (
              <tr key={fila.proveedorId}>
                <th scope="row">{fila.proveedorRazonSocial}</th>
                <td className="control-calidad-insumos-cifra">{fila.aprobadas}</td>
                <td className="control-calidad-insumos-cifra">{fila.rechazadas}</td>
                <td className="control-calidad-insumos-cifra">{formatearPorcentaje(fila.tasaRechazoPorcentaje)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
