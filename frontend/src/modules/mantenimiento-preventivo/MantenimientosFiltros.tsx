import { CampoConError } from '../../components/CampoConError';
import { SelectorCentro } from './SelectorCentro';
import { estadosMantenimiento, etiquetasEstado, type CentroOpcion, type EstadoMantenimiento } from './tipos';
import { mensajes } from './validacion';

export interface ValoresFiltros {
  busqueda: string;
  centro: CentroOpcion | null;
  estado: EstadoMantenimiento | '';
  desde: string;
  hasta: string;
}

interface FiltrosProps {
  valores: ValoresFiltros;
  cambiar: (cambios: Partial<ValoresFiltros>) => void;
  rangoInvalido: boolean;
}

export function MantenimientosFiltros({ valores, cambiar, rangoInvalido }: FiltrosProps) {
  const errorRango = rangoInvalido ? mensajes.rangoFechas : undefined;

  return (
    <div className="mantenimiento-preventivo-filtros">
      <div className="form-field">
        <label htmlFor="mantenimiento-preventivo-busqueda">Buscar por número o descripción</label>
        <input
          id="mantenimiento-preventivo-busqueda"
          maxLength={50}
          onChange={(event) => cambiar({ busqueda: event.currentTarget.value })}
          type="search"
          value={valores.busqueda}
        />
      </div>
      <SelectorCentro
        cambiar={(centro) => cambiar({ centro })}
        etiqueta="Filtrar por centro"
        seleccionado={valores.centro}
        soloActivos={false}
        textoBoton="Todos los centros"
      />
      <div className="form-field">
        <label htmlFor="mantenimiento-preventivo-estado">Filtrar por estado</label>
        <select
          id="mantenimiento-preventivo-estado"
          onChange={(event) => cambiar({ estado: event.currentTarget.value as EstadoMantenimiento | '' })}
          value={valores.estado}
        >
          <option value="">Todos</option>
          {estadosMantenimiento.map((estado) => <option key={estado} value={estado}>{etiquetasEstado[estado]}</option>)}
        </select>
      </div>
      <CampoConError etiqueta="Desde" error={errorRango} htmlFor="mantenimiento-preventivo-desde" idError="mantenimiento-preventivo-rango-error">
        <input
          aria-describedby={errorRango ? 'mantenimiento-preventivo-rango-error' : undefined}
          aria-invalid={rangoInvalido}
          id="mantenimiento-preventivo-desde"
          onChange={(event) => cambiar({ desde: event.currentTarget.value })}
          type="date"
          value={valores.desde}
        />
      </CampoConError>
      <div className="form-field">
        <label htmlFor="mantenimiento-preventivo-hasta">Hasta</label>
        <input
          aria-describedby={errorRango ? 'mantenimiento-preventivo-rango-error' : undefined}
          aria-invalid={rangoInvalido}
          id="mantenimiento-preventivo-hasta"
          onChange={(event) => cambiar({ hasta: event.currentTarget.value })}
          type="date"
          value={valores.hasta}
        />
      </div>
    </div>
  );
}
