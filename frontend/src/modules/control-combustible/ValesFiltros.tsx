export function ValesFiltros({
  busqueda,
  estado,
  tipoCombustible,
  cambiarBusqueda,
  cambiarEstado,
  cambiarTipo,
}: {
  busqueda: string;
  estado: string;
  tipoCombustible: string;
  cambiarBusqueda: (v: string) => void;
  cambiarEstado: (v: string) => void;
  cambiarTipo: (v: string) => void;
}) {
  return (
    <section aria-label="Filtros de vales" className="control-combustible-filters">
      <label>
        Buscar código, placa o estación
        <input type="search" value={busqueda} onChange={(e) => cambiarBusqueda(e.target.value)} />
      </label>
      <label>
        Estado
        <select value={estado} onChange={(e) => cambiarEstado(e.target.value)}>
          <option value="">Todos</option>
          {['Borrador', 'Confirmado', 'Anulado'].map((s) => (
            <option key={s}>{s}</option>
          ))}
        </select>
      </label>
      <label>
        Combustible
        <select value={tipoCombustible} onChange={(e) => cambiarTipo(e.target.value)}>
          <option value="">Todos</option>
          <option>Diesel</option>
          <option>Gasolina</option>
        </select>
      </label>
    </section>
  );
}
