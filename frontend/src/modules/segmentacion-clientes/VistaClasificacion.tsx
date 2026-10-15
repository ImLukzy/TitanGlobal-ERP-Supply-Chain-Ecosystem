import { ClasificacionFiltros } from './ClasificacionFiltros';
import { ClasificacionTabla } from './ClasificacionTabla';
import type { ClienteClasificado, Pagina, SegmentoListado } from './tipos';

interface Props {
  error: string;
  clasificacion: Pagina<ClienteClasificado> | null;
  busqueda: string;
  segmentoId: number | '' | 'sin-segmento';
  segmentos: SegmentoListado[];
  pagina: number;
  totalPaginas: number;
  cambiarBusqueda: (valor: string) => void;
  cambiarSegmento: (id: number | '' | 'sin-segmento') => void;
  cambiarPagina: (pagina: number) => void;
}

export function VistaClasificacion({
  error,
  clasificacion,
  busqueda,
  segmentoId,
  segmentos,
  pagina,
  totalPaginas,
  cambiarBusqueda,
  cambiarSegmento,
  cambiarPagina,
}: Props) {
  return (
    <div aria-labelledby="tab-clasificacion" className="segment-tab-panel" id="panel-clasificacion" role="tabpanel" tabIndex={0}>
      <section aria-labelledby="classification-title" className="clientes-list-panel classification-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">CLIENTES</div>
            <h2 id="classification-title">Clasificación calculada</h2>
          </div>
        </div>
        <ClasificacionFiltros
          busqueda={busqueda}
          cambiarBusqueda={cambiarBusqueda}
          cambiarSegmento={cambiarSegmento}
          segmentoId={segmentoId}
          segmentos={segmentos}
        />
        {error !== '' ? (
          <p className="clientes-error" role="alert">{error}</p>
        ) : clasificacion === null ? (
          <p className="clientes-status" role="status">Cargando clasificación…</p>
        ) : clasificacion.items.length === 0 ? (
          <p className="clientes-status" role="status">No hay clientes que coincidan con los filtros.</p>
        ) : (
          <ClasificacionTabla
            cambiarPagina={cambiarPagina}
            clientes={clasificacion.items}
            pagina={pagina}
            totalPaginas={totalPaginas}
          />
        )}
      </section>
    </div>
  );
}
