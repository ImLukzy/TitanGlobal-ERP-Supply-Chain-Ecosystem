import { useEffect, useRef, useState } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { iniciarMantenimiento, listarMantenimientos, obtenerMantenimiento, obtenerResumenMantenimientos } from './api';
import { CancelarMantenimientoDialog } from './CancelarMantenimientoDialog';
import { CompletarMantenimientoDialog } from './CompletarMantenimientoDialog';
import { MantenimientoDetallePanel } from './MantenimientoDetallePanel';
import { MantenimientoFormulario } from './MantenimientoFormulario';
import { MantenimientosFiltros, type ValoresFiltros } from './MantenimientosFiltros';
import { MantenimientosResumen } from './MantenimientosResumen';
import { MantenimientosTabla } from './MantenimientosTabla';
import type { MantenimientoDetalle, PaginaMantenimientos, ResumenMantenimientos } from './tipos';

const tamanoPagina = 20;
const esperaBusquedaMs = 300;
const idDetalle = 'mantenimiento-preventivo-detalle';
const filtrosIniciales: ValoresFiltros = { busqueda: '', centro: null, estado: '', desde: '', hasta: '' };

type Resultado<T> = { datos: T; error?: never } | { datos?: never; error: string };
type Dialogo = { tipo: 'completar' | 'cancelar'; mantenimiento: MantenimientoDetalle };

function consultar<T>(peticion: Promise<T>, recibir: (resultado: Resultado<T>) => void): () => void {
  let vigente = true;
  peticion
    .then((datos) => {
      if (vigente) recibir({ datos });
    })
    .catch((causa: unknown) => {
      if (vigente) recibir({ error: causa instanceof Error ? causa.message : 'No se pudo cargar la información.' });
    });
  return () => {
    vigente = false;
  };
}

export function MantenimientoPreventivoPage() {
  const [filtros, setFiltros] = useState(filtrosIniciales);
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [pagina, setPagina] = useState(1);
  const [seleccion, setSeleccion] = useState<number | null>(null);
  const [revision, setRevision] = useState(0);
  const [lectura, setLectura] = useState<{ clave: string; resultado: Resultado<PaginaMantenimientos> }>();
  const [resumen, setResumen] = useState<{ clave: number; resultado: Resultado<ResumenMantenimientos> }>();
  const [detalle, setDetalle] = useState<{ clave: string; resultado: Resultado<MantenimientoDetalle> }>();
  const [formulario, setFormulario] = useState<{ mantenimiento: MantenimientoDetalle | null }>();
  const [dialogo, setDialogo] = useState<Dialogo>();
  const [aviso, setAviso] = useState('');
  const [errorAccion, setErrorAccion] = useState('');
  const [iniciando, setIniciando] = useState(false);
  const focoPendiente = useRef<string | null>(null);
  const rangoInvalido = filtros.desde !== '' && filtros.hasta !== '' && filtros.desde > filtros.hasta;
  const busqueda = busquedaAplicada.trim();
  const centroTrabajoId = filtros.centro?.id;
  const { estado, desde, hasta } = filtros;
  const claveLectura = [busqueda, centroTrabajoId ?? '', estado, desde, hasta, pagina, revision].join('/');
  const claveDetalle = `${seleccion ?? ''}/${revision}`;
  const resultadoLista = lectura?.clave === claveLectura ? lectura.resultado : undefined;
  const resultadoResumen = resumen?.clave === revision ? resumen.resultado : undefined;
  const resultadoDetalle = detalle?.clave === claveDetalle ? detalle.resultado : undefined;
  const datos = resultadoLista?.datos;

  useEffect(() => {
    const temporizador = window.setTimeout(() => setBusquedaAplicada(filtros.busqueda), esperaBusquedaMs);
    return () => window.clearTimeout(temporizador);
  }, [filtros.busqueda]);

  useEffect(() => {
    if (rangoInvalido) {
      return undefined;
    }
    const peticion = listarMantenimientos({ busqueda, centroTrabajoId, estado, desde, hasta, pagina, tamanoPagina });
    return consultar(peticion, (resultado) => setLectura({ clave: claveLectura, resultado }));
  }, [busqueda, centroTrabajoId, claveLectura, desde, estado, hasta, pagina, rangoInvalido]);

  useEffect(() => consultar(obtenerResumenMantenimientos(), (resultado) => setResumen({ clave: revision, resultado })), [revision]);

  useEffect(() => {
    if (seleccion === null) {
      return undefined;
    }
    return consultar(obtenerMantenimiento(seleccion), (resultado) => setDetalle({ clave: claveDetalle, resultado }));
  }, [claveDetalle, seleccion]);

  // Devuelve el foco al botón de origen; si desapareció (cambió el estado), al encabezado del detalle.
  useEffect(() => {
    if (focoPendiente.current !== null && dialogo === undefined && formulario === undefined) {
      const objetivo = focoPendiente.current;
      focoPendiente.current = null;
      window.requestAnimationFrame(() => {
        (document.getElementById(objetivo) ?? document.getElementById(idDetalle))?.focus();
      });
    }
  }, [dialogo, formulario, resultadoDetalle]);

  function cambiarFiltros(cambios: Partial<ValoresFiltros>): void {
    setFiltros((actuales) => ({ ...actuales, ...cambios }));
    setPagina(1);
  }

  function actualizar(): void {
    setRevision((actual) => actual + 1);
  }

  function limpiarMensajes(): void {
    setAviso('');
    setErrorAccion('');
  }

  function cerrarFormulario(objetivo: string): void {
    focoPendiente.current = objetivo;
    setFormulario(undefined);
  }

  function cerrarDialogo(objetivo: string): void {
    focoPendiente.current = objetivo;
    setDialogo(undefined);
  }

  function terminarDialogo(mensaje: string): void {
    setAviso(mensaje);
    cerrarDialogo(idDetalle);
    actualizar();
  }

  async function iniciar(mantenimiento: MantenimientoDetalle): Promise<void> {
    if (iniciando) {
      return;
    }
    limpiarMensajes();
    setIniciando(true);
    try {
      await iniciarMantenimiento(mantenimiento.id);
      setAviso('Mantenimiento iniciado.');
      focoPendiente.current = idDetalle;
      actualizar();
    } catch (causa: unknown) {
      setErrorAccion(causa instanceof Error ? causa.message : 'No se pudo iniciar el mantenimiento.');
    } finally {
      setIniciando(false);
    }
  }

  return (
    <PaginaModulo className="mantenimiento-preventivo-page" modulo="39-mantenimiento-preventivo">
      <div inert={dialogo !== undefined}>
        <p className="module-intro">Programa las paradas de mantenimiento de cada centro de trabajo, sigue las vencidas y las próximas, y registra las horas reales al cerrarlas.</p>
        {aviso && <p className="mantenimiento-preventivo-aviso" role="status">{aviso}</p>}
        <MantenimientosResumen datos={resultadoResumen?.datos} error={resultadoResumen?.error} reintentar={actualizar} />
        {formulario ? (
          <MantenimientoFormulario
            cancelar={() => cerrarFormulario(formulario.mantenimiento ? 'mantenimiento-preventivo-editar' : 'mantenimiento-preventivo-nuevo')}
            guardado={(id) => {
              setSeleccion(id);
              setAviso(formulario.mantenimiento ? 'Mantenimiento actualizado.' : 'Mantenimiento programado.');
              cerrarFormulario(idDetalle);
              actualizar();
            }}
            mantenimiento={formulario.mantenimiento}
          />
        ) : (
          <div className="mantenimiento-preventivo-area">
            <section aria-labelledby="mantenimiento-preventivo-listado" className="mantenimiento-preventivo-tarjeta">
              <div className="mantenimiento-preventivo-encabezado">
                <h2 id="mantenimiento-preventivo-listado" tabIndex={-1}>Paradas programadas</h2>
                <button
                  className="primary-button"
                  id="mantenimiento-preventivo-nuevo"
                  onClick={() => {
                    limpiarMensajes();
                    setFormulario({ mantenimiento: null });
                  }}
                  type="button"
                >
                  Nuevo mantenimiento
                </button>
              </div>
              <MantenimientosFiltros cambiar={cambiarFiltros} rangoInvalido={rangoInvalido} valores={filtros} />
              {!rangoInvalido && resultadoLista === undefined && <p role="status">Cargando mantenimientos…</p>}
              {resultadoLista?.error && (
                <div className="mantenimiento-preventivo-error">
                  <p className="field-error" role="alert">{resultadoLista.error}</p>
                  <button className="secondary-button" onClick={actualizar} type="button">Reintentar listado</button>
                </div>
              )}
              {!rangoInvalido && datos?.items.length === 0 && <p role="status">No se encontraron mantenimientos.</p>}
              {!rangoInvalido && datos && datos.items.length > 0 && (
                <MantenimientosTabla
                  cambiarPagina={setPagina}
                  datos={datos}
                  seleccion={seleccion}
                  seleccionar={(id) => {
                    setErrorAccion('');
                    setSeleccion(id);
                    document.getElementById(idDetalle)?.focus();
                  }}
                />
              )}
            </section>
            <MantenimientoDetallePanel
              cancelar={(mantenimiento) => {
                limpiarMensajes();
                setDialogo({ tipo: 'cancelar', mantenimiento });
              }}
              completar={(mantenimiento) => {
                limpiarMensajes();
                setDialogo({ tipo: 'completar', mantenimiento });
              }}
              editar={(mantenimiento) => {
                limpiarMensajes();
                setFormulario({ mantenimiento });
              }}
              error={resultadoDetalle?.error}
              errorAccion={errorAccion}
              iniciando={iniciando}
              iniciar={(mantenimiento) => void iniciar(mantenimiento)}
              mantenimiento={resultadoDetalle?.datos}
              reintentar={actualizar}
              seleccion={seleccion}
            />
          </div>
        )}
      </div>
      {dialogo?.tipo === 'completar' && (
        <CompletarMantenimientoDialog
          cerrar={() => cerrarDialogo('mantenimiento-preventivo-completar')}
          confirmado={() => terminarDialogo('Mantenimiento completado.')}
          mantenimiento={dialogo.mantenimiento}
        />
      )}
      {dialogo?.tipo === 'cancelar' && (
        <CancelarMantenimientoDialog
          cerrar={() => cerrarDialogo('mantenimiento-preventivo-cancelar')}
          confirmado={() => terminarDialogo('Mantenimiento cancelado.')}
          mantenimiento={dialogo.mantenimiento}
        />
      )}
    </PaginaModulo>
  );
}
