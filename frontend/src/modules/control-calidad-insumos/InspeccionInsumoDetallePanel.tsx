import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearCantidad, formatearDia, formatearInstantePeru, formatearPorcentaje } from './presentacion';
import type { InspeccionInsumoDetalle } from './tipos';

interface DetalleProps {
  seleccion: number | null;
  inspeccion?: InspeccionInsumoDetalle;
  error?: string;
  reintentar: () => void;
  editar: (inspeccion: InspeccionInsumoDetalle) => void;
  dictaminar: (inspeccion: InspeccionInsumoDetalle) => void;
  anular: (inspeccion: InspeccionInsumoDetalle) => void;
}

export function InspeccionInsumoDetallePanel({ seleccion, inspeccion, error, reintentar, editar, dictaminar, anular }: DetalleProps) {
  return (
    <PanelDetalle ariaLabelledBy="control-calidad-insumos-detalle" className="control-calidad-insumos-tarjeta">
      <h2 id="control-calidad-insumos-detalle" tabIndex={-1}>
        {inspeccion ? `Inspección ${inspeccion.numero}` : 'Detalle de la inspección'}
      </h2>
      {seleccion === null && <p>Selecciona una inspección para ver su detalle.</p>}
      {seleccion !== null && inspeccion === undefined && error === undefined && <p role="status">Cargando detalle…</p>}
      {error !== undefined && (
        <div className="control-calidad-insumos-error">
          <p className="field-error" role="alert">{error}</p>
          <button className="secondary-button" onClick={reintentar} type="button">Reintentar detalle</button>
        </div>
      )}
      {inspeccion && (
        <>
          <dl className="control-calidad-insumos-datos">
            <dt>Estado</dt>
            <dd><span className={`control-calidad-insumos-estado is-${inspeccion.estado.toLowerCase()}`}>{inspeccion.estado}</span></dd>
            <dt>Insumo</dt>
            <dd>{inspeccion.productoNombre}</dd>
            <dt>Proveedor</dt>
            <dd>{inspeccion.proveedorRazonSocial}</dd>
            <dt>Lote</dt>
            <dd className="control-calidad-insumos-cifra">{inspeccion.lote}</dd>
            <dt>Fecha de inspección</dt>
            <dd className="control-calidad-insumos-cifra">{formatearDia(inspeccion.fechaInspeccion)}</dd>
            <dt>Cantidad recibida</dt>
            <dd className="control-calidad-insumos-cifra">{formatearCantidad(inspeccion.cantidadRecibida)} {inspeccion.unidadMedida}</dd>
            <dt>Muestra</dt>
            <dd className="control-calidad-insumos-cifra">{formatearCantidad(inspeccion.cantidadMuestra)} {inspeccion.unidadMedida}</dd>
            <dt>Tolerancia de defectos</dt>
            <dd className="control-calidad-insumos-cifra">{formatearPorcentaje(inspeccion.toleranciaDefectosPorcentaje)}</dd>
            <dt>Inspector</dt>
            <dd>{inspeccion.inspector}</dd>
            {inspeccion.cantidadDefectuosa !== null && (
              <>
                <dt>Cantidad defectuosa</dt>
                <dd className="control-calidad-insumos-cifra">{formatearCantidad(inspeccion.cantidadDefectuosa)} {inspeccion.unidadMedida}</dd>
                <dt>Defectos frente a la tolerancia</dt>
                <dd>
                  <span className="control-calidad-insumos-cifra">
                    {formatearPorcentaje(inspeccion.porcentajeDefectos)} / {formatearPorcentaje(inspeccion.toleranciaDefectosPorcentaje)}
                  </span>{' '}
                  <span className={`control-calidad-insumos-tolerancia${inspeccion.superaTolerancia ? ' is-supera' : ''}`}>
                    {inspeccion.superaTolerancia ? 'Supera la tolerancia' : 'Dentro de la tolerancia'}
                  </span>
                </dd>
              </>
            )}
            {inspeccion.fechaDictamen !== null && (
              <>
                <dt>Dictaminada</dt>
                <dd className="control-calidad-insumos-cifra">{formatearInstantePeru(inspeccion.fechaDictamen)}</dd>
              </>
            )}
            {inspeccion.observaciones !== null && (
              <>
                <dt>Observaciones</dt>
                <dd>{inspeccion.observaciones}</dd>
              </>
            )}
            {inspeccion.motivoAnulacion !== null && (
              <>
                <dt>Motivo de anulación</dt>
                <dd>{inspeccion.motivoAnulacion}</dd>
              </>
            )}
          </dl>
          {inspeccion.estado === 'Pendiente' && (
            <div className="control-calidad-insumos-acciones">
              <button className="secondary-button" id="control-calidad-insumos-editar" onClick={() => editar(inspeccion)} type="button">
                Editar
              </button>
              <button className="primary-button" id="control-calidad-insumos-dictaminar" onClick={() => dictaminar(inspeccion)} type="button">
                Dictaminar
              </button>
              <button className="danger-button" id="control-calidad-insumos-anular" onClick={() => anular(inspeccion)} type="button">
                Anular
              </button>
            </div>
          )}
        </>
      )}
    </PanelDetalle>
  );
}
