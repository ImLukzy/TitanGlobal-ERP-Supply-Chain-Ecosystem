import { PanelDetalle } from '../../components/PanelDetalle';
import { fechaUtc } from './presentacion';
import type { ValeDetalle } from './tipos';
export function ValeDetallePanel({
  datos,
  ocupado,
  editar,
  eliminar,
  confirmar,
  anular,
}: {
  datos: ValeDetalle;
  ocupado: boolean;
  editar: () => void;
  eliminar: () => void;
  confirmar: () => void;
  anular: () => void;
}) {
  return (
    <PanelDetalle ariaLabelledBy="vale-detalle-title" className="control-combustible-card">
      <p className="page-eyebrow">Vale de combustible</p>
      <h2 id="vale-detalle-title">{datos.codigo}</h2>
      <p>
        Estado:{' '}
        <strong
          className={`control-combustible-badge control-combustible-${datos.estado.toLowerCase()}`}
        >
          {datos.estado}
        </strong>
      </p>
      <dl className="control-combustible-details">
        {[
          ['Placa', datos.placa],
          ['Estación', datos.estacion],
          ['Carga (UTC)', fechaUtc(datos.fechaCarga)],
          ['Combustible', datos.tipoCombustible],
          ['Litros', `${datos.litros} L`],
          ['Precio por litro', `${datos.precioLitro} ${datos.moneda}/L`],
          ['Importe', `${datos.importe} ${datos.moneda}`],
          ['Odómetro inicial', `${datos.odometroInicial} km`],
          [
            'Odómetro final',
            datos.odometroFinal === null ? 'Pendiente' : `${datos.odometroFinal} km`,
          ],
          [
            'Distancia del intervalo',
            datos.kilometrosRecorridos === null ? 'Pendiente' : `${datos.kilometrosRecorridos} km`,
          ],
          ['Rendimiento (km/L)', datos.kmPorLitro ?? 'Pendiente'],
          ['Consumo (L/100 km)', datos.litrosPor100Km ?? 'Pendiente'],
          ['Observaciones', datos.observaciones || '—'],
        ].map(([l, v]) => (
          <div key={l}>
            <dt>{l}</dt>
            <dd>{v}</dd>
          </div>
        ))}
        {datos.fechaConfirmacion && (
          <>
            <dt>Confirmado (UTC)</dt>
            <dd>{fechaUtc(datos.fechaConfirmacion)}</dd>
          </>
        )}
        {datos.fechaAnulacion && (
          <>
            <dt>Anulado (UTC)</dt>
            <dd>{fechaUtc(datos.fechaAnulacion)}</dd>
            <dt>Motivo de anulación</dt>
            <dd>{datos.motivoAnulacion}</dd>
          </>
        )}
      </dl>
      <p>
        Rendimiento orientativo del intervalo declarado; no es una medición automática del consumo.
      </p>
      <div className="control-combustible-actions">
        {datos.estado === 'Borrador' && (
          <>
            <button className="secondary-button" type="button" disabled={ocupado} onClick={editar}>
              Editar vale
            </button>
            <button className="primary-button" type="button" disabled={ocupado} onClick={confirmar}>
              Confirmar vale
            </button>
            <button className="danger-button" type="button" disabled={ocupado} onClick={eliminar}>
              Eliminar vale
            </button>
          </>
        )}
        {datos.estado !== 'Anulado' && (
          <button className="secondary-button" type="button" disabled={ocupado} onClick={anular}>
            Anular vale
          </button>
        )}
      </div>
    </PanelDetalle>
  );
}
