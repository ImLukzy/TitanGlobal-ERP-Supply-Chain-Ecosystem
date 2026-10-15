import { PanelDetalle } from "../../components/PanelDetalle";
import { formateadorMoneda, formatearFecha } from "../../lib/formato";
import type { ListaDetalle } from "./tipos";

interface Props {
  lista: ListaDetalle | null;
  cargando: boolean;
  error: string;
  editar: () => void;
  eliminar: () => void;
  guardarBotonBaja: (element: HTMLButtonElement | null) => void;
}

function estadoLista(lista: ListaDetalle): string {
  if (!lista.activa) return "Inactiva";
  return lista.vigente ? "Vigente" : "No vigente";
}

function claseEstado(lista: ListaDetalle): string {
  if (!lista.activa) return "is-cancelado";
  return lista.vigente ? "is-vigente" : "is-finalizado";
}

function formatearDiferencia(valor: number): string {
  const signo = valor < 0 ? "−" : "";
  return `${signo}${Math.abs(valor).toFixed(2)} %`;
}

export function ListaDetallePanel({
  lista,
  cargando,
  error,
  editar,
  eliminar,
  guardarBotonBaja,
}: Props) {
  return (
    <PanelDetalle
      ariaLabelledBy="lista-detail-title"
      className="solicitudes-panel contratos-detail listas-detail"
    >
      {error && (
        <p className="clientes-error" role="alert">
          {error}
        </p>
      )}
      {!error && cargando && <p role="status">Cargando detalle…</p>}
      {!error && !cargando && lista === null && (
        <p role="status">Selecciona una lista para ver sus precios.</p>
      )}
      {!error && !cargando && lista !== null && (
        <>
          <div className="solicitudes-panel-heading">
            <div>
              <span className="page-eyebrow">DETALLE DE LISTA</span>
              <h2 id="lista-detail-title">{lista.codigo}</h2>
            </div>
            <span className={`contrato-status ${claseEstado(lista)}`}>
              {estadoLista(lista)}
            </span>
          </div>
          <div className="panel-detail-actions">
            <button className="secondary-button" onClick={editar} type="button">
              Editar
            </button>
            <button
              className="danger-button"
              onClick={eliminar}
              ref={guardarBotonBaja}
              type="button"
            >
              Eliminar
            </button>
          </div>
          <h3>{lista.nombre}</h3>
          <dl className="contrato-detail-grid listas-detail-grid">
            <div>
              <dt>Segmento</dt>
              <dd>{lista.segmentoCodigo ?? "General"}</dd>
            </div>
            <div>
              <dt>Prioridad</dt>
              <dd>{lista.prioridad}</dd>
            </div>
            <div>
              <dt>Vigencia</dt>
              <dd>
                {formatearFecha(lista.fechaInicio)}
                {lista.fechaFin
                  ? ` – ${formatearFecha(lista.fechaFin)}`
                  : " en adelante"}
              </dd>
            </div>
            <div>
              <dt>Estado</dt>
              <dd>{estadoLista(lista)}</dd>
            </div>
          </dl>
          <div
            aria-label="Precios de la lista"
            className="tabla-paginada-scroll listas-detail-scroll"
            role="region"
            tabIndex={0}
          >
            <table className="tabla-paginada listas-detail-table">
              <caption>Precios por producto</caption>
              <thead>
                <tr>
                  <th scope="col">Producto</th>
                  <th scope="col">Precio base</th>
                  <th scope="col">Precio de lista</th>
                  <th scope="col">Diferencia</th>
                </tr>
              </thead>
              <tbody>
                {lista.items.map((item) => (
                  <tr key={item.productoId}>
                    <th scope="row">{item.productoNombre}</th>
                    <td className="listas-nowrap">
                      {formateadorMoneda.format(item.precioBase)}
                    </td>
                    <td className="listas-nowrap">
                      {formateadorMoneda.format(item.precio)}
                    </td>
                    <td className="listas-nowrap">
                      {formatearDiferencia(item.diferenciaPorcentaje)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </PanelDetalle>
  );
}
