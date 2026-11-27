export function RutasFiltros({
  busqueda,
  zona,
  activo,
  zonas,
  cambiar,
}: {
  busqueda: string;
  zona: string;
  activo: string;
  zonas: string[];
  cambiar: (campo: 'busqueda' | 'zona' | 'activo', valor: string) => void;
}) {
  return (
    <section aria-label="Filtros de rutas" className="rutas-despacho-filters">
      <label>
        Buscar por código, nombre, origen o destino
        <input
          type="search"
          value={busqueda}
          onChange={(e) => cambiar('busqueda', e.target.value)}
        />
      </label>
      <label>
        Zona
        <select value={zona} onChange={(e) => cambiar('zona', e.target.value)}>
          <option value="">Todas</option>
          {zona && !zonas.includes(zona) && <option>{zona}</option>}
          {zonas.map((z) => (
            <option key={z}>{z}</option>
          ))}
        </select>
      </label>
      <label>
        Estado
        <select value={activo} onChange={(e) => cambiar('activo', e.target.value)}>
          <option value="">Todos</option>
          <option value="true">Activas</option>
          <option value="false">Inactivas</option>
        </select>
      </label>
    </section>
  );
}
