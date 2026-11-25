import { estados, etiquetasTipo } from './plazo';
import type { EstadoLicitacion, FiltrosLicitacion, TipoLicitacion } from './tipos';

interface LicitacionesFiltrosProps {
  busqueda: string;
  filtros: FiltrosLicitacion;
  cambiarBusqueda: (valor: string) => void;
  cambiarFiltros: (cambios: Partial<FiltrosLicitacion>) => void;
}

export function LicitacionesFiltros({ busqueda, filtros, cambiarBusqueda, cambiarFiltros }: LicitacionesFiltrosProps) {
  return (
    <div className="licitaciones-filters">
      <label>
        Buscar número o título
        <input aria-label="Buscar" onChange={(event) => cambiarBusqueda(event.currentTarget.value)} value={busqueda} />
      </label>
      <label>
        Estado
        <select
          aria-label="Estado"
          onChange={(event) => cambiarFiltros({ estado: event.currentTarget.value as EstadoLicitacion | '' })}
          value={filtros.estado ?? ''}
        >
          <option value="">Todos</option>
          {estados.map((estado) => <option key={estado} value={estado}>{estado}</option>)}
        </select>
      </label>
      <label>
        Tipo
        <select
          aria-label="Tipo"
          onChange={(event) => cambiarFiltros({ tipo: event.currentTarget.value as TipoLicitacion | '' })}
          value={filtros.tipo ?? ''}
        >
          <option value="">Todos</option>
          {(Object.keys(etiquetasTipo) as TipoLicitacion[]).map((tipo) => <option key={tipo} value={tipo}>{etiquetasTipo[tipo]}</option>)}
        </select>
      </label>
    </div>
  );
}
