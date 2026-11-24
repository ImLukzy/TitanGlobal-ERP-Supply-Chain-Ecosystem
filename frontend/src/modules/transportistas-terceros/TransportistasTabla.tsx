import { TablaPaginada } from '../../components/TablaPaginada';
import type { Pagina, Transportista } from './tipos';
export function TransportistasTabla({
  datos,
  pagina,
  cambiar,
  seleccionar,
}: {
  datos: Pagina<Transportista>;
  pagina: number;
  cambiar: (pagina: number) => void;
  seleccionar: (id: number) => void;
}) {
  return (
    <TablaPaginada
      caption="Transportistas terceros"
      etiquetaRegion="Transportistas terceros"
      columnas={[
        { id: 'nombre', etiqueta: 'Razón social / documento' },
        { id: 'pais', etiqueta: 'País' },
        { id: 'servicio', etiqueta: 'Servicio' },
        { id: 'estado', etiqueta: 'Estado' },
        { id: 'accion', etiqueta: 'Detalle' },
      ]}
      elementos={datos.items}
      obtenerClave={(t) => t.id}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(datos.total / 10))}
      cambiarPagina={cambiar}
      renderizarFila={(t) => (
        <tr>
          <th scope="row">
            {t.razonSocial}
            <small className="transportistas-terceros-muted">{t.documentoFiscal}</small>
          </th>
          <td>{t.pais}</td>
          <td>{t.tipoServicio}</td>
          <td>
            <span
              className={`transportistas-terceros-badge ${t.activo ? '' : 'transportistas-terceros-inactivo'}`}
            >
              {t.activo ? 'Activo' : 'Inactivo'}
            </span>
          </td>
          <td>
            <button
              className="secondary-button"
              type="button"
              onClick={() => seleccionar(t.id)}
              aria-label={`Ver transportista ${t.razonSocial}`}
            >
              Ver
            </button>
          </td>
        </tr>
      )}
    />
  );
}
