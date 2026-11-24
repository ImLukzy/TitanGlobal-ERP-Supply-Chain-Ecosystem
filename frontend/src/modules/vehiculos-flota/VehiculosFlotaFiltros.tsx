import { nombreTipo } from './presentacion';

interface Props {
  busqueda: string;
  tipos: string[];
  tipo: string;
  activo: boolean | '';
  onBusqueda: (valor: string) => void;
  onTipo: (valor: string) => void;
  onActivo: (valor: boolean | '') => void;
}

export function VehiculosFlotaFiltros(props: Props) {
  return (
    <div className="vehiculos-flota-filters">
      <div><label htmlFor="vehiculos-busqueda">Buscar por placa, marca o modelo</label>
        <input id="vehiculos-busqueda" type="search" autoComplete="off" placeholder="Escribe para buscar…"
          value={props.busqueda} onChange={(event) => props.onBusqueda(event.currentTarget.value)} /></div>
      <div><label htmlFor="vehiculos-tipo">Tipo</label>
        <select id="vehiculos-tipo" value={props.tipo} onChange={(event) => props.onTipo(event.currentTarget.value)}>
          <option value="">Todos</option>{props.tipos.map((tipo) => <option key={tipo} value={tipo}>{nombreTipo(tipo)}</option>)}
        </select></div>
      <div><label htmlFor="vehiculos-estado">Estado</label>
        <select id="vehiculos-estado" value={props.activo === '' ? '' : String(props.activo)} onChange={(event) => {
          const valor = event.currentTarget.value;
          props.onActivo(valor === '' ? '' : valor === 'true');
        }}><option value="">Todos</option><option value="true">Activos</option><option value="false">Inactivos</option></select>
      </div>
    </div>
  );
}
