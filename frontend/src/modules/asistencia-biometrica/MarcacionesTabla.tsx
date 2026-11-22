import { TablaPaginada } from '../../components/TablaPaginada';
import { aFechaHoraLocal } from './presentacion';
import type { Marcacion } from './tipos';

interface Props {
  items: Marcacion[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  puedeEscribir: boolean;
  eliminar: (marcacion: Marcacion) => void;
  cambiarPagina: (pagina: number) => void;
}

function detalleMarcacion(marcacion: Marcacion): string {
  if (marcacion.origen === 'Reloj') {
    return `${marcacion.dispositivo ?? ''} · ${marcacion.numeroRegistro ?? ''}`;
  }
  return marcacion.observacion ?? '';
}

export function MarcacionesTabla({ items, error, cargando, pagina, totalPaginas, puedeEscribir, eliminar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando marcaciones…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay marcaciones con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de marcaciones"
      etiquetaRegion="Tabla de marcaciones"
      columnas={[
        { id: 'instante', etiqueta: 'Instante' },
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'tipo', etiqueta: 'Tipo' },
        { id: 'origen', etiqueta: 'Origen' },
        { id: 'detalle', etiqueta: 'Dispositivo u observación' },
        { id: 'acciones', etiqueta: 'Acciones' },
      ]}
      elementos={items}
      obtenerClave={(marcacion) => marcacion.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="asistencia-table asistencia-table-marcaciones"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(marcacion) => (
        <tr>
          <td className="asistencia-numero">{aFechaHoraLocal(marcacion.instante)}</td>
          <td>{`${marcacion.empleadoLegajo} · ${marcacion.empleadoNombre}`}</td>
          <td>{marcacion.tipo}</td>
          <td>{marcacion.origen}</td>
          <td>{detalleMarcacion(marcacion)}</td>
          <td>
            {marcacion.origen === 'Manual' && (
              <button
                aria-label={`Eliminar marcación de ${marcacion.empleadoNombre}, ${aFechaHoraLocal(marcacion.instante)}`}
                className="danger-button asistencia-fila-boton"
                disabled={!puedeEscribir}
                onClick={() => eliminar(marcacion)}
                type="button"
              >
                Eliminar
              </button>
            )}
          </td>
        </tr>
      )}
    />
  );
}
