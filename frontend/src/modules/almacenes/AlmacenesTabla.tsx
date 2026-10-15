import { TablaPaginada } from '../../components/TablaPaginada';
import type { AlmacenListado } from './tipos';

const columnas = [
  { id: 'codigo', etiqueta: 'Código' }, { id: 'nombre', etiqueta: 'Nombre' },
  { id: 'tipo', etiqueta: 'Tipo' }, { id: 'ciudad', etiqueta: 'Ciudad' },
  { id: 'capacidad', etiqueta: 'Capacidad' }, { id: 'estado', etiqueta: 'Estado' },
];
const formatoCapacidad = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

interface Props {
  elementos: AlmacenListado[];
  pagina: number;
  totalPaginas: number;
  seleccionado: number | null;
  cambiarPagina: (pagina: number) => void;
  seleccionar: (almacen: AlmacenListado) => void;
}

export function AlmacenesTabla({ elementos, pagina, totalPaginas, seleccionado, cambiarPagina, seleccionar }: Props) {
  return (
    <TablaPaginada
      caption="Almacenes registrados"
      className="almacenes-table"
      classNameContenedor="almacenes-table-scroll"
      columnas={columnas}
      cambiarPagina={cambiarPagina}
      elementos={elementos}
      etiquetaRegion="Tabla de almacenes"
      obtenerClave={(item) => item.id}
      pagina={pagina}
      renderizarFila={(item) => (
        <tr className={seleccionado === item.id ? 'is-selected' : ''} key={item.id}>
          <td><button className="almacen-code-button" onClick={() => seleccionar(item)} type="button">{item.codigo}</button></td>
          <td>{item.nombre}</td><td>{item.tipo === 'Transito' ? 'Tránsito' : item.tipo}</td><td>{item.ciudad}</td>
          <td>{formatoCapacidad.format(item.capacidadM3)} m³</td>
          <td>
            <span className={`proveedor-status${item.activo ? ' is-active' : ' is-inactive'}`}>
              {item.activo ? 'Activo' : 'Inactivo'}
            </span>
          </td>
        </tr>
      )}
      totalPaginas={totalPaginas}
    />
  );
}
