import type { EstadoAcuerdo, FiltrosAcuerdo, ProveedorOpcion } from './tipos';
import { estados } from './vigencia';

interface AcuerdosFiltrosProps {
  busqueda: string;
  filtros: FiltrosAcuerdo;
  proveedores: ProveedorOpcion[];
  cambiarBusqueda: (valor: string) => void;
  cambiarFiltros: (cambios: Partial<FiltrosAcuerdo>) => void;
}

export function AcuerdosFiltros({ busqueda, filtros, proveedores, cambiarBusqueda, cambiarFiltros }: AcuerdosFiltrosProps) {
  return (
    <div className="acuerdos-filters">
      <label>
        Buscar número o proveedor
        <input aria-label="Buscar" onChange={(event) => cambiarBusqueda(event.currentTarget.value)} value={busqueda} />
      </label>
      <label>
        Estado
        <select
          aria-label="Estado"
          onChange={(event) => cambiarFiltros({ estado: event.currentTarget.value as EstadoAcuerdo | '' })}
          value={filtros.estado ?? ''}
        >
          <option value="">Todos</option>
          {estados.map((estado) => <option key={estado} value={estado}>{estado}</option>)}
        </select>
      </label>
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
    </div>
  );
}
