import { aEstadoCicloFiltro, ESTADOS_CICLO, textoEstadoCiclo } from './presentacion';
import type { EstadoCiclo } from './tipos';

interface Props {
  busqueda: string;
  estado: '' | EstadoCiclo;
  cambiarBusqueda: (valor: string) => void;
  cambiarEstado: (estado: '' | EstadoCiclo) => void;
}

export function CiclosFiltros({ busqueda, estado, cambiarBusqueda, cambiarEstado }: Props) {
  return (
    <div className="solicitudes-filters evaluacion-desempeno-filters">
      <div>
        <label htmlFor="ciclos-busqueda">Buscar por código o nombre</label>
        <input
          autoComplete="off"
          id="ciclos-busqueda"
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Escribe para buscar…"
          type="search"
          value={busqueda}
        />
      </div>
      <div>
        <label htmlFor="ciclos-estado">Estado</label>
        <select id="ciclos-estado" onChange={(event) => cambiarEstado(aEstadoCicloFiltro(event.currentTarget.value))} value={estado}>
          <option value="">Todos</option>
          {ESTADOS_CICLO.map((item) => <option key={item} value={item}>{textoEstadoCiclo(item)}</option>)}
        </select>
      </div>
    </div>
  );
}
