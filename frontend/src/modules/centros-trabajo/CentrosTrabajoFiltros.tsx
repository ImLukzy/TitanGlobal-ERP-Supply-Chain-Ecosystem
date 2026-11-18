import type { TipoCentroTrabajo } from './tipos';

interface Props {
  busqueda: string;
  plantas: string[];
  planta: string;
  tipo: TipoCentroTrabajo | '';
  activo: boolean | '';
  onBusqueda: (valor: string) => void;
  onPlanta: (valor: string) => void;
  onTipo: (valor: TipoCentroTrabajo | '') => void;
  onActivo: (valor: boolean | '') => void;
}

export function CentrosTrabajoFiltros(props: Props) {
  return (
    <div className="centros-trabajo-filters">
      <div><label htmlFor="centros-busqueda">Buscar por código o nombre</label>
        <input id="centros-busqueda" type="search" autoComplete="off" placeholder="Escribe para buscar…"
          value={props.busqueda} onChange={(event) => props.onBusqueda(event.currentTarget.value)} /></div>
      <div><label htmlFor="centros-planta">Planta</label>
        <select id="centros-planta" value={props.planta} onChange={(event) => props.onPlanta(event.currentTarget.value)}>
          <option value="">Todas</option>{props.plantas.map((planta) => <option key={planta} value={planta}>{planta}</option>)}
        </select></div>
      <div><label htmlFor="centros-tipo">Tipo</label>
        <select id="centros-tipo" value={props.tipo} onChange={(event) => props.onTipo(event.currentTarget.value as TipoCentroTrabajo | '')}>
          <option value="">Todos</option><option value="Linea">Línea</option><option value="Celda">Celda</option>
        </select></div>
      <div><label htmlFor="centros-estado">Estado</label>
        <select id="centros-estado" value={props.activo === '' ? '' : String(props.activo)} onChange={(event) => {
          const valor = event.currentTarget.value;
          props.onActivo(valor === '' ? '' : valor === 'true');
        }}>
          <option value="">Todos</option><option value="true">Activos</option><option value="false">Inactivos</option>
        </select></div>
    </div>
  );
}
