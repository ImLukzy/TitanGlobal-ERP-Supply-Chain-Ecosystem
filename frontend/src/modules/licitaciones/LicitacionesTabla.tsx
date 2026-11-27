import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda } from '../../lib/formato';
import { etiquetasTipo, formatearFechaHora, plazoVencido } from './plazo';
import type { LicitacionListado, PaginaLicitaciones } from './tipos';

interface LicitacionesTablaProps {
  lista: PaginaLicitaciones | null;
  error: string;
  seleccionadaId: number | null;
  seleccionar: (licitacion: LicitacionListado) => void;
  pagina: number;
  cambiarPagina: (pagina: number) => void;
}

const columnas = [
  { id: 'numero', etiqueta: 'Número' },
  { id: 'titulo', etiqueta: 'Título' },
  { id: 'tipo', etiqueta: 'Tipo' },
  { id: 'estado', etiqueta: 'Estado' },
  { id: 'presupuesto', etiqueta: 'Presupuesto' },
  { id: 'cierre', etiqueta: 'Cierre' },
  { id: 'ofertas', etiqueta: 'Ofertas' },
];

export function LicitacionesTabla({ lista, error, seleccionadaId, seleccionar, pagina, cambiarPagina }: LicitacionesTablaProps) {
  if (error !== '') {
    return <p className="licitaciones-error" role="alert">{error}</p>;
  }
  if (lista === null) {
    return <p className="licitaciones-loading" role="status">Cargando licitaciones…</p>;
  }
  if (lista.items.length === 0) {
    return <p className="licitaciones-empty" role="status">No hay licitaciones para mostrar.</p>;
  }
  return (
    <TablaPaginada
      caption="Licitaciones"
      className="licitaciones-table"
      classNameContenedor="licitaciones-table-scroll"
      classNamePaginacion="licitaciones-pagination"
      columnas={columnas}
      elementos={lista.items}
      etiquetaRegion="Listado de licitaciones"
      obtenerClave={(licitacion) => licitacion.id}
      renderizarFila={(licitacion) => (
        <tr aria-selected={seleccionadaId === licitacion.id} className={seleccionadaId === licitacion.id ? 'is-selected' : ''} key={licitacion.id}>
          <th scope="row">
            <button className="licitacion-row-select" onClick={() => seleccionar(licitacion)} type="button">{licitacion.numero}</button>
          </th>
          <td>{licitacion.titulo}</td>
          <td>{etiquetasTipo[licitacion.tipo]}</td>
          <td><span className={`licitacion-estado is-${licitacion.estado.toLowerCase()}`}>{licitacion.estado}</span></td>
          <td>{formateadorMoneda.format(licitacion.presupuestoReferencial)}</td>
          <td>
            {formatearFechaHora(licitacion.fechaCierre)}
            {plazoVencido(licitacion.estado, licitacion.fechaCierre) && <span className="licitacion-late-label">Plazo vencido</span>}
          </td>
          <td>{licitacion.totalOfertas}</td>
        </tr>
      )}
      cambiarPagina={cambiarPagina}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(lista.total / lista.tamanoPagina))}
    />
  );
}
