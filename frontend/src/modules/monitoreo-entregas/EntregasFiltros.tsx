import { etiquetaEstado } from './presentacion';
export function EntregasFiltros({
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
    <section aria-label="Filtros de entregas" className="monitoreo-entregas-filters">
      <label>
        Buscar código, guía o destinatario
        <input type="search" value={busqueda} onChange={(e) => cambiarBusqueda(e.target.value)} />
      </label>
      <label>
        Estado
        <select value={estado} onChange={(e) => cambiarEstado(e.target.value)}>
          <option value="">Todos</option>
          {['Programada', 'EnRuta', 'Entregada', 'Cancelada'].map((e) => (
            <option key={e} value={e}>
              {etiquetaEstado(e)}
            </option>
          ))}
        </select>
      </label>
    </section>
  );
}
