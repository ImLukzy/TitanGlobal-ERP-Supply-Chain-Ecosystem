import { aEstadoFiltro, aTipoFiltro, ESTADOS, textoEstado, textoTipo, TIPOS } from './presentacion';
import type { EstadoPrestamo, TipoPrestamo } from './tipos';

interface Props {
  busqueda: string;
  tipo: '' | TipoPrestamo;
  estado: '' | EstadoPrestamo;
  cambiarBusqueda: (valor: string) => void;
  cambiarTipo: (tipo: '' | TipoPrestamo) => void;
  cambiarEstado: (estado: '' | EstadoPrestamo) => void;
}

export function PrestamosFiltros({ busqueda, tipo, estado, cambiarBusqueda, cambiarTipo, cambiarEstado }: Props) {
  return (
    <div className="solicitudes-filters adelantos-prestamos-filters">
      <div>
        <label htmlFor="prestamos-busqueda">Buscar por número o empleado</label>
        <input
          autoComplete="off"
          id="prestamos-busqueda"
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Escribe para buscar…"
          type="search"
          value={busqueda}
        />
      </div>
      <div>
        <label htmlFor="prestamos-tipo">Tipo</label>
        <select id="prestamos-tipo" onChange={(event) => cambiarTipo(aTipoFiltro(event.currentTarget.value))} value={tipo}>
          <option value="">Todos</option>
          {TIPOS.map((item) => <option key={item} value={item}>{textoTipo(item)}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="prestamos-estado">Estado</label>
        <select id="prestamos-estado" onChange={(event) => cambiarEstado(aEstadoFiltro(event.currentTarget.value))} value={estado}>
          <option value="">Todos</option>
          {ESTADOS.map((item) => <option key={item} value={item}>{textoEstado(item)}</option>)}
        </select>
      </div>
    </div>
  );
}
