import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda } from '../../lib/formato';
import type { AcuerdoListado, PaginaAcuerdos } from './tipos';
import { avisoVigencia, formatearFechaCorta, porcentajeConsumo } from './vigencia';

interface AcuerdosTablaProps {
  lista: PaginaAcuerdos | null;
  error: string;
  seleccionadoId: number | null;
  seleccionar: (acuerdo: AcuerdoListado) => void;
  pagina: number;
  cambiarPagina: (pagina: number) => void;
}

const columnas = [
  { id: 'numero', etiqueta: 'Número' },
  { id: 'proveedor', etiqueta: 'Proveedor' },
  { id: 'estado', etiqueta: 'Estado' },
  { id: 'vigencia', etiqueta: 'Vigencia' },
  { id: 'maximo', etiqueta: 'Monto máximo' },
  { id: 'avance', etiqueta: 'Consumo' },
];

export function AcuerdosTabla({ lista, error, seleccionadoId, seleccionar, pagina, cambiarPagina }: AcuerdosTablaProps) {
  if (error !== '') {
    return <p className="acuerdos-error" role="alert">{error}</p>;
  }
  if (lista === null) {
    return <p className="acuerdos-loading" role="status">Cargando acuerdos…</p>;
  }
  if (lista.items.length === 0) {
    return <p className="acuerdos-empty" role="status">No hay acuerdos para mostrar.</p>;
  }
  return (
    <TablaPaginada
      caption="Acuerdos de abastecimiento"
      className="acuerdos-table"
      classNameContenedor="acuerdos-table-scroll"
      classNamePaginacion="acuerdos-pagination"
      columnas={columnas}
      elementos={lista.items}
      etiquetaRegion="Listado de acuerdos de abastecimiento"
      obtenerClave={(acuerdo) => acuerdo.id}
      renderizarFila={(acuerdo) => {
        const aviso = avisoVigencia(acuerdo);
        return (
          <tr aria-selected={seleccionadoId === acuerdo.id} className={seleccionadoId === acuerdo.id ? 'is-selected' : ''} key={acuerdo.id}>
            <th scope="row">
              <button className="acuerdo-row-select" onClick={() => seleccionar(acuerdo)} type="button">{acuerdo.numero}</button>
            </th>
            <td>{acuerdo.proveedorNombre}</td>
            <td><span className={`acuerdo-estado is-${acuerdo.estado.toLowerCase()}`}>{acuerdo.estado}</span></td>
            <td>
              {formatearFechaCorta(acuerdo.fechaInicio)} – {formatearFechaCorta(acuerdo.fechaFin)}
              {aviso !== null && <span className="acuerdo-aviso">{aviso}</span>}
            </td>
            <td>{formateadorMoneda.format(acuerdo.montoMaximo)}</td>
            <td>
              {formateadorMoneda.format(acuerdo.montoConsumido)} / {formateadorMoneda.format(acuerdo.montoComprometido)}
              <span className="acuerdo-porcentaje">{porcentajeConsumo(acuerdo.montoConsumido, acuerdo.montoComprometido)}%</span>
            </td>
          </tr>
        );
      }}
      cambiarPagina={cambiarPagina}
      pagina={pagina}
      totalPaginas={Math.max(1, Math.ceil(lista.total / lista.tamanoPagina))}
    />
  );
}
