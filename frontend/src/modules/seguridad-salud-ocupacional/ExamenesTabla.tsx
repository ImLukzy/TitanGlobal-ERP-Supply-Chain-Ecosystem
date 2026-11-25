import { TablaPaginada } from '../../components/TablaPaginada';
import { ResultadoPastilla, VencimientoPastilla } from './Pastillas';
import { textoFecha, textoValor } from './presentacion';
import type { ExamenMedico } from './tipos';

interface Props {
  items: ExamenMedico[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  cambiarPagina: (pagina: number) => void;
}

export function ExamenesTabla({ items, error, cargando, pagina, totalPaginas, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando exámenes médicos…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay exámenes médicos con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de exámenes médicos"
      etiquetaRegion="Tabla de exámenes médicos"
      columnas={[
        { id: 'empleado', etiqueta: 'Empleado' },
        { id: 'tipo', etiqueta: 'Tipo' },
        { id: 'fecha', etiqueta: 'Fecha' },
        { id: 'resultado', etiqueta: 'Resultado' },
        { id: 'vencimiento', etiqueta: 'Vencimiento' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(examen) => examen.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="sst-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(examen) => (
        <tr>
          <td>{examen.empleado}</td>
          <td>{textoValor(examen.tipo)}</td>
          <td className="sst-numero">{textoFecha(examen.fecha)}</td>
          <td>
            <ResultadoPastilla resultado={examen.resultado} />
            {examen.restricciones !== null && <span className="sst-restricciones">{examen.restricciones}</span>}
          </td>
          <td className="sst-numero">{examen.fechaVencimiento === null ? 'No aplica' : textoFecha(examen.fechaVencimiento)}</td>
          <td>{examen.fechaVencimiento === null ? <span className="sst-pastilla is-normal">Sin vencimiento</span> : <VencimientoPastilla masculino vencido={examen.vencido} />}</td>
        </tr>
      )}
    />
  );
}
