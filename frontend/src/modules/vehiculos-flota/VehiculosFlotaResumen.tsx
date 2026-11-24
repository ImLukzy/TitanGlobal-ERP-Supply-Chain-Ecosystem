import { formatearCantidad } from '../../lib/formato';
import { formatoCapacidad, nombreTipo } from './presentacion';
import type { ResumenVehiculosFlota } from './tipos';

export function VehiculosFlotaResumen({ datos, error }: { datos: ResumenVehiculosFlota | null; error: string }) {
  if (error !== '') { return <p className="clientes-error" role="alert">{error}</p>; }
  if (datos === null) { return <p className="clientes-status" role="status">Cargando resumen…</p>; }
  return (
    <section aria-label="Resumen de vehículos de flota" className="vehiculos-flota-summary">
      <article className="cliente-stat"><span>Total de vehículos</span><strong>{datos.totalVehiculos}</strong></article>
      <article className="cliente-stat"><span>Activos</span><strong>{datos.activos}</strong></article>
      <article className="cliente-stat"><span>Inactivos</span><strong>{datos.inactivos}</strong></article>
      <article className="cliente-stat"><span>Capacidad total</span><strong>{formatoCapacidad.format(datos.capacidadTotalKg)}</strong><small>kg</small></article>
      <section className="vehiculos-flota-summary-panel">
        <h2>Vehículos por tipo</h2>
        <table><thead><tr><th scope="col">Tipo</th><th scope="col">Vehículos</th><th scope="col">Activos</th></tr></thead>
          <tbody>{datos.porTipo.map((item) => <tr key={item.tipo}>
            <th scope="row">{nombreTipo(item.tipo)}</th><td>{formatearCantidad(item.totalVehiculos, 'vehículo', 'vehículos')}</td><td>{item.activos}</td>
          </tr>)}</tbody></table>
      </section>
    </section>
  );
}
