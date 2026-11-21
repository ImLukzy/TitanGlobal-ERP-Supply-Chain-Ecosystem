import { TablaPaginada } from '../../components/TablaPaginada';
import { numeroGuia, fechaUtc } from './presentacion';
import type { PaginaGuias } from './tipos';
export function GuiasTabla({
  datos,
  seleccion,
  pagina,
  cambiar,
  seleccionar,
}: {
  datos: PaginaGuias;
  seleccion: number | null;
  pagina: number;
  cambiar: (p: number) => void;
  seleccionar: (id: number) => void;
}) {
  return (
    <TablaPaginada
      caption="Documentos internos de transporte"
      className="guias-remision-table"
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(datos.total / 10))}
      cambiarPagina={cambiar}
      etiquetaRegion="Listado de guías"
      columnas={[
        'Serie / número',
        'Destinatario',
        'Inicio UTC',
        'Estado',
        'Peso kg',
        'Detalle',
      ].map((etiqueta) => ({ id: etiqueta, etiqueta }))}
      elementos={datos.items}
      obtenerClave={(g) => g.id}
      renderizarFila={(g) => (
        <tr key={g.id} className={seleccion === g.id ? 'guias-remision-selected' : ''}>
          <th scope="row">{numeroGuia(g.serie, g.numero)}</th>
          <td>
            {g.destinatarioNombre}
            <small>{g.destinatarioDocumento}</small>
          </td>
          <td>{fechaUtc(g.fechaInicioTraslado)}</td>
          <td>
            <span className={`guias-remision-badge guias-remision-${g.estado.toLowerCase()}`}>
              {g.estado}
            </span>
          </td>
          <td>{g.pesoBrutoKg}</td>
          <td>
            <button
              className="secondary-button"
              type="button"
              aria-label={`Ver guía ${numeroGuia(g.serie, g.numero)}`}
              onClick={() => seleccionar(g.id)}
            >
              Ver detalle
            </button>
          </td>
        </tr>
      )}
    />
  );
}
