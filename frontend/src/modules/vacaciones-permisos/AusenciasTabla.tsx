import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearFecha } from '../../lib/formato';
import { textoEstado, textoTipo } from './presentacion';
import type { AusenciaListado, EstadoAusencia } from './tipos';

interface Props {
  items: AusenciaListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

// El estado se muestra con texto, nunca solo con color.
export function EstadoPastilla({ estado }: { estado: EstadoAusencia }) {
  return <span className={`vacaciones-permisos-estado is-${estado.toLowerCase()}`}>{textoEstado(estado)}</span>;
}

export function AusenciasTabla({
  items,
  error,
  cargando,
  pagina,
  totalPaginas,
  seleccionadoId,
  seleccionar,
  cambiarPagina,
}: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando ausencias…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay ausencias con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de ausencias"
      etiquetaRegion="Tabla de ausencias"
      columnas={[
        { id: 'numero', etiqueta: 'Número' },
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'tipo', etiqueta: 'Tipo' },
        { id: 'desde', etiqueta: 'Desde' },
        { id: 'hasta', etiqueta: 'Hasta' },
        { id: 'dias', etiqueta: 'Días' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(ausencia) => ausencia.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="vacaciones-permisos-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(ausencia) => (
        <tr className={ausencia.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={ausencia.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(ausencia.id)}
              type="button"
            >
              {ausencia.numero}
            </button>
          </td>
          <td>{`${ausencia.empleadoLegajo} · ${ausencia.empleadoNombre}`}</td>
          <td>{textoTipo(ausencia.tipo)}</td>
          <td className="vacaciones-permisos-numero">{formatearFecha(ausencia.fechaInicio)}</td>
          <td className="vacaciones-permisos-numero">{formatearFecha(ausencia.fechaFin)}</td>
          <td className="vacaciones-permisos-numero">{ausencia.dias}</td>
          <td><EstadoPastilla estado={ausencia.estado} /></td>
        </tr>
      )}
    />
  );
}
