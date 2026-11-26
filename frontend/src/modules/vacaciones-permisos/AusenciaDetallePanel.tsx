import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha } from '../../lib/formato';
import { EstadoPastilla } from './AusenciasTabla';
import { puedeCancelar, TEXTO_SOLO_RRHH, textoTipo } from './presentacion';
import { SaldoVacacional } from './SaldoVacacional';
import type { AusenciaDetalle } from './tipos';
import { useSaldoVacacional } from './useAusenciasData';

interface Props {
  ausencia: AusenciaDetalle | null;
  cargando: boolean;
  error: string;
  errorAccion: string;
  procesando: boolean;
  revision: number;
  puedeDecidir: boolean;
  aprobar: () => void;
  rechazar: () => void;
  cancelar: () => void;
  botonAprobarRef: RefObject<HTMLButtonElement | null>;
  botonRechazarRef: RefObject<HTMLButtonElement | null>;
  botonCancelarRef: RefObject<HTMLButtonElement | null>;
  tituloRef: RefObject<HTMLHeadingElement | null>;
}

function textoOpcional(valor: string | null, respaldo: string): string {
  return valor === null || valor.trim() === '' ? respaldo : valor;
}

export function AusenciaDetallePanel({
  ausencia,
  cargando,
  error,
  errorAccion,
  procesando,
  revision,
  puedeDecidir,
  aprobar,
  rechazar,
  cancelar,
  botonAprobarRef,
  botonRechazarRef,
  botonCancelarRef,
  tituloRef,
}: Props) {
  const esVacaciones = ausencia?.tipo === 'Vacaciones';
  const saldo = useSaldoVacacional(esVacaciones ? ausencia.empleadoId : null, ausencia?.fechaInicio ?? '', revision);
  const pendiente = ausencia?.estado === 'Pendiente';
  const mostrarCancelar = ausencia !== null && puedeCancelar(ausencia.estado, ausencia.fechaInicio);
  const mostrarAcciones = pendiente || mostrarCancelar;

  return (
    <PanelDetalle ariaLabelledBy="ausencia-detail-title" className="clientes-detail-panel vacaciones-permisos-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando ausencia…</p>}
      {error === '' && !cargando && ausencia === null && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona una ausencia para ver su ficha.</p>
      )}
      {ausencia !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{ausencia.numero}</div>
              <h2 id="ausencia-detail-title" ref={tituloRef} tabIndex={-1}>{ausencia.empleadoNombre}</h2>
            </div>
            <EstadoPastilla estado={ausencia.estado} />
          </div>
          <section aria-labelledby="ausencia-general-title" className="vacaciones-permisos-detail-section">
            <h3 id="ausencia-general-title">Datos de la ausencia</h3>
            <dl className="vacaciones-permisos-detail-grid">
              <div>
                <dt>Empleado</dt>
                <dd>{`${ausencia.empleadoLegajo} · ${ausencia.empleadoNombre}`}</dd>
              </div>
              <div>
                <dt>Tipo</dt>
                <dd>{textoTipo(ausencia.tipo)}</dd>
              </div>
              <div>
                <dt>Desde</dt>
                <dd>{formatearFecha(ausencia.fechaInicio)}</dd>
              </div>
              <div>
                <dt>Hasta</dt>
                <dd>{formatearFecha(ausencia.fechaFin)}</dd>
              </div>
              <div>
                <dt>Días</dt>
                <dd>{ausencia.dias}</dd>
              </div>
              <div>
                <dt>Fecha de solicitud</dt>
                <dd>{formatearFecha(ausencia.fechaSolicitud)}</dd>
              </div>
              <div className="vacaciones-permisos-detail-wide">
                <dt>Motivo</dt>
                <dd>{textoOpcional(ausencia.motivo, 'Sin motivo')}</dd>
              </div>
            </dl>
          </section>
          {(ausencia.decididoPor !== null || ausencia.fechaDecision !== null || ausencia.motivoRechazo !== null) && (
            <section aria-labelledby="ausencia-decision-title" className="vacaciones-permisos-detail-section">
              <h3 id="ausencia-decision-title">Decisión</h3>
              <dl className="vacaciones-permisos-detail-grid">
                <div>
                  <dt>Decidido por</dt>
                  <dd>{textoOpcional(ausencia.decididoPor, 'Sin dato')}</dd>
                </div>
                <div>
                  <dt>Fecha de decisión</dt>
                  <dd>{ausencia.fechaDecision === null ? 'Sin dato' : formatearFecha(ausencia.fechaDecision)}</dd>
                </div>
                {ausencia.motivoRechazo !== null && (
                  <div className="vacaciones-permisos-detail-wide">
                    <dt>Motivo del rechazo</dt>
                    <dd>{ausencia.motivoRechazo}</dd>
                  </div>
                )}
              </dl>
            </section>
          )}
          {ausencia.fechaCancelacion !== null && (
            <section aria-labelledby="ausencia-cancelacion-title" className="vacaciones-permisos-detail-section">
              <h3 id="ausencia-cancelacion-title">Cancelación</h3>
              <dl className="vacaciones-permisos-detail-grid">
                <div>
                  <dt>Fecha de cancelación</dt>
                  <dd>{formatearFecha(ausencia.fechaCancelacion)}</dd>
                </div>
              </dl>
            </section>
          )}
          {esVacaciones && (
            <section aria-labelledby="ausencia-saldo-title" className="vacaciones-permisos-detail-section">
              <h3 id="ausencia-saldo-title">Saldo vacacional del empleado</h3>
              <SaldoVacacional cargando={saldo.cargando} error={saldo.error} saldo={saldo.saldo} />
            </section>
          )}
          {errorAccion !== '' && <p className="clientes-error" role="alert">{errorAccion}</p>}
          {mostrarAcciones && (
            <div className="clientes-detail-actions vacaciones-permisos-actions">
              {pendiente && (
                <>
                  <button
                    aria-describedby={puedeDecidir ? undefined : 'ausencia-solo-rrhh'}
                    className="primary-button"
                    disabled={!puedeDecidir || procesando}
                    onClick={aprobar}
                    ref={botonAprobarRef}
                    type="button"
                  >
                    {procesando ? 'Aprobando…' : 'Aprobar'}
                  </button>
                  <button
                    aria-describedby={puedeDecidir ? undefined : 'ausencia-solo-rrhh'}
                    className="secondary-button"
                    disabled={!puedeDecidir || procesando}
                    onClick={rechazar}
                    ref={botonRechazarRef}
                    type="button"
                  >
                    Rechazar
                  </button>
                </>
              )}
              {mostrarCancelar && (
                <button
                  className="danger-button"
                  disabled={procesando}
                  onClick={cancelar}
                  ref={botonCancelarRef}
                  type="button"
                >
                  Cancelar solicitud
                </button>
              )}
            </div>
          )}
          {pendiente && !puedeDecidir && <p className="vacaciones-permisos-aviso" id="ausencia-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        </>
      )}
    </PanelDetalle>
  );
}
