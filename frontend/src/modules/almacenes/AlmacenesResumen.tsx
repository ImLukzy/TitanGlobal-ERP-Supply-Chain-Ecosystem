import { formatearCantidad } from '../../lib/formato';
import type { ResumenAlmacenes } from './tipos';

const formatoCapacidad = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export function AlmacenesResumen({ datos, error }: { datos: ResumenAlmacenes | null; error: string }) {
  if (error !== '') {
    return <p className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p className="clientes-status" role="status">Cargando resumen…</p>;
  }
  return (
    <section aria-label="Resumen de almacenes" className="almacenes-summary">
      <article className="cliente-stat"><span>Total de almacenes</span><strong>{datos.totalAlmacenes}</strong></article>
      <article className="cliente-stat"><span>Activos</span><strong>{datos.activos}</strong></article>
      <article className="cliente-stat"><span>Inactivos</span><strong>{datos.inactivos}</strong></article>
      <article className="cliente-stat almacenes-capacity-stat">
        <span>Capacidad total</span>
        <strong className="almacenes-capacity-value">
          <span>{formatoCapacidad.format(datos.capacidadTotalM3)}</span>
          <small>m³</small>
        </strong>
      </article>
      <article className="cliente-stat almacenes-capacity-stat">
        <span>Capacidad activa</span>
        <strong className="almacenes-capacity-value">
          <span>{formatoCapacidad.format(datos.capacidadActivaM3)}</span>
          <small>m³</small>
        </strong>
      </article>
      <section className="almacenes-summary-panel">
        <h2>Almacenes por tipo</h2>
        <table><thead><tr><th>Tipo</th><th>Almacenes</th><th>Capacidad</th></tr></thead>
          <tbody>{datos.porTipo.map((item) => (
            <tr key={item.tipo}><th>{item.tipo === 'Transito' ? 'Tránsito' : item.tipo}</th>
              <td>{formatearCantidad(item.totalAlmacenes, 'almacén', 'almacenes')}</td>
              <td>{formatoCapacidad.format(item.capacidadM3)} m³</td></tr>
          ))}</tbody>
        </table>
      </section>
      <section className="almacenes-summary-panel">
        <h2>Almacenes por ciudad</h2>
        <table><thead><tr><th>Ciudad</th><th>Almacenes</th><th>Activos</th></tr></thead>
          <tbody>{datos.porCiudad.map((item) => (
            <tr key={item.ciudad}><th>{item.ciudad}</th>
              <td>{formatearCantidad(item.totalAlmacenes, 'almacén', 'almacenes')}</td><td>{item.activos}</td></tr>
          ))}</tbody>
        </table>
      </section>
    </section>
  );
}
