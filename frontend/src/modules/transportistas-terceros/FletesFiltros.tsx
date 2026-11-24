export function FletesFiltros({
  busqueda,
  estado,
  cambiarBusqueda,
  cambiarEstado,
}: {
  busqueda: string;
  estado: string;
  cambiarBusqueda: (valor: string) => void;
  cambiarEstado: (valor: string) => void;
}) {
  return (
    <section className="transportistas-terceros-filters" aria-label="Filtros de fletes">
      <label>
        Buscar referencia, origen o destino
        <input value={busqueda} onChange={(e) => cambiarBusqueda(e.target.value)} />
      </label>
      <label>
        Estado del flete
        <select value={estado} onChange={(e) => cambiarEstado(e.target.value)}>
          <option value="">Todos</option>
          <option>Registrado</option>
          <option>Cancelado</option>
        </select>
      </label>
    </section>
  );
}
