import { useEffect, useState } from 'react';
import { TablaPaginada } from '../../components/TablaPaginada';
import { activarEquipo, actualizarEquipo, crearEquipo, desactivarEquipo } from './api';
import { EquipoFormulario } from './EquipoFormulario';
import { ActivoPastilla } from './Pastillas';
import { aFiltroActivo, esConflicto, FILTROS_ACTIVO, mensajeError, TAMANO_PAGINA, TEXTO_SOLO_RRHH } from './presentacion';
import type { EquipoProteccion, FiltrosEquipos, FiltroActivo, GuardarEquipoRequest } from './tipos';
import { useEquiposData } from './useEppData';

interface Props {
  puedeEscribir: boolean;
  alCambiar: () => void;
}

type Modo = { tipo: 'cerrado' } | { tipo: 'nuevo' } | { tipo: 'editar'; equipo: EquipoProteccion };

const INICIALES: FiltrosEquipos = { busqueda: '', activo: 'Todos', pagina: 1, tamanoPagina: TAMANO_PAGINA };

// Catálogo de equipos: alta, edición, activar y desactivar desde la propia fila.
export function EquiposPanel({ puedeEscribir, alCambiar }: Props) {
  const [busqueda, setBusqueda] = useState('');
  const [filtros, setFiltros] = useState<FiltrosEquipos>(INICIALES);
  const [modo, setModo] = useState<Modo>({ tipo: 'cerrado' });
  const [enCursoId, setEnCursoId] = useState<number | null>(null);
  const [errorAccion, setErrorAccion] = useState('');
  const data = useEquiposData(filtros);

  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      setFiltros((actual) => {
        const termino = busqueda.trim();
        return actual.busqueda === termino ? actual : { ...actual, busqueda: termino, pagina: 1 };
      });
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarActivo(activo: FiltroActivo): void {
    setFiltros((actual) => ({ ...actual, activo, pagina: 1 }));
  }

  async function guardar(request: GuardarEquipoRequest): Promise<void> {
    try {
      if (modo.tipo === 'editar') {
        await actualizarEquipo(modo.equipo.id, request);
      } else {
        await crearEquipo(request);
      }
      setModo({ tipo: 'cerrado' });
      data.recargar();
      alCambiar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  async function alternar(equipo: EquipoProteccion): Promise<void> {
    setEnCursoId(equipo.id);
    setErrorAccion('');
    try {
      if (equipo.activo) {
        await desactivarEquipo(equipo.id);
      } else {
        await activarEquipo(equipo.id);
      }
      alCambiar();
    } catch (error: unknown) {
      setErrorAccion(mensajeError(error, 'No se pudo cambiar el estado del equipo.'));
    } finally {
      setEnCursoId(null);
      data.recargar();
    }
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const items = data.lista?.items ?? [];

  return (
    <>
      <section aria-labelledby="equipos-list-title" className="clientes-list-panel sst-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">EPP</div>
            <h2 id="equipos-list-title">Catálogo de equipos</h2>
          </div>
          <button aria-describedby={puedeEscribir ? undefined : 'equipos-solo-rrhh'} className="primary-button" disabled={!puedeEscribir} onClick={() => setModo({ tipo: 'nuevo' })} type="button">Nuevo equipo</button>
        </div>
        {!puedeEscribir && <p className="sst-aviso" id="equipos-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        <div className="solicitudes-filters sst-filters">
          <div>
            <label htmlFor="equipos-busqueda">Buscar por código o nombre</label>
            <input autoComplete="off" id="equipos-busqueda" onChange={(event) => setBusqueda(event.currentTarget.value)} placeholder="Escribe para buscar…" type="search" value={busqueda} />
          </div>
          <div>
            <label htmlFor="equipos-activo">Estado</label>
            <select id="equipos-activo" onChange={(event) => cambiarActivo(aFiltroActivo(event.currentTarget.value))} value={filtros.activo}>
              {FILTROS_ACTIVO.map((item) => <option key={item} value={item}>{item}</option>)}
            </select>
          </div>
        </div>
        {data.errorLista !== '' && <p aria-live="polite" className="clientes-error" role="alert">{data.errorLista}</p>}
        {data.errorLista === '' && data.lista === null && <p aria-live="polite" className="clientes-status" role="status">Cargando equipos…</p>}
        {data.errorLista === '' && data.lista !== null && items.length === 0 && <p aria-live="polite" className="clientes-status" role="status">No hay equipos con esos filtros.</p>}
        {data.errorLista === '' && items.length > 0 && (
          <TablaPaginada
            caption="Catálogo de equipos de protección"
            etiquetaRegion="Tabla de equipos de protección"
            columnas={[
              { id: 'codigo', etiqueta: 'Código' },
              { id: 'nombre', etiqueta: 'Nombre' },
              { id: 'vida', etiqueta: 'Vida útil (días)' },
              { id: 'estado', etiqueta: 'Estado' },
              { id: 'acciones', etiqueta: 'Acciones' },
            ]}
            elementos={items}
            obtenerClave={(equipo) => equipo.id}
            pagina={filtros.pagina}
            totalPaginas={paginas}
            cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))}
            className="sst-table"
            classNamePaginacion="clientes-pagination"
            renderizarFila={(equipo) => (
              <tr>
                <td>{equipo.codigo}</td>
                <td>{equipo.nombre}</td>
                <td className="sst-numero">{equipo.vidaUtilDias}</td>
                <td><ActivoPastilla activo={equipo.activo} /></td>
                <td>
                  <div className="sst-fila-acciones">
                    <button aria-label={`Editar equipo ${equipo.codigo}`} className="secondary-button sst-fila-boton" disabled={!puedeEscribir} onClick={() => setModo({ tipo: 'editar', equipo })} type="button">Editar</button>
                    <button aria-label={`${equipo.activo ? 'Desactivar' : 'Activar'} equipo ${equipo.codigo}`} className="secondary-button sst-fila-boton" disabled={!puedeEscribir || enCursoId === equipo.id} onClick={() => void alternar(equipo)} type="button">
                      {equipo.activo ? 'Desactivar' : 'Activar'}
                    </button>
                  </div>
                </td>
              </tr>
            )}
          />
        )}
        {errorAccion !== '' && <p aria-live="polite" className="clientes-error" role="alert">{errorAccion}</p>}
      </section>
      {modo.tipo !== 'cerrado' && puedeEscribir && (
        <EquipoFormulario cancelar={() => setModo({ tipo: 'cerrado' })} equipo={modo.tipo === 'editar' ? modo.equipo : null} guardar={guardar} key={modo.tipo === 'editar' ? modo.equipo.id : 'nuevo'} />
      )}
    </>
  );
}
