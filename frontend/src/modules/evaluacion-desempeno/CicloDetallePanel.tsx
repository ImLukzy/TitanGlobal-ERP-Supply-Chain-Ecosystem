import type { ReactNode, RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { EstadoCicloPastilla } from './CiclosTabla';
import { TEXTO_SOLO_RRHH, textoFecha } from './presentacion';
import type { CicloDetalle } from './tipos';
import { formatearDecimal } from './validacionEvaluacion';

interface Props {
  ciclo: CicloDetalle | null;
  cargando: boolean;
  error: string;
  puedeEscribir: boolean;
  abriendo: boolean;
  errorAccion: string;
  editar: () => void;
  abrir: () => void;
  cerrar: () => void;
  tituloRef: RefObject<HTMLHeadingElement | null>;
  children: ReactNode;
}

function Acciones({ ciclo, puedeEscribir, abriendo, editar, abrir, cerrar }: Pick<Props, 'puedeEscribir' | 'abriendo' | 'editar' | 'abrir' | 'cerrar'> & { ciclo: CicloDetalle }) {
  const sumaCompleta = Math.round(ciclo.sumaPesos * 100) === 10000;
  const idMotivo = 'ciclo-abrir-motivo';
  const describedBy = puedeEscribir ? undefined : 'ciclo-solo-rrhh';
  return (
    <div className="clientes-detail-actions evaluacion-desempeno-actions">
      {ciclo.estado === 'Configuracion' && (
        <>
          <button aria-describedby={describedBy} className="secondary-button" disabled={!puedeEscribir} onClick={editar} type="button">
            Editar ciclo
          </button>
          <button
            aria-describedby={sumaCompleta ? describedBy : idMotivo}
            className="primary-button"
            disabled={!puedeEscribir || !sumaCompleta || abriendo}
            onClick={abrir}
            type="button"
          >
            {abriendo ? 'Abriendo…' : 'Abrir ciclo'}
          </button>
          {!sumaCompleta && (
            <p className="evaluacion-desempeno-aviso" id={idMotivo}>
              {`Los pesos suman ${formatearDecimal(ciclo.sumaPesos)} de 100: ajusta las competencias para poder abrir el ciclo.`}
            </p>
          )}
        </>
      )}
      {ciclo.estado === 'Abierto' && (
        <button aria-describedby={describedBy} className="primary-button" disabled={!puedeEscribir} onClick={cerrar} type="button">
          Cerrar ciclo
        </button>
      )}
    </div>
  );
}

export function CicloDetallePanel(props: Props) {
  const { ciclo, cargando, error, puedeEscribir, errorAccion, tituloRef, children } = props;
  return (
    <PanelDetalle ariaLabelledBy="ciclo-detail-title" className="clientes-detail-panel evaluacion-desempeno-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando ciclo…</p>}
      {error === '' && !cargando && ciclo === null && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona un ciclo para ver su detalle.</p>
      )}
      {ciclo !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{ciclo.codigo}</div>
              <h2 id="ciclo-detail-title" ref={tituloRef} tabIndex={-1}>{ciclo.nombre}</h2>
            </div>
            <EstadoCicloPastilla estado={ciclo.estado} />
          </div>
          <dl className="evaluacion-desempeno-datos">
            <div><dt>Periodo</dt><dd>{`${ciclo.periodo}: ${textoFecha(ciclo.fechaInicioPeriodo)} al ${textoFecha(ciclo.fechaFinPeriodo)}`}</dd></div>
            <div><dt>Evaluaciones abiertas</dt><dd>{`${textoFecha(ciclo.fechaApertura)} al ${textoFecha(ciclo.fechaCierre)}`}</dd></div>
            <div><dt>Borradores</dt><dd>{ciclo.evaluaciones.borrador}</dd></div>
            <div><dt>Enviadas</dt><dd>{ciclo.evaluaciones.enviada}</dd></div>
            <div><dt>Cerradas</dt><dd>{ciclo.evaluaciones.cerrada}</dd></div>
          </dl>
          <Acciones abrir={props.abrir} abriendo={props.abriendo} cerrar={props.cerrar} ciclo={ciclo} editar={props.editar} puedeEscribir={puedeEscribir} />
          {!puedeEscribir && <p className="evaluacion-desempeno-aviso" id="ciclo-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
          {errorAccion !== '' && <p aria-live="polite" className="clientes-error" role="alert">{errorAccion}</p>}
          {children}
        </>
      )}
    </PanelDetalle>
  );
}
