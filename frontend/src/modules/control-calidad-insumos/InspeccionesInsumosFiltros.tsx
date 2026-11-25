import { CampoConError } from '../../components/CampoConError';
import { buscarProductos, buscarProveedoresActivos } from './api';
import { SelectorCatalogo } from './SelectorCatalogo';
import { estadosInspeccion, type EstadoInspeccion, type OpcionCatalogo } from './tipos';
import { mensajes } from './validacion';

export interface ValoresFiltros {
  busqueda: string;
  producto: OpcionCatalogo | null;
  proveedor: OpcionCatalogo | null;
  estado: EstadoInspeccion | '';
  desde: string;
  hasta: string;
}

interface FiltrosProps {
  valores: ValoresFiltros;
  cambiar: (cambios: Partial<ValoresFiltros>) => void;
  rangoInvalido: boolean;
}

export function InspeccionesInsumosFiltros({ valores, cambiar, rangoInvalido }: FiltrosProps) {
  const errorRango = rangoInvalido ? mensajes.rangoFechas : undefined;

  return (
    <div className="control-calidad-insumos-filtros">
      <div className="form-field">
        <label htmlFor="control-calidad-insumos-busqueda">Buscar por número o lote</label>
        <input
          id="control-calidad-insumos-busqueda"
          maxLength={50}
          onChange={(event) => cambiar({ busqueda: event.currentTarget.value })}
          type="search"
          value={valores.busqueda}
        />
      </div>
      <SelectorCatalogo
        buscar={buscarProductos}
        cambiar={(producto) => cambiar({ producto })}
        etiqueta="Filtrar por insumo"
        seleccionado={valores.producto}
        textoBoton="Todos los insumos"
      />
      <SelectorCatalogo
        buscar={buscarProveedoresActivos}
        cambiar={(proveedor) => cambiar({ proveedor })}
        etiqueta="Filtrar por proveedor"
        seleccionado={valores.proveedor}
        textoBoton="Todos los proveedores"
      />
      <div className="form-field">
        <label htmlFor="control-calidad-insumos-estado">Filtrar por estado</label>
        <select
          id="control-calidad-insumos-estado"
          onChange={(event) => cambiar({ estado: event.currentTarget.value as EstadoInspeccion | '' })}
          value={valores.estado}
        >
          <option value="">Todos</option>
          {estadosInspeccion.map((estado) => <option key={estado}>{estado}</option>)}
        </select>
      </div>
      <CampoConError etiqueta="Desde" error={errorRango} htmlFor="control-calidad-insumos-desde" idError="control-calidad-insumos-rango-error">
        <input
          aria-describedby={errorRango ? 'control-calidad-insumos-rango-error' : undefined}
          aria-invalid={rangoInvalido}
          id="control-calidad-insumos-desde"
          onChange={(event) => cambiar({ desde: event.currentTarget.value })}
          type="date"
          value={valores.desde}
        />
      </CampoConError>
      <div className="form-field">
        <label htmlFor="control-calidad-insumos-hasta">Hasta</label>
        <input
          aria-describedby={errorRango ? 'control-calidad-insumos-rango-error' : undefined}
          aria-invalid={rangoInvalido}
          id="control-calidad-insumos-hasta"
          onChange={(event) => cambiar({ hasta: event.currentTarget.value })}
          type="date"
          value={valores.hasta}
        />
      </div>
    </div>
  );
}
