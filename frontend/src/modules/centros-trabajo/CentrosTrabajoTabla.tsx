import { TablaPaginada } from '../../components/TablaPaginada';
import { formatoHoras, nombreTipo } from './presentacion';
import type { CentroTrabajoListado } from './tipos';

interface Props {
  elementos: CentroTrabajoListado[];
  pagina: number;
  totalPaginas: number;
  seleccionado: number | null;
  cambiarPagina: (pagina: number) => void;
  seleccionar: (id: number) => void;
}

const columnas = [
  { id: 'codigo', etiqueta: 'Código' }, { id: 'nombre', etiqueta: 'Nombre' },
  { id: 'tipo', etiqueta: 'Tipo' }, { id: 'planta', etiqueta: 'Planta' },
  { id: 'capacidad', etiqueta: 'h/día' }, { id: 'estado', etiqueta: 'Estado' },
];

export function CentrosTrabajoTabla(props: Props) {
  return (
    <TablaPaginada caption="Centros de trabajo registrados" etiquetaRegion="Tabla de centros de trabajo"
      columnas={columnas} elementos={props.elementos} obtenerClave={(item) => item.id}
      pagina={props.pagina} totalPaginas={props.totalPaginas} cambiarPagina={props.cambiarPagina}
      className="centros-trabajo-table" classNameContenedor="centros-trabajo-table-scroll"
      renderizarFila={(item) => (
        <tr className={props.seleccionado === item.id ? 'is-selected' : ''}>
          <td><button className="centros-trabajo-code-button" type="button" aria-pressed={props.seleccionado === item.id}
            onClick={() => props.seleccionar(item.id)}>{item.codigo}</button></td>
          <td>{item.nombre}</td><td>{nombreTipo(item.tipo)}</td><td>{item.planta}</td>
          <td className="centros-trabajo-number">{formatoHoras.format(item.capacidadHorasDia)}</td>
          <td><span className={`proveedor-status ${item.activo ? 'is-active' : 'is-inactive'}`}>{item.activo ? 'Activo' : 'Inactivo'}</span></td>
        </tr>
      )} />
  );
}
