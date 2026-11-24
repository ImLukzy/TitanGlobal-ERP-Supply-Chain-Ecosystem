import { TablaPaginada } from '../../components/TablaPaginada';
import { formatoCapacidad, nombreTipo } from './presentacion';
import type { VehiculoFlotaListado } from './tipos';

interface Props {
  elementos: VehiculoFlotaListado[];
  pagina: number;
  totalPaginas: number;
  seleccionado: number | null;
  cambiarPagina: (pagina: number) => void;
  seleccionar: (id: number) => void;
}

const columnas = [
  { id: 'placa', etiqueta: 'Placa' }, { id: 'vehiculo', etiqueta: 'Marca / modelo' },
  { id: 'anio', etiqueta: 'Año' }, { id: 'tipo', etiqueta: 'Tipo' },
  { id: 'capacidad', etiqueta: 'kg' }, { id: 'estado', etiqueta: 'Estado' },
];

export function VehiculosFlotaTabla(props: Props) {
  return (
    <TablaPaginada caption="Vehículos de flota registrados" etiquetaRegion="Tabla de vehículos de flota"
      columnas={columnas} elementos={props.elementos} obtenerClave={(item) => item.id}
      pagina={props.pagina} totalPaginas={props.totalPaginas} cambiarPagina={props.cambiarPagina}
      className="vehiculos-flota-table" classNameContenedor="vehiculos-flota-table-scroll"
      renderizarFila={(item) => (
        <tr className={props.seleccionado === item.id ? 'is-selected' : ''}>
          <td><button className="vehiculos-flota-code-button" type="button" aria-pressed={props.seleccionado === item.id}
            onClick={() => props.seleccionar(item.id)}>{item.placa}</button></td>
          <td>{item.marca} {item.modelo}</td><td>{item.anio}</td><td>{nombreTipo(item.tipo)}</td>
          <td className="vehiculos-flota-number">{formatoCapacidad.format(item.capacidadKg)}</td>
          <td><span className={`proveedor-status ${item.activo ? 'is-active' : 'is-inactive'}`}>{item.activo ? 'Activo' : 'Inactivo'}</span></td>
        </tr>
      )} />
  );
}
