export function SucursalesFiltros({
  busqueda,
  pais,
  tipo,
  estado,
  cambiarBusqueda,
  cambiarPais,
  cambiarTipo,
  cambiarEstado,
}: {
  busqueda: string;
  pais: string;
  tipo: string;
  estado: string;
  cambiarBusqueda: (v: string) => void;
  cambiarPais: (v: string) => void;
  cambiarTipo: (v: string) => void;
  cambiarEstado: (v: string) => void;
}) {
  return (
    <div className="sucursales-filters">
      <label>
        Buscar código, nombre o ciudad
        <input type="search" value={busqueda} onChange={(e) => cambiarBusqueda(e.target.value)} />
      </label>
      <label>
        País (coincidencia exacta)
        <input value={pais} onChange={(e) => cambiarPais(e.target.value)} />
      </label>
      <label>
        Tipo
        <select value={tipo} onChange={(e) => cambiarTipo(e.target.value)}>
          <option value="">Todos</option>
          {['Filial', 'Planta', 'Oficina'].map((t) => (
            <option key={t}>{t}</option>
          ))}
        </select>
      </label>
      <label>
        Estado
        <select value={estado} onChange={(e) => cambiarEstado(e.target.value)}>
          <option value="">Todos</option>
          <option value="true">Activas</option>
          <option value="false">Inactivas</option>
        </select>
      </label>
    </div>
  );
}
