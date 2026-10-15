import { formateadorMoneda, formatearCantidad } from '../../lib/formato';
import { TablaPaginada } from '../../components/TablaPaginada';
import type { ClienteClasificado } from './tipos';

const columnas = [
  { id: 'cliente', etiqueta: 'Cliente' },
  { id: 'ruc', etiqueta: 'RUC' },
  { id: 'ordenes', etiqueta: 'Órdenes' },
  { id: 'monto', etiqueta: 'Monto total' },
  { id: 'segmento', etiqueta: 'Segmento' },
];

interface Props {
  clientes: ClienteClasificado[];
  pagina: number;
  totalPaginas: number;
  cambiarPagina: (pagina: number) => void;
}

export function ClasificacionTabla({ clientes, pagina, totalPaginas, cambiarPagina }: Props) {
  return (
    <TablaPaginada
      caption="Clasificación de clientes"
      className="clasificacion-table"
      classNameContenedor="clasificacion-table-scroll"
      cambiarPagina={cambiarPagina}
      columnas={columnas}
      elementos={clientes}
      etiquetaRegion="Tabla de clasificación de clientes"
      obtenerClave={(cliente) => cliente.clienteId}
      pagina={pagina}
      renderizarFila={(cliente) => (
        <tr key={cliente.clienteId}>
          <td>{cliente.nombre}</td>
          <td>{cliente.ruc}</td>
          <td>{formatearCantidad(cliente.totalOrdenes, 'orden', 'órdenes')}</td>
          <td>{formateadorMoneda.format(cliente.montoTotal)}</td>
          <td>{cliente.segmentoCodigo === null
            ? <span className="segmento-empty-label">Sin segmento</span>
            : <span className="segmento-classification-label">{cliente.segmentoCodigo} · {cliente.segmentoNombre}</span>}</td>
        </tr>
      )}
      totalPaginas={totalPaginas}
    />
  );
}
