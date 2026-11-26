import { PanelDetalle } from '../../components/PanelDetalle';
import { fechaUtc } from './presentacion';
import type { FleteDetalle } from './tipos';
export function FleteDetallePanel({
  datos,
  cancelar,
}: {
  datos: FleteDetalle;
  cancelar: () => void;
}) {
  return (
    <PanelDetalle ariaLabelledBy="flete-detalle-title" className="transportistas-terceros-card">
      <h2 id="flete-detalle-title">Flete {datos.referencia}</h2>
      <dl className="transportistas-terceros-details">
        {Object.entries({
          Transportista: datos.transportistaRazonSocial,
          'Fecha UTC': fechaUtc(datos.fecha),
          Origen: datos.origen,
          Destino: datos.destino,
          Importe: `${datos.importe} ${datos.moneda}`,
          Estado: datos.estado,
          Observaciones: datos.observaciones,
          ...(datos.estado === 'Cancelado'
            ? {
                'Motivo de cancelación': datos.motivoCancelacion,
                'Cancelado el (UTC)': fechaUtc(datos.fechaCancelacion),
              }
            : {}),
        }).map(([campo, valor]) => (
          <div key={campo}>
            <dt>{campo}</dt>
            <dd>{valor || '—'}</dd>
          </div>
        ))}
      </dl>
      {datos.estado === 'Registrado' && (
        <button className="danger-button" type="button" onClick={cancelar}>
          Cancelar flete
        </button>
      )}
    </PanelDetalle>
  );
}
