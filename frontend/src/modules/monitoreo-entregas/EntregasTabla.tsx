import { TablaPaginada } from '../../components/TablaPaginada';
import { fechaUtc, etiquetaEstado } from './presentacion';
import type { PaginaEntregas } from './tipos';
export function EntregasTabla({
  datos,
  seleccion,
  pagina,
  cambiar,
  seleccionar,
}: {
  datos: PaginaEntregas;
  seleccion: number | null;
  pagina: number;
  cambiar: (p: number) => void;
  seleccionar: (id: number) => void;
}) {
  return (
    <TablaPaginada
      caption="Seguimiento manual de despachos"
      etiquetaRegion="Listado de entregas"
      className="monitoreo-entregas-table"
      columnas={['Código', 'Guía / destinatario', 'Programada UTC', 'Estado', 'Detalle'].map(
        (etiqueta) => ({ id: etiqueta, etiqueta }),
      )}
      elementos={datos.items}
      obtenerClave={(e) => e.id}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(datos.total / 10))}
      cambiarPagina={cambiar}
      renderizarFila={(e) => (
        <tr className={seleccion === e.id ? 'monitoreo-entregas-selected' : ''}>
          <th scope="row">{e.codigo}</th>
          <td>
            {e.referenciaGuia}
            <small>{e.destinatario}</small>
          </td>
          <td>{fechaUtc(e.fechaProgramada)}</td>
          <td>
            <span
              className={`monitoreo-entregas-badge monitoreo-entregas-${e.estado.toLowerCase()}`}
            >
              {etiquetaEstado(e.estado)}
            </span>
          </td>
          <td>
            <button
              type="button"
              className="secondary-button"
              aria-label={`Ver entrega ${e.codigo}`}
              onClick={() => seleccionar(e.id)}
            >
              Ver detalle
            </button>
          </td>
        </tr>
      )}
    />
  );
}
