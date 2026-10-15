import { Link } from 'react-router';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import { nombreCondicionPago } from './presentacion';
import type { ContratoDetalle } from './tipos';

interface Props { contrato: ContratoDetalle | null; error: string; cargando: boolean; editar: () => void; accion: (accion: 'activar' | 'finalizar' | 'cancelar' | 'eliminar', boton: HTMLButtonElement) => void; }

export function ContratoDetallePanel({ contrato, error, cargando, editar, accion }: Props) {
  return (
    <PanelDetalle ariaLabelledBy="contrato-detail-title" className="solicitudes-panel contratos-detail">
      {error && <p className="clientes-error" role="alert">{error}</p>}
      {!error && cargando && <p role="status">Cargando contrato…</p>}
      {!error && !cargando && contrato === null && <p role="status">Selecciona un contrato para ver el detalle.</p>}
      {!error && !cargando && contrato !== null && <>
        <div className="solicitudes-panel-heading"><div><span className="page-eyebrow">DETALLE DE CONTRATO</span><h2 id="contrato-detail-title">{contrato.numero}</h2></div><span className={`contrato-status is-${contrato.estado.toLowerCase()}`}>{contrato.estado}</span></div>
        <h3>{contrato.titulo}</h3>
        <dl className="contrato-detail-grid">
          <div><dt>Cliente</dt><dd>{contrato.clienteNombre}</dd></div><div><dt>Periodo</dt><dd>{formatearFecha(contrato.fechaInicio)} – {formatearFecha(contrato.fechaFin)}</dd></div>
          <div><dt>Condición de pago</dt><dd>{nombreCondicionPago(contrato.condicionPago)}</dd></div><div><dt>Monto comprometido</dt><dd>{formateadorMoneda.format(contrato.montoComprometido)}</dd></div>
          <div><dt>Monto consumido</dt><dd>{formateadorMoneda.format(contrato.montoConsumido)}</dd></div><div><dt>Saldo</dt><dd className={contrato.saldo < 0 ? 'contrato-overdrawn' : ''}>{formateadorMoneda.format(contrato.saldo)}{contrato.saldo < 0 && ' · Excedido'}</dd></div>
          <div><dt>Fecha de activación</dt><dd>{contrato.fechaActivacion ? formatearFecha(contrato.fechaActivacion) : '—'}</dd></div><div><dt>Fecha de cierre</dt><dd>{contrato.fechaCierre ? formatearFecha(contrato.fechaCierre) : '—'}</dd></div>
        </dl>
        <section className="contrato-orders"><h3>Órdenes del periodo</h3>{contrato.ordenesPeriodo.length === 0 ? <p role="status">Sin órdenes en el periodo.</p> : <div className="contrato-orders-scroll"><table className="tabla-paginada"><caption>Órdenes de compra contabilizadas en el consumo</caption><thead><tr><th scope="col">Número</th><th scope="col">Fecha</th><th scope="col">Total</th></tr></thead><tbody>{contrato.ordenesPeriodo.map((orden) => <tr key={orden.ordenId}><th scope="row"><Link to="/modulos/04-ordenes-compra">{orden.numeroOrden}</Link></th><td>{formatearFecha(orden.fecha)}</td><td>{formateadorMoneda.format(orden.total)}</td></tr>)}</tbody></table></div>}</section>
        {contrato.observaciones && <section className="solicitud-detail-note"><h3>Observaciones</h3><p>{contrato.observaciones}</p></section>}
        {contrato.motivoCancelacion && <p className="solicitudes-error">Motivo de cancelación: {contrato.motivoCancelacion}</p>}
        <div className="solicitud-actions">
          {contrato.estado === 'Borrador' && <><button className="secondary-button" onClick={editar} type="button">Editar</button><button className="primary-button" onClick={(event) => accion('activar', event.currentTarget)} type="button">Activar</button><button className="danger-button" onClick={(event) => accion('cancelar', event.currentTarget)} type="button">Cancelar contrato</button><button className="secondary-button" onClick={(event) => accion('eliminar', event.currentTarget)} type="button">Eliminar</button></>}
          {contrato.estado === 'Vigente' && <><button className="primary-button" onClick={(event) => accion('finalizar', event.currentTarget)} type="button">Finalizar</button><button className="danger-button" onClick={(event) => accion('cancelar', event.currentTarget)} type="button">Cancelar contrato</button></>}
        </div>
      </>}
    </PanelDetalle>
  );
}
