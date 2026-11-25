import { aEstadoFiltro, aTipoFiltro, ESTADOS, textoEstado, textoTipo, TIPOS } from './presentacion';
import type { EstadoAusencia, TipoAusencia } from './tipos';

interface Props {
  busqueda: string;
  tipo: '' | TipoAusencia;
  estado: '' | EstadoAusencia;
  desde: string;
  hasta: string;
  cambiarBusqueda: (valor: string) => void;
  cambiarTipo: (tipo: '' | TipoAusencia) => void;
  cambiarEstado: (estado: '' | EstadoAusencia) => void;
  cambiarDesde: (fecha: string) => void;
  cambiarHasta: (fecha: string) => void;
}

export function AusenciasFiltros({
  busqueda,
  tipo,
  estado,
  desde,
  hasta,
  cambiarBusqueda,
  cambiarTipo,
  cambiarEstado,
  cambiarDesde,
  cambiarHasta,
}: Props) {
  return (
    <div className="solicitudes-filters vacaciones-permisos-filters">
      <div>
        <label htmlFor="ausencias-busqueda">Buscar por número o empleado</label>
        <input
          autoComplete="off"
          id="ausencias-busqueda"
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Escribe para buscar…"
          type="search"
          value={busqueda}
        />
      </div>
      <div>
        <label htmlFor="ausencias-tipo">Tipo</label>
        <select id="ausencias-tipo" onChange={(event) => cambiarTipo(aTipoFiltro(event.currentTarget.value))} value={tipo}>
          <option value="">Todos</option>
          {TIPOS.map((item) => <option key={item} value={item}>{textoTipo(item)}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="ausencias-estado">Estado</label>
        <select id="ausencias-estado" onChange={(event) => cambiarEstado(aEstadoFiltro(event.currentTarget.value))} value={estado}>
          <option value="">Todos</option>
          {ESTADOS.map((item) => <option key={item} value={item}>{textoEstado(item)}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="ausencias-desde">Desde</label>
        <input id="ausencias-desde" onChange={(event) => cambiarDesde(event.currentTarget.value)} type="date" value={desde} />
      </div>
      <div>
        <label htmlFor="ausencias-hasta">Hasta</label>
        <input id="ausencias-hasta" onChange={(event) => cambiarHasta(event.currentTarget.value)} type="date" value={hasta} />
      </div>
    </div>
  );
}
