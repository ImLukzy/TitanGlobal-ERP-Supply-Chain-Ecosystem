import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha } from '../../lib/formato';
import { Link } from 'react-router';
import { claseSeveridad, nombreTipo, rutaAlerta } from './presentacion';
import type { Alerta } from './tipos';

interface Props {
  alerta: Alerta | null;
}

export function AlertaDetallePanel({ alerta }: Props) {
  return (
    <PanelDetalle ariaLabelledBy="alerta-detail-title" className="solicitudes-panel alertas-detail">
      {alerta === null ? (
        <p role="status">Selecciona una alerta para ver el detalle.</p>
      ) : (
        <>
          <div className="solicitudes-panel-heading">
            <div>
              <span className="page-eyebrow">DETALLE DE ALERTA</span>
              <h2 id="alerta-detail-title">{nombreTipo(alerta.tipo)}</h2>
            </div>
            <span className={claseSeveridad(alerta.severidad)}>{alerta.severidad}</span>
          </div>
          <p className="alertas-detail-message">{alerta.mensaje}</p>
          <dl className="alertas-detail-grid">
            <div><dt>Tipo</dt><dd>{nombreTipo(alerta.tipo)}</dd></div>
            <div><dt>Severidad</dt><dd>{alerta.severidad}</dd></div>
            <div><dt>Módulo de origen</dt><dd>{alerta.modulo}</dd></div>
            <div><dt>Registro</dt><dd>{alerta.registroId}</dd></div>
            <div><dt>Fecha de referencia</dt><dd>{formatearFecha(alerta.fechaReferencia)}</dd></div>
            <div><dt>Estado</dt><dd>{alerta.descartada ? 'Descartada' : 'Pendiente'}</dd></div>
          </dl>
          <p className="alertas-detail-link"><Link to={rutaAlerta(alerta)}>Ver registro relacionado</Link></p>
        </>
      )}
    </PanelDetalle>
  );
}
