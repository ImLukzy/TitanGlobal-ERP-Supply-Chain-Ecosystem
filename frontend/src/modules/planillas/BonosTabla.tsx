import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda } from '../../lib/formato';
import { textoPeriodo } from './presentacion';
import type { BonoPlanilla } from './tipos';

interface Props {
  items: BonoPlanilla[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  puedeEscribir: boolean;
  eliminar: (bono: BonoPlanilla) => void;
  cambiarPagina: (pagina: number) => void;
}

export function BonosTabla({ items, error, cargando, pagina, totalPaginas, puedeEscribir, eliminar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando bonos…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay bonos con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de bonos"
      etiquetaRegion="Tabla de bonos"
      columnas={[
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'periodo', etiqueta: 'Periodo' },
        { id: 'tipo', etiqueta: 'Tipo' },
        { id: 'concepto', etiqueta: 'Concepto' },
        { id: 'monto', etiqueta: 'Monto' },
        { id: 'planilla', etiqueta: 'Planilla' },
        { id: 'acciones', etiqueta: 'Acciones' },
      ]}
      elementos={items}
      obtenerClave={(bono) => bono.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="planillas-table planillas-table-bonos"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(bono) => (
        <tr>
          <td>{`${bono.empleadoLegajo} · ${bono.empleadoNombre}`}</td>
          <td className="planillas-numero">{textoPeriodo(bono.mes, bono.anio)}</td>
          <td>{bono.tipoPlanilla}</td>
          <td>{bono.concepto}</td>
          <td className="planillas-numero">{formateadorMoneda.format(bono.monto)}</td>
          <td className="planillas-numero">{bono.planillaNumero ?? 'Sin planilla'}</td>
          <td>
            <button
              aria-label={`Eliminar bono ${bono.concepto} de ${bono.empleadoNombre}`}
              className="danger-button planillas-fila-boton"
              disabled={!puedeEscribir}
              onClick={() => eliminar(bono)}
              type="button"
            >
              Eliminar
            </button>
          </td>
        </tr>
      )}
    />
  );
}
