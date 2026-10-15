import type { ClienteOpcion, EstadoReclamo, FiltrosReclamos, PrioridadReclamo, TipoReclamo } from './tipos';

export function ReclamosFiltros({ busqueda, filtros, clientes, cambiarBusqueda, cambiarFiltro }: {
  busqueda: string;
  filtros: FiltrosReclamos;
  clientes: ClienteOpcion[];
  cambiarBusqueda: (valor: string) => void;
  cambiarFiltro: (cambio: Partial<FiltrosReclamos>) => void;
}) {
  return (
    <div className="reclamos-filters">
      <label>Buscar<input aria-label="Buscar" onChange={(event) => cambiarBusqueda(event.currentTarget.value)} value={busqueda} /></label>
      <label>Estado<select aria-label="Estado" onChange={(event) => cambiarFiltro({ estado: event.currentTarget.value as EstadoReclamo | '' })} value={filtros.estado ?? ''}>
        <option value="">Todos</option><option>Abierto</option><option value="EnProceso">En proceso</option><option>Resuelto</option><option>Cerrado</option><option>Rechazado</option>
      </select></label>
      <label>Tipo<select aria-label="Tipo" onChange={(event) => cambiarFiltro({ tipo: event.currentTarget.value as TipoReclamo | '' })} value={filtros.tipo ?? ''}>
        <option value="">Todos</option><option>Defecto</option><option>Retraso</option><option>Faltante</option><option value="Facturacion">Facturación</option><option>Otro</option>
      </select></label>
      <label>Prioridad<select aria-label="Prioridad" onChange={(event) => cambiarFiltro({ prioridad: event.currentTarget.value as PrioridadReclamo | '' })} value={filtros.prioridad ?? ''}>
        <option value="">Todas</option><option>Alta</option><option>Media</option><option>Baja</option>
      </select></label>
      <label>Cliente<select aria-label="Cliente" onChange={(event) => cambiarFiltro({ clienteId: event.currentTarget.value === '' ? '' : Number(event.currentTarget.value) })} value={filtros.clienteId ?? ''}>
        <option value="">Todos</option>{clientes.map((cliente) => <option key={cliente.id} value={cliente.id}>{cliente.nombre}</option>)}
      </select></label>
      <label className="reclamos-checkbox"><input aria-label="Solo vencidos" checked={filtros.vencidos ?? false} onChange={(event) => cambiarFiltro({ vencidos: event.currentTarget.checked })} type="checkbox" />Solo vencidos</label>
    </div>
  );
}
