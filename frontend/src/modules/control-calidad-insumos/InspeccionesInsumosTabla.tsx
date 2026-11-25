import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearCantidad, formatearDia, formatearPorcentaje } from './presentacion';
import type { PaginaInspeccionesInsumos } from './tipos';

const columnas = [
  { id: 'numero', etiqueta: 'Número' },
  { id: 'insumo', etiqueta: 'Insumo' },
  { id: 'proveedor', etiqueta: 'Proveedor' },
  { id: 'lote', etiqueta: 'Lote' },
  { id: 'fecha', etiqueta: 'Fecha' },
  { id: 'recibida', etiqueta: 'Recibida' },
  { id: 'estado', etiqueta: 'Estado' },
  { id: 'defectos', etiqueta: '% defectos' },
];

interface TablaProps {
  datos: PaginaInspeccionesInsumos;
  seleccion: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

export function InspeccionesInsumosTabla({ datos, seleccion, seleccionar, cambiarPagina }: TablaProps) {
  return (
    <TablaPaginada
      cambiarPagina={cambiarPagina}
      caption="Inspecciones de lotes de insumos"
      className="control-calidad-insumos-tabla"
      classNameContenedor="control-calidad-insumos-tabla-scroll"
      columnas={columnas}
      elementos={datos.items}
      etiquetaRegion="Inspecciones de insumos"
      obtenerClave={(fila) => fila.id}
      pagina={datos.pagina}
      renderizarFila={(fila) => (
        <tr className={fila.id === seleccion ? 'is-selected' : undefined}>
          <th scope="row">
            <button
              aria-label={`Ver inspección ${fila.numero}`}
              aria-pressed={fila.id === seleccion}
              className="control-calidad-insumos-enlace"
              onClick={() => seleccionar(fila.id)}
              type="button"
            >
              {fila.numero}
            </button>
          </th>
          <td>{fila.productoNombre}</td>
          <td>{fila.proveedorRazonSocial}</td>
          <td className="control-calidad-insumos-cifra">{fila.lote}</td>
          <td className="control-calidad-insumos-cifra">{formatearDia(fila.fechaInspeccion)}</td>
          <td className="control-calidad-insumos-cifra">{formatearCantidad(fila.cantidadRecibida)} {fila.unidadMedida}</td>
          <td><span className={`control-calidad-insumos-estado is-${fila.estado.toLowerCase()}`}>{fila.estado}</span></td>
          <td className="control-calidad-insumos-cifra">{formatearPorcentaje(fila.porcentajeDefectos)}</td>
        </tr>
      )}
      totalPaginas={Math.max(1, Math.ceil(datos.total / datos.tamanoPagina))}
    />
  );
}
