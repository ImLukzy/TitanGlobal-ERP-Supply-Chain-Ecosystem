import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearFecha } from '../../lib/formato';
import type { Alerta } from './tipos';
import { claseSeveridad, nombreTipo, rutaAlerta } from './presentacion';
import { Link } from 'react-router';

interface Props {
  alertas: Alerta[];
  total: number;
  pagina: number;
  cargando: boolean;
  error: string;
  seleccionar: (alerta: Alerta) => void;
  actuar: (alerta: Alerta, boton: HTMLButtonElement) => void;
  seleccionadaClave: string | null;
  cambiarPagina: (pagina: number) => void;
}

export function AlertasTabla({ alertas, total, pagina, cargando, error, seleccionar, actuar, seleccionadaClave, cambiarPagina }: Props) {
  const paginas = Math.max(1, Math.ceil(total / 20));
  if (error !== '') {
    return <p className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p role="status">Cargando alertas…</p>;
  }
  if (alertas.length === 0) {
    return <p role="status">No hay alertas pendientes.</p>;
  }
  return (
    <TablaPaginada
      caption="Centro de alertas"
      etiquetaRegion="Tabla de alertas"
      columnas={[
        { id: 'severidad', etiqueta: 'Severidad' },
        { id: 'alerta', etiqueta: 'Alerta' },
        { id: 'fecha', etiqueta: 'Fecha de referencia' },
        { id: 'accion', etiqueta: 'Acción' },
      ]}
      elementos={alertas}
      obtenerClave={(alerta) => alerta.clave}
      pagina={pagina}
      totalPaginas={paginas}
      cambiarPagina={cambiarPagina}
      className="alertas-table"
      classNameContenedor="alertas-table-scroll"
      classNamePaginacion="alertas-pagination"
      renderizarFila={(alerta) => (
        <tr className={alerta.clave === seleccionadaClave ? 'is-selected' : ''} key={alerta.clave}>
          <td><span className={claseSeveridad(alerta.severidad)}>{alerta.severidad}</span></td>
          <td className="alertas-message-cell">
            <button
              aria-label={`Ver detalle: ${nombreTipo(alerta.tipo)}`}
              className="alertas-row-message"
              onClick={() => seleccionar(alerta)}
              type="button"
            >
              {alerta.mensaje}
            </button>
            <small>{nombreTipo(alerta.tipo)} · Módulo {alerta.modulo}</small>
            <Link className="alertas-view-link" to={rutaAlerta(alerta)}>Ver</Link>
          </td>
          <td className="alertas-date-cell">{formatearFecha(alerta.fechaReferencia)}</td>
          <td>
            <button
              className={alerta.descartada ? 'alertas-restore-button' : 'alertas-dismiss-button'}
              onClick={(event) => actuar(alerta, event.currentTarget)}
              type="button"
            >
              {alerta.descartada ? 'Restaurar' : 'Descartar'}
            </button>
          </td>
        </tr>
      )}
    />
  );
}
