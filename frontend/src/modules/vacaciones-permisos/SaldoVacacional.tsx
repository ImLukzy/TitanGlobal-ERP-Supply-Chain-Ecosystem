import { formatearFecha } from '../../lib/formato';
import { formatearDecimal } from './presentacion';
import type { SaldoVacacional as DatosSaldo } from './tipos';

interface Props {
  saldo: DatosSaldo | null;
  cargando: boolean;
  error: string;
  diasSolicitud?: number | null;
}

// Saldo vacacional del empleado a una fecha: devengado, gozado, programado y saldo, con los días que pide la solicitud si se conocen.
export function SaldoVacacional({ saldo, cargando, error, diasSolicitud = null }: Props) {
  if (error !== '') {
    return <p className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando || saldo === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando saldo vacacional…</p>;
  }
  return (
    <div aria-label="Saldo vacacional" className="vacaciones-permisos-saldo" role="group">
      <p className="vacaciones-permisos-aviso">{`Saldo a la fecha ${formatearFecha(saldo.fecha)}`}</p>
      <dl className="vacaciones-permisos-saldo-grid">
        <div>
          <dt>Días devengados</dt>
          <dd>{formatearDecimal(saldo.diasDevengados)}</dd>
        </div>
        <div>
          <dt>Días gozados</dt>
          <dd>{formatearDecimal(saldo.diasGozados)}</dd>
        </div>
        <div>
          <dt>Días programados</dt>
          <dd>{formatearDecimal(saldo.diasProgramados)}</dd>
        </div>
        <div>
          <dt>Saldo</dt>
          <dd>{formatearDecimal(saldo.saldo)}</dd>
        </div>
      </dl>
      {diasSolicitud !== null && (
        <p className={diasSolicitud > saldo.saldo ? 'vacaciones-permisos-saldo-excedido' : 'vacaciones-permisos-aviso'}>
          {`La solicitud pide ${diasSolicitud} ${diasSolicitud === 1 ? 'día' : 'días'}.`}
        </p>
      )}
    </div>
  );
}
