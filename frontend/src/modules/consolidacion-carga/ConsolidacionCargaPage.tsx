import { useCallback, useRef, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { actualizarCarga, cancelarCarga, confirmarCarga, crearCarga, eliminarCarga } from './api';
import { ConsolidacionesFiltros } from './ConsolidacionesFiltros';
import { ConsolidacionesResumen } from './ConsolidacionesResumen';
import { ConsolidacionesTabla } from './ConsolidacionesTabla';
import { ConsolidacionDetallePanel } from './ConsolidacionDetallePanel';
import { ConsolidacionFormulario } from './ConsolidacionFormulario';
import { DialogoCancelarCarga } from './DialogoCancelarCarga';
import { mensajeError } from './presentacion';
import type { CargaDetalle, GuardarCarga } from './tipos';
import { useConsolidacionesData } from './useConsolidacionesData';

export function ConsolidacionCargaPage() {
  const datos = useConsolidacionesData();
  const [formulario, setFormulario] = useState(false);
  const [inicial, setInicial] = useState<CargaDetalle | null>(null);
  const [baja, setBaja] = useState(false);
  const [cancelacion, setCancelacion] = useState(false);
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState('');
  const candado = useRef(false);
  const origenBaja = useRef<HTMLElement | null>(null);
  const botonNuevo = useRef<HTMLButtonElement>(null);
  function abrir(ruta: CargaDetalle | null) {
    setInicial(ruta);
    setFormulario(true);
    setError('');
  }
  const cerrarBaja = useCallback(() => {
    if (!candado.current) {
      setBaja(false);
      requestAnimationFrame(() => origenBaja.current?.focus());
    }
  }, []);
  async function guardar(solicitud: GuardarCarga) {
    if (candado.current) return;
    candado.current = true;
    setOcupado(true);
    setError('');
    try {
      const respuesta = inicial
        ? await actualizarCarga(inicial.id, solicitud)
        : await crearCarga(solicitud);
      setFormulario(false);
      datos.seleccionar(respuesta.id);
      datos.refrescar();
    } catch (e) {
      setError(mensajeError(e));
    } finally {
      candado.current = false;
      setOcupado(false);
    }
  }
  async function operar(accion: 'eliminar' | 'confirmar' | 'cancelar', motivo = '') {
    if (!datos.detalle.datos || candado.current) return;
    candado.current = true;
    setOcupado(true);
    setError('');
    try {
      const id = datos.detalle.datos.id;
      if (accion === 'eliminar') {
        await eliminarCarga(id);
        datos.seleccionar(null);
        setBaja(false);
        requestAnimationFrame(() => botonNuevo.current?.focus());
      } else if (accion === 'confirmar') {
        await confirmarCarga(id);
      } else {
        await cancelarCarga(id, motivo);
        setCancelacion(false);
      }
      datos.refrescar();
    } catch (e) {
      setError(mensajeError(e));
    } finally {
      candado.current = false;
      setOcupado(false);
    }
  }
  return (
    <PaginaModulo modulo={44} className="consolidacion-carga-page">
      <p>Prepara el picking y el packing de cada línea y consulta su cubicaje estimado.</p>
      {datos.resumen.error ? (
        <p role="alert">
          {datos.resumen.error}
          <button type="button" onClick={datos.refrescar}>
            Reintentar resumen
          </button>
        </p>
      ) : datos.resumen.datos ? (
        <ConsolidacionesResumen datos={datos.resumen.datos} />
      ) : (
        <p role="status">Cargando resumen…</p>
      )}
      <div className="consolidacion-carga-toolbar">
        <h2>Preparación de carga</h2>
        <button
          ref={botonNuevo}
          className="primary-button"
          type="button"
          disabled={ocupado}
          onClick={() => abrir(null)}
        >
          Nueva consolidación
        </button>
      </div>
      <ConsolidacionesFiltros
        busqueda={datos.busqueda}
        estado={datos.estado}
        cambiarBusqueda={datos.setBusqueda}
        cambiarEstado={datos.cambiarEstado}
      />
      <div className="consolidacion-carga-layout">
        <section className="consolidacion-carga-card" aria-label="Listado de consolidaciones">
          {datos.lista.cargando && <p role="status">Cargando consolidaciones…</p>}
          {datos.lista.error && (
            <p role="alert">
              {datos.lista.error}
              <button type="button" onClick={datos.refrescar}>
                Reintentar listado
              </button>
            </p>
          )}
          {datos.lista.datos && (
            <>
              <p>{datos.lista.datos.total} consolidaciones</p>
              {datos.lista.datos.items.length ? (
                <ConsolidacionesTabla
                  datos={datos.lista.datos}
                  seleccion={datos.seleccion}
                  pagina={datos.pagina}
                  cambiar={datos.cambiarPagina}
                  seleccionar={(id) => {
                    if (!ocupado) {
                      datos.seleccionar(id);
                      setFormulario(false);
                      setError('');
                    }
                  }}
                />
              ) : (
                <p role="status">Sin consolidaciones coincidentes</p>
              )}
            </>
          )}
        </section>
        <div>
          {error && !formulario && !baja && !cancelacion && (
            <p role="alert" className="field-error">
              {error}
            </p>
          )}
          {formulario ? (
            <ConsolidacionFormulario
              key={inicial?.id ?? 'nueva'}
              inicial={inicial}
              ocupado={ocupado}
              error={error}
              guardar={guardar}
              cerrar={() => setFormulario(false)}
            />
          ) : datos.detalle.error ? (
            <p role="alert">
              {datos.detalle.error}
              <button type="button" onClick={datos.refrescar}>
                Reintentar detalle
              </button>
            </p>
          ) : datos.detalle.cargando ? (
            <p role="status">Cargando detalle…</p>
          ) : datos.detalle.datos ? (
            <ConsolidacionDetallePanel
              datos={datos.detalle.datos}
              ocupado={ocupado}
              editar={() => abrir(datos.detalle.datos)}
              confirmar={() => operar('confirmar')}
              eliminar={() => {
                origenBaja.current = document.activeElement as HTMLElement;
                setBaja(true);
                setError('');
              }}
              cancelar={() => {
                setCancelacion(true);
                setError('');
              }}
            />
          ) : (
            <p role="status">Selecciona una consolidación para ver el detalle.</p>
          )}
        </div>
      </div>
      {baja && datos.detalle.datos && (
        <DialogoBaja
          entidad="carga"
          nombre={datos.detalle.datos.nombre}
          cerrar={cerrarBaja}
          confirmar={() => operar('eliminar')}
          eliminando={ocupado}
          error={error}
        />
      )}
      {cancelacion && datos.detalle.datos && (
        <DialogoCancelarCarga
          referencia={datos.detalle.datos.codigo}
          ocupado={ocupado}
          error={error}
          cerrar={() => {
            if (!candado.current) setCancelacion(false);
          }}
          confirmar={(motivo) => operar('cancelar', motivo)}
        />
      )}
    </PaginaModulo>
  );
}
