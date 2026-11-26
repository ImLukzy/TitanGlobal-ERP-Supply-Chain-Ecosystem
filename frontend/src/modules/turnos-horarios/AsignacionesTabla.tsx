import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearFecha } from '../../lib/formato';
import { TEXTO_SIN_FIN } from './presentacion';
import type { AsignacionTurnoListado, EstadoAsignacion } from './tipos';

// El estado se muestra con texto, nunca solo con color.
export function AsignacionEstadoPastilla({ estado }: { estado: EstadoAsignacion }) {
  return <span className={`turnos-horarios-estado is-${estado.toLowerCase()}`}>{estado}</span>;
}

interface Props {
  items: AsignacionTurnoListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (asignacion: AsignacionTurnoListado) => void;
  cambiarPagina: (pagina: number) => void;
}

export function AsignacionesTabla({ items, error, cargando, pagina, totalPaginas, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando asignaciones…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay asignaciones con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de asignaciones de turno"
      etiquetaRegion="Tabla de asignaciones de turno"
      columnas={[
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'turno', etiqueta: 'Turno' },
        { id: 'inicio', etiqueta: 'Inicio' },
        { id: 'fin', etiqueta: 'Fin' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(asignacion) => asignacion.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="turnos-horarios-table turnos-horarios-table-asignaciones"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(asignacion) => (
        <tr className={asignacion.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={asignacion.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(asignacion)}
              type="button"
            >
              {`${asignacion.empleadoLegajo} · ${asignacion.empleadoNombre}`}
            </button>
          </td>
          <td>{`${asignacion.turnoCodigo} · ${asignacion.turnoNombre}`}</td>
          <td className="turnos-horarios-numero">{formatearFecha(asignacion.fechaInicio)}</td>
          <td className="turnos-horarios-numero">{asignacion.fechaFin === null ? TEXTO_SIN_FIN : formatearFecha(asignacion.fechaFin)}</td>
          <td><AsignacionEstadoPastilla estado={asignacion.estado} /></td>
        </tr>
      )}
    />
  );
}
