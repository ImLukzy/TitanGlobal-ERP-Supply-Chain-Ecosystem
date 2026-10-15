import type { FiltrosOrdenAbastecimiento, ProveedorOrdenOpcion } from './tipos';

interface OrdenesFiltrosProps {
  busqueda: string;
  filtros: FiltrosOrdenAbastecimiento;
  proveedores: ProveedorOrdenOpcion[];
  errorFecha: string;
  cambiarBusqueda: (valor: string) => void;
  cambiarFiltros: (cambios: Partial<FiltrosOrdenAbastecimiento>) => void;
}

export function OrdenesFiltros({
  busqueda,
  filtros,
  proveedores,
  errorFecha,
  cambiarBusqueda,
  cambiarFiltros,
}: OrdenesFiltrosProps) {
  return (
    <div className="ordenes-filters">
      <label>
        Buscar número o proveedor
        <input aria-label="Buscar" onChange={(event) => cambiarBusqueda(event.currentTarget.value)} value={busqueda} />
      </label>
      <label>
        Estado
        <select aria-label="Estado" onChange={(event) => cambiarFiltros({ estado: event.currentTarget.value as FiltrosOrdenAbastecimiento['estado'] })} value={filtros.estado ?? ''}>
          <option value="">Todos</option>
          <option value="Emitida">Emitida</option>
          <option value="Recibida">Recibida</option>
          <option value="Anulada">Anulada</option>
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
      <label>
        Desde
        <input aria-label="Fecha desde" onChange={(event) => cambiarFiltros({ fechaDesde: event.currentTarget.value })} type="date" value={filtros.fechaDesde ?? ''} />
      </label>
      <label>
        Hasta
        <input aria-label="Fecha hasta" onChange={(event) => cambiarFiltros({ fechaHasta: event.currentTarget.value })} type="date" value={filtros.fechaHasta ?? ''} />
      </label>
      {errorFecha !== '' && <p className="ordenes-field-error" role="alert">{errorFecha}</p>}
    </div>
  );
}
