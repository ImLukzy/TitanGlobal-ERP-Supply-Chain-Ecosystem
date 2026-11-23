import { TablaPaginada } from '../../components/TablaPaginada';
import { textoFecha } from './presentacion';
import type { EstadoProgramacion, ProgramacionListado } from './tipos';

interface Props {
  items: ProgramacionListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

// El estado se muestra con texto, nunca solo con color.
export function EstadoProgramacionPastilla({ estado }: { estado: EstadoProgramacion }) {
  return <span className={`capacitaciones-estado is-${estado.toLowerCase()}`}>{estado}</span>;
}

export function ProgramacionesTabla({ items, error, cargando, pagina, totalPaginas, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando programaciones…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay programaciones con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de programaciones de cursos"
      etiquetaRegion="Tabla de programaciones de cursos"
      columnas={[
        { id: 'curso', etiqueta: 'Curso' },
        { id: 'fechas', etiqueta: 'Fechas' },
        { id: 'lugar', etiqueta: 'Lugar' },
        { id: 'cupo', etiqueta: 'Inscritos / Cupo' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(programacion) => programacion.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="capacitaciones-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(programacion) => (
        <tr className={programacion.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={programacion.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(programacion.id)}
              type="button"
            >
              {programacion.cursoNombre}
            </button>
          </td>
          <td className="capacitaciones-numero">{`${textoFecha(programacion.fechaInicio)} - ${textoFecha(programacion.fechaFin)}`}</td>
          <td>{programacion.lugar ?? 'Sin lugar'}</td>
          <td className="capacitaciones-numero">{`${programacion.inscritos} / ${programacion.cupo}`}</td>
          <td><EstadoProgramacionPastilla estado={programacion.estado} /></td>
        </tr>
      )}
    />
  );
}
