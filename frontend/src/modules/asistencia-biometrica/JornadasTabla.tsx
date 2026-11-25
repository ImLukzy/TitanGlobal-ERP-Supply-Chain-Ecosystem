import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearFecha } from '../../lib/formato';
import { formatearMinutos, horaDeJornada, textoEstado, textoHorasExtra } from './presentacion';
import type { EstadoHorasExtra, EstadoJornada, JornadaAsistencia } from './tipos';

// Los estados se muestran siempre con texto, nunca solo con color.
export function EstadoJornadaPastilla({ estado }: { estado: EstadoJornada }) {
  return <span className={`asistencia-estado is-${estado.toLowerCase()}`}>{textoEstado(estado)}</span>;
}

export function HorasExtraPastilla({ estado }: { estado: EstadoHorasExtra }) {
  return <span className={`asistencia-estado is-extra-${estado.toLowerCase()}`}>{textoHorasExtra(estado)}</span>;
}

interface Props {
  items: JornadaAsistencia[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (jornada: JornadaAsistencia) => void;
  cambiarPagina: (pagina: number) => void;
}

export function JornadasTabla({ items, error, cargando, pagina, totalPaginas, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando jornadas…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay jornadas con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de jornadas de asistencia"
      etiquetaRegion="Tabla de jornadas de asistencia"
      columnas={[
        { id: 'fecha', etiqueta: 'Fecha' },
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'turno', etiqueta: 'Turno' },
        { id: 'entrada', etiqueta: 'Entrada' },
        { id: 'salida', etiqueta: 'Salida' },
        { id: 'trabajado', etiqueta: 'Trabajado' },
        { id: 'tardanza', etiqueta: 'Tardanza' },
        { id: 'extra', etiqueta: 'Extra' },
        { id: 'estado', etiqueta: 'Estado' },
        { id: 'horas-extra', etiqueta: 'Horas extra' },
      ]}
      elementos={items}
      obtenerClave={(jornada) => jornada.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="asistencia-table asistencia-table-jornadas"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(jornada) => (
        <tr className={jornada.id === seleccionadoId ? 'is-selected' : ''}>
          <td className="asistencia-numero">{formatearFecha(jornada.fecha)}</td>
          <td>
            <button
              aria-current={jornada.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(jornada)}
              type="button"
            >
              {`${jornada.empleadoLegajo} · ${jornada.empleadoNombre}`}
            </button>
          </td>
          <td>{jornada.turnoCodigo ?? '—'}</td>
          <td className="asistencia-numero">{horaDeJornada(jornada.entrada, jornada.fecha)}</td>
          <td className="asistencia-numero">{horaDeJornada(jornada.salida, jornada.fecha)}</td>
          <td className="asistencia-numero">{formatearMinutos(jornada.minutosTrabajados)}</td>
          <td className="asistencia-numero">{formatearMinutos(jornada.minutosTardanza)}</td>
          <td className="asistencia-numero">{formatearMinutos(jornada.minutosExtra)}</td>
          <td><EstadoJornadaPastilla estado={jornada.estado} /></td>
          <td><HorasExtraPastilla estado={jornada.estadoHorasExtra} /></td>
        </tr>
      )}
    />
  );
}
