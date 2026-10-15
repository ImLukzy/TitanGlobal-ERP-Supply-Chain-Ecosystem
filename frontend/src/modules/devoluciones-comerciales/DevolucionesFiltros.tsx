import type { ClienteOpcion, EstadoDevolucion, FiltrosDevolucion, MotivoDevolucion } from './tipos';

interface Props {
  busqueda: string;
  filtros: FiltrosDevolucion;
  clientes: ClienteOpcion[];
  cambiarBusqueda: (valor: string) => void;
  cambiarFiltro: (cambios: Partial<FiltrosDevolucion>) => void;
}

export function DevolucionesFiltros({ busqueda, filtros, clientes, cambiarBusqueda, cambiarFiltro }: Props) {
  return (
    <div className="solicitudes-filters devoluciones-filters">
      <div>
        <label htmlFor="devoluciones-busqueda">Buscar</label>
        <input
          id="devoluciones-busqueda"
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Número, orden o cliente"
          type="search"
          value={busqueda}
        />
      </div>
      <div>
        <label htmlFor="devoluciones-estado">Estado</label>
        <select
          id="devoluciones-estado"
          onChange={(event) => cambiarFiltro({ estado: event.currentTarget.value as EstadoDevolucion | '' })}
          value={filtros.estado}
        >
          <option value="">Todos</option>
          <option>Solicitada</option>
          <option>Aprobada</option>
          <option>Recibida</option>
          <option>Rechazada</option>
        </select>
      </div>
      <div>
        <label htmlFor="devoluciones-motivo">Motivo</label>
        <select
          id="devoluciones-motivo"
          onChange={(event) => cambiarFiltro({ motivo: event.currentTarget.value as MotivoDevolucion | '' })}
          value={filtros.motivo}
        >
          <option value="">Todos</option>
          <option value="Defecto">Defecto</option>
          <option value="ErrorPedido">Error de pedido</option>
          <option>Arrepentimiento</option>
          <option>Otro</option>
        </select>
      </div>
      <div>
        <label htmlFor="devoluciones-cliente">Cliente</label>
        <select
          id="devoluciones-cliente"
          onChange={(event) => cambiarFiltro({ clienteId: event.currentTarget.value ? Number(event.currentTarget.value) : '' })}
          value={filtros.clienteId}
        >
          <option value="">Todos</option>
          {clientes.map((cliente) => (
            <option key={cliente.id} value={cliente.id}>{cliente.nombre}</option>
          ))}
        </select>
      </div>
    </div>
  );
}
