import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { EstadoIncidentePastilla, SeveridadPastilla } from './Pastillas';
import { textoFecha, textoValor, TEXTO_SOLO_RRHH } from './presentacion';
import type { AccionCorrectiva, IncidenteDetalle } from './tipos';

interface Props {
  incidente: IncidenteDetalle | null;
  cargando: boolean;
  error: string;
  errorAccion: string;
  puedeEscribir: boolean;
  enCurso: boolean;
  agregarAccion: () => void;
  completarAccion: (accion: AccionCorrectiva) => void;
  cerrar: () => void;
  tituloRef: RefObject<HTMLHeadingElement | null>;
}

// Texto visible que explica por qué el cierre está deshabilitado (mensajes 31 y 32).
export function motivoCierreBloqueado(incidente: IncidenteDetalle): string {
  if (incidente.acciones.length === 0) {
    return 'Registra al menos una acción correctiva para poder cerrar el incidente.';
  }
  const pendientes = incidente.acciones.filter((accion) => !accion.completada).length;
  if (pendientes > 0) {
    return `Hay ${pendientes} ${pendientes === 1 ? 'acción correctiva sin completar' : 'acciones correctivas sin completar'}.`;
  }
  return '';
}

function AccionesTabla({ incidente, puedeEscribir, enCurso, completarAccion }: Pick<Props, 'puedeEscribir' | 'enCurso' | 'completarAccion'> & { incidente: IncidenteDetalle }) {
  if (incidente.acciones.length === 0) {
    return <p className="clientes-status">Sin acciones correctivas registradas.</p>;
  }
  return (
    <div className="sst-scroll">
      <table className="sst-mini-tabla">
        <caption>Acciones correctivas</caption>
        <thead>
          <tr>
            <th scope="col">Descripción</th>
            <th scope="col">Responsable</th>
            <th scope="col">Compromiso</th>
            <th scope="col">Completada</th>
          </tr>
        </thead>
        <tbody>
          {incidente.acciones.map((accion) => (
            <tr key={accion.id}>
              <td>{accion.descripcion}</td>
              <td>{accion.responsable}</td>
              <td className="sst-numero">{textoFecha(accion.fechaCompromiso)}</td>
              <td>
                {accion.completada
                  ? <span className="sst-pastilla is-vigente">{`Completada ${textoFecha(accion.fechaCompletada)}`}</span>
                  : (
                    <button
                      aria-label={`Completar acción ${accion.descripcion}`}
                      className="secondary-button sst-fila-boton"
                      disabled={!puedeEscribir || enCurso || incidente.estado === 'Cerrado'}
                      onClick={() => completarAccion(accion)}
                      type="button"
                    >
                      Completar
                    </button>
                  )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function IncidenteDetallePanel({ incidente, cargando, error, errorAccion, puedeEscribir, enCurso, agregarAccion, completarAccion, cerrar, tituloRef }: Props) {
  const describedBy = puedeEscribir ? undefined : 'incidente-solo-rrhh';
  const abierto = incidente !== null && incidente.estado === 'Abierto';
  const motivo = incidente !== null && abierto ? motivoCierreBloqueado(incidente) : '';
  return (
    <PanelDetalle ariaLabelledBy="incidente-detail-title" className="clientes-detail-panel sst-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando incidente…</p>}
      {error === '' && !cargando && incidente === null && <p aria-live="polite" className="clientes-status" role="status">Selecciona un incidente para ver su detalle.</p>}
      {incidente !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{incidente.numero}</div>
              <h2 id="incidente-detail-title" ref={tituloRef} tabIndex={-1}>{`Incidente ${incidente.numero}`}</h2>
            </div>
            <EstadoIncidentePastilla estado={incidente.estado} />
          </div>
          <dl className="sst-datos">
            <div><dt>Empleado</dt><dd>{incidente.empleado}</dd></div>
            <div><dt>Fecha</dt><dd>{textoFecha(incidente.fecha)}</dd></div>
            <div><dt>Tipo</dt><dd>{textoValor(incidente.tipo)}</dd></div>
            <div><dt>Severidad</dt><dd><SeveridadPastilla severidad={incidente.severidad} /></dd></div>
            <div><dt>Días de baja</dt><dd>{incidente.diasBaja}</dd></div>
            <div><dt>Lugar</dt><dd>{incidente.lugar ?? 'Sin lugar'}</dd></div>
            {incidente.fechaCierre !== null && <div><dt>Cierre</dt><dd>{textoFecha(incidente.fechaCierre)}</dd></div>}
            <div className="sst-dato-ancho"><dt>Descripción</dt><dd>{incidente.descripcion}</dd></div>
          </dl>
          <div className="sst-section">
            <h3>Acciones correctivas</h3>
            <AccionesTabla completarAccion={completarAccion} enCurso={enCurso} incidente={incidente} puedeEscribir={puedeEscribir} />
          </div>
          {abierto && (
            <div className="clientes-detail-actions sst-actions">
              <button aria-describedby={describedBy} className="secondary-button" disabled={!puedeEscribir || enCurso} onClick={agregarAccion} type="button">Agregar acción</button>
              <button aria-describedby={describedBy ?? (motivo === '' ? undefined : 'incidente-motivo-cierre')} className="primary-button" disabled={!puedeEscribir || enCurso || motivo !== ''} onClick={cerrar} type="button">Cerrar incidente</button>
            </div>
          )}
          {!puedeEscribir && <p className="sst-aviso" id="incidente-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
          {puedeEscribir && motivo !== '' && <p className="sst-aviso" id="incidente-motivo-cierre">{motivo}</p>}
          {errorAccion !== '' && <p aria-live="polite" className="clientes-error" role="alert">{errorAccion}</p>}
        </>
      )}
    </PanelDetalle>
  );
}
