import type { PaginaEmpleados } from '../empleados/tipos';
import { aEstadoHorasExtra, aEstadoJornada, ESTADOS_HORAS_EXTRA, ESTADOS_JORNADA, textoEstado, textoHorasExtra } from './presentacion';
import type { EstadoHorasExtra, EstadoJornada } from './tipos';

interface Props {
  empleados: PaginaEmpleados | null;
  empleadoId: number | null;
  desde: string;
  hasta: string;
  estado: '' | EstadoJornada;
  estadoHorasExtra: '' | EstadoHorasExtra;
  cambiarEmpleado: (id: number | null) => void;
  cambiarDesde: (fecha: string) => void;
  cambiarHasta: (fecha: string) => void;
  cambiarEstado: (estado: '' | EstadoJornada) => void;
  cambiarEstadoHorasExtra: (estado: '' | EstadoHorasExtra) => void;
}

export function JornadasFiltros({
  empleados,
  empleadoId,
  desde,
  hasta,
  estado,
  estadoHorasExtra,
  cambiarEmpleado,
  cambiarDesde,
  cambiarHasta,
  cambiarEstado,
  cambiarEstadoHorasExtra,
}: Props) {
  return (
    <div className="solicitudes-filters asistencia-filters">
      <div>
        <label htmlFor="jornadas-empleado">Empleado</label>
        <select
          id="jornadas-empleado"
          onChange={(event) => cambiarEmpleado(event.currentTarget.value === '' ? null : Number(event.currentTarget.value))}
          value={empleadoId === null ? '' : String(empleadoId)}
        >
          <option value="">Todos</option>
          {(empleados?.items ?? []).map((empleado) => (
            <option key={empleado.id} value={String(empleado.id)}>
              {`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}
            </option>
          ))}
        </select>
      </div>
      <div>
        <label htmlFor="jornadas-desde">Desde</label>
        <input id="jornadas-desde" onChange={(event) => cambiarDesde(event.currentTarget.value)} type="date" value={desde} />
      </div>
      <div>
        <label htmlFor="jornadas-hasta">Hasta</label>
        <input id="jornadas-hasta" onChange={(event) => cambiarHasta(event.currentTarget.value)} type="date" value={hasta} />
      </div>
      <div>
        <label htmlFor="jornadas-estado">Estado</label>
        <select id="jornadas-estado" onChange={(event) => cambiarEstado(aEstadoJornada(event.currentTarget.value))} value={estado}>
          <option value="">Todos</option>
          {ESTADOS_JORNADA.map((item) => <option key={item} value={item}>{textoEstado(item)}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="jornadas-horas-extra">Horas extra</label>
        <select
          id="jornadas-horas-extra"
          onChange={(event) => cambiarEstadoHorasExtra(aEstadoHorasExtra(event.currentTarget.value))}
          value={estadoHorasExtra}
        >
          <option value="">Todas</option>
          {ESTADOS_HORAS_EXTRA.map((item) => <option key={item} value={item}>{textoHorasExtra(item)}</option>)}
        </select>
      </div>
    </div>
  );
}
