import { aEstadoFiltro, aTipoFiltro, ESTADOS_PLANILLA, TIPOS_PLANILLA } from './presentacion';
import type { EstadoPlanilla, TipoPlanilla } from './tipos';

interface Props {
  anioTexto: string;
  tipo: '' | TipoPlanilla;
  estado: '' | EstadoPlanilla;
  cambiarAnio: (texto: string) => void;
  cambiarTipo: (tipo: '' | TipoPlanilla) => void;
  cambiarEstado: (estado: '' | EstadoPlanilla) => void;
}

export function PlanillasFiltros({ anioTexto, tipo, estado, cambiarAnio, cambiarTipo, cambiarEstado }: Props) {
  return (
    <div className="solicitudes-filters planillas-filters">
      <div>
        <label htmlFor="planillas-anio">Año</label>
        <input
          autoComplete="off"
          id="planillas-anio"
          inputMode="numeric"
          maxLength={4}
          onChange={(event) => cambiarAnio(event.currentTarget.value)}
          placeholder="Todos"
          type="text"
          value={anioTexto}
        />
      </div>
      <div>
        <label htmlFor="planillas-tipo">Tipo</label>
        <select id="planillas-tipo" onChange={(event) => cambiarTipo(aTipoFiltro(event.currentTarget.value))} value={tipo}>
          <option value="">Todos</option>
          {TIPOS_PLANILLA.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="planillas-estado">Estado</label>
        <select id="planillas-estado" onChange={(event) => cambiarEstado(aEstadoFiltro(event.currentTarget.value))} value={estado}>
          <option value="">Todos</option>
          {ESTADOS_PLANILLA.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
    </div>
  );
}
