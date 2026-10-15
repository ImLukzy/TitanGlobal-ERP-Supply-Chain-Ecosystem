interface Props {
  busqueda: string;
  activo: boolean | '';
  cambiarBusqueda: (valor: string) => void;
  cambiarActivo: (valor: boolean | '') => void;
}

export function SegmentosFiltros({ busqueda, activo, cambiarBusqueda, cambiarActivo }: Props) {
  return (
    <div className="segmentos-filters">
      <label htmlFor="segmento-busqueda">Buscar código o nombre
        <input autoComplete="off" id="segmento-busqueda" onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Escribe para buscar…" type="search" value={busqueda} />
      </label>
      <label htmlFor="segmento-activo">Estado
        <select id="segmento-activo" onChange={(event) => cambiarActivo(event.currentTarget.value === '' ? '' : event.currentTarget.value === 'true')}
          value={activo === '' ? '' : String(activo)}>
          <option value="">Todos</option>
          <option value="true">Activos</option>
          <option value="false">Inactivos</option>
        </select>
      </label>
    </div>
  );
}
