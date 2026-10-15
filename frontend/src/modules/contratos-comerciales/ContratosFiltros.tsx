import type { ClienteOpcion, EstadoContrato, FiltrosContrato } from './tipos';

interface Props {
  busqueda: string;
  filtros: FiltrosContrato;
  clientes: ClienteOpcion[];
  cambiarBusqueda: (valor: string) => void;
  cambiarFiltro: (cambios: Partial<FiltrosContrato>) => void;
}

export function ContratosFiltros({ busqueda, filtros, clientes, cambiarBusqueda, cambiarFiltro }: Props) {
  return (
    <div className="solicitudes-filters contratos-filters">
      <div><label htmlFor="contratos-busqueda">Buscar por número, título o cliente</label><input id="contratos-busqueda" onChange={(event) => cambiarBusqueda(event.currentTarget.value)} placeholder="Número, título o cliente" type="search" value={busqueda} /></div>
      <div><label htmlFor="contratos-estado">Estado</label><select id="contratos-estado" onChange={(event) => cambiarFiltro({ estado: event.currentTarget.value as EstadoContrato | '' })} value={filtros.estado ?? ''}>
        <option value="">Todos</option><option>Borrador</option><option>Vigente</option><option>Finalizado</option><option>Cancelado</option>
      </select></div>
      <div><label htmlFor="contratos-cliente">Cliente</label><select id="contratos-cliente" onChange={(event) => cambiarFiltro({ clienteId: event.currentTarget.value ? Number(event.currentTarget.value) : '' })} value={filtros.clienteId ?? ''}>
        <option value="">Todos</option>{clientes.map((cliente) => <option key={cliente.id} value={cliente.id}>{cliente.nombre}</option>)}
      </select></div>
      <label className="cotizaciones-expired"><input checked={Boolean(filtros.vencidos)} onChange={(event) => cambiarFiltro({ vencidos: event.currentTarget.checked })} type="checkbox" />Solo vencidos</label>
    </div>
  );
}
