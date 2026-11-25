import { PanelDetalle } from "../../components/PanelDetalle";
import { fechaUtc } from "./presentacion";
import type { CargaDetalle } from "./tipos";
export function ConsolidacionDetallePanel({
  datos,
  ocupado,
  editar,
  eliminar,
  confirmar,
  cancelar,
}: {
  datos: CargaDetalle;
  ocupado: boolean;
  editar: () => void;
  eliminar: () => void;
  confirmar: () => void;
  cancelar: () => void;
}) {
  return (
    <PanelDetalle
      ariaLabelledBy="carga-detalle-title"
      className="consolidacion-carga-card"
    >
      <p className="page-eyebrow">{datos.codigo}</p>
      <h2 id="carga-detalle-title">{datos.nombre}</h2>
      <p>
        Estado:{" "}
        <strong
          className={`consolidacion-carga-badge consolidacion-carga-${datos.estado.toLowerCase()}`}
        >
          {datos.estado}
        </strong>
      </p>
      <p>
        Picking: {datos.unidadesRecogidas} / {datos.totalUnidades} unidades
      </p>
      <p className="consolidacion-carga-totales">
        {`Estimación: ${datos.totalBultos}\u00a0bultos · ${datos.volumenM3}\u00a0m³ · ${datos.pesoKg}\u00a0kg`}
      </p>
      <p>
        Dimensiones y peso estimados por bulto completo, incluido el último
        parcial.
      </p>
      <div
        className="tabla-paginada-scroll"
        role="region"
        aria-label="Líneas y packing de la carga"
        tabIndex={0}
      >
        <table className="tabla-paginada consolidacion-carga-table">
          <caption>Picking y packing</caption>
          <thead>
            <tr>
              {[
                "SKU / descripción",
                "Recogido / previsto",
                "Unidades por bulto",
                "Dimensiones cm",
                "Kg por bulto",
                "Bultos",
                "m³",
                "kg",
              ].map((t) => (
                <th key={t} scope="col">
                  {t}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {datos.lineas.map((l) => (
              <tr key={l.id}>
                <th scope="row">
                  {l.sku}
                  <small>{l.descripcion}</small>
                </th>
                <td>
                  {l.cantidadRecogida} / {l.cantidad}
                </td>
                <td>{l.unidadesPorBulto}</td>
                <td>
                  {l.largoCm} × {l.anchoCm} × {l.altoCm}
                </td>
                <td>{l.pesoBultoKg}</td>
                <td>{l.bultos}</td>
                <td>{l.volumenM3}</td>
                <td>{l.pesoKg}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <dl className="consolidacion-carga-details">
        <dt>Observaciones</dt>
        <dd>{datos.observaciones || "—"}</dd>
        {datos.fechaConfirmacion && (
          <>
            <dt>Confirmada (UTC)</dt>
            <dd>{fechaUtc(datos.fechaConfirmacion)}</dd>
          </>
        )}
        {datos.fechaCancelacion && (
          <>
            <dt>Cancelada (UTC)</dt>
            <dd>{fechaUtc(datos.fechaCancelacion)}</dd>
            <dt>Motivo de cancelación</dt>
            <dd>{datos.motivoCancelacion}</dd>
          </>
        )}
      </dl>
      <div className="consolidacion-carga-actions">
        {datos.estado === "Borrador" && (
          <>
            <button
              className="secondary-button"
              type="button"
              disabled={ocupado}
              onClick={editar}
            >
              Editar carga
            </button>
            <button
              className="primary-button"
              type="button"
              disabled={ocupado}
              onClick={confirmar}
            >
              Confirmar carga
            </button>
            <button
              className="danger-button"
              type="button"
              disabled={ocupado}
              onClick={eliminar}
            >
              Eliminar carga
            </button>
          </>
        )}
        {datos.estado !== "Cancelada" && (
          <button
            className="secondary-button"
            type="button"
            disabled={ocupado}
            onClick={cancelar}
          >
            Cancelar carga
          </button>
        )}
      </div>
    </PanelDetalle>
  );
}
