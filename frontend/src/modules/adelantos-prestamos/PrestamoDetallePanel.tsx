import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha, formateadorMoneda } from '../../lib/formato';
import { textoMes } from './cronograma';
import { motivoNoAnulable, TEXTO_SOLO_RRHH, textoTipo } from './presentacion';
import { EstadoPastilla } from './PrestamosTabla';
import type { PrestamoDetalle } from './tipos';

interface Props {
  prestamo: PrestamoDetalle | null;
  cargando: boolean;
  error: string;
  puedeEscribir: boolean;
  anular: () => void;
  botonAnularRef: RefObject<HTMLButtonElement | null>;
  tituloRef: RefObject<HTMLHeadingElement | null>;
}

export function PrestamoDetallePanel({
  prestamo,
  cargando,
  error,
  puedeEscribir,
  anular,
  botonAnularRef,
  tituloRef,
}: Props) {
  const motivoBloqueo = prestamo === null ? null : motivoNoAnulable(prestamo);
  // El aviso visible explica por qué «Anular» está deshabilitado: primero el rol, después el estado del préstamo.
  const aviso = !puedeEscribir ? TEXTO_SOLO_RRHH : motivoBloqueo;

  return (
    <PanelDetalle ariaLabelledBy="prestamo-detail-title" className="clientes-detail-panel adelantos-prestamos-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando préstamo…</p>}
      {error === '' && !cargando && prestamo === null && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona un préstamo para ver su ficha.</p>
      )}
      {prestamo !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{prestamo.numero}</div>
              <h2 id="prestamo-detail-title" ref={tituloRef} tabIndex={-1}>{prestamo.empleadoNombre}</h2>
            </div>
            <EstadoPastilla estado={prestamo.estado} />
          </div>
          <section aria-labelledby="prestamo-general-title" className="adelantos-prestamos-detail-section">
            <h3 id="prestamo-general-title">Datos del préstamo</h3>
            <dl className="adelantos-prestamos-detail-grid">
              <div>
                <dt>Empleado</dt>
                <dd>{`${prestamo.empleadoLegajo} · ${prestamo.empleadoNombre}`}</dd>
              </div>
              <div>
                <dt>Tipo</dt>
                <dd>{textoTipo(prestamo.tipo)}</dd>
              </div>
              <div>
                <dt>Monto</dt>
                <dd>{formateadorMoneda.format(prestamo.monto)}</dd>
              </div>
              <div>
                <dt>Saldo</dt>
                <dd>{formateadorMoneda.format(prestamo.saldo)}</dd>
              </div>
              <div>
                <dt>Cuotas</dt>
                <dd>{prestamo.numeroCuotas}</dd>
              </div>
              <div>
                <dt>Fecha de otorgamiento</dt>
                <dd>{formatearFecha(prestamo.fechaOtorgamiento)}</dd>
              </div>
              <div>
                <dt>Primer descuento</dt>
                <dd>{textoMes(prestamo.primerAnio, prestamo.primerMes)}</dd>
              </div>
              <div className="adelantos-prestamos-detail-wide">
                <dt>Motivo</dt>
                <dd>{prestamo.motivo}</dd>
              </div>
            </dl>
          </section>
          {(prestamo.fechaAnulacion !== null || prestamo.motivoAnulacion !== null) && (
            <section aria-labelledby="prestamo-anulacion-title" className="adelantos-prestamos-detail-section">
              <h3 id="prestamo-anulacion-title">Anulación</h3>
              <dl className="adelantos-prestamos-detail-grid">
                <div>
                  <dt>Fecha de anulación</dt>
                  <dd>{prestamo.fechaAnulacion === null ? 'Sin dato' : formatearFecha(prestamo.fechaAnulacion)}</dd>
                </div>
                <div className="adelantos-prestamos-detail-wide">
                  <dt>Motivo de la anulación</dt>
                  <dd>{prestamo.motivoAnulacion ?? 'Sin dato'}</dd>
                </div>
              </dl>
            </section>
          )}
          <section aria-labelledby="prestamo-cuotas-title" className="adelantos-prestamos-detail-section">
            <h3 id="prestamo-cuotas-title">Cuotas</h3>
            <div className="adelantos-prestamos-cuotas-scroll" role="region" aria-label="Tabla de cuotas" tabIndex={0}>
              <table className="adelantos-prestamos-cuotas">
                <caption className="sr-only">Cronograma de cuotas del préstamo</caption>
                <thead>
                  <tr>
                    <th scope="col">N.º</th>
                    <th scope="col">Mes</th>
                    <th scope="col">Monto</th>
                    <th scope="col">Descontado</th>
                    <th scope="col">Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {prestamo.cuotas.map((cuota) => (
                    <tr key={cuota.id}>
                      <td className="adelantos-prestamos-numero">{cuota.numeroCuota}</td>
                      <td className="adelantos-prestamos-numero">{textoMes(cuota.anio, cuota.mes)}</td>
                      <td className="adelantos-prestamos-numero">{formateadorMoneda.format(cuota.monto)}</td>
                      <td className="adelantos-prestamos-numero">{formateadorMoneda.format(cuota.montoDescontado)}</td>
                      <td>{cuota.estado}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
          <div className="clientes-detail-actions adelantos-prestamos-actions">
            <button
              aria-describedby={aviso === null ? undefined : 'prestamo-anular-aviso'}
              className="danger-button"
              disabled={aviso !== null}
              onClick={anular}
              ref={botonAnularRef}
              type="button"
            >
              Anular
            </button>
          </div>
          {aviso !== null && <p className="adelantos-prestamos-aviso" id="prestamo-anular-aviso">{aviso}</p>}
        </>
      )}
    </PanelDetalle>
  );
}
