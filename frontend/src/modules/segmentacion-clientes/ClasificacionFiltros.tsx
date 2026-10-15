import type { SegmentoListado } from './tipos';

interface Props {
  busqueda: string;
  segmentoId: number | '' | 'sin-segmento';
  cambiarBusqueda: (valor: string) => void;
  cambiarSegmento: (id: number | '' | 'sin-segmento') => void;
  segmentos: SegmentoListado[];
}

export function ClasificacionFiltros({ busqueda, segmentoId, cambiarBusqueda, cambiarSegmento, segmentos }: Props) {
  return (
    <div className="segmentos-filters clasificacion-filters">
      <label htmlFor="clasificacion-busqueda">Buscar cliente
        <input autoComplete="off" id="clasificacion-busqueda" onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Nombre o RUC" type="search" value={busqueda} />
      </label>
      <label htmlFor="clasificacion-segmento">Segmento
        <select id="clasificacion-segmento" onChange={(event) => {
          const valor = event.currentTarget.value;
          cambiarSegmento(valor === '' || valor === 'sin-segmento' ? valor : Number(valor));
        }} value={segmentoId === '' ? '' : String(segmentoId)}>
          <option value="">Todos</option>
          {segmentos.filter((segmento) => segmento.activo).map((segmento) => (
            <option key={segmento.id} value={segmento.id}>{segmento.codigo} · {segmento.nombre}</option>
          ))}
          <option value="sin-segmento">Sin segmento</option>
        </select>
      </label>
    </div>
  );
}
