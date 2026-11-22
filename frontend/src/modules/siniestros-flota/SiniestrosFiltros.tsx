export function SiniestrosFiltros({
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
    <div className="siniestros-flota-filters">
      <label>
        Buscar código, placa o lugar
        <input type="search" value={busqueda} onChange={(e) => cambiarBusqueda(e.target.value)} />
      </label>
      <label>
        Estado
        <select value={estado} onChange={(e) => cambiarEstado(e.target.value)}>
          <option value="">Todos</option>
          {['Registrado', 'EnReclamo', 'Cerrado', 'Anulado'].map((x) => (
            <option key={x}>{x}</option>
          ))}
        </select>
      </label>
      <label>
        Tipo
        <select value={tipo} onChange={(e) => cambiarTipo(e.target.value)}>
          <option value="">Todos</option>
          {['Accidente', 'Robo', 'DanoMaterial'].map((x) => (
            <option key={x}>{x}</option>
          ))}
        </select>
      </label>
    </div>
  );
}
