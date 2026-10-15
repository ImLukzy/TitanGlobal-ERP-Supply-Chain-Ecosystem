import type { TemperaturaZona, TipoZona } from './tipos';

interface Props {
  busqueda: string;
  tipo: TipoZona | '';
  temperatura: TemperaturaZona | '';
  activa: boolean | '';
  buscar: (busqueda: string) => void;
  cambiarTipo: (tipo: TipoZona | '') => void;
  cambiarTemperatura: (temperatura: TemperaturaZona | '') => void;
  cambiarActiva: (activa: boolean | '') => void;
}

export function ZonasFiltros(props: Props) {
  return (
    <div className="zonas-filters">
      <div>
        <label htmlFor="zonas-busqueda">Buscar por código o nombre</label>
        <input
          autoComplete="off"
          id="zonas-busqueda"
          onChange={(event) => props.buscar(event.currentTarget.value)}
          placeholder="Escribe para buscar…"
          type="search"
          value={props.busqueda}
        />
      </div>
      <div>
        <label htmlFor="zonas-tipo">Tipo</label>
        <select id="zonas-tipo" onChange={(event) => props.cambiarTipo(event.currentTarget.value as TipoZona | '')} value={props.tipo}>
          <option value="">Todos</option>
          <option value="Recepcion">Recepción</option>
          <option value="Almacenamiento">Almacenamiento</option>
          <option value="Picking">Picking</option>
          <option value="Despacho">Despacho</option>
          <option value="Cuarentena">Cuarentena</option>
        </select>
      </div>
      <div>
        <label htmlFor="zonas-temperatura">Temperatura</label>
        <select id="zonas-temperatura" onChange={(event) => props.cambiarTemperatura(event.currentTarget.value as TemperaturaZona | '')} value={props.temperatura}>
          <option value="">Todas</option>
          <option value="Ambiente">Ambiente</option>
          <option value="Refrigerada">Refrigerada</option>
          <option value="Congelada">Congelada</option>
        </select>
      </div>
      <div>
        <label htmlFor="zonas-activa">Estado</label>
        <select
          id="zonas-activa"
          onChange={(event) => props.cambiarActiva(event.currentTarget.value === '' ? '' : event.currentTarget.value === 'true')}
          value={props.activa === '' ? '' : String(props.activa)}
        >
          <option value="">Todos</option>
          <option value="true">Activas</option>
          <option value="false">Inactivas</option>
        </select>
      </div>
    </div>
  );
}
