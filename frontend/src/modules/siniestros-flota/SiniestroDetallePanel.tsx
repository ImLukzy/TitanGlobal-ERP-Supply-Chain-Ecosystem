import { PanelDetalle } from '../../components/PanelDetalle';
import { fechaUtc } from './presentacion';
import type { SiniestroDetalle } from './tipos';
export function SiniestroDetallePanel({
  datos: d,
  ocupado,
  editar,
  eliminar,
  reclamar,
  cerrar,
  anular,
}: {
  datos: SiniestroDetalle;
  ocupado: boolean;
  editar: () => void;
  eliminar: () => void;
  reclamar: () => void;
  cerrar: () => void;
  anular: () => void;
}) {
  return (
    <PanelDetalle ariaLabelledBy="siniestro-detalle-title" className="siniestros-flota-card">
      <p>Siniestro de flota</p>
      <h2 id="siniestro-detalle-title">{d.codigo}</h2>
      <p>
        Estado:{' '}
        <span className={`siniestros-flota-badge siniestros-flota-${d.estado.toLowerCase()}`}>
          {d.estado}
        </span>
      </p>
      <dl className="siniestros-flota-details">
        {(
          [
            ['Placa', d.placa],
            ['Tipo', d.tipo],
            ['Lugar', d.lugar],
            ['Descripción', d.descripcion],
            ['Incidente (UTC)', fechaUtc(d.fechaIncidente)],
            ['Observaciones', d.observaciones || '—'],
          ] as const
        ).map(([l, v]) => (
          <div key={l}>
            <dt>{l}</dt>
            <dd className={l === 'Incidente (UTC)' ? 'siniestros-flota-number' : undefined}>{v}</dd>
          </div>
        ))}
        {d.fechaReclamo && (
          <>
            {(
              [
                ['Número de póliza', d.numeroPoliza],
                ['Aseguradora', d.aseguradora],
                ['Número de reclamo', d.numeroReclamo],
              ] as const
            ).map(([l, v]) => (
              <div key={l}>
                <dt>{l}</dt>
                <dd>{v}</dd>
              </div>
            ))}
            <div>
              <dt>Monto reclamado</dt>
              <dd className="siniestros-flota-number">
                {d.montoReclamado} {d.moneda}
              </dd>
            </div>
            <div>
              <dt>Reclamo (UTC)</dt>
              <dd className="siniestros-flota-number">{fechaUtc(d.fechaReclamo)}</dd>
            </div>
          </>
        )}
        {d.fechaCierre && (
          <>
            <div>
              <dt>Resultado</dt>
              <dd>{d.resultado}</dd>
            </div>
            <div>
              <dt>Monto reconocido</dt>
              <dd className="siniestros-flota-number">
                {d.montoReconocido} {d.moneda}
              </dd>
            </div>
            <div>
              <dt>Resolución</dt>
              <dd>{d.resolucion}</dd>
            </div>
            <div>
              <dt>Cierre (UTC)</dt>
              <dd className="siniestros-flota-number">{fechaUtc(d.fechaCierre)}</dd>
            </div>
          </>
        )}
        {d.fechaAnulacion && (
          <>
            <div>
              <dt>Motivo de anulación</dt>
              <dd>{d.motivoAnulacion}</dd>
            </div>
            <div>
              <dt>Anulación (UTC)</dt>
              <dd className="siniestros-flota-number">{fechaUtc(d.fechaAnulacion)}</dd>
            </div>
          </>
        )}
      </dl>
      <p>Seguimiento manual del reclamo. El importe reconocido no acredita un pago.</p>
      <div className="siniestros-flota-actions">
        {d.estado === 'Registrado' && (
          <>
            <button className="secondary-button" disabled={ocupado} onClick={editar}>
              Editar siniestro
            </button>
            <button className="primary-button" disabled={ocupado} onClick={reclamar}>
              Abrir reclamo
            </button>
            <button className="danger-button" disabled={ocupado} onClick={eliminar}>
              Eliminar siniestro
            </button>
          </>
        )}
        {d.estado === 'EnReclamo' && (
          <button className="primary-button" disabled={ocupado} onClick={cerrar}>
            Cerrar reclamo
          </button>
        )}
        {(d.estado === 'Registrado' || d.estado === 'EnReclamo') && (
          <button className="secondary-button" disabled={ocupado} onClick={anular}>
            Anular siniestro
          </button>
        )}
      </div>
    </PanelDetalle>
  );
}
