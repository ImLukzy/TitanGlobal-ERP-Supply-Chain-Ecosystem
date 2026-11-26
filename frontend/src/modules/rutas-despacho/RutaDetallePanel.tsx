import { PanelDetalle } from '../../components/PanelDetalle';
import type { RutaDetalle } from './tipos';
export function RutaDetallePanel({
  datos,
  editar,
  eliminar,
}: {
  datos: RutaDetalle;
  editar: () => void;
  eliminar: () => void;
}) {
  return (
    <PanelDetalle ariaLabelledBy="ruta-detalle-title" className="rutas-despacho-card">
      <p className="page-eyebrow">{datos.codigo}</p>
      <h2 id="ruta-detalle-title">{datos.nombre}</h2>
      <dl className="rutas-despacho-details">
        {Object.entries({
          Zona: datos.zona,
          Origen: datos.origen,
          Destino: datos.destino,
          Distancia: `${datos.distanciaKm} km`,
          'Peaje estimado': `${datos.peajeEstimado} ${datos.moneda}`,
          Estado: datos.activo ? 'Activa' : 'Inactiva',
          Observaciones: datos.observaciones,
        }).map(([campo, valor]) => (
          <div key={campo}>
            <dt>{campo}</dt>
            <dd>{valor || '—'}</dd>
          </div>
        ))}
      </dl>
      <p className="rutas-despacho-muted">Peaje estimado por recorrido en su moneda original.</p>
      <div className="rutas-despacho-actions">
        <button className="secondary-button" type="button" onClick={editar}>
          Editar ruta
        </button>
        <button className="danger-button" type="button" onClick={eliminar}>
          Eliminar ruta
        </button>
      </div>
    </PanelDetalle>
  );
}
