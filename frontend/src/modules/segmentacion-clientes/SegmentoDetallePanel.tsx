import { formateadorMoneda, formatearCantidad } from '../../lib/formato';
import { PanelDetalle } from '../../components/PanelDetalle';
import type { RefObject } from 'react';
import type { ClienteClasificado, Pagina, SegmentoDetalle } from './tipos';

interface Props {
  segmento: SegmentoDetalle | null;
  clientes: Pagina<ClienteClasificado> | null;
  cargando: boolean;
  error: string;
  cambiarPagina: (pagina: number) => void;
  editar: () => void;
  eliminar: () => void;
  botonEliminarRef: RefObject<HTMLButtonElement | null>;
}

export function SegmentoDetallePanel({ segmento, clientes, cargando, error, cambiarPagina, editar, eliminar, botonEliminarRef }: Props) {
  if (error !== '') {
    return <p className="clientes-error" role="alert">{error}</p>;
  }
  if (segmento === null) {
    return <p className="clientes-status" role="status">Selecciona un segmento para ver su detalle.</p>;
  }
  const paginas = Math.max(1, Math.ceil((clientes?.total ?? 0) / 10));
  return (
    <PanelDetalle ariaLabelledBy="segmento-detail-title" className="clientes-detail-panel segment-detail-panel">
      <div className="clientes-panel-heading">
        <div><div className="page-eyebrow">DETALLE</div><h2 id="segmento-detail-title">{segmento.nombre}</h2></div>
        <span className={`proveedor-status${segmento.activo ? ' is-active' : ' is-inactive'}`}>{segmento.activo ? 'Activo' : 'Inactivo'}</span>
      </div>
      <dl className="segment-detail-grid">
        <div><dt>Código</dt><dd>{segmento.codigo}</dd></div>
        <div><dt>Prioridad</dt><dd>{segmento.prioridad}</dd></div>
        <div><dt>Monto mínimo</dt><dd>{formateadorMoneda.format(segmento.montoMinimo)}</dd></div>
        <div><dt>Órdenes mínimas</dt><dd>{formatearCantidad(segmento.ordenesMinimas, 'orden', 'órdenes')}</dd></div>
        <div className="segment-description"><dt>Descripción</dt><dd>{segmento.descripcion || 'Sin descripción'}</dd></div>
      </dl>
      <section aria-labelledby="segment-clients-title" className="segment-clients">
        <h3 id="segment-clients-title">Clientes del segmento</h3>
        {cargando ? <p role="status">Cargando clientes…</p>
          : clientes === null || clientes.items.length === 0 ? <p role="status">No hay clientes asignados a este segmento.</p>
            : <ul>{clientes.items.map((cliente) => (
              <li key={cliente.clienteId}>
                <span><strong>{cliente.nombre}</strong><small>{cliente.ruc} · {formatearCantidad(cliente.totalOrdenes, 'orden', 'órdenes')}</small></span>
                <strong>{formateadorMoneda.format(cliente.montoTotal)}</strong>
              </li>
            ))}</ul>}
        {clientes !== null && clientes.total > 10 && (
          <nav aria-label="Paginación: clientes del segmento" className="tabla-paginada-nav">
            <button className="secondary-button" disabled={clientes.pagina <= 1} onClick={() => cambiarPagina(clientes.pagina - 1)} type="button">Anterior</button>
            <span>Página {clientes.pagina} de {paginas}</span>
            <button className="secondary-button" disabled={clientes.pagina >= paginas} onClick={() => cambiarPagina(clientes.pagina + 1)} type="button">Siguiente</button>
          </nav>
        )}
      </section>
      <div className="clientes-detail-actions">
        <button className="secondary-button" onClick={editar} type="button">Editar segmento</button>
        <button className="danger-button" onClick={eliminar} ref={botonEliminarRef} type="button">Eliminar segmento</button>
      </div>
    </PanelDetalle>
  );
}
