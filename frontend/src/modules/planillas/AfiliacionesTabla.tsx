import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearFecha } from '../../lib/formato';
import type { EmpleadoListado } from '../empleados/tipos';
import type { AfiliacionPension } from './tipos';

export interface FilaAfiliacion {
  empleado: EmpleadoListado;
  afiliacion: AfiliacionPension | null;
}

interface Props {
  items: FilaAfiliacion[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  puedeEscribir: boolean;
  editar: (fila: FilaAfiliacion) => void;
  cambiarPagina: (pagina: number) => void;
}

// «Sin afiliación» se muestra con texto destacado: esos empleados quedan excluidos del cálculo de la planilla.
function Sistema({ afiliacion }: { afiliacion: AfiliacionPension | null }) {
  if (afiliacion === null) {
    return <span className="planillas-incidencia">Sin afiliación</span>;
  }
  return <span>{`${afiliacion.codigoPension} (${afiliacion.sistema})`}</span>;
}

export function AfiliacionesTabla({ items, error, cargando, pagina, totalPaginas, puedeEscribir, editar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando afiliaciones…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay empleados con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Afiliaciones a sistemas de pensiones"
      etiquetaRegion="Tabla de afiliaciones"
      columnas={[
        { id: 'legajo', etiqueta: 'Legajo' },
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'sistema', etiqueta: 'Sistema de pensiones' },
        { id: 'cuspp', etiqueta: 'CUSPP' },
        { id: 'fecha', etiqueta: 'Afiliación' },
        { id: 'acciones', etiqueta: 'Acciones' },
      ]}
      elementos={items}
      obtenerClave={(fila) => fila.empleado.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="planillas-table planillas-table-afiliaciones"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(fila) => {
        const nombre = `${fila.empleado.apellidos}, ${fila.empleado.nombres}`;
        return (
          <tr>
            <td className="planillas-numero">{fila.empleado.legajo}</td>
            <td>{nombre}</td>
            <td><Sistema afiliacion={fila.afiliacion} /></td>
            <td className="planillas-numero">{fila.afiliacion?.cuspp ?? ''}</td>
            <td className="planillas-numero">{fila.afiliacion === null ? '' : formatearFecha(fila.afiliacion.fechaAfiliacion)}</td>
            <td>
              <button
                aria-label={`${fila.afiliacion === null ? 'Afiliar a' : 'Cambiar afiliación de'} ${nombre}`}
                className="secondary-button planillas-fila-boton"
                disabled={!puedeEscribir}
                onClick={() => editar(fila)}
                type="button"
              >
                {fila.afiliacion === null ? 'Afiliar' : 'Cambiar'}
              </button>
            </td>
          </tr>
        );
      }}
    />
  );
}
