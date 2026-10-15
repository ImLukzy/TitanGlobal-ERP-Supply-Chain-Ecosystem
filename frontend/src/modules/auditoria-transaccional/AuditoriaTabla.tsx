import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearFechaHoraUtc, textoAccion } from './presentacion';
import type { RegistroListado } from './tipos';

interface Props {
  items: RegistroListado[];
  total: number;
  pagina: number;
  error: string;
  cargando: boolean;
  seleccionadoId: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

export function AuditoriaTabla({ items, total, pagina, error, cargando, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  const totalPaginas = Math.max(1, Math.ceil(total / 20));
  if (error !== '') {
    return <p className="clientes-error" role="alert">{error}</p>;
  }
  if (cargando) {
    return <p role="status">Cargando registros de auditoría…</p>;
  }
  if (items.length === 0) {
    return <p role="status">No hay registros de auditoría que coincidan con los filtros.</p>;
  }

  return <TablaPaginada
    caption="Registros de auditoría"
    etiquetaRegion="Tabla de auditoría"
    columnas={[
      { id: 'fecha', etiqueta: 'Fecha y hora (UTC)' },
      { id: 'entidad', etiqueta: 'Entidad' },
      { id: 'entidad-id', etiqueta: 'Id' },
      { id: 'accion', etiqueta: 'Acción' },
      { id: 'usuario', etiqueta: 'Usuario' },
      { id: 'ruta', etiqueta: 'Ruta' },
      { id: 'cambios', etiqueta: 'Campos cambiados' },
    ]}
    elementos={items}
    obtenerClave={(item) => item.id}
    pagina={pagina}
    totalPaginas={totalPaginas}
    cambiarPagina={cambiarPagina}
    className="auditoria-table"
    classNameContenedor="auditoria-table-scroll"
    classNamePaginacion="auditoria-pagination"
    renderizarFila={(item) => <tr className={item.id === seleccionadoId ? 'is-selected' : ''} key={item.id}>
      <th scope="row"><button aria-label={`Ver registro ${item.id}`} className="auditoria-row-link" onClick={() => seleccionar(item.id)} type="button">{formatearFechaHoraUtc(item.fecha)}</button></th>
      <td>{item.entidad}</td>
      <td>{item.entidadId}</td>
      <td><span className={`auditoria-accion auditoria-accion-${item.accion.toLowerCase()}`}>{textoAccion(item.accion)}</span></td>
      <td>{item.usuario}</td>
      <td className="auditoria-ruta" title={item.ruta ?? undefined}>{item.ruta ?? '—'}</td>
      <td>{item.camposCambiados}</td>
    </tr>}
  />;
}
