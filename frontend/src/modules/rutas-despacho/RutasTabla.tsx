import { TablaPaginada } from '../../components/TablaPaginada';
import type { PaginaRutas } from './tipos';
export function RutasTabla({
  datos,
  seleccion,
  pagina,
  cambiar,
  seleccionar,
}: {
  datos: PaginaRutas;
  seleccion: number | null;
  pagina: number;
  cambiar: (pagina: number) => void;
  seleccionar: (id: number) => void;
}) {
  return (
    <TablaPaginada
      caption="Rutas de despacho"
      etiquetaRegion="Rutas de despacho"
      className="rutas-despacho-table"
      columnas={[
        { id: 'codigo', etiqueta: 'Código / nombre' },
        { id: 'zona', etiqueta: 'Zona' },
        { id: 'trayecto', etiqueta: 'Origen / destino' },
        { id: 'distancia', etiqueta: 'Distancia' },
        { id: 'peaje', etiqueta: 'Peaje estimado' },
        { id: 'estado', etiqueta: 'Estado' },
        { id: 'detalle', etiqueta: 'Detalle' },
      ]}
      elementos={datos.items}
      obtenerClave={(r) => r.id}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(datos.total / 10))}
      cambiarPagina={cambiar}
      renderizarFila={(r) => (
        <tr className={r.id === seleccion ? 'rutas-despacho-selected' : ''}>
          <th scope="row">
            {r.codigo}
            <small className="rutas-despacho-muted">{r.nombre}</small>
          </th>
          <td>{r.zona}</td>
          <td>
            {r.origen}
            <small className="rutas-despacho-muted">{r.destino}</small>
          </td>
          <td className="rutas-despacho-number">{r.distanciaKm} km</td>
          <td className="rutas-despacho-number">
            {r.peajeEstimado} {r.moneda}
          </td>
          <td>
            <span className={`rutas-despacho-badge ${r.activo ? '' : 'rutas-despacho-inactive'}`}>
              {r.activo ? 'Activa' : 'Inactiva'}
            </span>
          </td>
          <td>
            <button
              className="secondary-button"
              type="button"
              onClick={() => seleccionar(r.id)}
              aria-label={`Ver ruta ${r.codigo}`}
              aria-pressed={r.id === seleccion}
            >
              Ver
            </button>
          </td>
        </tr>
      )}
    />
  );
}
