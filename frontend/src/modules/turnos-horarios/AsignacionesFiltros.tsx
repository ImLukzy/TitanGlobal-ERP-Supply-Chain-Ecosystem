import type { PaginaEmpleados } from '../empleados/tipos';
import { aEstadoAsignacion, ESTADOS_ASIGNACION } from './presentacion';
import type { EstadoAsignacion, PaginaTurnos } from './tipos';

interface Props {
  empleados: PaginaEmpleados | null;
  turnos: PaginaTurnos | null;
  empleadoId: number | null;
  turnoId: number | null;
  estado: '' | EstadoAsignacion;
  cambiarEmpleado: (id: number | null) => void;
  cambiarTurno: (id: number | null) => void;
  cambiarEstado: (estado: '' | EstadoAsignacion) => void;
}

function aId(valor: string): number | null {
  return valor === '' ? null : Number(valor);
}

export function AsignacionesFiltros({
  empleados,
  turnos,
  empleadoId,
  turnoId,
  estado,
  cambiarEmpleado,
  cambiarTurno,
  cambiarEstado,
}: Props) {
  return (
    <div className="solicitudes-filters turnos-horarios-filters">
      <div>
        <label htmlFor="asignaciones-empleado">Empleado</label>
        <select
          id="asignaciones-empleado"
          onChange={(event) => cambiarEmpleado(aId(event.currentTarget.value))}
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
        <label htmlFor="asignaciones-turno">Turno</label>
        <select
          id="asignaciones-turno"
          onChange={(event) => cambiarTurno(aId(event.currentTarget.value))}
          value={turnoId === null ? '' : String(turnoId)}
        >
          <option value="">Todos</option>
          {(turnos?.items ?? []).map((turno) => (
            <option key={turno.id} value={String(turno.id)}>{`${turno.codigo} · ${turno.nombre}`}</option>
          ))}
        </select>
      </div>
      <div>
        <label htmlFor="asignaciones-estado">Estado</label>
        <select
          id="asignaciones-estado"
          onChange={(event) => cambiarEstado(aEstadoAsignacion(event.currentTarget.value))}
          value={estado}
        >
          <option value="">Todos</option>
          {ESTADOS_ASIGNACION.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
    </div>
  );
}
