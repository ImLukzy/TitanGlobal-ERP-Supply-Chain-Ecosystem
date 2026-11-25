import type { ReactNode, RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha, formateadorMoneda } from '../../lib/formato';
import { TEXTO_SOLO_RRHH, textoPeriodo } from './presentacion';
import { EstadoPastilla } from './PlanillasTabla';
import type { PlanillaDetalle } from './tipos';

interface Props {
  planilla: PlanillaDetalle | null;
  cargando: boolean;
  error: string;
  puedeEscribir: boolean;
  calculando: boolean;
  resultadoCalculo: string;
  errorAccion: string;
  calcular: () => void;
  aprobar: () => void;
  pagar: () => void;
  tituloRef: RefObject<HTMLHeadingElement | null>;
  children: ReactNode;
}

function fechaHora(valor: string | null): string {
  return valor === null ? 'Sin dato' : formatearFecha(valor);
}

function Acciones({ planilla, puedeEscribir, calculando, calcular, aprobar, pagar }: Pick<Props, 'puedeEscribir' | 'calculando' | 'calcular' | 'aprobar' | 'pagar'> & { planilla: PlanillaDetalle }) {
  const puedeCalcular = planilla.estado === 'Borrador' || planilla.estado === 'Calculada';
  const describedBy = puedeEscribir ? undefined : 'planilla-solo-rrhh';
  return (
    <div className="clientes-detail-actions planillas-actions">
      {puedeCalcular && (
        <button aria-describedby={describedBy} className="secondary-button" disabled={!puedeEscribir || calculando} onClick={calcular} type="button">
          {calculando ? 'Calculando…' : planilla.estado === 'Borrador' ? 'Calcular' : 'Recalcular'}
        </button>
      )}
      {planilla.estado === 'Calculada' && (
        <button aria-describedby={describedBy} className="primary-button" disabled={!puedeEscribir || calculando} onClick={aprobar} type="button">
          Aprobar
        </button>
      )}
      {planilla.estado === 'Aprobada' && (
        <button aria-describedby={describedBy} className="primary-button" disabled={!puedeEscribir} onClick={pagar} type="button">
          Registrar pago
        </button>
      )}
    </div>
  );
}

export function PlanillaDetallePanel(props: Props) {
  const { planilla, cargando, error, puedeEscribir, resultadoCalculo, errorAccion, tituloRef, children } = props;
  return (
    <PanelDetalle ariaLabelledBy="planilla-detail-title" className="clientes-detail-panel planillas-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando planilla…</p>}
      {error === '' && !cargando && planilla === null && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona una planilla para ver su detalle.</p>
      )}
      {planilla !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{planilla.numero}</div>
              <h2 id="planilla-detail-title" ref={tituloRef} tabIndex={-1}>{`Planilla ${textoPeriodo(planilla.mes, planilla.anio)} · ${planilla.tipo}`}</h2>
            </div>
            <EstadoPastilla estado={planilla.estado} />
          </div>
          <dl aria-label="Totales de la planilla" className="planillas-totales">
            <div><dt>Total ingresos</dt><dd>{formateadorMoneda.format(planilla.totalIngresos)}</dd></div>
            <div><dt>Total descuentos</dt><dd>{formateadorMoneda.format(planilla.totalDescuentos)}</dd></div>
            <div><dt>Total neto</dt><dd>{formateadorMoneda.format(planilla.totalNeto)}</dd></div>
            <div><dt>Aportes del empleador</dt><dd>{formateadorMoneda.format(planilla.totalAportesEmpleador)}</dd></div>
            <div><dt>Empleados</dt><dd>{planilla.empleados}</dd></div>
            <div><dt>Incidencias</dt><dd>{planilla.incidencias}</dd></div>
          </dl>
          <dl className="planillas-fechas">
            <div><dt>Calculada</dt><dd>{planilla.calculadoPor === null ? fechaHora(planilla.fechaCalculo) : `${fechaHora(planilla.fechaCalculo)} por ${planilla.calculadoPor}`}</dd></div>
            <div><dt>Aprobada</dt><dd>{planilla.aprobadoPor === null ? fechaHora(planilla.fechaAprobacion) : `${fechaHora(planilla.fechaAprobacion)} por ${planilla.aprobadoPor}`}</dd></div>
            <div><dt>Fecha de pago</dt><dd>{fechaHora(planilla.fechaPago)}</dd></div>
          </dl>
          <Acciones
            aprobar={props.aprobar}
            calcular={props.calcular}
            calculando={props.calculando}
            pagar={props.pagar}
            planilla={planilla}
            puedeEscribir={puedeEscribir}
          />
          {!puedeEscribir && <p className="planillas-aviso" id="planilla-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
          {resultadoCalculo !== '' && <p aria-live="polite" className="planillas-ok" role="status">{resultadoCalculo}</p>}
          {errorAccion !== '' && <p aria-live="polite" className="clientes-error" role="alert">{errorAccion}</p>}
          {children}
        </>
      )}
    </PanelDetalle>
  );
}
