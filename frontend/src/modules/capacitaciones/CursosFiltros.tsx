import { aFiltroActivo, aTipoFiltro, FILTROS_ACTIVO, TIPOS_CURSO } from './presentacion';
import type { FiltroActivo, TipoCurso } from './tipos';

interface Props {
  busqueda: string;
  tipo: '' | TipoCurso;
  activo: FiltroActivo;
  cambiarBusqueda: (valor: string) => void;
  cambiarTipo: (tipo: '' | TipoCurso) => void;
  cambiarActivo: (activo: FiltroActivo) => void;
}

export function CursosFiltros({ busqueda, tipo, activo, cambiarBusqueda, cambiarTipo, cambiarActivo }: Props) {
  return (
    <div className="solicitudes-filters capacitaciones-filters capacitaciones-filters-tres">
      <div>
        <label htmlFor="cursos-busqueda">Buscar por código o nombre</label>
        <input
          autoComplete="off"
          id="cursos-busqueda"
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Escribe para buscar…"
          type="search"
          value={busqueda}
        />
      </div>
      <div>
        <label htmlFor="cursos-tipo">Tipo</label>
        <select id="cursos-tipo" onChange={(event) => cambiarTipo(aTipoFiltro(event.currentTarget.value))} value={tipo}>
          <option value="">Todos</option>
          {TIPOS_CURSO.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="cursos-activo">Estado</label>
        <select id="cursos-activo" onChange={(event) => cambiarActivo(aFiltroActivo(event.currentTarget.value))} value={activo}>
          {FILTROS_ACTIVO.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
    </div>
  );
}
