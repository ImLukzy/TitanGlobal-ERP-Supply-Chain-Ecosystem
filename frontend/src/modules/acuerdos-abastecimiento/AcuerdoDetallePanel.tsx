import { PanelDetalle } from '../../components/PanelDetalle';
import { formateadorMoneda } from '../../lib/formato';
import type { AcuerdoDetalle, LineaAcuerdo } from './tipos';
import { avisoVigencia, formatearFechaCorta, porcentajeConsumo } from './vigencia';

export interface AccionesAcuerdo {
  editar: () => void;
  agregarLinea: () => void;
  consumir: (linea: LineaAcuerdo, boton: HTMLButtonElement) => void;
  quitarLinea: (linea: LineaAcuerdo, boton: HTMLButtonElement) => void;
  suspender: (boton: HTMLButtonElement) => void;
  reactivar: (boton: HTMLButtonElement) => void;
  cancelar: (boton: HTMLButtonElement) => void;
  eliminar: (boton: HTMLButtonElement) => void;
}

interface AcuerdoDetallePanelProps {
  acuerdo: AcuerdoDetalle | null;
  cargando: boolean;
  acciones: AccionesAcuerdo;
}

export function AcuerdoDetallePanel({ acuerdo, cargando, acciones }: AcuerdoDetallePanelProps) {
  return (
    <PanelDetalle ariaLabelledBy="acuerdo-detail-title" className="acuerdos-panel acuerdos-detail">
      {cargando && <p role="status">Cargando acuerdo…</p>}
      {!cargando && acuerdo === null && <p role="status">Selecciona un acuerdo para ver el detalle.</p>}
      {!cargando && acuerdo !== null && <Contenido acciones={acciones} acuerdo={acuerdo} />}
    </PanelDetalle>
  );
}

function motivoSinConsumo(acuerdo: AcuerdoDetalle): string | null {
  if (acuerdo.estado === 'Suspendido') {
    return 'El acuerdo está suspendido.';
  }
  const aviso = avisoVigencia(acuerdo);
  if (aviso === 'Vencido') {
    return 'El acuerdo está vencido.';
  }
  if (aviso === 'Aún no inicia') {
    return 'El acuerdo aún no inicia.';
  }
  return null;
}

function Contenido({ acuerdo, acciones }: { acuerdo: AcuerdoDetalle; acciones: AccionesAcuerdo }) {
  const aviso = avisoVigencia(acuerdo);
  const cancelado = acuerdo.estado === 'Cancelado';
  const motivo = motivoSinConsumo(acuerdo);
  const porcentaje = porcentajeConsumo(acuerdo.montoConsumido, acuerdo.montoComprometido);
  return (
    <>
      <div className="acuerdos-panel-heading">
        <div>
          <span className="page-eyebrow">DETALLE DE ACUERDO</span>
          <h2 id="acuerdo-detail-title">{acuerdo.numero}</h2>
        </div>
        <span className={`acuerdo-estado is-${acuerdo.estado.toLowerCase()}`}>{acuerdo.estado}</span>
      </div>
      {aviso !== null && <p className="acuerdo-aviso-banner">{aviso}</p>}
      <p className="acuerdo-proveedor">{acuerdo.proveedorNombre}</p>
      <dl className="acuerdo-detail-grid">
        <div><dt>Vigencia</dt><dd>{formatearFechaCorta(acuerdo.fechaInicio)} – {formatearFechaCorta(acuerdo.fechaFin)}</dd></div>
        <div><dt>Monto máximo</dt><dd>{formateadorMoneda.format(acuerdo.montoMaximo)}</dd></div>
        <div><dt>Comprometido</dt><dd>{formateadorMoneda.format(acuerdo.montoComprometido)} de {formateadorMoneda.format(acuerdo.montoMaximo)}</dd></div>
        <div><dt>Consumido</dt><dd>{formateadorMoneda.format(acuerdo.montoConsumido)} ({porcentaje}%)</dd></div>
        <div><dt>Registro</dt><dd>{formatearFechaCorta(acuerdo.fechaRegistro)}</dd></div>
      </dl>
      <progress aria-label="Avance de consumo" className="acuerdo-progreso" max={100} value={porcentaje} />
      <section className="acuerdo-detail-note">
        <h3>Descripción</h3>
        <p>{acuerdo.descripcion === null || acuerdo.descripcion === '' ? 'Sin descripción' : acuerdo.descripcion}</p>
      </section>
      <section aria-label="Líneas del acuerdo" className="acuerdo-detail-lineas">
        <h3>Líneas</h3>
        {acuerdo.lineas.length === 0 ? (
          <p>Aún no hay líneas.</p>
        ) : (
          <div className="acuerdo-lineas-scroll">
            <table className="acuerdo-lineas-table">
              <caption>Líneas con su saldo</caption>
              <thead>
                <tr>
                  <th scope="col">Producto</th>
                  <th scope="col">Precio</th>
                  <th scope="col">Comprometida</th>
                  <th scope="col">Consumida</th>
                  <th scope="col">Saldo</th>
                  {!cancelado && <th scope="col">Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {acuerdo.lineas.map((linea) => (
                  <tr key={linea.id}>
                    <th scope="row">{linea.productoNombre}</th>
                    <td className="acuerdo-num">{formateadorMoneda.format(linea.precioAcordado)}</td>
                    <td className="acuerdo-num">{linea.cantidadComprometida}</td>
                    <td className="acuerdo-num">{linea.cantidadConsumida}</td>
                    <td className="acuerdo-num">{linea.saldo}</td>
                    {!cancelado && (
                      <td className="acuerdo-linea-acciones">
                        {acuerdo.estado === 'Vigente' && (
                          <button
                            aria-label={`Registrar consumo de ${linea.productoNombre}`}
                            className="secondary-button"
                            disabled={motivo !== null || linea.saldo <= 0}
                            onClick={(event) => acciones.consumir(linea, event.currentTarget)}
                            type="button"
                          >
                            Registrar consumo
                          </button>
                        )}
                        {linea.cantidadConsumida === 0 && (
                          <button
                            aria-label={`Quitar línea de ${linea.productoNombre}`}
                            className="danger-button"
                            onClick={(event) => acciones.quitarLinea(linea, event.currentTarget)}
                            type="button"
                          >
                            Quitar línea
                          </button>
                        )}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {acuerdo.estado === 'Vigente' && motivo !== null && acuerdo.lineas.length > 0 && <p className="acuerdo-motivo">Registrar consumo no disponible: {motivo}</p>}
      </section>
      <BotonesAccion acciones={acciones} acuerdo={acuerdo} />
    </>
  );
}

function BotonesAccion({ acuerdo, acciones }: { acuerdo: AcuerdoDetalle; acciones: AccionesAcuerdo }) {
  if (acuerdo.estado === 'Cancelado') {
    return null;
  }
  const sinConsumo = acuerdo.montoConsumido === 0;
  return (
    <div className="acuerdo-actions">
      <button className="secondary-button" onClick={acciones.editar} type="button">Editar</button>
      <button className="secondary-button" onClick={acciones.agregarLinea} type="button">Agregar línea</button>
      {acuerdo.estado === 'Vigente'
        ? <button className="secondary-button" onClick={(event) => acciones.suspender(event.currentTarget)} type="button">Suspender</button>
        : <button className="secondary-button" onClick={(event) => acciones.reactivar(event.currentTarget)} type="button">Reactivar</button>}
      <button className="danger-button" onClick={(event) => acciones.cancelar(event.currentTarget)} type="button">Cancelar</button>
      {sinConsumo && <button className="danger-button" onClick={(event) => acciones.eliminar(event.currentTarget)} type="button">Eliminar</button>}
    </div>
  );
}
