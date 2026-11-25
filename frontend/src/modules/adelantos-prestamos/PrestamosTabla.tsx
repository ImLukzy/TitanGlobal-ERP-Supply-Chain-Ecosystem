import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearFecha, formateadorMoneda } from '../../lib/formato';
import { textoEstado, textoTipo } from './presentacion';
import type { EstadoPrestamo, PrestamoListado } from './tipos';

interface Props {
  items: PrestamoListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

// El estado se muestra con texto, nunca solo con color.
export function EstadoPastilla({ estado }: { estado: EstadoPrestamo }) {
  return <span className={`adelantos-prestamos-estado is-${estado.toLowerCase()}`}>{textoEstado(estado)}</span>;
}

export function PrestamosTabla({
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
    return <p aria-live="polite" className="clientes-status" role="status">Cargando préstamos…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay préstamos con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de préstamos"
      etiquetaRegion="Tabla de préstamos"
      columnas={[
        { id: 'numero', etiqueta: 'Número' },
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'tipo', etiqueta: 'Tipo' },
        { id: 'monto', etiqueta: 'Monto' },
        { id: 'cuotas', etiqueta: 'Cuotas' },
        { id: 'otorgado', etiqueta: 'Otorgado' },
        { id: 'saldo', etiqueta: 'Saldo' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(prestamo) => prestamo.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="adelantos-prestamos-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(prestamo) => (
        <tr className={prestamo.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={prestamo.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(prestamo.id)}
              type="button"
            >
              {prestamo.numero}
            </button>
          </td>
          <td>{`${prestamo.empleadoLegajo} · ${prestamo.empleadoNombre}`}</td>
          <td>{textoTipo(prestamo.tipo)}</td>
          <td className="adelantos-prestamos-numero">{formateadorMoneda.format(prestamo.monto)}</td>
          <td className="adelantos-prestamos-numero">{prestamo.numeroCuotas}</td>
          <td className="adelantos-prestamos-numero">{formatearFecha(prestamo.fechaOtorgamiento)}</td>
          <td className="adelantos-prestamos-numero">{formateadorMoneda.format(prestamo.saldo)}</td>
          <td><EstadoPastilla estado={prestamo.estado} /></td>
        </tr>
      )}
    />
  );
}
