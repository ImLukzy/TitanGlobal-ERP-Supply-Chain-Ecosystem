import type { RefObject } from 'react';
import { SegmentoDetallePanel } from './SegmentoDetallePanel';
import { SegmentosFiltros } from './SegmentosFiltros';
import { SegmentosTabla } from './SegmentosTabla';
import type { ClienteClasificado, Pagina, SegmentoDetalle, SegmentoListado } from './tipos';

interface Props {
  error: string;
  segmentos: Pagina<SegmentoListado> | null;
  busqueda: string;
  activo: boolean | '';
  pagina: number;
  totalPaginas: number;
  seleccionado: number | null;
  detalle: SegmentoDetalle | null;
  clientes: Pagina<ClienteClasificado> | null;
  cargandoClientes: boolean;
  errorDetalle: string;
  botonEliminarRef: RefObject<HTMLButtonElement | null>;
  nuevo: () => void;
  cambiarBusqueda: (valor: string) => void;
  cambiarActivo: (activo: boolean | '') => void;
  cambiarPagina: (pagina: number) => void;
  seleccionar: (segmento: SegmentoListado) => void;
  cambiarPaginaClientes: (pagina: number) => void;
  editar: () => void;
  eliminar: () => void;
}

export function VistaSegmentos({
  error,
  segmentos,
  busqueda,
  activo,
  pagina,
  totalPaginas,
  seleccionado,
  detalle,
  clientes,
  cargandoClientes,
  errorDetalle,
  botonEliminarRef,
  nuevo,
  cambiarBusqueda,
  cambiarActivo,
  cambiarPagina,
  seleccionar,
  cambiarPaginaClientes,
  editar,
  eliminar,
}: Props) {
  return (
    <div aria-labelledby="tab-segmentos" className="segment-tab-panel" id="panel-segmentos" role="tabpanel" tabIndex={0}>
      <div className="segment-work-area">
        <section aria-labelledby="segment-list-title" className="clientes-list-panel segment-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">DIRECTORIO</div>
              <h2 id="segment-list-title">Segmentos</h2>
            </div>
            <button className="primary-button" onClick={nuevo} type="button">Nuevo segmento</button>
          </div>
          <SegmentosFiltros
            activo={activo}
            busqueda={busqueda}
            cambiarActivo={cambiarActivo}
            cambiarBusqueda={cambiarBusqueda}
          />
          {error !== '' ? (
            <p className="clientes-error" role="alert">{error}</p>
          ) : segmentos === null ? (
            <p className="clientes-status" role="status">Cargando segmentos…</p>
          ) : segmentos.items.length === 0 ? (
            <p className="clientes-status" role="status">No hay segmentos que coincidan con los filtros.</p>
          ) : (
            <SegmentosTabla
              cambiarPagina={cambiarPagina}
              pagina={pagina}
              seleccionar={seleccionar}
              seleccionado={seleccionado}
              segmentos={segmentos.items}
              totalPaginas={totalPaginas}
            />
          )}
        </section>
        <SegmentoDetallePanel
          botonEliminarRef={botonEliminarRef}
          cambiarPagina={cambiarPaginaClientes}
          cargando={cargandoClientes}
          clientes={clientes}
          editar={editar}
          eliminar={eliminar}
          error={errorDetalle}
          segmento={detalle}
        />
      </div>
    </div>
  );
}
