import type { FiltrosListas, SegmentoOpcion } from "./tipos";

interface Props {
  busqueda: string;
  filtros: FiltrosListas;
  segmentos: SegmentoOpcion[];
  cambiarBusqueda: (value: string) => void;
  cambiarFiltro: (value: Partial<FiltrosListas>) => void;
}
export function ListasFiltros({
  busqueda,
  filtros,
  segmentos,
  cambiarBusqueda,
  cambiarFiltro,
}: Props) {
  return (
    <div className="solicitudes-filters listas-filters">
      <div>
        <label htmlFor="listas-busqueda">Buscar por código o nombre</label>
        <input
          id="listas-busqueda"
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Código o nombre"
          type="search"
          value={busqueda}
        />
      </div>
      <div>
        <label htmlFor="listas-segmento">Segmento</label>
        <select
          id="listas-segmento"
          onChange={(event) =>
            cambiarFiltro({
              segmentoId: event.currentTarget.value
                ? Number(event.currentTarget.value)
                : "",
            })
          }
          value={filtros.segmentoId ?? ""}
        >
          <option value="">Todos</option>
          <option value="0">Generales</option>
          {segmentos.map((item) => (
            <option key={item.id} value={item.id}>
              {item.nombre}
            </option>
          ))}
        </select>
      </div>
      <label className="cotizaciones-expired">
        <input
          checked={Boolean(filtros.vigentes)}
          onChange={(event) =>
            cambiarFiltro({ vigentes: event.currentTarget.checked })
          }
          type="checkbox"
        />
        Solo vigentes
      </label>
    </div>
  );
}
