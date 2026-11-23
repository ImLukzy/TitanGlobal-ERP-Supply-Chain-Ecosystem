interface Props {
  busqueda: string;
  habilitado: boolean | '';
  onBusqueda: (valor: string) => void;
  onHabilitado: (valor: boolean | '') => void;
}

export function ConductoresFiltros(props: Props) {
  return (
    <div className="conductores-filters">
      <div><label htmlFor="conductores-busqueda">Buscar por nombre o licencia</label>
        <input id="conductores-busqueda" type="search" autoComplete="off" placeholder="Escribe para buscar…"
          value={props.busqueda} onChange={(event) => props.onBusqueda(event.currentTarget.value)} /></div>
      <div><label htmlFor="conductores-habilitado">Habilitación</label>
        <select id="conductores-habilitado" value={props.habilitado === '' ? '' : String(props.habilitado)} onChange={(event) => {
          const valor = event.currentTarget.value;
          props.onHabilitado(valor === '' ? '' : valor === 'true');
        }}><option value="">Todos</option><option value="true">Habilitados</option><option value="false">No habilitados</option></select>
      </div>
    </div>
  );
}
