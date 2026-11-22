import { TablaPaginada } from '../../components/TablaPaginada';
import { fechaUtc } from './presentacion';
import type { PaginaSiniestros } from './tipos';
export function SiniestrosTabla({
  datos,
  seleccion,
  pagina,
  cambiar,
  seleccionar,
}: {
  datos: PaginaSiniestros;
  seleccion: number | null;
  pagina: number;
  cambiar: (v: number) => void;
  seleccionar: (id: number) => void;
}) {
  return (
    <TablaPaginada
      caption="Siniestros de flota"
      etiquetaRegion="Listado de siniestros"
      className="siniestros-flota-table"
      columnas={['Código', 'Placa', 'Tipo / lugar', 'Incidente UTC', 'Estado', 'Detalle'].map(
        (etiqueta) => ({ id: etiqueta, etiqueta }),
      )}
      elementos={datos.items}
      obtenerClave={(s) => s.id}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(datos.total / 10))}
      cambiarPagina={cambiar}
      renderizarFila={(s) => (
        <tr className={seleccion === s.id ? 'siniestros-flota-selected' : ''}>
          <th scope="row">{s.codigo}</th>
          <td>{s.placa}</td>
          <td>
            {s.tipo}
            <small>{s.lugar}</small>
          </td>
          <td>{fechaUtc(s.fechaIncidente)}</td>
          <td>
            <span className={`siniestros-flota-badge siniestros-flota-${s.estado.toLowerCase()}`}>
              {s.estado}
            </span>
          </td>
          <td>
            <button
              type="button"
              className="secondary-button"
              aria-label={`Ver siniestro ${s.codigo}`}
              onClick={() => seleccionar(s.id)}
            >
              Ver detalle
            </button>
          </td>
        </tr>
      )}
    />
  );
}
