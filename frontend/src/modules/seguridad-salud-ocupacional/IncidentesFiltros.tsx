import { aSeveridad, aTipoIncidente, SEVERIDADES, textoValor, TIPOS_INCIDENTE } from './presentacion';
import type { EstadoIncidente, FiltrosIncidentes } from './tipos';

interface Props {
  busqueda: string;
  filtros: FiltrosIncidentes;
  cambiarBusqueda: (valor: string) => void;
  cambiar: (cambios: Partial<FiltrosIncidentes>) => void;
}

function aEstado(valor: string): '' | EstadoIncidente {
  return valor === 'Abierto' || valor === 'Cerrado' ? valor : '';
}

export function IncidentesFiltros({ busqueda, filtros, cambiarBusqueda, cambiar }: Props) {
  return (
    <div className="solicitudes-filters sst-filters sst-filters-seis">
      <div>
        <label htmlFor="incidentes-busqueda">Buscar por número o empleado</label>
        <input autoComplete="off" id="incidentes-busqueda" onChange={(event) => cambiarBusqueda(event.currentTarget.value)} placeholder="Escribe para buscar…" type="search" value={busqueda} />
      </div>
      <div>
        <label htmlFor="incidentes-tipo">Tipo</label>
        <select id="incidentes-tipo" onChange={(event) => cambiar({ tipo: aTipoIncidente(event.currentTarget.value) })} value={filtros.tipo}>
          <option value="">Todos</option>
          {TIPOS_INCIDENTE.map((tipo) => <option key={tipo} value={tipo}>{textoValor(tipo)}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="incidentes-severidad">Severidad</label>
        <select id="incidentes-severidad" onChange={(event) => cambiar({ severidad: aSeveridad(event.currentTarget.value) })} value={filtros.severidad}>
          <option value="">Todas</option>
          {SEVERIDADES.map((severidad) => <option key={severidad} value={severidad}>{severidad}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="incidentes-estado">Estado</label>
        <select id="incidentes-estado" onChange={(event) => cambiar({ estado: aEstado(event.currentTarget.value) })} value={filtros.estado}>
          <option value="">Todos</option>
          <option value="Abierto">Abierto</option>
          <option value="Cerrado">Cerrado</option>
        </select>
      </div>
      <div>
        <label htmlFor="incidentes-desde">Desde</label>
        <input id="incidentes-desde" onChange={(event) => cambiar({ desde: event.currentTarget.value })} type="date" value={filtros.desde} />
      </div>
      <div>
        <label htmlFor="incidentes-hasta">Hasta</label>
        <input id="incidentes-hasta" onChange={(event) => cambiar({ hasta: event.currentTarget.value })} type="date" value={filtros.hasta} />
      </div>
    </div>
  );
}
