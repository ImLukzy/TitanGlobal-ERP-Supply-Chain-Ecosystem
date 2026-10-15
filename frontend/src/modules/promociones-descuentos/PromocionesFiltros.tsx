import type { FiltrosPromociones, OpcionSegmento, TipoPromocion } from './tipos';

interface Props {
  busqueda: string;
  filtros: FiltrosPromociones;
  segmentos: OpcionSegmento[];
  cambiarBusqueda: (valor: string) => void;
  cambiarFiltro: (cambios: Partial<FiltrosPromociones>) => void;
}

export function PromocionesFiltros({ busqueda, filtros, segmentos, cambiarBusqueda, cambiarFiltro }: Props) {
  return <div className="solicitudes-filters promociones-filters">
    <div><label htmlFor="promociones-busqueda">Buscar por código o nombre</label><input id="promociones-busqueda" onChange={(evento) => cambiarBusqueda(evento.currentTarget.value)} placeholder="Código o nombre" type="search" value={busqueda} /></div>
    <div><label htmlFor="promociones-tipo">Tipo</label><select id="promociones-tipo" onChange={(evento) => cambiarFiltro({ tipo: evento.currentTarget.value as TipoPromocion | '' })} value={filtros.tipo ?? ''}><option value="">Todos</option><option value="Producto">Producto</option><option value="MontoMinimo">Monto mínimo</option></select></div>
    <div><label htmlFor="promociones-segmento">Segmento</label><select id="promociones-segmento" onChange={(evento) => cambiarFiltro({ segmentoId: evento.currentTarget.value ? Number(evento.currentTarget.value) : '' })} value={filtros.segmentoId ?? ''}><option value="">Todos</option>{segmentos.map((segmento) => <option key={segmento.id} value={segmento.id}>{segmento.codigo} · {segmento.nombre}</option>)}</select></div>
    <label className="promociones-checkbox"><input checked={Boolean(filtros.vigentes)} onChange={(evento) => cambiarFiltro({ vigentes: evento.currentTarget.checked })} type="checkbox" />Solo vigentes</label>
  </div>;
}
