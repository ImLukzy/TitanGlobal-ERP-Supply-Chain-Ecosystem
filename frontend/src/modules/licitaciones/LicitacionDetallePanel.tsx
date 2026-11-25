import { PanelDetalle } from '../../components/PanelDetalle';
import { formateadorMoneda } from '../../lib/formato';
import { etiquetasTipo, formatearFechaHora, plazoVencido } from './plazo';
import type { LicitacionDetalle, OfertaLicitacion } from './tipos';

export interface AccionesLicitacion {
  editar: () => void;
  ofertar: (boton: HTMLButtonElement) => void;
  mejorar: (oferta: OfertaLicitacion, boton: HTMLButtonElement) => void;
  cerrar: (boton: HTMLButtonElement) => void;
  adjudicar: (boton: HTMLButtonElement) => void;
  cancelar: (boton: HTMLButtonElement) => void;
  eliminar: (boton: HTMLButtonElement) => void;
}

interface LicitacionDetallePanelProps {
  licitacion: LicitacionDetalle | null;
  cargando: boolean;
  acciones: AccionesLicitacion;
}

export function LicitacionDetallePanel({ licitacion, cargando, acciones }: LicitacionDetallePanelProps) {
  return (
    <PanelDetalle ariaLabelledBy="licitacion-detail-title" className="licitaciones-panel licitaciones-detail">
      {cargando && <p role="status">Cargando licitación…</p>}
      {!cargando && licitacion === null && <p role="status">Selecciona una licitación para ver el detalle.</p>}
      {!cargando && licitacion !== null && <Contenido acciones={acciones} licitacion={licitacion} />}
    </PanelDetalle>
  );
}

function Contenido({ licitacion, acciones }: { licitacion: LicitacionDetalle; acciones: AccionesLicitacion }) {
  const vencida = plazoVencido(licitacion.estado, licitacion.fechaCierre);
  const abierta = licitacion.estado === 'Abierta';
  const subasta = licitacion.tipo === 'SubastaInversa';
  return (
    <>
      <div className="licitaciones-panel-heading">
        <div>
          <span className="page-eyebrow">DETALLE DE LICITACIÓN</span>
          <h2 id="licitacion-detail-title">{licitacion.numero}</h2>
        </div>
        <span className={`licitacion-estado is-${licitacion.estado.toLowerCase()}`}>{licitacion.estado}</span>
      </div>
      {vencida && <p className="licitacion-late-banner">Plazo vencido</p>}
      <p className="licitacion-titulo">{licitacion.titulo}</p>
      <dl className="licitacion-detail-grid">
        <div><dt>Tipo</dt><dd>{etiquetasTipo[licitacion.tipo]}</dd></div>
        <div><dt>Presupuesto referencial</dt><dd>{formateadorMoneda.format(licitacion.presupuestoReferencial)}</dd></div>
        <div><dt>Fecha de cierre</dt><dd>{formatearFechaHora(licitacion.fechaCierre)}</dd></div>
        <div><dt>Fecha de registro</dt><dd>{formatearFechaHora(licitacion.fechaRegistro)}</dd></div>
        {licitacion.estado === 'Adjudicada' && (
          <>
            <div><dt>Proveedor adjudicado</dt><dd>{licitacion.proveedorAdjudicadoNombre}</dd></div>
            <div><dt>Monto adjudicado</dt><dd>{licitacion.montoAdjudicado === null ? '' : formateadorMoneda.format(licitacion.montoAdjudicado)}</dd></div>
          </>
        )}
      </dl>
      <section className="licitacion-detail-note">
        <h3>Descripción</h3>
        <p>{licitacion.descripcion === null || licitacion.descripcion === '' ? 'Sin descripción' : licitacion.descripcion}</p>
      </section>
      <section aria-label="Ofertas de la licitación" className="licitacion-detail-ofertas">
        <h3>Ofertas</h3>
        {licitacion.ofertas.length === 0 ? (
          <p>Aún no hay ofertas.</p>
        ) : (
          <div className="licitacion-ofertas-scroll">
            <table className="licitacion-ofertas-table">
              <caption>Ofertas comparadas por monto</caption>
              <thead>
                <tr>
                  <th scope="col">Proveedor</th>
                  <th scope="col">Monto</th>
                  <th scope="col">Plazo</th>
                  <th scope="col">Observaciones</th>
                  {abierta && subasta && !vencida && <th scope="col">Acción</th>}
                </tr>
              </thead>
              <tbody>
                {licitacion.ofertas.map((oferta) => (
                  <tr key={oferta.id}>
                    <th scope="row">
                      {oferta.proveedorNombre}
                      {oferta.esGanadora && <span className="licitacion-ganadora">Ganadora</span>}
                    </th>
                    <td>{formateadorMoneda.format(oferta.monto)}</td>
                    <td>{oferta.plazoEntregaDias} días</td>
                    <td>{oferta.observaciones === null || oferta.observaciones === '' ? 'Sin observaciones' : oferta.observaciones}</td>
                    {abierta && subasta && !vencida && (
                      <td>
                        <button
                          aria-label={`Mejorar oferta de ${oferta.proveedorNombre}`}
                          className="secondary-button"
                          onClick={(event) => acciones.mejorar(oferta, event.currentTarget)}
                          type="button"
                        >
                          Mejorar oferta
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
      <BotonesAccion acciones={acciones} licitacion={licitacion} vencida={vencida} />
    </>
  );
}

function BotonesAccion({ licitacion, acciones, vencida }: { licitacion: LicitacionDetalle; acciones: AccionesLicitacion; vencida: boolean }) {
  if (licitacion.estado === 'Abierta') {
    return (
      <div className="licitacion-actions">
        <button className="secondary-button" onClick={acciones.editar} type="button">Editar</button>
        {!vencida && <button className="primary-button" onClick={(event) => acciones.ofertar(event.currentTarget)} type="button">Presentar oferta</button>}
        <button className="secondary-button" onClick={(event) => acciones.cerrar(event.currentTarget)} type="button">Cerrar</button>
        <button className="danger-button" onClick={(event) => acciones.cancelar(event.currentTarget)} type="button">Cancelar</button>
        {licitacion.ofertas.length === 0 && (
          <button className="danger-button" onClick={(event) => acciones.eliminar(event.currentTarget)} type="button">Eliminar</button>
        )}
      </div>
    );
  }
  if (licitacion.estado === 'Cerrada') {
    return (
      <div className="licitacion-actions">
        <button className="primary-button" onClick={(event) => acciones.adjudicar(event.currentTarget)} type="button">Adjudicar</button>
        <button className="danger-button" onClick={(event) => acciones.cancelar(event.currentTarget)} type="button">Cancelar</button>
      </div>
    );
  }
  return null;
}
