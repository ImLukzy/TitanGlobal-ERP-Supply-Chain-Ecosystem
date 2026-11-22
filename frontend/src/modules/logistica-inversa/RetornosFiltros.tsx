export function RetornosFiltros({
  busqueda,
  estado,
  tipo,
  cambiarBusqueda,
  cambiarEstado,
  cambiarTipo,
}: {
  busqueda: string;
  estado: string;
  tipo: string;
  cambiarBusqueda: (v: string) => void;
  cambiarEstado: (v: string) => void;
  cambiarTipo: (v: string) => void;
}) {
  return (
    <div className="logistica-inversa-filters">
      <label>
        Buscar código, referencia, contraparte o material
        <input type="search" value={busqueda} onChange={(e) => cambiarBusqueda(e.target.value)} />
      </label>
      <label>
        Estado
        <select value={estado} onChange={(e) => cambiarEstado(e.target.value)}>
          <option value="">Todos</option>
          {['Programado', 'Recogido', 'Recibido', 'Cancelado'].map((x) => (
            <option key={x}>{x}</option>
          ))}
        </select>
      </label>
      <label>
        Tipo
        <select value={tipo} onChange={(e) => cambiarTipo(e.target.value)}>
          <option value="">Todos</option>
          {['Pallet', 'Retornable', 'DevolucionRuta'].map((x) => (
            <option key={x}>{x}</option>
          ))}
        </select>
      </label>
    </div>
  );
}
