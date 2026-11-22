import { useCallback, useRef, useState } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { DialogoBaja } from '../../components/DialogoBaja';
import {
  crearEntrega,
  actualizarEntrega,
  eliminarEntrega,
  iniciarEntrega,
  completarEntrega,
  cancelarEntrega,
} from './api';
import { useEntregasData } from './useEntregasData';
import { EntregasResumen } from './EntregasResumen';
import { EntregasFiltros } from './EntregasFiltros';
import { EntregasTabla } from './EntregasTabla';
import { EntregaDetallePanel } from './EntregaDetallePanel';
import { EntregaFormulario } from './EntregaFormulario';
import { DialogoSalidaEntrega } from './DialogoSalidaEntrega';
import { DialogoCancelarEntrega } from './DialogoCancelarEntrega';
import { DialogoPodEntrega } from './DialogoPodEntrega';
import { mensajeError } from './presentacion';
import type { EntregaDetalle, GuardarEntrega, RegistrarPod } from './tipos';
export function MonitoreoEntregasPage() {
  const datos = useEntregasData();
  const [formulario, setFormulario] = useState(false);
  const [inicial, setInicial] = useState<EntregaDetalle | null>(null);
  const [dialogo, setDialogo] = useState<'baja' | 'salida' | 'pod' | 'cancelacion' | null>(null);
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState('');
  const candado = useRef(false),
    origenBaja = useRef<HTMLElement | null>(null),
    botonNuevo = useRef<HTMLButtonElement>(null);
  const cerrarBaja = useCallback(() => {
    if (!candado.current) {
      setDialogo(null);
      requestAnimationFrame(() => origenBaja.current?.focus());
    }
  }, []);
  function abrir(d: EntregaDetalle | null) {
    setInicial(d);
    setFormulario(true);
    setError('');
  }
  function mostrar(d: 'baja' | 'salida' | 'pod' | 'cancelacion') {
    origenBaja.current = document.activeElement as HTMLElement;
    setDialogo(d);
    setError('');
  }
  async function mutar(accion: () => Promise<unknown>, despues: () => void) {
    if (candado.current) return;
    candado.current = true;
    setOcupado(true);
    setError('');
    try {
      await accion();
      despues();
      datos.refrescar();
    } catch (e) {
      setError(mensajeError(e));
    } finally {
      candado.current = false;
      setOcupado(false);
    }
  }
  async function guardar(d: GuardarEntrega) {
    await mutar(
      async () => {
        const r = inicial ? await actualizarEntrega(inicial.id, d) : await crearEntrega(d);
        datos.seleccionar(r.id);
      },
      () => setFormulario(false),
    );
  }
  async function operar(
    accion: 'baja' | 'salida' | 'pod' | 'cancelacion',
    solicitud?: RegistrarPod | string,
  ) {
    if (!datos.detalle.datos) return;
    const id = datos.detalle.datos.id;
    await mutar(
      () =>
        accion === 'baja'
          ? eliminarEntrega(id)
          : accion === 'salida'
            ? iniciarEntrega(id)
            : accion === 'pod'
              ? completarEntrega(id, solicitud as RegistrarPod)
              : cancelarEntrega(id, solicitud as string),
      () => {
        setDialogo(null);
        if (accion === 'baja') {
          datos.seleccionar(null);
          requestAnimationFrame(() => botonNuevo.current?.focus());
        }
      },
    );
  }
  function cerrarDialogo() {
    if (!candado.current) setDialogo(null);
  }
  return (
    <PaginaModulo modulo={46} className="monitoreo-entregas-page">
      <p>Seguimiento manual · Constancia de recepción textual.</p>
      {datos.resumen.error ? (
        <p role="alert">
          {datos.resumen.error}
          <button type="button" onClick={datos.refrescar}>
            Reintentar resumen
          </button>
        </p>
      ) : datos.resumen.datos ? (
        <EntregasResumen datos={datos.resumen.datos} />
      ) : (
        <p role="status">Cargando resumen…</p>
      )}
      <div className="monitoreo-entregas-toolbar">
        <h2>Despachos y recepción</h2>
        <button
          ref={botonNuevo}
          className="primary-button"
          type="button"
          disabled={ocupado}
          onClick={() => abrir(null)}
        >
          Nueva entrega
        </button>
      </div>
      <EntregasFiltros
        busqueda={datos.busqueda}
        estado={datos.estado}
        cambiarBusqueda={datos.setBusqueda}
        cambiarEstado={datos.cambiarEstado}
      />
      <div className="monitoreo-entregas-layout">
        <section className="monitoreo-entregas-card" aria-label="Listado de entregas">
          {datos.lista.cargando && <p role="status">Cargando entregas…</p>}
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
              <p>{datos.lista.datos.total} entregas</p>
              {datos.lista.datos.items.length ? (
                <EntregasTabla
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
                <p role="status">Sin entregas coincidentes</p>
              )}
            </>
          )}
        </section>
        <div>
          {error && !formulario && !dialogo && (
            <p role="alert" className="field-error">
              {error}
            </p>
          )}
          {formulario ? (
            <EntregaFormulario
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
            <EntregaDetallePanel
              datos={datos.detalle.datos}
              ocupado={ocupado}
              editar={() => abrir(datos.detalle.datos)}
              eliminar={() => mostrar('baja')}
              iniciar={() => mostrar('salida')}
              pod={() => mostrar('pod')}
              cancelar={() => mostrar('cancelacion')}
            />
          ) : (
            <p role="status">Selecciona una entrega para ver el detalle.</p>
          )}
        </div>
      </div>
      {dialogo === 'baja' && datos.detalle.datos && (
        <DialogoBaja
          entidad="entrega"
          nombre={datos.detalle.datos.codigo}
          cerrar={cerrarBaja}
          confirmar={() => operar('baja')}
          eliminando={ocupado}
          error={error}
        />
      )}
      {dialogo === 'salida' && (
        <DialogoSalidaEntrega
          ocupado={ocupado}
          error={error}
          cerrar={cerrarDialogo}
          confirmar={() => operar('salida')}
        />
      )}
      {dialogo === 'pod' && (
        <DialogoPodEntrega
          ocupado={ocupado}
          error={error}
          cerrar={cerrarDialogo}
          confirmar={(d) => operar('pod', d)}
        />
      )}
      {dialogo === 'cancelacion' && (
        <DialogoCancelarEntrega
          ocupado={ocupado}
          error={error}
          cerrar={cerrarDialogo}
          confirmar={(m) => operar('cancelacion', m)}
        />
      )}
    </PaginaModulo>
  );
}
