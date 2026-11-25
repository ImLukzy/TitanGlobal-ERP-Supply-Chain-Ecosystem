import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha } from '../../lib/formato';
import { EstadoJornadaPastilla, HorasExtraPastilla } from './JornadasTabla';
import {
  aFechaHoraLocal,
  aHoraLocal,
  formatearHoraProgramada,
  formatearMinutos,
  horaDeJornada,
  TEXTO_SOLO_RRHH,
} from './presentacion';
import type { JornadaAsistencia, Marcacion } from './tipos';

interface Props {
  jornada: JornadaAsistencia | null;
  marcaciones: Marcacion[] | null;
  errorMarcaciones: string;
  errorAccion: string;
  puedeEscribir: boolean;
  procesando: boolean;
  aprobar: () => void;
  rechazar: () => void;
  botonRechazarRef: RefObject<HTMLButtonElement | null>;
  tituloRef: RefObject<HTMLHeadingElement | null>;
}

function detalleMarcacion(marcacion: Marcacion): string {
  if (marcacion.origen === 'Reloj') {
    return marcacion.dispositivo ?? '';
  }
  return marcacion.observacion ?? '';
}

export function JornadaDetallePanel({
  jornada,
  marcaciones,
  errorMarcaciones,
  errorAccion,
  puedeEscribir,
  procesando,
  aprobar,
  rechazar,
  botonRechazarRef,
  tituloRef,
}: Props) {
  const pendiente = jornada !== null && jornada.estadoHorasExtra === 'Pendiente';
  return (
    <PanelDetalle ariaLabelledBy="jornada-detail-title" className="clientes-detail-panel asistencia-detail-panel">
      {jornada === null && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona una jornada para ver su ficha.</p>
      )}
      {jornada !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{formatearFecha(jornada.fecha)}</div>
              <h2 id="jornada-detail-title" ref={tituloRef} tabIndex={-1}>{jornada.empleadoNombre}</h2>
            </div>
            <EstadoJornadaPastilla estado={jornada.estado} />
          </div>
          <section aria-labelledby="jornada-programado-title" className="asistencia-detail-section">
            <h3 id="jornada-programado-title">Datos programados</h3>
            <dl className="asistencia-detail-grid">
              <div>
                <dt>Empleado</dt>
                <dd>{`${jornada.empleadoLegajo} · ${jornada.empleadoNombre}`}</dd>
              </div>
              <div>
                <dt>Turno</dt>
                <dd>{jornada.turnoCodigo ?? '—'}</dd>
              </div>
              <div>
                <dt>Entrada programada</dt>
                <dd className="asistencia-numero">{formatearHoraProgramada(jornada.horaEntradaProgramada)}</dd>
              </div>
              <div>
                <dt>Salida programada</dt>
                <dd className="asistencia-numero">{formatearHoraProgramada(jornada.horaSalidaProgramada)}</dd>
              </div>
              <div>
                <dt>Tolerancia</dt>
                <dd>{`${jornada.toleranciaMinutos} min`}</dd>
              </div>
            </dl>
          </section>
          <section aria-labelledby="jornada-resultado-title" className="asistencia-detail-section">
            <h3 id="jornada-resultado-title">Resultado del día</h3>
            <dl className="asistencia-detail-grid">
              <div>
                <dt>Entrada</dt>
                <dd className="asistencia-numero">{horaDeJornada(jornada.entrada, jornada.fecha)}</dd>
              </div>
              <div>
                <dt>Salida</dt>
                <dd className="asistencia-numero">{horaDeJornada(jornada.salida, jornada.fecha)}</dd>
              </div>
              <div>
                <dt>Trabajado</dt>
                <dd>{formatearMinutos(jornada.minutosTrabajados)}</dd>
              </div>
              <div>
                <dt>Tardanza</dt>
                <dd>{formatearMinutos(jornada.minutosTardanza)}</dd>
              </div>
              <div>
                <dt>Horas extra</dt>
                <dd>{formatearMinutos(jornada.minutosExtra)}</dd>
              </div>
              <div>
                <dt>Estado de horas extra</dt>
                <dd><HorasExtraPastilla estado={jornada.estadoHorasExtra} /></dd>
              </div>
              {jornada.decididoPor !== null && (
                <div>
                  <dt>Decidido por</dt>
                  <dd>{jornada.decididoPor}</dd>
                </div>
              )}
              {jornada.fechaDecision !== null && (
                <div>
                  <dt>Fecha de decisión</dt>
                  <dd className="asistencia-numero">{aFechaHoraLocal(jornada.fechaDecision)}</dd>
                </div>
              )}
              {jornada.motivoRechazo !== null && (
                <div className="asistencia-detail-wide">
                  <dt>Motivo del rechazo</dt>
                  <dd>{jornada.motivoRechazo}</dd>
                </div>
              )}
            </dl>
          </section>
          <section aria-labelledby="jornada-marcaciones-title" className="asistencia-detail-section">
            <h3 id="jornada-marcaciones-title">Marcaciones del día</h3>
            {errorMarcaciones !== '' && <p className="clientes-error" role="alert">{errorMarcaciones}</p>}
            {errorMarcaciones === '' && marcaciones === null && (
              <p aria-live="polite" className="clientes-status" role="status">Cargando marcaciones…</p>
            )}
            {marcaciones !== null && marcaciones.length === 0 && (
              <p aria-live="polite" className="clientes-status" role="status">El empleado no tiene marcaciones en esos días.</p>
            )}
            {marcaciones !== null && marcaciones.length > 0 && (
              <div className="asistencia-scroll">
                <table className="asistencia-mini-tabla">
                  <caption>Marcaciones del empleado</caption>
                  <thead>
                    <tr>
                      <th scope="col">Hora</th>
                      <th scope="col">Tipo</th>
                      <th scope="col">Origen</th>
                      <th scope="col">Detalle</th>
                    </tr>
                  </thead>
                  <tbody>
                    {marcaciones.map((marcacion) => (
                      <tr key={marcacion.id}>
                        <td className="asistencia-numero">{aHoraLocal(marcacion.instante)}</td>
                        <td>{marcacion.tipo}</td>
                        <td>{marcacion.origen}</td>
                        <td>{detalleMarcacion(marcacion)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
          {pendiente && (
            <>
              <div className="clientes-detail-actions asistencia-actions">
                <button
                  aria-describedby={puedeEscribir ? undefined : 'jornada-aviso-detalle'}
                  className="primary-button"
                  disabled={!puedeEscribir || procesando}
                  onClick={aprobar}
                  type="button"
                >
                  {procesando ? 'Procesando…' : 'Aprobar horas extra'}
                </button>
                <button
                  aria-describedby={puedeEscribir ? undefined : 'jornada-aviso-detalle'}
                  className="danger-button"
                  disabled={!puedeEscribir || procesando}
                  onClick={rechazar}
                  ref={botonRechazarRef}
                  type="button"
                >
                  Rechazar
                </button>
              </div>
              {!puedeEscribir && <p className="asistencia-aviso" id="jornada-aviso-detalle">{TEXTO_SOLO_RRHH}</p>}
            </>
          )}
          {errorAccion !== '' && <p aria-live="polite" className="clientes-error" role="alert">{errorAccion}</p>}
        </>
      )}
    </PanelDetalle>
  );
}
