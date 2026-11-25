import { PanelDetalle } from '../../components/PanelDetalle';
import type { TransportistaDetalle } from './tipos';
export function TransportistaDetallePanel({
  datos,
  editar,
  eliminar,
}: {
  datos: TransportistaDetalle;
  editar: () => void;
  eliminar: () => void;
}) {
  return (
    <PanelDetalle
      ariaLabelledBy="transportista-detalle-title"
      className="transportistas-terceros-card"
    >
      <h2 id="transportista-detalle-title">{datos.razonSocial}</h2>
      <dl className="transportistas-terceros-details">
        {Object.entries({
          'Documento fiscal': datos.documentoFiscal,
          País: datos.pais,
          Servicio: datos.tipoServicio,
          Estado: datos.activo ? 'Activo' : 'Inactivo',
          Contacto: datos.contacto,
          Email: datos.email,
          Teléfono: datos.telefono,
          Observaciones: datos.observaciones,
        }).map(([campo, valor]) => (
          <div key={campo}>
            <dt>{campo}</dt>
            <dd>{valor || '—'}</dd>
          </div>
        ))}
      </dl>
      <div className="transportistas-terceros-actions">
        <button className="secondary-button" type="button" onClick={editar}>
          Editar transportista
        </button>
        <button className="danger-button" type="button" onClick={eliminar}>
          Eliminar transportista
        </button>
      </div>
    </PanelDetalle>
  );
}
