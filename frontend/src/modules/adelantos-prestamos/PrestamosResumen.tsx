import { formateadorMoneda } from '../../lib/formato';
import type { ResumenPrestamos } from './tipos';

interface Props {
  datos: ResumenPrestamos | null;
  error: string;
}

export function PrestamosResumen({ datos, error }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen de préstamos…</p>;
  }
  return (
    <section aria-label="Resumen de préstamos" className="adelantos-prestamos-summary">
      <article className="cliente-stat">
        <span>Préstamos activos</span>
        <strong>{datos.activos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Saldo pendiente</span>
        <strong>{formateadorMoneda.format(datos.saldoPendienteTotal)}</strong>
      </article>
      <article className="cliente-stat">
        <span>Otorgado este mes</span>
        <strong>{formateadorMoneda.format(datos.otorgadoMes)}</strong>
      </article>
      <article className="cliente-stat">
        <span>Cuotas del mes</span>
        <strong>{formateadorMoneda.format(datos.cuotasDelMes)}</strong>
      </article>
    </section>
  );
}
