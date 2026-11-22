import { DialogoEstadoSucursal } from './DialogoEstadoSucursal';
import { useCallback, useRef, useState } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { DialogoBaja } from '../../components/DialogoBaja';
import * as api from './api';
import { useSucursalesData } from './useSucursalesData';
import { SucursalesResumen } from './SucursalesResumen';
import { SucursalesFiltros } from './SucursalesFiltros';
import { SucursalesTabla } from './SucursalesTabla';
import { SucursalDetallePanel } from './SucursalDetallePanel';
import { SucursalFormulario } from './SucursalFormulario';
import { mensajeError } from './presentacion';
import type { SucursalDetalle, GuardarSucursal } from './tipos';
type Dialogo = 'baja' | 'estado' | null;
export function SucursalesPage() {
  const datos = useSucursalesData();
  const [formulario, setFormulario] = useState(false),
    [inicial, setInicial] = useState<SucursalDetalle | null>(null),
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
  function abrir(r: SucursalDetalle | null) {
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
  async function guardar(d: GuardarSucursal) {
    await mutar(
      async () => {
        const r = inicial
          ? await api.actualizarSucursal(inicial.id, d)
          : await api.crearSucursal(d);
        datos.seleccionar(r.id);
      },
      () => setFormulario(false),
    );
  }
  function cambiarEstado() {
    if (!datos.detalle.datos) return;
    const sucursal = datos.detalle.datos;
    void mutar(
      () =>
        sucursal.activa ? api.desactivarSucursal(sucursal.id) : api.activarSucursal(sucursal.id),
      () => setDialogo(null),
    );
  }
  function eliminar() {
    if (!datos.detalle.datos) return;
    const id = datos.detalle.datos.id;
    void mutar(
      () => api.eliminarSucursal(id),
      () => {
        setDialogo(null);
        datos.seleccionar(null);
        requestAnimationFrame(() => nuevo.current?.focus());
      },
    );
  }
  return (
    <PaginaModulo modulo={73} className="sucursales-page">
      <p>Maestro de filiales, plantas y oficinas con ubicación y contacto.</p>
      {datos.resumen.error ? (
        <p role="alert">
          {datos.resumen.error}
          <button onClick={datos.refrescar}>Reintentar resumen</button>
        </p>
      ) : datos.resumen.datos ? (
        <SucursalesResumen datos={datos.resumen.datos} />
      ) : (
        <p role="status">Cargando resumen…</p>
      )}
      <div className="sucursales-toolbar">
        <h2>Sucursales</h2>
        <button
          ref={nuevo}
          type="button"
          className="primary-button"
          disabled={ocupado}
          onClick={() => abrir(null)}
        >
          Nueva sucursal
        </button>
      </div>
      <SucursalesFiltros
        busqueda={datos.busqueda}
        estado={datos.estado}
        tipo={datos.tipo}
        pais={datos.pais}
        cambiarPais={datos.setPais}
        cambiarBusqueda={datos.setBusqueda}
        cambiarEstado={datos.cambiarEstado}
        cambiarTipo={datos.cambiarTipo}
      />
      <div className="sucursales-layout">
        <section className="sucursales-card" aria-label="Listado de sucursales">
          {datos.lista.cargando && <p role="status">Cargando sucursales…</p>}
          {datos.lista.error && (
            <p role="alert">
              {datos.lista.error}
              <button onClick={datos.refrescar}>Reintentar listado</button>
            </p>
          )}
          {datos.lista.datos && (
            <>
              <p>{datos.lista.datos.total} sucursales</p>
              {datos.lista.datos.items.length ? (
                <SucursalesTabla
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
                <p role="status">Sin sucursales coincidentes</p>
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
            <SucursalFormulario
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
            <SucursalDetallePanel
              datos={datos.detalle.datos}
              ocupado={ocupado}
              editar={() => abrir(datos.detalle.datos)}
              eliminar={() => mostrar('baja')}
              cambiarEstado={() => mostrar('estado')}
            />
          ) : (
            <p role="status">Selecciona una sucursal para ver el detalle.</p>
          )}
        </div>
      </div>
      {dialogo === 'baja' && datos.detalle.datos && (
        <DialogoBaja
          entidad="sucursal"
          nombre={datos.detalle.datos.codigo}
          cerrar={cerrarBaja}
          confirmar={eliminar}
          eliminando={ocupado}
          error={error}
        />
      )}
      {dialogo === 'estado' && datos.detalle.datos && (
        <DialogoEstadoSucursal
          activa={datos.detalle.datos.activa}
          ocupado={ocupado}
          error={error}
          cerrar={cerrar}
          confirmar={cambiarEstado}
        />
      )}
    </PaginaModulo>
  );
}
