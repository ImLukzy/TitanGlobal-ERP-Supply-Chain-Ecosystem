import { useCallback, useRef, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { actualizarGuia, anularGuia, emitirGuia, crearGuia, eliminarGuia } from './api';
import { GuiasFiltros } from './GuiasFiltros';
import { GuiasResumen } from './GuiasResumen';
import { GuiasTabla } from './GuiasTabla';
import { GuiaDetallePanel } from './GuiaDetallePanel';
import { GuiaFormulario } from './GuiaFormulario';
import { DialogoAnularGuia } from './DialogoAnularGuia';
import { DialogoEmitirGuia } from './DialogoEmitirGuia';
import { numeroGuia, mensajeError } from './presentacion';
import type { GuiaDetalle, GuardarGuia } from './tipos';
import { useGuiasData } from './useGuiasData';

export function GuiasRemisionPage() {
  const datos = useGuiasData();
  const [formulario, setFormulario] = useState(false);
  const [inicial, setInicial] = useState<GuiaDetalle | null>(null);
  const [baja, setBaja] = useState(false);
  const [anulacion, setAnulacion] = useState(false);
  const [emision, setEmision] = useState(false);
  const [ocupado, setOcupado] = useState(false);
  const [error, setError] = useState('');
  const candado = useRef(false);
  const origenBaja = useRef<HTMLElement | null>(null);
  const botonNuevo = useRef<HTMLButtonElement>(null);
  function abrir(ruta: GuiaDetalle | null) {
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
  async function guardar(solicitud: GuardarGuia) {
    if (candado.current) return;
    candado.current = true;
    setOcupado(true);
    setError('');
    try {
      const respuesta = inicial
        ? await actualizarGuia(inicial.id, solicitud)
        : await crearGuia(solicitud);
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
  async function operar(accion: 'eliminar' | 'emitir' | 'anular', motivo = '') {
    if (!datos.detalle.datos || candado.current) return;
    candado.current = true;
    setOcupado(true);
    setError('');
    try {
      const id = datos.detalle.datos.id;
      if (accion === 'eliminar') {
        await eliminarGuia(id);
        datos.seleccionar(null);
        setBaja(false);
        requestAnimationFrame(() => botonNuevo.current?.focus());
      } else if (accion === 'emitir') {
        await emitirGuia(id);
        setEmision(false);
      } else {
        await anularGuia(id, motivo);
        setAnulacion(false);
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
    <PaginaModulo modulo={45} className="guias-remision-page">
      <p>
        Documento interno de transporte: registra mercancías y conserva el historial de tus guías.
      </p>
      {datos.resumen.error ? (
        <p role="alert">
          {datos.resumen.error}
          <button type="button" onClick={datos.refrescar}>
            Reintentar resumen
          </button>
        </p>
      ) : datos.resumen.datos ? (
        <GuiasResumen datos={datos.resumen.datos} />
      ) : (
        <p role="status">Cargando resumen…</p>
      )}
      <div className="guias-remision-toolbar">
        <h2>Documentos de transporte</h2>
        <button
          ref={botonNuevo}
          className="primary-button"
          type="button"
          disabled={ocupado}
          onClick={() => abrir(null)}
        >
          Nueva guía
        </button>
      </div>
      <GuiasFiltros
        busqueda={datos.busqueda}
        estado={datos.estado}
        cambiarBusqueda={datos.setBusqueda}
        cambiarEstado={datos.cambiarEstado}
      />
      <div className="guias-remision-layout">
        <section className="guias-remision-card" aria-label="Listado de guías">
          {datos.lista.cargando && <p role="status">Cargando guías…</p>}
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
              <p>{datos.lista.datos.total} guías</p>
              {datos.lista.datos.items.length ? (
                <GuiasTabla
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
                <p role="status">Sin guías coincidentes</p>
              )}
            </>
          )}
        </section>
        <div>
          {error && !formulario && !baja && !anulacion && !emision && (
            <p role="alert" className="field-error">
              {error}
            </p>
          )}
          {formulario ? (
            <GuiaFormulario
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
            <GuiaDetallePanel
              datos={datos.detalle.datos}
              ocupado={ocupado}
              editar={() => abrir(datos.detalle.datos)}
              emitir={() => {
                setEmision(true);
                setError('');
              }}
              eliminar={() => {
                origenBaja.current = document.activeElement as HTMLElement;
                setBaja(true);
                setError('');
              }}
              anular={() => {
                setAnulacion(true);
                setError('');
              }}
            />
          ) : (
            <p role="status">Selecciona una guía de remisión para ver el detalle.</p>
          )}
        </div>
      </div>
      {baja && datos.detalle.datos && (
        <DialogoBaja
          entidad="guía"
          nombre={numeroGuia(datos.detalle.datos.serie, datos.detalle.datos.numero)}
          cerrar={cerrarBaja}
          confirmar={() => operar('eliminar')}
          eliminando={ocupado}
          error={error}
        />
      )}
      {emision && datos.detalle.datos && (
        <DialogoEmitirGuia
          ocupado={ocupado}
          error={error}
          cerrar={() => {
            if (!candado.current) setEmision(false);
          }}
          confirmar={() => operar('emitir')}
        />
      )}
      {anulacion && datos.detalle.datos && (
        <DialogoAnularGuia
          referencia={numeroGuia(datos.detalle.datos.serie, datos.detalle.datos.numero)}
          ocupado={ocupado}
          error={error}
          cerrar={() => {
            if (!candado.current) setAnulacion(false);
          }}
          confirmar={(motivo) => operar('anular', motivo)}
        />
      )}
    </PaginaModulo>
  );
}
