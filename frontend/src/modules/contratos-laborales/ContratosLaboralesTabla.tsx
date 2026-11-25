import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearFecha, formateadorMoneda } from '../../lib/formato';
import { ContratoLaboralEstadoPastilla } from './ContratoLaboralEstadoPastilla';
import { TEXTO_SIN_FECHA_FIN, textoModalidad } from './presentacion';
import type { ContratoLaboralListado } from './tipos';

interface Props {
  items: ContratoLaboralListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

export function ContratosLaboralesTabla({
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
    return <p aria-live="polite" className="clientes-status" role="status">Cargando contratos…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay contratos con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de contratos laborales"
      etiquetaRegion="Tabla de contratos laborales"
      columnas={[
        { id: 'numero', etiqueta: 'Número' },
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'modalidad', etiqueta: 'Modalidad' },
        { id: 'inicio', etiqueta: 'Inicio' },
        { id: 'fin', etiqueta: 'Fin' },
        { id: 'sueldo', etiqueta: 'Sueldo básico' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(contrato) => contrato.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="contratos-laborales-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(contrato) => (
        <tr className={contrato.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={contrato.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(contrato.id)}
              type="button"
            >
              {contrato.numero}
            </button>
          </td>
          <td>{`${contrato.empleadoLegajo} · ${contrato.empleadoNombre}`}</td>
          <td>{textoModalidad(contrato.modalidad)}</td>
          <td>{formatearFecha(contrato.fechaInicio)}</td>
          <td>{contrato.fechaFin === null ? TEXTO_SIN_FECHA_FIN : formatearFecha(contrato.fechaFin)}</td>
          <td className="contratos-laborales-numero">{formateadorMoneda.format(contrato.sueldoBasico)}</td>
          <td><ContratoLaboralEstadoPastilla estado={contrato.estado} /></td>
        </tr>
      )}
    />
  );
}
