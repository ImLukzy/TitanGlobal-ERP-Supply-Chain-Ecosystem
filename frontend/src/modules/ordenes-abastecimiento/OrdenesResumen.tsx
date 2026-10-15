import { formateadorMoneda } from '../../lib/formato';
import type { ResumenOrdenesAbastecimiento } from './tipos';

interface OrdenesResumenProps {
  datos: ResumenOrdenesAbastecimiento | null;
  error: string;
}

export function OrdenesResumen({ datos, error }: OrdenesResumenProps) {
  if (error !== '') {
    return <p className="ordenes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p className="ordenes-loading" role="status">Cargando resumen…</p>;
  }
  const tarjetas = [
    { etiqueta: 'Emitidas', valor: datos.emitidas },
    { etiqueta: 'Atrasadas', valor: datos.atrasadas },
    { etiqueta: 'Recibidas', valor: datos.recibidas },
    { etiqueta: 'Anuladas', valor: datos.anuladas },
    { etiqueta: 'Monto por recibir', valor: formateadorMoneda.format(datos.montoPorRecibir) },
    { etiqueta: 'Monto recibido', valor: formateadorMoneda.format(datos.montoRecibido) },
  ];
  return (
    <section aria-label="Resumen de órdenes de abastecimiento" className="ordenes-summary">
      {tarjetas.map((tarjeta) => (
        <article key={tarjeta.etiqueta}>
          <span>{tarjeta.etiqueta}</span>
          <strong>{tarjeta.valor}</strong>
        </article>
      ))}
      <section aria-labelledby="ordenes-top-title" className="ordenes-top-panel">
        <h2 id="ordenes-top-title">Proveedores principales por monto</h2>
        {datos.topProveedores.length === 0 ? (
          <p>No hay órdenes para resumir.</p>
        ) : (
          <ol>
            {datos.topProveedores.map((proveedor) => (
              <li key={proveedor.proveedorId}>
                <span>{proveedor.razonSocial}</span>
                <strong>{proveedor.ordenes} órdenes · {formateadorMoneda.format(proveedor.monto)}</strong>
              </li>
            ))}
          </ol>
        )}
      </section>
    </section>
  );
}
