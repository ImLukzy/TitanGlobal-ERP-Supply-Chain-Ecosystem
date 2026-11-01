import type { FiltrosUsuarios, RolOpcion } from './tipos';

interface Props {
  busqueda: string;
  filtros: FiltrosUsuarios;
  roles: RolOpcion[];
  cambiarBusqueda: (valor: string) => void;
  cambiarFiltro: (cambios: Partial<FiltrosUsuarios>) => void;
}

function valorRol(valor: string): number | '' {
  if (valor === '') {
    return '';
  }
  return Number(valor);
}

function valorActivo(valor: string): boolean | '' {
  if (valor === '') {
    return '';
  }
  return valor === 'true';
}

export function UsuariosFiltros({ busqueda, filtros, roles, cambiarBusqueda, cambiarFiltro }: Props) {
  const rolSeleccionado = filtros.rolId ?? '';
  const estadoSeleccionado = filtros.activo === undefined || filtros.activo === '' ? '' : String(filtros.activo);
  return (
    <div className="solicitudes-filters usuarios-filters">
      <div>
        <label htmlFor="usuarios-busqueda">Buscar por usuario, nombre o email</label>
        <input
          autoComplete="off"
          id="usuarios-busqueda"
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Escribe para buscar…"
          type="search"
          value={busqueda}
        />
      </div>
      <div>
        <label htmlFor="usuarios-rol">Rol</label>
        <select
          id="usuarios-rol"
          onChange={(event) => cambiarFiltro({ rolId: valorRol(event.currentTarget.value) })}
          value={rolSeleccionado}
        >
          <option value="">Todos</option>
          {roles.map((rol) => <option key={rol.id} value={rol.id}>{rol.codigo} · {rol.nombre}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="usuarios-estado">Estado</label>
        <select
          id="usuarios-estado"
          onChange={(event) => cambiarFiltro({ activo: valorActivo(event.currentTarget.value) })}
          value={estadoSeleccionado}
        >
          <option value="">Todos</option>
          <option value="true">Activos</option>
          <option value="false">Inactivos</option>
        </select>
      </div>
    </div>
  );
}
