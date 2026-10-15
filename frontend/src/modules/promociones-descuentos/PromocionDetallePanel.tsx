import { PanelDetalle } from '../../components/PanelDetalle';
import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import type { PromocionDetalle } from './tipos';

interface Props { promocion: PromocionDetalle | null; error: string; cargando: boolean; editar: () => void; eliminar: () => void; }

export function PromocionDetallePanel({ promocion, error, cargando, editar, eliminar }: Props) {
  return <PanelDetalle ariaLabelledBy="promocion-detail-title" className="solicitudes-panel promociones-detail">
    {error && <p className="clientes-error" role="alert">{error}</p>}
    {!error && cargando && <p role="status">Cargando promoción…</p>}
    {!error && !cargando && promocion === null && <p id="promocion-detail-title" role="status">Selecciona una promoción para ver el detalle.</p>}
    {!error && !cargando && promocion !== null && <>
      <div className="solicitudes-panel-heading"><div><span className="page-eyebrow">DETALLE DE PROMOCIÓN</span><h2 id="promocion-detail-title">{promocion.codigo}</h2></div><span className={`promocion-estado ${promocion.activa && promocion.vigente ? 'is-vigente' : promocion.activa ? 'is-novigente' : 'is-inactiva'}`}>{!promocion.activa ? 'Inactiva' : promocion.vigente ? 'Vigente' : 'No vigente'}</span></div>
      <h3>{promocion.nombre}</h3>
      <dl className="promocion-detail-grid">
        <div><dt>Tipo</dt><dd>{promocion.tipo === 'Producto' ? `Producto: ${promocion.productoNombre ?? '—'}` : `Compra desde ${formateadorMoneda.format(promocion.montoMinimo ?? 0)}`}</dd></div>
        <div><dt>Descuento</dt><dd>{promocion.porcentajeDescuento.toFixed(2)} %</dd></div>
        <div><dt>Segmento</dt><dd>{promocion.segmentoCodigo ?? 'Todos los clientes'}</dd></div>
        <div><dt>Vigencia</dt><dd>{formatearFecha(promocion.fechaInicio)} – {formatearFecha(promocion.fechaFin)}</dd></div>
        <div><dt>Estado</dt><dd>{promocion.activa ? 'Activa' : 'Inactiva'}</dd></div>
      </dl>
      <div className="solicitud-actions"><button className="secondary-button" onClick={editar} type="button">Editar</button><button className="danger-button" onClick={eliminar} type="button">Eliminar</button></div>
    </>}
  </PanelDetalle>;
}
