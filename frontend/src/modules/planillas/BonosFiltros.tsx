import type { PaginaEmpleados } from '../empleados/tipos';
import { MESES } from './presentacion';

interface Props {
  empleados: PaginaEmpleados | null;
  anioTexto: string;
  mes: number | null;
  empleadoId: number | null;
  cambiarAnio: (texto: string) => void;
  cambiarMes: (mes: number | null) => void;
  cambiarEmpleado: (id: number | null) => void;
}

export function BonosFiltros({ empleados, anioTexto, mes, empleadoId, cambiarAnio, cambiarMes, cambiarEmpleado }: Props) {
  return (
    <div className="solicitudes-filters planillas-filters">
      <div>
        <label htmlFor="bonos-anio">Año</label>
        <input
          autoComplete="off"
          id="bonos-anio"
          inputMode="numeric"
          maxLength={4}
          onChange={(event) => cambiarAnio(event.currentTarget.value)}
          placeholder="Todos"
          type="text"
          value={anioTexto}
        />
      </div>
      <div>
        <label htmlFor="bonos-mes">Mes</label>
        <select
          id="bonos-mes"
          onChange={(event) => cambiarMes(event.currentTarget.value === '' ? null : Number(event.currentTarget.value))}
          value={mes === null ? '' : String(mes)}
        >
          <option value="">Todos</option>
          {MESES.map((nombre, indice) => <option key={nombre} value={String(indice + 1)}>{nombre}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="bonos-empleado">Empleado</label>
        <select
          id="bonos-empleado"
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
    </div>
  );
}
