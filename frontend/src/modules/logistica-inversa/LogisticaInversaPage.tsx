import { useCallback, useRef, useState } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { DialogoBaja } from '../../components/DialogoBaja';
import * as api from './api';
import { useRetornosData } from './useRetornosData';
import { RetornosResumen } from './RetornosResumen';
import { RetornosFiltros } from './RetornosFiltros';
import { RetornosTabla } from './RetornosTabla';
import { RetornoDetallePanel } from './RetornoDetallePanel';
import { RetornoFormulario } from './RetornoFormulario';
import { DialogoCantidadRetorno } from './DialogoCantidadRetorno';
import { DialogoCancelarRetorno } from './DialogoCancelarRetorno';
import { mensajeError } from './presentacion';
import type { RetornoDetalle, GuardarRetorno, RegistrarCantidad } from './tipos';
type Dialogo = 'baja' | 'recogida' | 'recepcion' | 'cancelacion' | null;
export function LogisticaInversaPage() {
  const datos = useRetornosData();
  const [formulario, setFormulario] = useState(false),
    [inicial, setInicial] = useState<RetornoDetalle | null>(null),
    [dialogo, setDialogo] = useState<Dialogo>(null),
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
  function cerrar() {
    if (!candado.current) setDialogo(null);
  }
  function mostrar(d: Dialogo) {
    origen.current = document.activeElement as HTMLElement;
    setDialogo(d);
    setError('');
  }
  function abrir(r: RetornoDetalle | null) {
    setInicial(r);
    setFormulario(true);
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
  async function guardar(d: GuardarRetorno) {
    await mutar(
      async () => {
        const r = inicial ? await api.actualizarRetorno(inicial.id, d) : await api.crearRetorno(d);
        datos.seleccionar(r.id);
      },
      () => setFormulario(false),
    );
  }
  function cantidad(d: RegistrarCantidad) {
    if (!datos.detalle.datos) return;
    const id = datos.detalle.datos.id;
    void mutar(
      () => (dialogo === 'recogida' ? api.recogerRetorno(id, d) : api.recibirRetorno(id, d)),
      () => setDialogo(null),
    );
  }
  function cancelar(motivo: string) {
    if (!datos.detalle.datos) return;
    const id = datos.detalle.datos.id;
    void mutar(
      () => api.cancelarRetorno(id, motivo),
      () => setDialogo(null),
    );
  }
  function eliminar() {
    if (!datos.detalle.datos) return;
    const id = datos.detalle.datos.id;
    void mutar(
      () => api.eliminarRetorno(id),
      () => {
        setDialogo(null);
        datos.seleccionar(null);
        requestAnimationFrame(() => nuevo.current?.focus());
      },
    );
  }
  return (
    <PaginaModulo modulo={49} className="logistica-inversa-page">
      <p>Retornables, pallets y devoluciones en ruta con control de cantidades por etapa.</p>
      {datos.resumen.error ? (
        <p role="alert">
          {datos.resumen.error}
          <button onClick={datos.refrescar}>Reintentar resumen</button>
        </p>
      ) : datos.resumen.datos ? (
        <RetornosResumen datos={datos.resumen.datos} />
      ) : (
        <p role="status">Cargando resumen…</p>
      )}
      <div className="logistica-inversa-toolbar">
        <h2>Retornos logísticos</h2>
        <button
          ref={nuevo}
          type="button"
          className="primary-button"
          disabled={ocupado}
          onClick={() => abrir(null)}
        >
          Nuevo retorno
        </button>
      </div>
      <RetornosFiltros
        busqueda={datos.busqueda}
        estado={datos.estado}
        tipo={datos.tipo}
        cambiarBusqueda={datos.setBusqueda}
        cambiarEstado={datos.cambiarEstado}
        cambiarTipo={datos.cambiarTipo}
      />
      <div className="logistica-inversa-layout">
        <section className="logistica-inversa-card" aria-label="Listado de retornos">
          {datos.lista.cargando && <p role="status">Cargando retornos…</p>}
          {datos.lista.error && (
            <p role="alert">
              {datos.lista.error}
              <button onClick={datos.refrescar}>Reintentar listado</button>
            </p>
          )}
          {datos.lista.datos && (
            <>
              <p>{datos.lista.datos.total} retornos</p>
              {datos.lista.datos.items.length ? (
                <RetornosTabla
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
                <p role="status">Sin retornos coincidentes</p>
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
            <RetornoFormulario
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
              <button onClick={datos.refrescar}>Reintentar detalle</button>
            </p>
          ) : datos.detalle.cargando ? (
            <p role="status">Cargando detalle…</p>
          ) : datos.detalle.datos ? (
            <RetornoDetallePanel
              datos={datos.detalle.datos}
              ocupado={ocupado}
              editar={() => abrir(datos.detalle.datos)}
              eliminar={() => mostrar('baja')}
              recoger={() => mostrar('recogida')}
              recibir={() => mostrar('recepcion')}
              cancelar={() => mostrar('cancelacion')}
            />
          ) : (
            <p role="status">Selecciona un retorno para ver el detalle.</p>
          )}
        </div>
      </div>
      {dialogo === 'baja' && datos.detalle.datos && (
        <DialogoBaja
          entidad="retorno"
          nombre={datos.detalle.datos.codigo}
          cerrar={cerrarBaja}
          confirmar={eliminar}
          eliminando={ocupado}
          error={error}
        />
      )}
      {(dialogo === 'recogida' || dialogo === 'recepcion') && datos.detalle.datos && (
        <DialogoCantidadRetorno
          etapa={dialogo}
          maximo={
            dialogo === 'recogida'
              ? datos.detalle.datos.cantidadProgramada
              : datos.detalle.datos.cantidadRecogida!
          }
          unidad={datos.detalle.datos.unidad}
          ocupado={ocupado}
          error={error}
          cerrar={cerrar}
          confirmar={cantidad}
        />
      )}
      {dialogo === 'cancelacion' && (
        <DialogoCancelarRetorno
          ocupado={ocupado}
          error={error}
          cerrar={cerrar}
          confirmar={cancelar}
        />
      )}
    </PaginaModulo>
  );
}
