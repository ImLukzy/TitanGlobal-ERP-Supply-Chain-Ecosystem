import { aEstadoEvaluacionFiltro, ESTADOS_EVALUACION } from './presentacion';
import type { CicloListado, EstadoEvaluacion } from './tipos';

interface Props {
  busqueda: string;
  cicloId: number | null;
  estado: '' | EstadoEvaluacion;
  ciclos: CicloListado[];
  cambiarBusqueda: (valor: string) => void;
  cambiarCiclo: (cicloId: number | null) => void;
  cambiarEstado: (estado: '' | EstadoEvaluacion) => void;
}

export function EvaluacionesFiltros({ busqueda, cicloId, estado, ciclos, cambiarBusqueda, cambiarCiclo, cambiarEstado }: Props) {
  return (
    <div className="solicitudes-filters evaluacion-desempeno-filters evaluacion-desempeno-filters-tres">
      <div>
        <label htmlFor="evaluaciones-busqueda">Buscar por empleado</label>
        <input
          autoComplete="off"
          id="evaluaciones-busqueda"
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Escribe para buscar…"
          type="search"
          value={busqueda}
        />
      </div>
      <div>
        <label htmlFor="evaluaciones-ciclo">Ciclo</label>
        <select id="evaluaciones-ciclo" onChange={(event) => cambiarCiclo(event.currentTarget.value === '' ? null : Number(event.currentTarget.value))} value={cicloId === null ? '' : String(cicloId)}>
          <option value="">Todos</option>
          {ciclos.map((ciclo) => <option key={ciclo.id} value={String(ciclo.id)}>{ciclo.codigo}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="evaluaciones-estado">Estado</label>
        <select id="evaluaciones-estado" onChange={(event) => cambiarEstado(aEstadoEvaluacionFiltro(event.currentTarget.value))} value={estado}>
          <option value="">Todos</option>
          {ESTADOS_EVALUACION.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
    </div>
  );
}
