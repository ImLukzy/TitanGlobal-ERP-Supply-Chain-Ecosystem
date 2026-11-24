import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatoCapacidad, nombreTipo } from './presentacion';
import type { VehiculoFlotaDetalle } from './tipos';

interface Props {
  vehiculo: VehiculoFlotaDetalle | null;
  cargando: boolean;
  error: string;
  editar: () => void;
  eliminar: () => void;
  botonEliminarRef: RefObject<HTMLButtonElement | null>;
  botonEditarRef: RefObject<HTMLButtonElement | null>;
}

export function VehiculoFlotaDetallePanel({ vehiculo, cargando, error, editar, eliminar, botonEliminarRef, botonEditarRef }: Props) {
  return (
    <PanelDetalle ariaLabelledBy="vehiculo-detalle-title" className="clientes-detail-panel vehiculos-flota-detail-panel">
      <div className="clientes-panel-heading"><div><div className="page-eyebrow">DETALLE</div>
        <h2 id="vehiculo-detalle-title">{vehiculo?.placa ?? 'Vehículo de flota'}</h2></div></div>
      {error !== '' ? <p className="clientes-error" role="alert">{error}</p>
        : cargando ? <p className="clientes-status" role="status">Cargando detalle…</p>
          : vehiculo === null ? <p className="clientes-status">Selecciona un vehículo para ver su detalle.</p>
            : <>
              <dl className="vehiculos-flota-detail-grid">
                <div><dt>Placa</dt><dd>{vehiculo.placa}</dd></div>
                <div><dt>Marca</dt><dd>{vehiculo.marca}</dd></div>
                <div><dt>Modelo</dt><dd>{vehiculo.modelo}</dd></div>
                <div><dt>Año</dt><dd>{vehiculo.anio}</dd></div>
                <div><dt>Tipo</dt><dd>{nombreTipo(vehiculo.tipo)}</dd></div>
                <div><dt>Capacidad</dt><dd className="vehiculos-flota-number">{formatoCapacidad.format(vehiculo.capacidadKg)} kg</dd></div>
                <div><dt>Observaciones</dt><dd>{vehiculo.observaciones ?? 'Sin observaciones'}</dd></div>
                <div><dt>Estado</dt><dd>{vehiculo.activo ? 'Activo' : 'Inactivo'}</dd></div>
              </dl>
              <div className="clientes-detail-actions">
                <button className="secondary-button" onClick={editar} ref={botonEditarRef} type="button">Editar vehículo</button>
                <button className="danger-button" onClick={eliminar} ref={botonEliminarRef} type="button">Eliminar vehículo</button>
              </div>
            </>}
    </PanelDetalle>
  );
}
