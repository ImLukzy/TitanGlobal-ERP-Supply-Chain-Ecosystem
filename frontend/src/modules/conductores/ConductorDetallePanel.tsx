import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha } from '../../lib/formato';
import { licenciaVencida } from './presentacion';
import type { ConductorDetalle } from './tipos';

interface Props {
  conductor: ConductorDetalle | null;
  cargando: boolean;
  error: string;
  editar: () => void;
  eliminar: () => void;
  botonEliminarRef: RefObject<HTMLButtonElement | null>;
  botonEditarRef: RefObject<HTMLButtonElement | null>;
}

export function ConductorDetallePanel({ conductor, cargando, error, editar, eliminar, botonEliminarRef, botonEditarRef }: Props) {
  return (
    <PanelDetalle ariaLabelledBy="conductor-detalle-title" className="clientes-detail-panel conductores-detail-panel">
      <div className="clientes-panel-heading"><div><div className="page-eyebrow">DETALLE</div>
        <h2 id="conductor-detalle-title">{conductor?.nombre ?? 'Conductor'}</h2></div></div>
      {error !== '' ? <p className="clientes-error" role="alert">{error}</p>
        : cargando ? <p className="clientes-status" role="status">Cargando detalle…</p>
          : conductor === null ? <p className="clientes-status">Selecciona un conductor para ver su detalle.</p>
            : <>
              <dl className="conductores-detail-grid">
                <div><dt>Nombre</dt><dd>{conductor.nombre}</dd></div>
                <div><dt>Documento de licencia</dt><dd>{conductor.documentoLicencia}</dd></div>
                <div><dt>Vencimiento</dt><dd>{formatearFecha(conductor.vencimientoLicencia)}</dd></div>
                <div><dt>Vigencia de licencia</dt><dd>{licenciaVencida(conductor.vencimientoLicencia) ? 'Vencida' : 'Vigente'}</dd></div>
                <div><dt>Habilitación administrativa</dt><dd>{conductor.habilitado ? 'Habilitado' : 'No habilitado'}</dd></div>
                <div><dt>Estado</dt><dd>{conductor.activo ? 'Activo' : 'Inactivo'}</dd></div>
                <div><dt>Observaciones</dt><dd>{conductor.observaciones ?? 'Sin observaciones'}</dd></div>
              </dl>
              {licenciaVencida(conductor.vencimientoLicencia) && <p className="conductores-warning" role="note">
                Licencia vencida. La habilitación administrativa no acredita una licencia vigente.
              </p>}
              <div className="clientes-detail-actions">
                <button className="secondary-button" onClick={editar} ref={botonEditarRef} type="button">Editar conductor</button>
                <button className="danger-button" onClick={eliminar} ref={botonEliminarRef} type="button">Eliminar conductor</button>
              </div>
            </>}
    </PanelDetalle>
  );
}
