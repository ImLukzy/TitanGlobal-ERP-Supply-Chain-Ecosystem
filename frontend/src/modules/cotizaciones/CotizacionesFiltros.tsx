import type { EstadoCotizacion, FiltrosCotizacion } from './tipos';

interface Props {
  busqueda: string;
  filtros: FiltrosCotizacion;
  clientes: { id: number; nombre: string }[];
  errorFecha: string;
  cambiarBusqueda: (valor: string) => void;
  cambiarFiltros: (cambios: Partial<FiltrosCotizacion>) => void;
}

export function CotizacionesFiltros({
  busqueda,
  filtros,
  clientes,
  errorFecha,
  cambiarBusqueda,
  cambiarFiltros,
}: Props) {
  return (
    <div className="solicitudes-filters cotizaciones-filters">
      <div>
        <label htmlFor="cotizaciones-busqueda">
          Buscar por número o cliente
        </label>
        <input
          id="cotizaciones-busqueda"
          type="search"
          value={busqueda}
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
        />
      </div>
      <div>
        <label htmlFor="cotizaciones-estado">Estado</label>
        <select
          id="cotizaciones-estado"
          value={filtros.estado ?? ''}
          onChange={(event) =>
            cambiarFiltros({
              estado: event.currentTarget.value as EstadoCotizacion | '',
            })
          }
        >
          <option value="">Todos</option>
          <option>Emitida</option>
          <option>Aceptada</option>
          <option>Rechazada</option>
        </select>
      </div>
      <div>
        <label htmlFor="cotizaciones-cliente">Cliente</label>
        <select
          id="cotizaciones-cliente"
          value={filtros.clienteId ?? ''}
          onChange={(event) =>
            cambiarFiltros({
              clienteId: event.currentTarget.value
                ? Number(event.currentTarget.value)
                : '',
            })
          }
        >
          <option value="">Todos</option>
          {clientes.map((cliente) => (
            <option key={cliente.id} value={cliente.id}>
              {cliente.nombre}
            </option>
          ))}
        </select>
      </div>
      <div>
        <label htmlFor="cotizaciones-desde">Desde</label>
        <input
          id="cotizaciones-desde"
          type="date"
          value={filtros.fechaDesde ?? ''}
          aria-describedby={errorFecha ? 'cotizaciones-fecha-error' : undefined}
          aria-invalid={Boolean(errorFecha)}
          onChange={(event) =>
            cambiarFiltros({ fechaDesde: event.currentTarget.value })
          }
        />
      </div>
      <div>
        <label htmlFor="cotizaciones-hasta">Hasta</label>
        <input
          id="cotizaciones-hasta"
          type="date"
          value={filtros.fechaHasta ?? ''}
          aria-describedby={errorFecha ? 'cotizaciones-fecha-error' : undefined}
          aria-invalid={Boolean(errorFecha)}
          onChange={(event) =>
            cambiarFiltros({ fechaHasta: event.currentTarget.value })
          }
        />
      </div>
      <label className="cotizaciones-expired">
        <input
          type="checkbox"
          checked={Boolean(filtros.vencidas)}
          onChange={(event) =>
            cambiarFiltros({ vencidas: event.currentTarget.checked })
          }
        />{' '}
        Solo vencidas
      </label>
      {errorFecha && (
        <p
          className="solicitudes-field-error"
          id="cotizaciones-fecha-error"
          role="alert"
        >
          {errorFecha}
        </p>
      )}
    </div>
  );
}
