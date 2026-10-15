import { PanelDetalle } from '../../components/PanelDetalle';
import type { AlmacenDetalle } from './tipos';
import type { RefObject } from 'react';

const formatoCapacidad = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

interface Props {
  almacen: AlmacenDetalle | null;
  error: string;
  editar: () => void;
  eliminar: () => void;
  botonEliminarRef: RefObject<HTMLButtonElement | null>;
}

export function AlmacenDetallePanel({ almacen, error, editar, eliminar, botonEliminarRef }: Props) {
  if (error !== '') {
    return <p className="clientes-error" role="alert">{error}</p>;
  }
  if (almacen === null) {
    return <p className="clientes-status" role="status">Selecciona un almacén para ver su detalle.</p>;
  }
  return (
    <PanelDetalle ariaLabelledBy="almacen-detail-title" className="clientes-detail-panel almacenes-detail-panel">
      <div className="clientes-panel-heading"><div><div className="page-eyebrow">DETALLE</div>
        <h2 id="almacen-detail-title">{almacen.nombre}</h2></div>
        <span className={`proveedor-status${almacen.activo ? ' is-active' : ' is-inactive'}`}>
          {almacen.activo ? 'Activo' : 'Inactivo'}</span></div>
      <dl className="almacenes-detail-grid">
        <div><dt>Código</dt><dd>{almacen.codigo}</dd></div>
        <div><dt>Tipo</dt><dd>{almacen.tipo === 'Transito' ? 'Tránsito' : almacen.tipo}</dd></div>
        <div><dt>Ciudad</dt><dd>{almacen.ciudad}</dd></div>
        <div><dt>Capacidad</dt><dd>{formatoCapacidad.format(almacen.capacidadM3)} m³</dd></div>
        <div><dt>Dirección</dt><dd>{almacen.direccion}</dd></div>
        <div><dt>Responsable</dt><dd>{almacen.responsable ?? 'Sin responsable'}</dd></div>
      </dl>
      <div className="clientes-detail-actions">
        <button className="secondary-button" onClick={editar} type="button">Editar almacén</button>
        <button className="danger-button" onClick={eliminar} ref={botonEliminarRef} type="button">Eliminar almacén</button>
      </div>
    </PanelDetalle>
  );
}
