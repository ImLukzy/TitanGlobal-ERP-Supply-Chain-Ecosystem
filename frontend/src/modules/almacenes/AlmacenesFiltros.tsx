import type { ChangeEvent } from 'react';
import type { TipoAlmacen } from './tipos';

interface Props {
  busqueda: string;
  ciudades: string[];
  ciudad: string;
  tipo: TipoAlmacen | '';
  activo: boolean | '';
  onBusqueda: (valor: string) => void;
  onCiudad: (valor: string) => void;
  onTipo: (valor: TipoAlmacen | '') => void;
  onActivo: (valor: boolean | '') => void;
}

export function AlmacenesFiltros(props: Props) {
  function cambiarEstado(event: ChangeEvent<HTMLSelectElement>): void {
    const valor = event.currentTarget.value;
    props.onActivo(valor === '' ? '' : valor === 'true');
  }
  return (
    <div className="almacenes-filters">
      <div><label htmlFor="almacenes-busqueda">Buscar por código o nombre</label>
        <input autoComplete="off" id="almacenes-busqueda" onChange={(event) => props.onBusqueda(event.currentTarget.value)}
          placeholder="Escribe para buscar…" type="search" value={props.busqueda} /></div>
      <div><label htmlFor="almacenes-ciudad">Ciudad</label>
        <select id="almacenes-ciudad" onChange={(event) => props.onCiudad(event.currentTarget.value)} value={props.ciudad}>
          <option value="">Todas</option>{props.ciudades.map((ciudad) => <option key={ciudad} value={ciudad}>{ciudad}</option>)}
        </select></div>
      <div><label htmlFor="almacenes-tipo">Tipo</label>
        <select id="almacenes-tipo" onChange={(event) => props.onTipo(event.currentTarget.value as TipoAlmacen | '')} value={props.tipo}>
          <option value="">Todos</option><option value="Central">Central</option><option value="Regional">Regional</option>
          <option value="Transito">Tránsito</option>
        </select></div>
      <div><label htmlFor="almacenes-estado">Estado</label>
        <select id="almacenes-estado" onChange={cambiarEstado} value={props.activo === '' ? '' : String(props.activo)}>
          <option value="">Todos</option><option value="true">Activos</option><option value="false">Inactivos</option>
        </select></div>
    </div>
  );
}
