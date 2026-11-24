import { aFiltroActivo, aTipoFiltro, FILTROS_ACTIVO, TIPOS } from './presentacion';
import type { FiltroActivo, TipoTurno } from './tipos';

interface Props {
  busqueda: string;
  tipo: '' | TipoTurno;
  activo: FiltroActivo;
  cambiarBusqueda: (valor: string) => void;
  cambiarTipo: (tipo: '' | TipoTurno) => void;
  cambiarActivo: (activo: FiltroActivo) => void;
}

export function TurnosFiltros({ busqueda, tipo, activo, cambiarBusqueda, cambiarTipo, cambiarActivo }: Props) {
  return (
    <div className="solicitudes-filters turnos-horarios-filters">
      <div>
        <label htmlFor="turnos-busqueda">Buscar por código o nombre</label>
        <input
          autoComplete="off"
          id="turnos-busqueda"
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Escribe para buscar…"
          type="search"
          value={busqueda}
        />
      </div>
      <div>
        <label htmlFor="turnos-tipo">Tipo</label>
        <select id="turnos-tipo" onChange={(event) => cambiarTipo(aTipoFiltro(event.currentTarget.value))} value={tipo}>
          <option value="">Todos</option>
          {TIPOS.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="turnos-activo">Estado</label>
        <select id="turnos-activo" onChange={(event) => cambiarActivo(aFiltroActivo(event.currentTarget.value))} value={activo}>
          {FILTROS_ACTIVO.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
    </div>
  );
}
