import { Link } from 'react-router';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFechaHoraUtc, textoAccion } from './presentacion';
import type { RegistroDetalle } from './tipos';

interface Props {
  registro: RegistroDetalle | null;
  error: string;
  cargando: boolean;
  verHistorial: (entidad: string, entidadId: string) => void;
}

function valorCambio(valor: string | null): string {
  return valor === null ? '—' : valor;
}

export function AuditoriaDetallePanel({ registro, error, cargando, verHistorial }: Props) {
  return <PanelDetalle ariaLabelledBy="auditoria-detail-title" className="solicitudes-panel auditoria-detail">
    {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
    {error === '' && cargando && <p role="status">Cargando detalle del registro…</p>}
    {error === '' && !cargando && registro === null && <p role="status">Selecciona un registro para ver el detalle.</p>}
    {error === '' && registro !== null && <>
      <div className="solicitudes-panel-heading"><div><span className="page-eyebrow">DETALLE DE AUDITORÍA</span><h2 id="auditoria-detail-title">{textoAccion(registro.accion)} · {registro.entidad} #{registro.entidadId}</h2></div><span className={`auditoria-accion auditoria-accion-${registro.accion.toLowerCase()}`}>{textoAccion(registro.accion)}</span></div>
      <dl className="auditoria-detail-grid">
        <div><dt>Fecha y hora (UTC)</dt><dd>{formatearFechaHoraUtc(registro.fecha)}</dd></div>
        <div><dt>Usuario</dt><dd>{registro.usuario}</dd></div>
        <div><dt>Ruta</dt><dd className="auditoria-value">{registro.ruta ?? '—'}</dd></div>
        <div><dt>Campos cambiados</dt><dd>{registro.camposCambiados}</dd></div>
      </dl>
      <section aria-labelledby="auditoria-cambios-title" className="auditoria-cambios">
        <h3 id="auditoria-cambios-title">Cambios</h3>
        {registro.cambios.length === 0 ? <p>Este registro no contiene cambios detallados.</p> : <div className="auditoria-cambios-scroll"><table className="tabla-paginada auditoria-cambios-table"><caption>Cambios del registro de auditoría</caption><thead><tr><th scope="col">Campo</th><th scope="col">Antes</th><th scope="col">Después</th></tr></thead><tbody>{registro.cambios.map((cambio) => <tr key={cambio.campo}><th scope="row">{cambio.campo}</th><td className="auditoria-value">{valorCambio(cambio.antes)}</td><td className="auditoria-value">{valorCambio(cambio.despues)}</td></tr>)}</tbody></table></div>}
      </section>
      <Link className="auditoria-history-link" onClick={() => verHistorial(registro.entidad, registro.entidadId)} to={`/modulos/77-auditoria-transaccional?entidad=${encodeURIComponent(registro.entidad)}&entidadId=${encodeURIComponent(registro.entidadId)}`}>Ver historial de este registro</Link>
    </>}
  </PanelDetalle>;
}
