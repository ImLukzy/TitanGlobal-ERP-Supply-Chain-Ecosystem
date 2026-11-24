import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearCantidad } from '../../lib/formato';
import type { RolDetalle } from './tipos';

interface RolDetallePanelProps {
  detalle: RolDetalle | null;
  cargando: boolean;
  error: string;
  editar: () => void;
  editarPermisos: () => void;
  eliminar: () => void;
  botonEliminarRef: RefObject<HTMLButtonElement | null>;
  botonPermisosRef: RefObject<HTMLButtonElement | null>;
}

export function RolDetallePanel({
  detalle,
  cargando,
  error,
  editar,
  editarPermisos,
  eliminar,
  botonEliminarRef,
  botonPermisosRef,
}: RolDetallePanelProps) {
  return (
    <PanelDetalle ariaLabelledBy="rol-detail-title" className="clientes-detail-panel roles-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {cargando && error === '' && (
        <p aria-live="polite" className="clientes-status" role="status">Cargando detalle del rol…</p>
      )}
      {detalle === null && !cargando && error === '' && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona un rol para ver su detalle.</p>
      )}
      {detalle !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">FICHA DEL ROL</div>
              <h2 id="rol-detail-title">{detalle.nombre}</h2>
            </div>
            <span aria-hidden="true" className="cliente-initials">
              {detalle.codigo.slice(0, 1)}
            </span>
          </div>
          <dl className="roles-detail-grid">
            <div>
              <dt>Código</dt>
              <dd>{detalle.codigo}</dd>
            </div>
            <div>
              <dt>Tipo</dt>
              <dd>{detalle.esSistema ? 'De sistema' : 'Personalizado'}</dd>
            </div>
            <div className="roles-detail-ancho">
              <dt>Descripción</dt>
              <dd>{detalle.descripcion || 'Sin descripción'}</dd>
            </div>
            <div>
              <dt>Módulos con lectura</dt>
              <dd>{formatearCantidad(detalle.modulosConLectura, 'módulo', 'módulos')}</dd>
            </div>
            <div>
              <dt>Módulos con escritura</dt>
              <dd>{formatearCantidad(detalle.modulosConEscritura, 'módulo', 'módulos')}</dd>
            </div>
          </dl>
          <div className="clientes-detail-actions">
            <button className="primary-button" onClick={editarPermisos} ref={botonPermisosRef} type="button">Editar permisos</button>
            <button className="secondary-button" onClick={editar} type="button">Editar</button>
            <button className="danger-button" onClick={eliminar} ref={botonEliminarRef} type="button">Eliminar</button>
          </div>
        </>
      )}
    </PanelDetalle>
  );
}
