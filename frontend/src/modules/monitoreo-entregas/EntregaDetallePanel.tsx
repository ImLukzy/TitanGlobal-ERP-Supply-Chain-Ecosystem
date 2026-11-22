import { PanelDetalle } from '../../components/PanelDetalle';
import { fechaUtc, etiquetaEstado } from './presentacion';
import type { EntregaDetalle } from './tipos';
export function EntregaDetallePanel({
  datos,
  ocupado,
  editar,
  eliminar,
  iniciar,
  pod,
  cancelar,
}: {
  datos: EntregaDetalle;
  ocupado: boolean;
  editar: () => void;
  eliminar: () => void;
  iniciar: () => void;
  pod: () => void;
  cancelar: () => void;
}) {
  return (
    <PanelDetalle ariaLabelledBy="entrega-detalle-title" className="monitoreo-entregas-card">
      <p className="page-eyebrow">Seguimiento manual</p>
      <h2 id="entrega-detalle-title">{datos.codigo}</h2>
      <p>
        Estado:{' '}
        <strong
          className={`monitoreo-entregas-badge monitoreo-entregas-${datos.estado.toLowerCase()}`}
        >
          {etiquetaEstado(datos.estado)}
        </strong>
      </p>
      <dl className="monitoreo-entregas-details">
        {[
          ['Referencia de guía', datos.referenciaGuia],
          ['Destinatario', datos.destinatario],
          ['Dirección de destino', datos.direccionDestino],
          ['Programada (UTC)', fechaUtc(datos.fechaProgramada)],
          ['Observaciones', datos.observaciones || '—'],
        ].map(([label, valor]) => (
          <div key={label}>
            <dt>{label}</dt>
            <dd>{valor}</dd>
          </div>
        ))}
        {datos.fechaSalida && (
          <>
            <dt>Salida (UTC)</dt>
            <dd>{fechaUtc(datos.fechaSalida)}</dd>
          </>
        )}
        {datos.fechaEntrega && (
          <>
            <dt>Recepción (UTC)</dt>
            <dd>{fechaUtc(datos.fechaEntrega)}</dd>
            <dt>Receptor</dt>
            <dd>{datos.receptor}</dd>
            <dt>Documento del receptor</dt>
            <dd>{datos.documentoReceptor}</dd>
            <dt>Referencia de constancia</dt>
            <dd>{datos.referenciaConstancia}</dd>
            <dt>Observaciones de entrega</dt>
            <dd>{datos.observacionesEntrega || '—'}</dd>
          </>
        )}
        {datos.fechaCancelacion && (
          <>
            <dt>Cancelada (UTC)</dt>
            <dd>{fechaUtc(datos.fechaCancelacion)}</dd>
            <dt>Motivo de cancelación</dt>
            <dd>{datos.motivoCancelacion}</dd>
          </>
        )}
      </dl>
      <h3>Historial de estados</h3>
      <ol className="monitoreo-entregas-history">
        {datos.historial.map((e) => (
          <li key={e.id}>
            <strong>{etiquetaEstado(e.estado)}</strong>
            <time dateTime={e.fecha}>{fechaUtc(e.fecha)} UTC</time>
            <p>{e.descripcion}</p>
          </li>
        ))}
      </ol>
      <div className="monitoreo-entregas-actions">
        {datos.estado === 'Programada' && (
          <>
            <button type="button" className="secondary-button" disabled={ocupado} onClick={editar}>
              Editar entrega
            </button>
            <button type="button" className="primary-button" disabled={ocupado} onClick={iniciar}>
              Registrar salida
            </button>
            <button type="button" className="danger-button" disabled={ocupado} onClick={eliminar}>
              Eliminar entrega
            </button>
          </>
        )}
        {datos.estado === 'EnRuta' && (
          <button type="button" className="primary-button" disabled={ocupado} onClick={pod}>
            Registrar recepción
          </button>
        )}
        {(datos.estado === 'Programada' || datos.estado === 'EnRuta') && (
          <button type="button" className="secondary-button" disabled={ocupado} onClick={cancelar}>
            Cancelar entrega
          </button>
        )}
      </div>
    </PanelDetalle>
  );
}
