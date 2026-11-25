import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha } from '../../lib/formato';
import { diasOCiclo, horarioTurno, nombresDias, TEXTO_SOLO_RRHH, TEXTO_TURNO_CON_ASIGNACIONES } from './presentacion';
import type { TurnoDetalle } from './tipos';
import { TurnoEstadoPastilla } from './TurnosTabla';

interface Props {
  turno: TurnoDetalle | null;
  cargando: boolean;
  error: string;
  puedeEscribir: boolean;
  enCurso: boolean;
  mensajeAccion: string;
  editar: () => void;
  cambiarActivo: () => void;
  tituloRef: RefObject<HTMLHeadingElement | null>;
}

export function TurnoDetallePanel({
  turno,
  cargando,
  error,
  puedeEscribir,
  enCurso,
  mensajeAccion,
  editar,
  cambiarActivo,
  tituloRef,
}: Props) {
  const conAsignaciones = turno !== null && turno.totalAsignaciones > 0;
  const puedeEditar = puedeEscribir && !conAsignaciones;
  let aviso = '';
  if (turno !== null) {
    if (!puedeEscribir) {
      aviso = TEXTO_SOLO_RRHH;
    } else if (conAsignaciones) {
      aviso = TEXTO_TURNO_CON_ASIGNACIONES;
    }
  }

  return (
    <PanelDetalle ariaLabelledBy="turno-detail-title" className="clientes-detail-panel turnos-horarios-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando turno…</p>}
      {error === '' && !cargando && turno === null && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona un turno para ver su ficha.</p>
      )}
      {turno !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{turno.codigo}</div>
              <h2 id="turno-detail-title" ref={tituloRef} tabIndex={-1}>{turno.nombre}</h2>
            </div>
            <TurnoEstadoPastilla activo={turno.activo} />
          </div>
          <section aria-labelledby="turno-general-title" className="turnos-horarios-detail-section">
            <h3 id="turno-general-title">Datos del turno</h3>
            <dl className="turnos-horarios-detail-grid">
              <div>
                <dt>Tipo</dt>
                <dd>{turno.tipo}</dd>
              </div>
              <div>
                <dt>Horario</dt>
                <dd>{horarioTurno(turno.horaEntrada, turno.horaSalida, turno.cruzaMedianoche)}</dd>
              </div>
              <div>
                <dt>Cruza medianoche</dt>
                <dd>{turno.cruzaMedianoche ? 'Sí' : 'No'}</dd>
              </div>
              <div>
                <dt>Tolerancia</dt>
                <dd>{`${turno.toleranciaMinutos} minutos`}</dd>
              </div>
              {turno.tipo === 'Fijo' && (
                <div className="turnos-horarios-detail-wide">
                  <dt>Días laborables</dt>
                  <dd>{nombresDias(turno.diasLaborables)}</dd>
                </div>
              )}
              {turno.tipo === 'Rotativo' && (
                <>
                  <div>
                    <dt>Ciclo (trabajo x descanso)</dt>
                    <dd>{diasOCiclo(turno)}</dd>
                  </div>
                  <div>
                    <dt>Inicio del ciclo</dt>
                    <dd>{turno.fechaInicioCiclo === null ? 'Sin dato' : formatearFecha(turno.fechaInicioCiclo)}</dd>
                  </div>
                </>
              )}
              <div>
                <dt>Asignaciones totales</dt>
                <dd>{turno.totalAsignaciones}</dd>
              </div>
              <div>
                <dt>Empleados con turno hoy</dt>
                <dd>{turno.empleadosConTurnoHoy}</dd>
              </div>
            </dl>
          </section>
          <div className="clientes-detail-actions turnos-horarios-actions">
            <button
              aria-describedby={aviso === '' ? undefined : 'turno-aviso-detalle'}
              className="secondary-button"
              disabled={!puedeEditar || enCurso}
              onClick={editar}
              type="button"
            >
              Editar
            </button>
            <button
              aria-describedby={puedeEscribir ? undefined : 'turno-aviso-detalle'}
              className={turno.activo ? 'danger-button' : 'secondary-button'}
              disabled={!puedeEscribir || enCurso}
              onClick={cambiarActivo}
              type="button"
            >
              {turno.activo ? 'Desactivar' : 'Activar'}
            </button>
          </div>
          {aviso !== '' && <p className="turnos-horarios-aviso" id="turno-aviso-detalle">{aviso}</p>}
          {mensajeAccion !== '' && <p className="clientes-error" role="alert">{mensajeAccion}</p>}
        </>
      )}
    </PanelDetalle>
  );
}
