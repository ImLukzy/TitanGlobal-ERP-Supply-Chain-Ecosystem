export function TransportistasFiltros({
  busqueda,
  pais,
  servicio,
  activo,
  cambiar,
}: {
  busqueda: string;
  pais: string;
  servicio: string;
  activo: string;
  cambiar: (campo: string, valor: string) => void;
}) {
  return (
    <section aria-label="Filtros de transportistas" className="transportistas-terceros-filters">
      <label>
        Buscar razón social o documento
        <input value={busqueda} onChange={(e) => cambiar('busqueda', e.target.value)} />
      </label>
      <label>
        País del transportista
        <input value={pais} onChange={(e) => cambiar('pais', e.target.value)} />
      </label>
      <label>
        Servicio
        <select value={servicio} onChange={(e) => cambiar('servicio', e.target.value)}>
          <option value="">Todos</option>
          {['3PL', 'Flete', 'Ambos'].map((s) => (
            <option key={s}>{s}</option>
          ))}
        </select>
      </label>
      <label>
        Estado del transportista
        <select value={activo} onChange={(e) => cambiar('activo', e.target.value)}>
          <option value="">Todos</option>
          <option value="true">Activos</option>
          <option value="false">Inactivos</option>
        </select>
      </label>
    </section>
  );
}
