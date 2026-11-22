import { TablaPaginada } from '../../components/TablaPaginada';
import type { PaginaSucursales } from './tipos';
export function SucursalesTabla({
  datos,
  seleccion,
  pagina,
  cambiar,
  seleccionar,
}: {
  datos: PaginaSucursales;
  seleccion: number | null;
  pagina: number;
  cambiar: (v: number) => void;
  seleccionar: (id: number) => void;
}) {
  return (
    <TablaPaginada
      caption="Sucursales"
      etiquetaRegion="Listado de sucursales"
      className="sucursales-table"
      columnas={['Código / nombre', 'Tipo', 'País / ciudad', 'Estado', 'Detalle'].map(
        (etiqueta) => ({ id: etiqueta, etiqueta }),
      )}
      elementos={datos.items}
      obtenerClave={(s) => s.id}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(datos.total / 10))}
      cambiarPagina={cambiar}
      renderizarFila={(s) => (
        <tr className={seleccion === s.id ? 'sucursales-selected' : ''}>
          <th scope="row">
            {s.codigo}
            <small>{s.nombre}</small>
          </th>
          <td>{s.tipo}</td>
          <td>
            {s.pais}
            <small>{s.ciudad}</small>
          </td>
          <td>
            <span className={`sucursales-badge sucursales-${s.activa ? 'activa' : 'inactiva'}`}>
              {s.activa ? 'Activa' : 'Inactiva'}
            </span>
          </td>
          <td>
            <button
              className="secondary-button"
              type="button"
              aria-label={`Ver sucursal ${s.codigo}`}
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
