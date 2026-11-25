import type { ReactNode, RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearHoras, TEXTO_SOLO_RRHH, textoFecha } from './presentacion';
import { EstadoProgramacionPastilla } from './ProgramacionesTabla';
import type { ProgramacionDetalle } from './tipos';

interface Props {
  programacion: ProgramacionDetalle | null;
  cargando: boolean;
  error: string;
  errorAccion: string;
  puedeEscribir: boolean;
  enCurso: boolean;
  inscribir: () => void;
  realizar: () => void;
  cancelar: () => void;
  tituloRef: RefObject<HTMLHeadingElement | null>;
  children: ReactNode;
}

function Acciones({ programacion, puedeEscribir, enCurso, inscribir, realizar, cancelar }: Omit<Props, 'cargando' | 'error' | 'errorAccion' | 'tituloRef' | 'children' | 'programacion'> & { programacion: ProgramacionDetalle }) {
  const sinCupo = programacion.cupoLibre <= 0;
  const motivoCupo = `La programación ${programacion.id} no tiene cupo libre.`;
  const describedBy = puedeEscribir ? undefined : 'programacion-solo-rrhh';
  if (programacion.estado !== 'Programada') {
    return null;
  }
  return (
    <div className="clientes-detail-actions capacitaciones-actions">
      <button aria-describedby={sinCupo && puedeEscribir ? 'programacion-sin-cupo' : describedBy} className="primary-button" disabled={!puedeEscribir || sinCupo || enCurso} onClick={inscribir} type="button">
        Inscribir
      </button>
      <button aria-describedby={describedBy} className="secondary-button" disabled={!puedeEscribir || enCurso} onClick={realizar} type="button">
        Marcar realizada
      </button>
      <button aria-describedby={describedBy} className="secondary-button" disabled={!puedeEscribir || enCurso} onClick={cancelar} type="button">
        Cancelar programación
      </button>
      {sinCupo && <p className="capacitaciones-aviso" id="programacion-sin-cupo">{motivoCupo}</p>}
    </div>
  );
}

export function ProgramacionDetallePanel(props: Props) {
  const { programacion, cargando, error, errorAccion, puedeEscribir, tituloRef, children } = props;
  return (
    <PanelDetalle ariaLabelledBy="programacion-detail-title" className="clientes-detail-panel capacitaciones-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando programación…</p>}
      {error === '' && !cargando && programacion === null && <p aria-live="polite" className="clientes-status" role="status">Selecciona una programación para ver su detalle.</p>}
      {programacion !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{programacion.cursoCodigo}</div>
              <h2 id="programacion-detail-title" ref={tituloRef} tabIndex={-1}>{programacion.cursoNombre}</h2>
            </div>
            <EstadoProgramacionPastilla estado={programacion.estado} />
          </div>
          <dl className="capacitaciones-datos">
            <div><dt>Fechas</dt><dd>{`${textoFecha(programacion.fechaInicio)} al ${textoFecha(programacion.fechaFin)}`}</dd></div>
            <div><dt>Lugar</dt><dd>{programacion.lugar ?? 'Sin lugar'}</dd></div>
            <div><dt>Horas</dt><dd>{formatearHoras(programacion.horas)}</dd></div>
            <div><dt>Cupo</dt><dd>{`${programacion.inscritos} de ${programacion.cupo} (libres: ${programacion.cupoLibre})`}</dd></div>
          </dl>
          <Acciones cancelar={props.cancelar} enCurso={props.enCurso} inscribir={props.inscribir} programacion={programacion} puedeEscribir={puedeEscribir} realizar={props.realizar} />
          {!puedeEscribir && <p className="capacitaciones-aviso" id="programacion-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
          {errorAccion !== '' && <p aria-live="polite" className="clientes-error" role="alert">{errorAccion}</p>}
          {children}
        </>
      )}
    </PanelDetalle>
  );
}
