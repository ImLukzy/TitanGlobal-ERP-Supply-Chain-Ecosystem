import { TablaPaginada } from '../../components/TablaPaginada';
import { textoEstadoCiclo, textoFecha } from './presentacion';
import type { CicloListado, EstadoCiclo } from './tipos';

interface Props {
  items: CicloListado[];
  error: string;
  cargando: boolean;
  pagina: number;
  totalPaginas: number;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

// El estado se muestra con texto, nunca solo con color.
export function EstadoCicloPastilla({ estado }: { estado: EstadoCiclo }) {
  return <span className={`evaluacion-desempeno-estado is-${estado.toLowerCase()}`}>{textoEstadoCiclo(estado)}</span>;
}

export function CiclosTabla({ items, error, cargando, pagina, totalPaginas, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando ciclos…</p>;
  }
  if (items.length === 0) {
    return <p aria-live="polite" className="clientes-status" role="status">No hay ciclos con esos filtros.</p>;
  }
  return (
    <TablaPaginada
      caption="Listado de ciclos de evaluación"
      etiquetaRegion="Tabla de ciclos de evaluación"
      columnas={[
        { id: 'codigo', etiqueta: 'Código' },
        { id: 'nombre', etiqueta: 'Nombre' },
        { id: 'periodo', etiqueta: 'Periodo' },
        { id: 'fechas', etiqueta: 'Fechas' },
        { id: 'estado', etiqueta: 'Estado' },
      ]}
      elementos={items}
      obtenerClave={(ciclo) => ciclo.id}
      pagina={pagina}
      totalPaginas={totalPaginas}
      cambiarPagina={cambiarPagina}
      className="evaluacion-desempeno-table"
      classNamePaginacion="clientes-pagination"
      renderizarFila={(ciclo) => (
        <tr className={ciclo.id === seleccionadoId ? 'is-selected' : ''}>
          <td>
            <button
              aria-current={ciclo.id === seleccionadoId ? 'true' : undefined}
              className="cliente-row-select"
              onClick={() => seleccionar(ciclo.id)}
              type="button"
            >
              {ciclo.codigo}
            </button>
          </td>
          <td>{ciclo.nombre}</td>
          <td>{ciclo.periodo}</td>
          <td className="evaluacion-desempeno-numero">{`${textoFecha(ciclo.fechaInicioPeriodo)} - ${textoFecha(ciclo.fechaFinPeriodo)}`}</td>
          <td><EstadoCicloPastilla estado={ciclo.estado} /></td>
        </tr>
      )}
    />
  );
}
