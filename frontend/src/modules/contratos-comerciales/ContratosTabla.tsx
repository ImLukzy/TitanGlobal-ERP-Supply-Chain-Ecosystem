import { formateadorMoneda, formatearFecha } from '../../lib/formato';
import { TablaPaginada } from '../../components/TablaPaginada';
import type { ContratoListado } from './tipos';

interface Props { items: ContratoListado[]; total: number; pagina: number; error: string; cargando: boolean; seleccionadoId: number | null; seleccionar: (id: number) => void; cambiarPagina: (pagina: number) => void; }

export function ContratosTabla({ items, total, pagina, error, cargando, seleccionadoId, seleccionar, cambiarPagina }: Props) {
  const paginas = Math.max(1, Math.ceil(total / 10));
  return (
    <>
      {error && <p className="clientes-error" role="alert">{error}</p>}
      {!error && cargando && <p role="status">Cargando contratos…</p>}
      {!error && !cargando && items.length === 0 && <p role="status">No hay contratos que coincidan con los filtros.</p>}
      {items.length > 0 && <TablaPaginada
        caption="Contratos comerciales"
        etiquetaRegion="Tabla de contratos comerciales"
        columnas={[{ id: 'numero', etiqueta: 'Número' }, { id: 'cliente', etiqueta: 'Cliente' }, { id: 'titulo', etiqueta: 'Título' }, { id: 'periodo', etiqueta: 'Periodo' }, { id: 'estado', etiqueta: 'Estado' }, { id: 'comprometido', etiqueta: 'Compromiso' }, { id: 'consumo', etiqueta: 'Consumo' }]}
        elementos={items}
        obtenerClave={(item) => item.id}
        pagina={pagina}
        totalPaginas={paginas}
        cambiarPagina={cambiarPagina}
        className="solicitudes-table contratos-table"
        classNameContenedor="solicitudes-table-scroll contratos-table-scroll"
        classNamePaginacion="solicitudes-pagination"
        renderizarFila={(item) => <tr className={item.id === seleccionadoId ? 'is-selected' : ''}>
          <th scope="row"><button className="solicitud-row-select" onClick={() => seleccionar(item.id)} type="button">{item.numero}</button></th>
          <td>{item.clienteNombre}</td>
          <td>{item.titulo}</td>
          <td>{formatearFecha(item.fechaInicio)} – {formatearFecha(item.fechaFin)}</td>
          <td><span className={`contrato-status is-${item.estado.toLowerCase()}`}>{item.estado}</span>{item.vencido && <span className="contrato-expired-badge">Vencido</span>}</td>
          <td>{formateadorMoneda.format(item.montoComprometido)}</td>
          <td>
            <div aria-label={`Consumo de ${item.numero}`} aria-valuemax={100} aria-valuemin={0} aria-valuenow={Math.min(100, Math.max(0, item.porcentajeConsumido))} aria-valuetext={`${formateadorMoneda.format(item.montoConsumido)} de ${formateadorMoneda.format(item.montoComprometido)} · ${item.porcentajeConsumido.toFixed(2)} %${item.porcentajeConsumido > 100 ? ' · Excedido' : ''}`} className="zonas-meter" role="meter"><span style={{ width: `${Math.min(100, Math.max(0, item.porcentajeConsumido))}%` }} /></div>
            <small className="contrato-consumption-label">
              <span>{item.porcentajeConsumido.toFixed(2)} %</span>
              {item.porcentajeConsumido > 100 && <span className="contrato-exceeded-label">Excedido</span>}
            </small>
          </td>
        </tr>}
      />}
    </>
  );
}
