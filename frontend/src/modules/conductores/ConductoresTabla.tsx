import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearFecha } from '../../lib/formato';
import { licenciaVencida } from './presentacion';
import type { ConductorListado } from './tipos';

interface Props {
  elementos: ConductorListado[];
  pagina: number;
  totalPaginas: number;
  seleccionado: number | null;
  cambiarPagina: (pagina: number) => void;
  seleccionar: (id: number) => void;
}

const columnas = [
  { id: 'licencia', etiqueta: 'Licencia' }, { id: 'nombre', etiqueta: 'Nombre' },
  { id: 'vencimiento', etiqueta: 'Vencimiento' }, { id: 'vigencia', etiqueta: 'Vigencia' },
  { id: 'habilitado', etiqueta: 'Habilitado' }, { id: 'activo', etiqueta: 'Activo' },
];

export function ConductoresTabla(props: Props) {
  return (
    <TablaPaginada caption="Conductores registrados" etiquetaRegion="Tabla de conductores"
      columnas={columnas} elementos={props.elementos} obtenerClave={(item) => item.id}
      pagina={props.pagina} totalPaginas={props.totalPaginas} cambiarPagina={props.cambiarPagina}
      className="conductores-table" classNameContenedor="conductores-table-scroll"
      renderizarFila={(item) => (
        <tr className={props.seleccionado === item.id ? 'is-selected' : ''}>
          <td><button className="conductores-code-button" type="button" aria-pressed={props.seleccionado === item.id}
            onClick={() => props.seleccionar(item.id)}>{item.documentoLicencia}</button></td>
          <td>{item.nombre}</td><td className="conductores-date">{formatearFecha(item.vencimientoLicencia)}</td>
          <td><span className={`proveedor-status ${licenciaVencida(item.vencimientoLicencia) ? 'is-inactive' : 'is-active'}`}>
            {licenciaVencida(item.vencimientoLicencia) ? 'Vencida' : 'Vigente'}</span></td>
          <td>{item.habilitado ? 'Sí' : 'No'}</td><td>{item.activo ? 'Sí' : 'No'}</td>
        </tr>
      )} />
  );
}
