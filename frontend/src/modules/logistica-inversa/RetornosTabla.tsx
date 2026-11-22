import { TablaPaginada } from '../../components/TablaPaginada';
import { fechaUtc as fecha } from './presentacion';
import type { PaginaRetornos } from './tipos';
export function RetornosTabla({
  datos,
  seleccion,
  pagina,
  cambiar,
  seleccionar,
}: {
  datos: PaginaRetornos;
  seleccion: number | null;
  pagina: number;
  cambiar: (p: number) => void;
  seleccionar: (id: number) => void;
}) {
  return (
    <TablaPaginada
      caption="Retornos logísticos"
      etiquetaRegion="Listado de retornos"
      className="logistica-inversa-table"
      columnas={[
        'Código',
        'Tipo / material',
        'Contraparte',
        'Programación UTC',
        'Programado',
        'Estado',
        'Detalle',
      ].map((etiqueta) => ({ id: etiqueta, etiqueta }))}
      elementos={datos.items}
      obtenerClave={(r) => r.id}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(datos.total / 10))}
      cambiarPagina={cambiar}
      renderizarFila={(r) => (
        <tr className={seleccion === r.id ? 'logistica-inversa-selected' : ''}>
          <th scope="row">{r.codigo}</th>
          <td>
            {r.tipo}
            <small>{r.descripcion}</small>
          </td>
          <td>{r.contraparte}</td>
          <td>{fecha(r.fechaProgramada)}</td>
          <td>
            {r.cantidadProgramada} {r.unidad}
          </td>
          <td>
            <span className="logistica-inversa-badge">{r.estado}</span>
          </td>
          <td>
            <button
              type="button"
              className="secondary-button"
              aria-label={`Ver retorno ${r.codigo}`}
              aria-pressed={seleccion === r.id}
              onClick={() => seleccionar(r.id)}
            >
              Ver detalle
            </button>
          </td>
        </tr>
      )}
    />
  );
}
