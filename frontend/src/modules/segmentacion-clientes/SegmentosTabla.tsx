import { formateadorMoneda, formatearCantidad } from '../../lib/formato';
import { TablaPaginada } from '../../components/TablaPaginada';
import type { SegmentoListado } from './tipos';

const columnas = [
  { id: 'prioridad', etiqueta: 'Prioridad' },
  { id: 'codigo', etiqueta: 'Código' },
  { id: 'nombre', etiqueta: 'Nombre' },
  { id: 'criterios', etiqueta: 'Criterios' },
  { id: 'clientes', etiqueta: 'Clientes' },
  { id: 'estado', etiqueta: 'Estado' },
];

function criterios(segmento: SegmentoListado): string {
  const valores: string[] = [];
  if (segmento.montoMinimo > 0) {
    valores.push(`≥ ${formateadorMoneda.format(segmento.montoMinimo)}`);
  }
  if (segmento.ordenesMinimas > 0) {
    valores.push(`≥ ${formatearCantidad(segmento.ordenesMinimas, 'orden', 'órdenes')}`);
  }
  return valores.length === 0 ? 'Sin criterios' : valores.join(' y ');
}

interface Props {
  segmentos: SegmentoListado[];
  pagina: number;
  totalPaginas: number;
  seleccionado: number | null;
  cambiarPagina: (pagina: number) => void;
  seleccionar: (segmento: SegmentoListado) => void;
}

export function SegmentosTabla({ segmentos, pagina, totalPaginas, seleccionado, cambiarPagina, seleccionar }: Props) {
  return (
    <TablaPaginada
      caption="Segmentos de clientes"
      className="segmentos-table"
      classNameContenedor="segmentos-table-scroll"
      cambiarPagina={cambiarPagina}
      columnas={columnas}
      elementos={segmentos}
      etiquetaRegion="Tabla de segmentos"
      obtenerClave={(segmento) => segmento.id}
      pagina={pagina}
      renderizarFila={(segmento) => (
        <tr className={seleccionado === segmento.id ? 'is-selected' : ''} key={segmento.id}>
          <td>{segmento.prioridad}</td>
          <td><button className="segmento-code-button" onClick={() => seleccionar(segmento)} type="button">{segmento.codigo}</button></td>
          <td>{segmento.nombre}</td>
          <td>{criterios(segmento)}</td>
          <td>{formatearCantidad(segmento.clientes, 'cliente', 'clientes')}</td>
          <td><span className={`proveedor-status${segmento.activo ? ' is-active' : ' is-inactive'}`}>{segmento.activo ? 'Activo' : 'Inactivo'}</span></td>
        </tr>
      )}
      totalPaginas={totalPaginas}
    />
  );
}
