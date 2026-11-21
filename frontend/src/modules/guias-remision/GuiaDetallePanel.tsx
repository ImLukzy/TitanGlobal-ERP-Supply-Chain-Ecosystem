import { PanelDetalle } from '../../components/PanelDetalle';
import { camposCabecera } from './validacion';
import { fechaUtc, numeroGuia } from './presentacion';
import type { GuiaDetalle } from './tipos';
export function GuiaDetallePanel({
  datos,
  ocupado,
  editar,
  eliminar,
  emitir,
  anular,
}: {
  datos: GuiaDetalle;
  ocupado: boolean;
  editar: () => void;
  eliminar: () => void;
  emitir: () => void;
  anular: () => void;
}) {
  return (
    <PanelDetalle ariaLabelledBy="guia-detalle-title" className="guias-remision-card">
      <p className="page-eyebrow">Documento interno de transporte</p>
      <h2 id="guia-detalle-title">{numeroGuia(datos.serie, datos.numero)}</h2>
      <p>
        Estado:{' '}
        <strong className={`guias-remision-badge guias-remision-${datos.estado.toLowerCase()}`}>
          {datos.estado}
        </strong>
      </p>
      <dl className="guias-remision-details">
        <dt>Inicio del traslado (UTC)</dt>
        <dd>{fechaUtc(datos.fechaInicioTraslado)}</dd>
        {camposCabecera.map(([campo, label]) => (
          <div key={campo}>
            <dt>{label}</dt>
            <dd>{datos[campo]}</dd>
          </div>
        ))}
        <dt>Peso bruto declarado</dt>
        <dd>
          {datos.pesoBrutoKg}
          {'\u00a0'}kg
        </dd>
        <dt>Observaciones</dt>
        <dd>{datos.observaciones || '—'}</dd>
        {datos.fechaEmision && (
          <>
            <dt>Emisión interna (UTC)</dt>
            <dd>{fechaUtc(datos.fechaEmision)}</dd>
          </>
        )}
        {datos.fechaAnulacion && (
          <>
            <dt>Anulada (UTC)</dt>
            <dd>{fechaUtc(datos.fechaAnulacion)}</dd>
            <dt>Motivo de anulación</dt>
            <dd>{datos.motivoAnulacion}</dd>
          </>
        )}
      </dl>
      <div
        className="tabla-paginada-scroll"
        role="region"
        aria-label="Mercancías de la guía"
        tabIndex={0}
      >
        <table className="tabla-paginada guias-remision-table">
          <caption>Mercancías declaradas</caption>
          <thead>
            <tr>
              <th scope="col">SKU / descripción</th>
              <th scope="col">Cantidad</th>
              <th scope="col">Unidad</th>
            </tr>
          </thead>
          <tbody>
            {datos.lineas.map((l) => (
              <tr key={l.id}>
                <th scope="row">
                  {l.sku}
                  <small>{l.descripcion}</small>
                </th>
                <td>{l.cantidad}</td>
                <td>{l.unidadMedida}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="guias-remision-actions">
        {datos.estado === 'Borrador' && (
          <>
            <button className="secondary-button" disabled={ocupado} onClick={editar} type="button">
              Editar guía
            </button>
            <button className="primary-button" disabled={ocupado} onClick={emitir} type="button">
              Emitir internamente
            </button>
            <button className="danger-button" disabled={ocupado} onClick={eliminar} type="button">
              Eliminar guía
            </button>
          </>
        )}
        {datos.estado !== 'Anulada' && (
          <button className="secondary-button" disabled={ocupado} onClick={anular} type="button">
            Anular guía
          </button>
        )}
      </div>
    </PanelDetalle>
  );
}
