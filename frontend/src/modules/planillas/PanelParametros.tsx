import { formateadorMoneda } from '../../lib/formato';
import { useParametros } from './usePlanillasData';

const formateadorNumero = new Intl.NumberFormat('es-PE', { maximumFractionDigits: 4 });

function porcentaje(tasa: number): string {
  return `${formateadorNumero.format(tasa * 100)} %`;
}

// Panel de solo lectura: los parámetros legales se cambian por script SQL, no desde la pantalla.
export function PanelParametros({ cerrar }: { cerrar: () => void }) {
  const { parametros, error } = useParametros();
  return (
    <section aria-labelledby="planilla-parametros-title" className="clientes-form-panel planillas-parametros-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">SOLO LECTURA</div>
          <h2 id="planilla-parametros-title">Parámetros de planilla</h2>
        </div>
        <button className="secondary-button" onClick={cerrar} type="button">Cerrar</button>
      </div>
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && parametros === null && <p aria-live="polite" className="clientes-status" role="status">Cargando parámetros…</p>}
      {parametros !== null && (
        <div className="planillas-parametros-grid">
          <div className="planillas-scroll" role="region" aria-label="Tabla de parámetros generales" tabIndex={0}>
            <table className="planillas-mini-tabla">
              <caption>Parámetros generales</caption>
              <thead><tr><th scope="col">Clave</th><th scope="col">Valor</th><th scope="col">Descripción</th></tr></thead>
              <tbody>
                {parametros.parametros.map((parametro) => (
                  <tr key={parametro.clave}>
                    <td>{parametro.clave}</td>
                    <td className="planillas-numero">{formateadorNumero.format(parametro.valor)}</td>
                    <td>{parametro.descripcion}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="planillas-scroll" role="region" aria-label="Tabla de sistemas de pensiones" tabIndex={0}>
            <table className="planillas-mini-tabla">
              <caption>Sistemas de pensiones</caption>
              <thead>
                <tr>
                  <th scope="col">Código</th><th scope="col">Sistema</th><th scope="col">Aporte</th>
                  <th scope="col">Prima</th><th scope="col">Comisión</th><th scope="col">Tope asegurable</th>
                </tr>
              </thead>
              <tbody>
                {parametros.pensiones.map((pension) => (
                  <tr key={pension.codigo}>
                    <td>{pension.codigo}</td>
                    <td>{pension.sistema}</td>
                    <td className="planillas-numero">{porcentaje(pension.tasaAporte)}</td>
                    <td className="planillas-numero">{porcentaje(pension.tasaPrimaSeguro)}</td>
                    <td className="planillas-numero">{porcentaje(pension.tasaComision)}</td>
                    <td className="planillas-numero">
                      {pension.remuneracionMaximaAsegurable === null ? '—' : formateadorMoneda.format(pension.remuneracionMaximaAsegurable)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="planillas-scroll" role="region" aria-label="Tabla de tramos de quinta categoría" tabIndex={0}>
            <table className="planillas-mini-tabla">
              <caption>Tramos de quinta categoría</caption>
              <thead><tr><th scope="col">Orden</th><th scope="col">Hasta (UIT)</th><th scope="col">Tasa</th></tr></thead>
              <tbody>
                {parametros.tramos.map((tramo) => (
                  <tr key={tramo.orden}>
                    <td className="planillas-numero">{tramo.orden}</td>
                    <td className="planillas-numero">{tramo.hastaUit === null ? 'En adelante' : formateadorNumero.format(tramo.hastaUit)}</td>
                    <td className="planillas-numero">{porcentaje(tramo.tasa)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  );
}
