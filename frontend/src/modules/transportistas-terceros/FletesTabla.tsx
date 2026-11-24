import { TablaPaginada } from '../../components/TablaPaginada';
import { fechaUtc } from './presentacion';
import type { Flete, Pagina } from './tipos';
export function FletesTabla({
  datos,
  pagina,
  cambiar,
  seleccionar,
}: {
  datos: Pagina<Flete>;
  pagina: number;
  cambiar: (pagina: number) => void;
  seleccionar: (id: number) => void;
}) {
  return (
    <TablaPaginada
      caption="Fletes de terceros"
      etiquetaRegion="Fletes de terceros"
      columnas={[
        { id: 'referencia', etiqueta: 'Referencia / transportista' },
        { id: 'fecha', etiqueta: 'Fecha UTC' },
        { id: 'ruta', etiqueta: 'Ruta' },
        { id: 'importe', etiqueta: 'Importe' },
        { id: 'estado', etiqueta: 'Estado' },
        { id: 'accion', etiqueta: 'Detalle' },
      ]}
      elementos={datos.items}
      obtenerClave={(f) => f.id}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(datos.total / 10))}
      cambiarPagina={cambiar}
      renderizarFila={(f) => (
        <tr>
          <th scope="row">
            {f.referencia}
            <small className="transportistas-terceros-muted">{f.transportistaRazonSocial}</small>
          </th>
          <td>{fechaUtc(f.fecha)}</td>
          <td>
            {f.origen} → {f.destino}
          </td>
          <td className="transportistas-terceros-money">
            {f.importe} {f.moneda}
          </td>
          <td>
            <span
              className={`transportistas-terceros-badge ${f.estado === 'Cancelado' ? 'transportistas-terceros-inactivo' : ''}`}
            >
              {f.estado}
            </span>
          </td>
          <td>
            <button
              type="button"
              className="secondary-button"
              onClick={() => seleccionar(f.id)}
              aria-label={`Ver flete ${f.referencia}`}
            >
              Ver
            </button>
          </td>
        </tr>
      )}
    />
  );
}
