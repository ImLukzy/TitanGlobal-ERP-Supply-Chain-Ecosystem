import { calificaciones } from './puntaje';
import type { CalificacionEvaluacion, FiltrosEvaluacion, ProveedorOpcion } from './tipos';

interface EvaluacionesFiltrosProps {
  filtros: FiltrosEvaluacion;
  proveedores: ProveedorOpcion[];
  cambiarFiltros: (cambios: Partial<FiltrosEvaluacion>) => void;
}

export function EvaluacionesFiltros({ filtros, proveedores, cambiarFiltros }: EvaluacionesFiltrosProps) {
  const periodo = filtros.periodo ?? '';
  return (
    <div className="evaluaciones-filters">
      <label>
        Proveedor
        <select
          aria-label="Proveedor"
          onChange={(event) => cambiarFiltros({ proveedorId: event.currentTarget.value === '' ? '' : Number(event.currentTarget.value) })}
          value={filtros.proveedorId ?? ''}
        >
          <option value="">Todos</option>
          {proveedores.map((proveedor) => <option key={proveedor.id} value={proveedor.id}>{proveedor.razonSocial}</option>)}
        </select>
      </label>
      <div className="evaluaciones-filter-periodo">
        <label>
          Periodo
          <input aria-label="Periodo" onChange={(event) => cambiarFiltros({ periodo: event.currentTarget.value })} type="month" value={periodo} />
        </label>
        {periodo !== '' && (
          <button className="secondary-button" onClick={() => cambiarFiltros({ periodo: '' })} type="button">Quitar</button>
        )}
      </div>
      <label>
        Calificación
        <select
          aria-label="Calificación"
          onChange={(event) => cambiarFiltros({ calificacion: event.currentTarget.value as CalificacionEvaluacion | '' })}
          value={filtros.calificacion ?? ''}
        >
          <option value="">Todas</option>
          {calificaciones.map((calificacion) => <option key={calificacion} value={calificacion}>{calificacion}</option>)}
        </select>
      </label>
    </div>
  );
}
