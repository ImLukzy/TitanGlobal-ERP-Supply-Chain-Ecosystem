import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha } from '../../lib/formato';
import { AsignacionEstadoPastilla } from './AsignacionesTabla';
import {
  diaDeLaSemana,
  horarioTurno,
  TEXTO_SIN_FIN,
  TEXTO_SOLO_FUTURAS,
  TEXTO_SOLO_RRHH,
  textoDia,
} from './presentacion';
import type { AsignacionTurnoListado, DiaProgramado } from './tipos';

interface Props {
  asignacion: AsignacionTurnoListado | null;
  programacion: DiaProgramado[] | null;
  errorProgramacion: string;
  puedeEscribir: boolean;
  fijarFin: () => void;
  eliminar: () => void;
  botonFijarFinRef: RefObject<HTMLButtonElement | null>;
  botonEliminarRef: RefObject<HTMLButtonElement | null>;
  tituloRef: RefObject<HTMLHeadingElement | null>;
}

function horarioDia(dia: DiaProgramado): string {
  if (!dia.laborable || dia.horaEntrada === null || dia.horaSalida === null) {
    return '—';
  }
  return horarioTurno(dia.horaEntrada, dia.horaSalida, dia.cruzaMedianoche);
}

export function AsignacionDetallePanel({
  asignacion,
  programacion,
  errorProgramacion,
  puedeEscribir,
  fijarFin,
  eliminar,
  botonFijarFinRef,
  botonEliminarRef,
  tituloRef,
}: Props) {
  const puedeFijarFin = puedeEscribir && asignacion !== null && asignacion.estado !== 'Finalizada';
  const puedeEliminar = puedeEscribir && asignacion !== null && asignacion.estado === 'Futura';
  let aviso = '';
  if (asignacion !== null) {
    if (!puedeEscribir) {
      aviso = TEXTO_SOLO_RRHH;
    } else if (asignacion.estado !== 'Futura') {
      aviso = TEXTO_SOLO_FUTURAS;
    }
  }

  return (
    <PanelDetalle ariaLabelledBy="asignacion-detail-title" className="clientes-detail-panel turnos-horarios-detail-panel">
      {asignacion === null && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona una asignación para ver su ficha.</p>
      )}
      {asignacion !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{asignacion.turnoCodigo}</div>
              <h2 id="asignacion-detail-title" ref={tituloRef} tabIndex={-1}>{asignacion.empleadoNombre}</h2>
            </div>
            <AsignacionEstadoPastilla estado={asignacion.estado} />
          </div>
          <section aria-labelledby="asignacion-general-title" className="turnos-horarios-detail-section">
            <h3 id="asignacion-general-title">Datos de la asignación</h3>
            <dl className="turnos-horarios-detail-grid">
              <div>
                <dt>Empleado</dt>
                <dd>{`${asignacion.empleadoLegajo} · ${asignacion.empleadoNombre}`}</dd>
              </div>
              <div>
                <dt>Turno</dt>
                <dd>{`${asignacion.turnoCodigo} · ${asignacion.turnoNombre}`}</dd>
              </div>
              <div>
                <dt>Inicio</dt>
                <dd>{formatearFecha(asignacion.fechaInicio)}</dd>
              </div>
              <div>
                <dt>Fin</dt>
                <dd>{asignacion.fechaFin === null ? TEXTO_SIN_FIN : formatearFecha(asignacion.fechaFin)}</dd>
              </div>
            </dl>
          </section>
          <section aria-labelledby="asignacion-programacion-title" className="turnos-horarios-detail-section">
            <h3 id="asignacion-programacion-title">Programación de los próximos 14 días</h3>
            {errorProgramacion !== '' && <p className="clientes-error" role="alert">{errorProgramacion}</p>}
            {errorProgramacion === '' && programacion === null && (
              <p aria-live="polite" className="clientes-status" role="status">Cargando programación…</p>
            )}
            {programacion !== null && (
              <div className="turnos-horarios-programacion-scroll">
                <table className="turnos-horarios-programacion">
                  <caption>Programación del empleado</caption>
                  <thead>
                    <tr>
                      <th scope="col">Fecha</th>
                      <th scope="col">Día</th>
                      <th scope="col">Turno</th>
                      <th scope="col">Horario</th>
                      <th scope="col">Estado</th>
                    </tr>
                  </thead>
                  <tbody>
                    {programacion.map((dia) => (
                      <tr key={dia.fecha}>
                        <td className="turnos-horarios-numero">{formatearFecha(dia.fecha)}</td>
                        <td>{diaDeLaSemana(dia.fecha)}</td>
                        <td>{dia.turnoCodigo ?? '—'}</td>
                        <td className="turnos-horarios-numero">{horarioDia(dia)}</td>
                        <td>
                          <span className={`turnos-horarios-dia ${dia.laborable ? 'is-laborable' : 'is-libre'}`}>{textoDia(dia)}</span>
                          {dia.laborable && dia.toleranciaMinutos !== null && (
                            <span className="turnos-horarios-tolerancia">{` ±${dia.toleranciaMinutos} min`}</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
          <div className="clientes-detail-actions turnos-horarios-actions">
            <button
              aria-describedby={puedeEscribir ? undefined : 'asignacion-aviso-detalle'}
              className="secondary-button"
              disabled={!puedeFijarFin}
              onClick={fijarFin}
              ref={botonFijarFinRef}
              type="button"
            >
              Fijar fin
            </button>
            <button
              aria-describedby={aviso === '' ? undefined : 'asignacion-aviso-detalle'}
              className="danger-button"
              disabled={!puedeEliminar}
              onClick={eliminar}
              ref={botonEliminarRef}
              type="button"
            >
              Eliminar
            </button>
          </div>
          {aviso !== '' && <p className="turnos-horarios-aviso" id="asignacion-aviso-detalle">{aviso}</p>}
        </>
      )}
    </PanelDetalle>
  );
}
