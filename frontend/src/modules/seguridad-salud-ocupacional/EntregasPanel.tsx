import { useState } from 'react';
import { TablaPaginada } from '../../components/TablaPaginada';
import type { EmpleadoListado } from '../empleados/tipos';
import { registrarEntrega } from './api';
import { EntregaFormulario } from './EntregaFormulario';
import { VencimientoPastilla } from './Pastillas';
import { aNumeroOnulo, TAMANO_PAGINA, TEXTO_SOLO_RRHH, textoFecha } from './presentacion';
import type { EquipoProteccion, FiltrosEntregas, RegistrarEntregaRequest } from './tipos';
import { useEntregasData } from './useEppData';

interface Props {
  puedeEscribir: boolean;
  alCambiar: () => void;
  empleados: EmpleadoListado[];
  equipos: EquipoProteccion[];
  vencidasInicial: boolean;
}

export function EntregasPanel({ puedeEscribir, alCambiar, empleados, equipos, vencidasInicial }: Props) {
  const [filtros, setFiltros] = useState<FiltrosEntregas>({ empleadoId: null, equipoId: null, vencidas: vencidasInicial, pagina: 1, tamanoPagina: TAMANO_PAGINA });
  const [formulario, setFormulario] = useState(false);
  const data = useEntregasData(filtros);

  async function registrar(request: RegistrarEntregaRequest): Promise<void> {
    await registrarEntrega(request);
    setFormulario(false);
    data.recargar();
    alCambiar();
  }

  function cambiar(cambios: Partial<FiltrosEntregas>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: 1 }));
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const items = data.lista?.items ?? [];

  return (
    <>
      <section aria-labelledby="entregas-list-title" className="clientes-list-panel sst-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">EPP</div>
            <h2 id="entregas-list-title">Entregas de EPP</h2>
          </div>
          <button aria-describedby={puedeEscribir ? undefined : 'entregas-solo-rrhh'} className="primary-button" disabled={!puedeEscribir} onClick={() => setFormulario(true)} type="button">Nueva entrega</button>
        </div>
        {!puedeEscribir && <p className="sst-aviso" id="entregas-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        <div className="solicitudes-filters sst-filters sst-filters-tres">
          <div>
            <label htmlFor="entregas-empleado">Empleado</label>
            <select id="entregas-empleado" onChange={(event) => cambiar({ empleadoId: aNumeroOnulo(event.currentTarget.value) })} value={filtros.empleadoId === null ? '' : String(filtros.empleadoId)}>
              <option value="">Todos</option>
              {empleados.map((empleado) => <option key={empleado.id} value={String(empleado.id)}>{`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}</option>)}
            </select>
          </div>
          <div>
            <label htmlFor="entregas-equipo">Equipo</label>
            <select id="entregas-equipo" onChange={(event) => cambiar({ equipoId: aNumeroOnulo(event.currentTarget.value) })} value={filtros.equipoId === null ? '' : String(filtros.equipoId)}>
              <option value="">Todos</option>
              {equipos.map((equipo) => <option key={equipo.id} value={String(equipo.id)}>{`${equipo.codigo} · ${equipo.nombre}`}</option>)}
            </select>
          </div>
          <label className="sst-check" htmlFor="entregas-vencidas">
            <input checked={filtros.vencidas} id="entregas-vencidas" onChange={(event) => cambiar({ vencidas: event.currentTarget.checked })} type="checkbox" />
            Solo vencidas
          </label>
        </div>
        {data.errorLista !== '' && <p aria-live="polite" className="clientes-error" role="alert">{data.errorLista}</p>}
        {data.errorLista === '' && data.lista === null && <p aria-live="polite" className="clientes-status" role="status">Cargando entregas…</p>}
        {data.errorLista === '' && data.lista !== null && items.length === 0 && <p aria-live="polite" className="clientes-status" role="status">No hay entregas con esos filtros.</p>}
        {data.errorLista === '' && items.length > 0 && (
          <TablaPaginada
            caption="Listado de entregas de EPP"
            etiquetaRegion="Tabla de entregas de EPP"
            columnas={[
              { id: 'empleado', etiqueta: 'Empleado' },
              { id: 'equipo', etiqueta: 'Equipo' },
              { id: 'entrega', etiqueta: 'Entrega' },
              { id: 'cantidad', etiqueta: 'Cantidad' },
              { id: 'vencimiento', etiqueta: 'Vencimiento' },
              { id: 'estado', etiqueta: 'Estado' },
            ]}
            elementos={items}
            obtenerClave={(entrega) => entrega.id}
            pagina={filtros.pagina}
            totalPaginas={paginas}
            cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))}
            className="sst-table"
            classNamePaginacion="clientes-pagination"
            renderizarFila={(entrega) => (
              <tr>
                <td>{entrega.empleado}</td>
                <td>{`${entrega.equipoCodigo} · ${entrega.equipoNombre}`}</td>
                <td className="sst-numero">{textoFecha(entrega.fechaEntrega)}</td>
                <td className="sst-numero">{entrega.cantidad}</td>
                <td className="sst-numero">{textoFecha(entrega.fechaVencimiento)}</td>
                <td><VencimientoPastilla vencido={entrega.vencida} /></td>
              </tr>
            )}
          />
        )}
      </section>
      {formulario && puedeEscribir && <EntregaFormulario cancelar={() => setFormulario(false)} empleados={empleados} equipos={equipos} guardar={registrar} />}
    </>
  );
}
