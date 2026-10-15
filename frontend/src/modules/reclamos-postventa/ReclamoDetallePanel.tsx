import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha } from '../../lib/formato';
import type { ReclamoDetalle } from './tipos';

export function ReclamoDetallePanel({ reclamo, abrirFormulario, accion, cargando }: {
  reclamo: ReclamoDetalle | null;
  abrirFormulario: () => void;
  accion: (tipo: 'iniciar' | 'resolver' | 'cerrar' | 'rechazar', boton: HTMLButtonElement) => void;
  cargando: boolean;
}) {
  return (
    <PanelDetalle ariaLabelledBy="reclamo-detail-title" className="reclamos-panel reclamos-detail">
      {cargando && <p role="status">Cargando detalle…</p>}
      {!cargando && reclamo === null && <p role="status">Selecciona un reclamo para ver el detalle.</p>}
      {!cargando && reclamo !== null && <>
        <div className="reclamos-panel-heading"><div><span className="page-eyebrow">DETALLE DE RECLAMO</span><h2 id="reclamo-detail-title">{reclamo.numero}</h2></div><span className={`reclamo-status is-${reclamo.estado.toLowerCase()}`}>{reclamo.estado === 'EnProceso' ? 'En proceso' : reclamo.estado}</span></div>
        <dl className="reclamo-detail-grid">
          <div><dt>Cliente</dt><dd>{reclamo.clienteNombre}</dd></div>
          <div><dt>Orden de compra</dt><dd><a className="reclamo-order-link" href={`/modulos/04-ordenes-compra?orden=${reclamo.ordenCompraId}`}>{reclamo.numeroOrden}</a></dd></div>
          <div><dt>Producto</dt><dd>{reclamo.productoNombre ?? 'No especificado'}</dd></div>
          <div><dt>Tipo</dt><dd>{reclamo.tipo === 'Facturacion' ? 'Facturación' : reclamo.tipo}</dd></div>
          <div><dt>Prioridad</dt><dd>{reclamo.prioridad} · Plazo {reclamo.prioridad === 'Alta' ? 2 : reclamo.prioridad === 'Media' ? 5 : 10} días</dd></div>
          <div><dt>Fecha límite</dt><dd>{formatearFecha(reclamo.fechaLimite)}{reclamo.vencido ? ' · Vencido' : ''}</dd></div>
        </dl>
        <section className="reclamo-detail-note"><h3>Descripción</h3><p>{reclamo.descripcion}</p></section>
        {reclamo.resolucion && <section className="reclamo-detail-note"><h3>Resolución</h3><p>{reclamo.resolucion}</p></section>}
        {reclamo.motivoRechazo && <section className="reclamo-detail-note"><h3>Motivo de rechazo</h3><p>{reclamo.motivoRechazo}</p></section>}
        {reclamo.atendidoATiempo !== null && <p className={reclamo.atendidoATiempo ? 'reclamo-on-time' : 'reclamo-late'}>{reclamo.atendidoATiempo ? 'Atendido a tiempo' : 'Atendido fuera de plazo'}</p>}
        <section aria-label="Seguimiento del reclamo" className="reclamo-timeline"><h3>Seguimiento</h3><ol>
          <li><strong>Alta</strong><time dateTime={reclamo.fecha}>{formatearFecha(reclamo.fecha)}</time></li>
          {reclamo.fechaInicio && <li><strong>En proceso</strong><time dateTime={reclamo.fechaInicio}>{formatearFecha(reclamo.fechaInicio)}</time></li>}
          {reclamo.fechaResolucion && <li><strong>Resuelto</strong><time dateTime={reclamo.fechaResolucion}>{formatearFecha(reclamo.fechaResolucion)}</time></li>}
          {reclamo.fechaCierre && <li><strong>{reclamo.estado === 'Rechazado' ? 'Rechazado' : 'Cerrado'}</strong><time dateTime={reclamo.fechaCierre}>{formatearFecha(reclamo.fechaCierre)}</time></li>}
        </ol></section>
        <div className="reclamos-actions">
          {reclamo.estado === 'Abierto' && <><button className="secondary-button" onClick={abrirFormulario} type="button">Editar</button><button className="primary-button" onClick={(event) => accion('iniciar', event.currentTarget)} type="button">Iniciar</button><button className="secondary-button" onClick={(event) => accion('resolver', event.currentTarget)} type="button">Resolver</button><button className="danger-button" onClick={(event) => accion('rechazar', event.currentTarget)} type="button">Rechazar</button></>}
          {reclamo.estado === 'EnProceso' && <><button className="primary-button" onClick={(event) => accion('resolver', event.currentTarget)} type="button">Resolver</button><button className="danger-button" onClick={(event) => accion('rechazar', event.currentTarget)} type="button">Rechazar</button></>}
          {reclamo.estado === 'Resuelto' && <button className="primary-button" onClick={(event) => accion('cerrar', event.currentTarget)} type="button">Cerrar</button>}
        </div>
      </>}
    </PanelDetalle>
  );
}
