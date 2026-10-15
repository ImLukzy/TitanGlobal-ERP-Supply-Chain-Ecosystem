import type { EstadoSolicitud, FiltrosSolicitud, ProveedorOpcion } from './tipos';

interface SolicitudesFiltrosProps {
  busqueda: string;
  filtros: FiltrosSolicitud;
  proveedores: ProveedorOpcion[];
  errorFecha: string;
  cambiarBusqueda: (valor: string) => void;
  cambiarFiltros: (cambios: Partial<FiltrosSolicitud>) => void;
}

const estados: EstadoSolicitud[] = ['Pendiente', 'Aprobada', 'Rechazada'];

export function SolicitudesFiltros({
  busqueda,
  filtros,
  proveedores,
  errorFecha,
  cambiarBusqueda,
  cambiarFiltros,
}: SolicitudesFiltrosProps) {
  return (
    <div className="solicitudes-filters">
      <div>
        <label htmlFor="solicitudes-busqueda">Buscar por número o solicitante</label>
        <input
          id="solicitudes-busqueda"
          aria-label="Buscar por número o solicitante"
          type="search"
          value={busqueda}
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
        />
      </div>
      <div>
        <label htmlFor="solicitudes-estado">Estado</label>
        <select
          id="solicitudes-estado"
          value={filtros.estado ?? ''}
          onChange={(event) => cambiarFiltros({ estado: event.currentTarget.value as EstadoSolicitud | '' })}
        >
          <option value="">Todos</option>
          {estados.map((estado) => <option key={estado} value={estado}>{estado}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="solicitudes-proveedor">Proveedor</label>
        <select
          id="solicitudes-proveedor"
          value={filtros.proveedorId ?? ''}
          onChange={(event) => cambiarFiltros({ proveedorId: event.currentTarget.value === '' ? '' : Number(event.currentTarget.value) })}
        >
          <option value="">Todos</option>
          {proveedores.map((proveedor) => (
            <option key={proveedor.id} value={proveedor.id}>{proveedor.razonSocial}</option>
          ))}
        </select>
      </div>
      <div>
        <label htmlFor="solicitudes-fecha-desde">Desde</label>
        <input
          id="solicitudes-fecha-desde"
          aria-describedby={errorFecha ? 'solicitudes-fecha-error' : undefined}
          aria-invalid={Boolean(errorFecha)}
          type="date"
          value={filtros.fechaDesde ?? ''}
          onChange={(event) => cambiarFiltros({ fechaDesde: event.currentTarget.value })}
        />
      </div>
      <div>
        <label htmlFor="solicitudes-fecha-hasta">Hasta</label>
        <input
          id="solicitudes-fecha-hasta"
          aria-describedby={errorFecha ? 'solicitudes-fecha-error' : undefined}
          aria-invalid={Boolean(errorFecha)}
          type="date"
          value={filtros.fechaHasta ?? ''}
          onChange={(event) => cambiarFiltros({ fechaHasta: event.currentTarget.value })}
        />
      </div>
      {errorFecha && (
        <p className="solicitudes-field-error" id="solicitudes-fecha-error" role="alert">
          {errorFecha}
        </p>
      )}
    </div>
  );
}
