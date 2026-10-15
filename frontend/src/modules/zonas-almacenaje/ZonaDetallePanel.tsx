import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import type { ZonaDetalle } from './tipos';

const formatoM3 = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

interface Props {
  zona: ZonaDetalle | null;
  error: string;
  editar: () => void;
  eliminar: () => void;
  botonEliminarRef: RefObject<HTMLButtonElement | null>;
}

export function ZonaDetallePanel({ zona, error, editar, eliminar, botonEliminarRef }: Props) {
  if (error !== '') {
    return <p className="clientes-error" role="alert">{error}</p>;
  }
  if (zona === null) {
    return <p className="clientes-status" role="status">Selecciona una zona para ver su detalle.</p>;
  }
  return (
    <PanelDetalle ariaLabelledBy="zona-detail-title" className="clientes-detail-panel zonas-detail-panel">
      <div className="clientes-panel-heading">
        <div><div className="page-eyebrow">DETALLE</div><h2 id="zona-detail-title">{zona.nombre}</h2></div>
        <span className={`proveedor-status${zona.activa ? ' is-active' : ' is-inactive'}`}>{zona.activa ? 'Activa' : 'Inactiva'}</span>
      </div>
      <dl className="zonas-detail-grid">
        <div><dt>Almacén</dt><dd>{zona.almacenCodigo} · {zona.almacenNombre}</dd></div>
        <div><dt>Código de zona</dt><dd>{zona.codigo}</dd></div>
        <div><dt>Tipo</dt><dd>{zona.tipo === 'Recepcion' ? 'Recepción' : zona.tipo}</dd></div>
        <div><dt>Temperatura</dt><dd>{zona.temperatura}</dd></div>
        <div><dt>Capacidad</dt><dd>{formatoM3.format(zona.capacidadM3)} m³</dd></div>
      </dl>
      <div className="clientes-detail-actions">
        <button className="secondary-button" onClick={editar} type="button">Editar zona</button>
        <button className="danger-button" onClick={eliminar} ref={botonEliminarRef} type="button">Eliminar zona</button>
      </div>
    </PanelDetalle>
  );
}
