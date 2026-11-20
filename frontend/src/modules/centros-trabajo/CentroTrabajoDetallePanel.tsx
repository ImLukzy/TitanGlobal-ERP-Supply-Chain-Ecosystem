import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formateadorMoneda } from '../../lib/formato';
import { formatoHoras, nombreTipo } from './presentacion';
import type { CentroTrabajoDetalle } from './tipos';

interface Props {
  centro: CentroTrabajoDetalle | null;
  cargando: boolean;
  error: string;
  editar: () => void;
  eliminar: () => void;
  botonEliminarRef: RefObject<HTMLButtonElement | null>;
}

export function CentroTrabajoDetallePanel({ centro, cargando, error, editar, eliminar, botonEliminarRef }: Props) {
  return (
    <PanelDetalle ariaLabelledBy="centro-detalle-title" className="clientes-detail-panel centros-trabajo-detail-panel">
      <div className="clientes-panel-heading"><div><div className="page-eyebrow">DETALLE</div>
        <h2 id="centro-detalle-title">{centro?.nombre ?? 'Centro de trabajo'}</h2></div></div>
      {error !== '' ? <p className="clientes-error" role="alert">{error}</p>
        : cargando ? <p className="clientes-status" role="status">Cargando detalle…</p>
          : centro === null ? <p className="clientes-status">Selecciona un centro de trabajo para ver su detalle.</p>
            : <>
              <dl className="centros-trabajo-detail-grid">
                <div><dt>Código</dt><dd>{centro.codigo}</dd></div>
                <div><dt>Tipo</dt><dd>{nombreTipo(centro.tipo)}</dd></div>
                <div><dt>Planta</dt><dd>{centro.planta}</dd></div>
                <div><dt>Capacidad</dt><dd className="centros-trabajo-number">{formatoHoras.format(centro.capacidadHorasDia)} h/día</dd></div>
                <div><dt>Costo por hora</dt><dd className="centros-trabajo-number">{formateadorMoneda.format(centro.costoHora)}</dd></div>
                <div><dt>Responsable</dt><dd>{centro.responsable ?? 'Sin responsable'}</dd></div>
                <div><dt>Estado</dt><dd><span className={`proveedor-status ${centro.activo ? 'is-active' : 'is-inactive'}`}>{centro.activo ? 'Activo' : 'Inactivo'}</span></dd></div>
              </dl>
              <div className="clientes-detail-actions">
                <button className="secondary-button" onClick={editar} type="button">Editar centro de trabajo</button>
                <button className="danger-button" onClick={eliminar} ref={botonEliminarRef} type="button">Eliminar centro de trabajo</button>
              </div>
            </>}
    </PanelDetalle>
  );
}
