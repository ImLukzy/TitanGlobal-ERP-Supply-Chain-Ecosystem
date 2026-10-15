import { formateadorMoneda } from '../../lib/formato';
import { nombreCondicionPago } from './presentacion';
import type { ContratosResumen as Resumen } from './tipos';

interface Props { datos: Resumen | null; error: string; }

export function ContratosResumen({ datos, error }: Props) {
  if (error) return <p className="clientes-error" role="alert">{error}</p>;
  if (datos === null) return <p role="status">Cargando resumen de contratos…</p>;
  const compromiso = formateadorMoneda.format(datos.montoComprometidoVigente);
  const consumo = formateadorMoneda.format(datos.montoConsumidoVigente);
  const porcentaje = datos.porcentajeConsumoVigente;
  const texto = `${consumo} de ${compromiso} · ${porcentaje.toFixed(2)} %`;
  return (
    <section aria-label="Resumen de contratos comerciales" className="contratos-summary">
      <article><span>Borradores</span><strong>{datos.borradores}</strong></article>
      <article><span>Vigentes</span><strong>{datos.vigentes}</strong></article>
      <article><span>Vencidos</span><strong>{datos.vencidos}</strong></article>
      <article><span>Finalizados</span><strong>{datos.finalizados}</strong></article>
      <article><span>Cancelados</span><strong>{datos.cancelados}</strong></article>
      <article className="contratos-consumption-summary">
        <span>Consumo vigente</span>
        <div aria-label="Consumo de contratos vigentes" aria-valuemax={100} aria-valuemin={0} aria-valuenow={Math.min(100, Math.max(0, porcentaje))} aria-valuetext={texto} className="zonas-meter" role="meter">
          <span style={{ width: `${Math.min(100, Math.max(0, porcentaje))}%` }} />
        </div>
        <strong>{texto}{porcentaje > 100 ? ' · Excedido' : ''}</strong>
      </article>
      <section className="solicitudes-top-panel contratos-terms-summary">
        <h2>Contratos por condición de pago</h2>
        <ol>{datos.porCondicion.map((item) => <li key={item.condicionPago}>{nombreCondicionPago(item.condicionPago)}<strong>{item.contratos} {item.contratos === 1 ? 'contrato' : 'contratos'}</strong></li>)}</ol>
      </section>
    </section>
  );
}
