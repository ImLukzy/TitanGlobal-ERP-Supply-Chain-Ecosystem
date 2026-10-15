import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import type { PromocionListado } from './tipos';

interface Props { items: PromocionListado[]; total: number; pagina: number; error: string; cargando: boolean; seleccionadoId: number | null; seleccionar: (id: number) => void; cambiarPagina: (pagina: number) => void; }

export function PromocionesTabla({ items, total, pagina, error, cargando, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  const paginas = Math.max(1, Math.ceil(total / 10));
  return <>
    {error && <p className="clientes-error" role="alert">{error}</p>}
    {!error && cargando && <p role="status">Cargando promociones…</p>}
    {!error && !cargando && items.length === 0 && <p role="status">No hay promociones que coincidan con los filtros.</p>}
    {items.length > 0 && <TablaPaginada caption="Promociones" etiquetaRegion="Tabla de promociones" columnas={[
      { id: 'codigo', etiqueta: 'Código' }, { id: 'nombre', etiqueta: 'Nombre' }, { id: 'tipo', etiqueta: 'Tipo' },
      { id: 'descuento', etiqueta: 'Descuento' }, { id: 'segmento', etiqueta: 'Segmento' }, { id: 'vigencia', etiqueta: 'Vigencia' }, { id: 'estado', etiqueta: 'Estado' },
    ]} elementos={items} obtenerClave={(item) => item.id} pagina={pagina} totalPaginas={paginas} cambiarPagina={cambiarPagina} className="solicitudes-table promociones-table" classNameContenedor="solicitudes-table-scroll promociones-table-scroll" classNamePaginacion="solicitudes-pagination" renderizarFila={(item) => <tr className={item.id === seleccionadoId ? 'is-selected' : ''}>
      <th scope="row"><button className="solicitud-row-select" onClick={() => seleccionar(item.id)} type="button">{item.codigo}</button></th>
      <td>{item.nombre}</td>
      <td>{item.tipo === 'Producto' ? `Producto: ${item.productoNombre ?? '—'}` : `Compra desde ${formateadorMoneda.format(item.montoMinimo ?? 0)}`}</td>
      <td className="promociones-no-partir">{item.porcentajeDescuento.toFixed(2)} %</td>
      <td>{item.segmentoCodigo ?? 'Todos los clientes'}</td>
      <td><span aria-label={`Desde ${formatearFecha(item.fechaInicio)} hasta ${formatearFecha(item.fechaFin)}`} className="promociones-fecha"><span>{formatearFecha(item.fechaInicio)}</span><span>{formatearFecha(item.fechaFin)}</span></span></td>
      <td><span className={`promocion-estado ${item.activa && item.vigente ? 'is-vigente' : item.activa ? 'is-novigente' : 'is-inactiva'}`}>{!item.activa ? 'Inactiva' : item.vigente ? 'Vigente' : 'No vigente'}</span></td>
    </tr>} />}
  </>;
}
