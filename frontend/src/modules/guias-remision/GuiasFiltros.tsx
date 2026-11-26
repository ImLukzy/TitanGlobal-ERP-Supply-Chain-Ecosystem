export function GuiasFiltros({
  busqueda,
  estado,
  cambiarBusqueda,
  cambiarEstado,
}: {
  busqueda: string;
  estado: string;
  cambiarBusqueda: (s: string) => void;
  cambiarEstado: (s: string) => void;
}) {
  return (
    <section aria-label="Filtros de guías" className="guias-remision-filters">
      <label>
        Buscar serie o destinatario
        <input type="search" value={busqueda} onChange={(e) => cambiarBusqueda(e.target.value)} />
      </label>
      <label>
        Estado
        <select value={estado} onChange={(e) => cambiarEstado(e.target.value)}>
          <option value="">Todos</option>
          {['Borrador', 'Emitida', 'Anulada'].map((e) => (
            <option key={e}>{e}</option>
          ))}
        </select>
      </label>
    </section>
  );
}
