import { aEstadoProgramacionFiltro, ESTADOS_PROGRAMACION } from './presentacion';
import type { CursoListado, EstadoProgramacion } from './tipos';

interface Props {
  cursoId: number | null;
  estado: '' | EstadoProgramacion;
  cursos: CursoListado[];
  cambiarCurso: (cursoId: number | null) => void;
  cambiarEstado: (estado: '' | EstadoProgramacion) => void;
}

export function ProgramacionesFiltros({ cursoId, estado, cursos, cambiarCurso, cambiarEstado }: Props) {
  return (
    <div className="solicitudes-filters capacitaciones-filters">
      <div>
        <label htmlFor="programaciones-curso">Curso</label>
        <select
          id="programaciones-curso"
          onChange={(event) => cambiarCurso(event.currentTarget.value === '' ? null : Number(event.currentTarget.value))}
          value={cursoId === null ? '' : String(cursoId)}
        >
          <option value="">Todos</option>
          {cursos.map((curso) => <option key={curso.id} value={String(curso.id)}>{`${curso.codigo} · ${curso.nombre}`}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="programaciones-estado">Estado</label>
        <select id="programaciones-estado" onChange={(event) => cambiarEstado(aEstadoProgramacionFiltro(event.currentTarget.value))} value={estado}>
          <option value="">Todos</option>
          {ESTADOS_PROGRAMACION.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
    </div>
  );
}
