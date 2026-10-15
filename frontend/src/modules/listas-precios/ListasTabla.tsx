import { TablaPaginada } from "../../components/TablaPaginada";
import { formatearFecha } from "../../lib/formato";
import type { ListaListado } from "./tipos";

interface Props {
  items: ListaListado[];
  total: number;
  pagina: number;
  cargando: boolean;
  error: string;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}
export function ListasTabla({
  items,
  total,
  pagina,
  cargando,
  error,
  seleccionadoId,
  seleccionar,
  cambiarPagina,
}: Props) {
  if (error)
    return (
      <p className="clientes-error" role="alert">
        {error}
      </p>
    );
  if (cargando) return <p role="status">Cargando listas de precios…</p>;
  if (items.length === 0)
    return <p role="status">No hay listas que coincidan con los filtros.</p>;
  return (
    <TablaPaginada
      caption="Listas de precios. La columna Prod. indica la cantidad de productos."
      etiquetaRegion="Tabla de listas de precios"
      columnas={[
        { id: "prioridad", etiqueta: "Prioridad" },
        { id: "codigo", etiqueta: "Código" },
        { id: "nombre", etiqueta: "Nombre" },
        { id: "segmento", etiqueta: "Segmento" },
        { id: "vigencia", etiqueta: "Vigencia" },
        { id: "productos", etiqueta: "Prod." },
        { id: "estado", etiqueta: "Estado" },
      ]}
      elementos={items}
      obtenerClave={(item) => item.id}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(total / 10))}
      cambiarPagina={cambiarPagina}
      className="solicitudes-table listas-table"
      classNameContenedor="solicitudes-table-scroll listas-table-scroll"
      classNamePaginacion="solicitudes-pagination"
      renderizarFila={(item) => (
        <tr className={item.id === seleccionadoId ? "is-selected" : ""}>
          <td>{item.prioridad}</td>
          <th scope="row">
            <button
              className="solicitud-row-select"
              onClick={() => seleccionar(item.id)}
              type="button"
            >
              {item.codigo}
            </button>
          </th>
          <td>{item.nombre}</td>
          <td>{item.segmentoCodigo ?? "General"}</td>
          <td className="listas-nowrap">
            {formatearFecha(item.fechaInicio)}
            {item.fechaFin
              ? ` – ${formatearFecha(item.fechaFin)}`
              : " en adelante"}
          </td>
          <td>{item.productos}</td>
          <td>
            <span
              className={`contrato-status ${item.vigente && item.activa ? "is-vigente" : item.activa ? "is-finalizado" : "is-cancelado"}`}
            >
              {!item.activa
                ? "Inactiva"
                : item.vigente
                  ? "Vigente"
                  : "No vigente"}
            </span>
          </td>
        </tr>
      )}
    />
  );
}
