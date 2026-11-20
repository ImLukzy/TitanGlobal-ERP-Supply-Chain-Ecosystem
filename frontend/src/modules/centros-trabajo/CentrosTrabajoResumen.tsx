import { formatearCantidad } from '../../lib/formato';
import { formatoHoras, nombreTipo } from './presentacion';
import type { ResumenCentrosTrabajo } from './tipos';

export function CentrosTrabajoResumen({ datos, error }: { datos: ResumenCentrosTrabajo | null; error: string }) {
  if (error !== '') { return <p className="clientes-error" role="alert">{error}</p>; }
  if (datos === null) { return <p className="clientes-status" role="status">Cargando resumen…</p>; }
  return (
    <section aria-label="Resumen de centros de trabajo" className="centros-trabajo-summary">
      <article className="cliente-stat"><span>Total de centros</span><strong>{datos.totalCentros}</strong></article>
      <article className="cliente-stat"><span>Activos</span><strong>{datos.activos}</strong></article>
      <article className="cliente-stat"><span>Inactivos</span><strong>{datos.inactivos}</strong></article>
      <article className="cliente-stat"><span>Capacidad total</span><strong>{formatoHoras.format(datos.capacidadTotalHorasDia)}</strong><small>h/día</small></article>
      <article className="cliente-stat"><span>Capacidad activa</span><strong>{formatoHoras.format(datos.capacidadActivaHorasDia)}</strong><small>h/día</small></article>
      <section className="centros-trabajo-summary-panel">
        <h2>Centros por tipo</h2>
        <table><thead><tr><th scope="col">Tipo</th><th scope="col">Centros</th><th scope="col">Capacidad</th></tr></thead>
          <tbody>{datos.porTipo.map((item) => <tr key={item.tipo}>
            <th scope="row">{nombreTipo(item.tipo)}</th><td>{formatearCantidad(item.totalCentros, 'centro', 'centros')}</td>
            <td className="centros-trabajo-number">{formatoHoras.format(item.capacidadHorasDia)} h/día</td>
          </tr>)}</tbody></table>
      </section>
      <section className="centros-trabajo-summary-panel">
        <h2>Centros por planta</h2>
        <table><thead><tr><th scope="col">Planta</th><th scope="col">Centros</th><th scope="col">Activos</th></tr></thead>
          <tbody>{datos.porPlanta.map((item) => <tr key={item.planta}>
            <th scope="row">{item.planta}</th><td>{formatearCantidad(item.totalCentros, 'centro', 'centros')}</td><td>{item.activos}</td>
          </tr>)}</tbody></table>
      </section>
    </section>
  );
}
