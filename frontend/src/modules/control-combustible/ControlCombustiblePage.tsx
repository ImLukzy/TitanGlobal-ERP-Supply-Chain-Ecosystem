import { useCallback, useRef, useState } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { DialogoBaja } from '../../components/DialogoBaja';
import { crearVale, actualizarVale, eliminarVale, confirmarVale, anularVale } from './api';
import { useValesData } from './useValesData';
import { ValesResumen } from './ValesResumen';
import { ValesFiltros } from './ValesFiltros';
import { ValesTabla } from './ValesTabla';
import { ValeDetallePanel } from './ValeDetallePanel';
import { ValeFormulario } from './ValeFormulario';
import { DialogoConfirmarVale } from './DialogoConfirmarVale';
import { DialogoAnularVale } from './DialogoAnularVale';
import { mensajeError } from './presentacion';
import type { ValeDetalle, GuardarVale } from './tipos';
export function ControlCombustiblePage() {
  const datos = useValesData();
  const [formulario, setFormulario] = useState(false),
    [inicial, setInicial] = useState<ValeDetalle | null>(null),
    [dialogo, setDialogo] = useState<'baja' | 'confirmacion' | 'anulacion' | null>(null),
    [ocupado, setOcupado] = useState(false),
    [error, setError] = useState('');
  const candado = useRef(false),
    origen = useRef<HTMLElement | null>(null),
    nuevo = useRef<HTMLButtonElement>(null);
  const cerrarBaja = useCallback(() => {
    if (!candado.current) {
      setDialogo(null);
      requestAnimationFrame(() => origen.current?.focus());
    }
  }, []);
  function abrir(v: ValeDetalle | null) {
    setInicial(v);
    setFormulario(true);
    setError('');
  }
  function mostrar(d: 'baja' | 'confirmacion' | 'anulacion') {
    origen.current = document.activeElement as HTMLElement;
    setDialogo(d);
    setError('');
  }
  function cerrar() {
    if (!candado.current) setDialogo(null);
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
  async function guardar(d: GuardarVale) {
    await mutar(
      async () => {
        const r = inicial ? await actualizarVale(inicial.id, d) : await crearVale(d);
        datos.seleccionar(r.id);
      },
      () => setFormulario(false),
    );
  }
  async function operar(accion: 'baja' | 'confirmacion' | 'anulacion', motivo = '') {
    if (!datos.detalle.datos) return;
    const id = datos.detalle.datos.id;
    await mutar(
      () =>
        accion === 'baja'
          ? eliminarVale(id)
          : accion === 'confirmacion'
            ? confirmarVale(id)
            : anularVale(id, motivo),
      () => {
        setDialogo(null);
        if (accion === 'baja') {
          datos.seleccionar(null);
          requestAnimationFrame(() => nuevo.current?.focus());
        }
      },
    );
  }
  return (
    <PaginaModulo modulo={47} className="control-combustible-page">
      <p>Vales de combustible y rendimiento orientativo del intervalo declarado.</p>
      {datos.resumen.error ? (
        <p role="alert">
          {datos.resumen.error}
          <button type="button" onClick={datos.refrescar}>
            Reintentar resumen
          </button>
        </p>
      ) : datos.resumen.datos ? (
        <ValesResumen datos={datos.resumen.datos} />
      ) : (
        <p role="status">Cargando resumen…</p>
      )}
      <div className="control-combustible-toolbar">
        <h2>Vales e intervalos de consumo</h2>
        <button
          ref={nuevo}
          className="primary-button"
          type="button"
          disabled={ocupado}
          onClick={() => abrir(null)}
        >
          Nuevo vale
        </button>
      </div>
      <ValesFiltros
        busqueda={datos.busqueda}
        estado={datos.estado}
        tipoCombustible={datos.tipoCombustible}
        cambiarBusqueda={datos.setBusqueda}
        cambiarEstado={datos.cambiarEstado}
        cambiarTipo={datos.cambiarTipo}
      />
      <div className="control-combustible-layout">
        <section className="control-combustible-card" aria-label="Listado de vales">
          {datos.lista.cargando && <p role="status">Cargando vales…</p>}
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
              <p>{datos.lista.datos.total} vales</p>
              {datos.lista.datos.items.length ? (
                <ValesTabla
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
                <p role="status">Sin vales coincidentes</p>
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
            <ValeFormulario
              key={inicial?.id ?? 'nuevo'}
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
            <ValeDetallePanel
              datos={datos.detalle.datos}
              ocupado={ocupado}
              editar={() => abrir(datos.detalle.datos)}
              eliminar={() => mostrar('baja')}
              confirmar={() => mostrar('confirmacion')}
              anular={() => mostrar('anulacion')}
            />
          ) : (
            <p role="status">Selecciona un vale para ver el detalle.</p>
          )}
        </div>
      </div>
      {dialogo === 'baja' && datos.detalle.datos && (
        <DialogoBaja
          entidad="vale"
          nombre={datos.detalle.datos.codigo}
          cerrar={cerrarBaja}
          confirmar={() => operar('baja')}
          eliminando={ocupado}
          error={error}
        />
      )}
      {dialogo === 'confirmacion' && (
        <DialogoConfirmarVale
          ocupado={ocupado}
          error={error}
          cerrar={cerrar}
          confirmar={() => operar('confirmacion')}
        />
      )}
      {dialogo === 'anulacion' && (
        <DialogoAnularVale
          ocupado={ocupado}
          error={error}
          cerrar={cerrar}
          confirmar={(m) => operar('anulacion', m)}
        />
      )}
    </PaginaModulo>
  );
}
