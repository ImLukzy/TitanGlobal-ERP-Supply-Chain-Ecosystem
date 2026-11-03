import type { EstadoEmpleado } from './tipos';

interface Props {
  busqueda: string;
  estado: '' | EstadoEmpleado;
  cambiarBusqueda: (valor: string) => void;
  cambiarEstado: (estado: '' | EstadoEmpleado) => void;
}

function valorEstado(valor: string): '' | EstadoEmpleado {
  if (valor === 'Activo' || valor === 'Cesado') {
    return valor;
  }
  return '';
}

export function EmpleadosFiltros({ busqueda, estado, cambiarBusqueda, cambiarEstado }: Props) {
  return (
    <div className="solicitudes-filters empleados-filters">
      <div>
        <label htmlFor="empleados-busqueda">Buscar por legajo, nombre o documento</label>
        <input
          autoComplete="off"
          id="empleados-busqueda"
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Escribe para buscar…"
          type="search"
          value={busqueda}
        />
      </div>
      <div>
        <label htmlFor="empleados-estado">Estado</label>
        <select
          id="empleados-estado"
          onChange={(event) => cambiarEstado(valorEstado(event.currentTarget.value))}
          value={estado}
        >
          <option value="">Todos</option>
          <option value="Activo">Activo</option>
          <option value="Cesado">Cesado</option>
        </select>
      </div>
    </div>
  );
}
