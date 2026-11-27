import { TablaPaginada } from '../../components/TablaPaginada';
import type { PaginaCargas } from './tipos';
export function ConsolidacionesTabla({
  datos,
  seleccion,
  pagina,
  cambiar,
  seleccionar,
}: {
  datos: PaginaCargas;
  seleccion: number | null;
  pagina: number;
  cambiar: (i: number) => void;
  seleccionar: (i: number) => void;
}) {
  return (
    <TablaPaginada
      caption="Consolidaciones de carga"
      etiquetaRegion="Consolidaciones de carga"
      className="consolidacion-carga-table"
      columnas={[
        { id: 'codigo', etiqueta: 'Código / nombre' },
        { id: 'estado', etiqueta: 'Estado' },
        { id: 'picking', etiqueta: 'Picking' },
        { id: 'bultos', etiqueta: 'Bultos' },
        { id: 'volumen', etiqueta: 'Volumen m³' },
        { id: 'peso', etiqueta: 'Peso kg' },
        { id: 'detalle', etiqueta: 'Detalle' },
      ]}
      elementos={datos.items}
      obtenerClave={(c) => c.id}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(datos.total / 10))}
      cambiarPagina={cambiar}
      renderizarFila={(c) => (
        <tr className={c.id === seleccion ? 'consolidacion-carga-selected' : ''}>
          <th scope="row">
            {c.codigo}
            <small>{c.nombre}</small>
          </th>
          <td>
            <span
              className={`consolidacion-carga-badge consolidacion-carga-${c.estado.toLowerCase()}`}
            >
              {c.estado}
            </span>
          </td>
          <td>
            {c.unidadesRecogidas} / {c.totalUnidades}
          </td>
          <td>{c.totalBultos}</td>
          <td>{c.volumenM3}</td>
          <td>{c.pesoKg}</td>
          <td>
            <button
              className="secondary-button"
              type="button"
              aria-label={`Ver carga ${c.codigo}`}
              aria-pressed={c.id === seleccion}
              onClick={() => seleccionar(c.id)}
            >
              Ver
            </button>
          </td>
        </tr>
      )}
    />
  );
}
