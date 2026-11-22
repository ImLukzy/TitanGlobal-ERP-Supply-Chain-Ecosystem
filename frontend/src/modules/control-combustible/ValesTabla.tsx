import { TablaPaginada } from '../../components/TablaPaginada';
import { fechaUtc } from './presentacion';
import type { PaginaVales } from './tipos';
export function ValesTabla({
  datos,
  seleccion,
  pagina,
  cambiar,
  seleccionar,
}: {
  datos: PaginaVales;
  seleccion: number | null;
  pagina: number;
  cambiar: (p: number) => void;
  seleccionar: (id: number) => void;
}) {
  return (
    <TablaPaginada
      caption="Vales de combustible"
      etiquetaRegion="Listado de vales"
      className="control-combustible-table"
      columnas={[
        'Código',
        'Placa / estación',
        'Carga UTC',
        'Combustible',
        'Litros',
        'Importe / moneda',
        'Estado',
        'Detalle',
      ].map((etiqueta) => ({ id: etiqueta, etiqueta }))}
      elementos={datos.items}
      obtenerClave={(v) => v.id}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(datos.total / 10))}
      cambiarPagina={cambiar}
      renderizarFila={(v) => (
        <tr className={seleccion === v.id ? 'control-combustible-selected' : ''}>
          <th scope="row">{v.codigo}</th>
          <td>
            {v.placa}
            <small>{v.estacion}</small>
          </td>
          <td>{fechaUtc(v.fechaCarga)}</td>
          <td>{v.tipoCombustible}</td>
          <td>{v.litros}</td>
          <td>
            {v.importe}
            {'\u00a0'}
            {v.moneda}
          </td>
          <td>
            <span
              className={`control-combustible-badge control-combustible-${v.estado.toLowerCase()}`}
            >
              {v.estado}
            </span>
          </td>
          <td>
            <button
              type="button"
              className="secondary-button"
              aria-label={`Ver vale ${v.codigo}`}
              onClick={() => seleccionar(v.id)}
            >
              Ver detalle
            </button>
          </td>
        </tr>
      )}
    />
  );
}
