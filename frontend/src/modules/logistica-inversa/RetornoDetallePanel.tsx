import { PanelDetalle } from '../../components/PanelDetalle';
import { fechaUtc as fecha } from './presentacion';
import type { RetornoDetalle } from './tipos';
export function RetornoDetallePanel({
  datos: d,
  ocupado,
  editar,
  eliminar,
  recoger,
  recibir,
  cancelar,
}: {
  datos: RetornoDetalle;
  ocupado: boolean;
  editar: () => void;
  eliminar: () => void;
  recoger: () => void;
  recibir: () => void;
  cancelar: () => void;
}) {
  const cantidades = [
    ['Programada', d.cantidadProgramada],
    ['Recogida', d.cantidadRecogida],
    ['No recogida', d.noRecogida],
    ['Recibida', d.cantidadRecibida],
    ['No recibida', d.noRecibida],
  ] as const;
  return (
    <PanelDetalle ariaLabelledBy="retorno-detalle-title" className="logistica-inversa-card">
      <p className="logistica-inversa-eyebrow">Retorno logístico</p>
      <h2 id="retorno-detalle-title">{d.codigo}</h2>
      <p>
        Estado:{' '}
        <span className={`logistica-inversa-badge logistica-inversa-${d.estado.toLowerCase()}`}>
          {d.estado}
        </span>
      </p>
      <dl className="logistica-inversa-details">
        {(
          [
            ['Tipo', d.tipo],
            ['Referencia de origen', d.referenciaOrigen],
            ['Contraparte', d.contraparte],
            ['Punto de recogida', d.puntoRecogida],
            ['Punto de recepción', d.puntoRecepcion],
            ['Material', d.descripcion],
            ['Programación (UTC)', fecha(d.fechaProgramada)],
            ['Observaciones', d.observaciones || '—'],
          ] as const
        ).map(([l, v]) => (
          <div key={l}>
            <dt>{l}</dt>
            <dd className={l === 'Programación (UTC)' ? 'logistica-inversa-number' : undefined}>
              {v}
            </dd>
          </div>
        ))}
        {cantidades.map(([l, v]) => (
          <div key={l}>
            <dt>{l}</dt>
            <dd className="logistica-inversa-number">{v ? `${v} ${d.unidad}` : 'Pendiente'}</dd>
          </div>
        ))}
        {d.fechaRecogida && (
          <>
            <div>
              <dt>Recogida (UTC)</dt>
              <dd className="logistica-inversa-number">{fecha(d.fechaRecogida)}</dd>
            </div>
            <div>
              <dt>Responsable de recogida</dt>
              <dd>{d.responsableRecogida}</dd>
            </div>
          </>
        )}
        {d.fechaRecepcion && (
          <>
            <div>
              <dt>Recepción (UTC)</dt>
              <dd className="logistica-inversa-number">{fecha(d.fechaRecepcion)}</dd>
            </div>
            <div>
              <dt>Responsable de recepción</dt>
              <dd>{d.responsableRecepcion}</dd>
            </div>
          </>
        )}
        {d.fechaCancelacion && (
          <>
            <div>
              <dt>Cancelación (UTC)</dt>
              <dd className="logistica-inversa-number">{fecha(d.fechaCancelacion)}</dd>
            </div>
            <div>
              <dt>Motivo</dt>
              <dd>{d.motivoCancelacion}</dd>
            </div>
          </>
        )}
      </dl>
      <p>Cantidades del material y unidad declarados. Sin movimientos automáticos de inventario.</p>
      <div className="logistica-inversa-actions">
        {d.estado === 'Programado' && (
          <>
            <button className="secondary-button" disabled={ocupado} onClick={editar}>
              Editar retorno
            </button>
            <button className="primary-button" disabled={ocupado} onClick={recoger}>
              Registrar recogida
            </button>
            <button className="danger-button" disabled={ocupado} onClick={eliminar}>
              Eliminar retorno
            </button>
          </>
        )}
        {d.estado === 'Recogido' && (
          <button className="primary-button" disabled={ocupado} onClick={recibir}>
            Registrar recepción
          </button>
        )}
        {(d.estado === 'Programado' || d.estado === 'Recogido') && (
          <button className="secondary-button" disabled={ocupado} onClick={cancelar}>
            Cancelar retorno
          </button>
        )}
      </div>
    </PanelDetalle>
  );
}
