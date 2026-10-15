import { formatearCantidad } from '../../lib/formato';
import type { ResumenZonas } from './tipos';

const formatoM3 = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const formatoPorcentaje = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

interface Props {
  resumen: ResumenZonas | null;
  error: string;
}

export function ZonasResumen({ resumen, error }: Props) {
  if (error !== '') {
    return <p className="clientes-error" role="alert">{error}</p>;
  }
  if (resumen === null) {
    return <p className="clientes-status" role="status">Cargando resumen de zonas…</p>;
  }
  return (
    <section aria-label="Resumen de zonas" className="zonas-summary">
      <div className="zonas-summary-stats">
        <article className="cliente-stat"><span>Total de zonas</span><strong>{resumen.totalZonas}</strong></article>
        <article className="cliente-stat"><span>Zonas activas</span><strong>{resumen.activas}</strong></article>
      </div>
      <section aria-labelledby="zonas-tipo-title" className="zonas-type-panel">
        <h2 id="zonas-tipo-title">Zonas por tipo</h2>
        <table>
          <thead><tr><th scope="col">Tipo</th><th scope="col">Zonas</th><th scope="col">Capacidad</th></tr></thead>
          <tbody>
            {resumen.porTipo.map((tipo) => (
              <tr key={tipo.tipo}>
                <th scope="row">{tipo.tipo === 'Recepcion' ? 'Recepción' : tipo.tipo}</th>
                <td>{formatearCantidad(tipo.totalZonas, 'zona', 'zonas')}</td>
                <td>{formatoM3.format(tipo.capacidadM3)} m³</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
      <section aria-labelledby="zonas-ocupacion-title" className="zonas-occupancy-panel">
        <h2 id="zonas-ocupacion-title">Ocupación de almacenes</h2>
        {resumen.ocupacion.length === 0 ? (
          <p role="status">No hay almacenes con zonas.</p>
        ) : (
          <ul>
            {resumen.ocupacion.map((item) => {
              const casiLleno = item.porcentajeAsignado >= 90;
              const texto = `${formatoM3.format(item.capacidadAsignadaM3)} de ${formatoM3.format(item.capacidadAlmacenM3)} m³ · ${formatoPorcentaje.format(item.porcentajeAsignado)}%`;
              return (
                <li key={item.almacenId}>
                  <div className="zonas-occupancy-heading">
                    <strong>{item.almacenCodigo}</strong>
                    {casiLleno && <span className="zonas-near-full">Casi lleno</span>}
                  </div>
                  <div
                    aria-label={`Ocupación de ${item.almacenCodigo}`}
                    aria-valuemax={100}
                    aria-valuemin={0}
                    aria-valuenow={item.porcentajeAsignado}
                    aria-valuetext={texto}
                    className={`zonas-meter${casiLleno ? ' is-near-full' : ''}`}
                    role="meter"
                  >
                    <span style={{ width: `${Math.min(100, Math.max(0, item.porcentajeAsignado))}%` }} />
                  </div>
                  <p>{texto}</p>
                </li>
              );
            })}
          </ul>
        )}
      </section>
    </section>
  );
}
