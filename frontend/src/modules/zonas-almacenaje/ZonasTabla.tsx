import { TablaPaginada } from '../../components/TablaPaginada';
import type { ZonaListado } from './tipos';

const columnasConAlmacen = [
  { id: 'almacen', etiqueta: 'Almacén' },
  { id: 'codigo', etiqueta: 'Código' },
  { id: 'nombre', etiqueta: 'Nombre' },
  { id: 'tipo', etiqueta: 'Tipo' },
  { id: 'temperatura', etiqueta: 'Temperatura' },
  { id: 'capacidad', etiqueta: 'Capacidad' },
  { id: 'estado', etiqueta: 'Estado' },
];
const columnasSinAlmacen = columnasConAlmacen.slice(1);

interface Props {
  zonas: ZonaListado[];
  pagina: number;
  totalPaginas: number;
  seleccionada: number | null;
  cambiarPagina: (pagina: number) => void;
  seleccionar: (zona: ZonaListado) => void;
  mostrarAlmacen: boolean;
}

export function ZonasTabla({ zonas, pagina, totalPaginas, seleccionada, cambiarPagina, seleccionar, mostrarAlmacen }: Props) {
  return (
    <TablaPaginada
      caption="Zonas de almacenaje"
      className={`zonas-table${mostrarAlmacen ? '' : ' is-filtered'}`}
      classNameContenedor="zonas-table-scroll"
      cambiarPagina={cambiarPagina}
      columnas={mostrarAlmacen ? columnasConAlmacen : columnasSinAlmacen}
      elementos={zonas}
      etiquetaRegion="Tabla de zonas de almacenaje"
      obtenerClave={(zona) => zona.id}
      pagina={pagina}
      renderizarFila={(zona) => (
        <tr className={seleccionada === zona.id ? 'is-selected' : ''} key={zona.id}>
          {mostrarAlmacen && <td>{zona.almacenCodigo}</td>}
          <td><button className="zona-code-button" onClick={() => seleccionar(zona)} type="button">{zona.codigo}</button></td>
          <td>{zona.nombre}</td>
          <td>{zona.tipo === 'Recepcion' ? 'Recepción' : zona.tipo}</td>
          <td>{zona.temperatura}</td>
          <td>{new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(zona.capacidadM3)} m³</td>
          <td><span className={`proveedor-status${zona.activa ? ' is-active' : ' is-inactive'}`}>{zona.activa ? 'Activa' : 'Inactiva'}</span></td>
        </tr>
      )}
      totalPaginas={totalPaginas}
    />
  );
}
