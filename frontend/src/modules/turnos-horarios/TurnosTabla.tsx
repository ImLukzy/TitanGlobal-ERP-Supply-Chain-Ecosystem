import { TablaPaginada } from '../../components/TablaPaginada';
import { diasOCiclo, horarioTurno } from './presentacion';
import type { TurnoListado } from './tipos';

// El estado se muestra con texto, nunca solo con color.
export function TurnoEstadoPastilla({ activo }: { activo: boolean }) {
  return (
    <span className={`turnos-horarios-estado ${activo ? 'is-activo' : 'is-inactivo'}`}>
      {activo ? 'Activo' : 'Inactivo'}
    </span>
  );
}

interface Props {
  items: TurnoListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

export function TurnosTabla({ items, error, cargando, pagina, totalPaginas, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando turnos…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay turnos con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de turnos"
      etiquetaRegion="Tabla de turnos"
      columnas={[
        { id: 'codigo', etiqueta: 'Código' },
        { id: 'nombre', etiqueta: 'Nombre' },
        { id: 'tipo', etiqueta: 'Tipo' },
        { id: 'horario', etiqueta: 'Horario' },
        { id: 'tolerancia', etiqueta: 'Tolerancia' },
        { id: 'dias', etiqueta: 'Días o ciclo' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(turno) => turno.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="turnos-horarios-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(turno) => (
        <tr className={turno.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={turno.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(turno.id)}
              type="button"
            >
              {turno.codigo}
            </button>
          </td>
          <td>{turno.nombre}</td>
          <td>{turno.tipo}</td>
          <td className="turnos-horarios-numero">{horarioTurno(turno.horaEntrada, turno.horaSalida, turno.cruzaMedianoche)}</td>
          <td className="turnos-horarios-numero">{`${turno.toleranciaMinutos} min`}</td>
          <td className="turnos-horarios-numero">{diasOCiclo(turno)}</td>
          <td><TurnoEstadoPastilla activo={turno.activo} /></td>
        </tr>
      )}
    />
  );
}
