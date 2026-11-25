import { TablaPaginada } from '../../components/TablaPaginada';
import { EstadoIncidentePastilla, SeveridadPastilla } from './Pastillas';
import { textoFecha, textoValor } from './presentacion';
import type { IncidenteListado } from './tipos';

interface Props {
  items: IncidenteListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

export function IncidentesTabla({ items, error, cargando, pagina, totalPaginas, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando incidentes…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay incidentes con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de incidentes"
      etiquetaRegion="Tabla de incidentes"
      columnas={[
        { id: 'numero', etiqueta: 'Número' },
        { id: 'fecha', etiqueta: 'Fecha' },
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'tipo', etiqueta: 'Tipo' },
        { id: 'severidad', etiqueta: 'Severidad' },
        { id: 'dias', etiqueta: 'Días de baja' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(incidente) => incidente.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="sst-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(incidente) => (
        <tr className={incidente.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button aria-current={incidente.id === seleccionadoId ? 'true' : undefined} className="cliente-row-select" onClick={() => seleccionar(incidente.id)} type="button">
              {incidente.numero}
            </button>
          </td>
          <td className="sst-numero">{textoFecha(incidente.fecha)}</td>
          <td>{incidente.empleado}</td>
          <td>{textoValor(incidente.tipo)}</td>
          <td><SeveridadPastilla severidad={incidente.severidad} /></td>
          <td className="sst-numero">{incidente.diasBaja}</td>
          <td><EstadoIncidentePastilla estado={incidente.estado} /></td>
        </tr>
      )}
    />
  );
}
