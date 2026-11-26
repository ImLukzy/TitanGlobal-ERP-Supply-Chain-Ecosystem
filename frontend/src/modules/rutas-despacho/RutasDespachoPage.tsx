import { useCallback, useRef, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { actualizarRuta, crearRuta, eliminarRuta } from './api';
import { mensajeError } from './presentacion';
import { RutaDetallePanel } from './RutaDetallePanel';
import { RutaFormulario } from './RutaFormulario';
import { RutasFiltros } from './RutasFiltros';
import { RutasResumen } from './RutasResumen';
import { RutasTabla } from './RutasTabla';
import { useRutasDespachoData } from './useRutasDespachoData';
import type { GuardarRuta, RutaDetalle } from './tipos';

export function RutasDespachoPage() {
  const datos = useRutasDespachoData();
  const [formulario, setFormulario] = useState(false);
  const [inicial, setInicial] = useState<RutaDetalle | null>(null);
  const [baja, setBaja] = useState(false);
  const [ocupado, setOcupado] = useState(false);
  const [errorOperacion, setErrorOperacion] = useState('');
  const candado = useRef(false);
  const origenBaja = useRef<HTMLElement | null>(null);
  const botonNuevo = useRef<HTMLButtonElement>(null);
  const cerrarBaja = useCallback(() => {
    if (!candado.current) {
      setBaja(false);
      requestAnimationFrame(() => origenBaja.current?.focus());
    }
  }, []);
  function abrirFormulario(ruta: RutaDetalle | null) {
    setInicial(ruta);
    setFormulario(true);
    setErrorOperacion('');
  }
  async function guardar(solicitud: GuardarRuta) {
    if (candado.current) return;
    candado.current = true;
    setOcupado(true);
    setErrorOperacion('');
    try {
      const resultado = inicial
        ? await actualizarRuta(inicial.id, solicitud)
        : await crearRuta(solicitud);
      setFormulario(false);
      datos.seleccionar(resultado.id);
      datos.refrescar();
    } catch (error) {
      setErrorOperacion(mensajeError(error));
    } finally {
      candado.current = false;
      setOcupado(false);
    }
  }
  async function confirmarBaja() {
    if (!datos.detalle.datos || candado.current) return;
    candado.current = true;
    setOcupado(true);
    setErrorOperacion('');
    try {
      await eliminarRuta(datos.detalle.datos.id);
      setBaja(false);
      datos.seleccionar(null);
      datos.refrescar();
      requestAnimationFrame(() => botonNuevo.current?.focus());
    } catch (error) {
      setErrorOperacion(mensajeError(error));
    } finally {
      candado.current = false;
      setOcupado(false);
    }
  }
  return (
    <PaginaModulo modulo={43} className="rutas-despacho-page">
      <p>Consulta zonas, distancias y peajes estimados por recorrido.</p>
      {datos.resumen.error ? (
        <p role="alert">
          {datos.resumen.error}
          <button type="button" className="secondary-button" onClick={datos.refrescar}>
            Reintentar resumen
          </button>
        </p>
      ) : datos.resumen.datos ? (
        <RutasResumen datos={datos.resumen.datos} />
      ) : (
        <p role="status">Cargando resumen…</p>
      )}
      <div className="rutas-despacho-toolbar">
        <h2>Maestro de rutas</h2>
        <button
          ref={botonNuevo}
          className="primary-button"
          type="button"
          disabled={ocupado}
          onClick={() => abrirFormulario(null)}
        >
          Nueva ruta
        </button>
      </div>
      <RutasFiltros
        {...datos.filtros}
        zonas={datos.zonas.datos ?? []}
        cambiar={datos.cambiarFiltro}
      />
      {datos.zonas.error && (
        <p role="alert">
          {datos.zonas.error}
          <button className="secondary-button" type="button" onClick={datos.refrescar}>
            Reintentar zonas
          </button>
        </p>
      )}
      {datos.zonas.cargando && !datos.zonas.datos && <p role="status">Cargando zonas…</p>}
      <div className="rutas-despacho-layout">
        <section className="rutas-despacho-card" aria-label="Listado de rutas">
          {datos.listado.cargando && <p role="status">Cargando rutas…</p>}
          {datos.listado.error && (
            <p role="alert">
              {datos.listado.error}
              <button className="secondary-button" type="button" onClick={datos.refrescar}>
                Reintentar rutas
              </button>
            </p>
          )}
          {datos.listado.datos && (
            <>
              <p>{datos.listado.datos.total} rutas</p>
              {datos.listado.datos.items.length ? (
                <RutasTabla
                  datos={datos.listado.datos}
                  seleccion={datos.seleccion}
                  pagina={datos.pagina}
                  cambiar={datos.cambiarPagina}
                  seleccionar={(id) => {
                    if (!ocupado) {
                      datos.seleccionar(id);
                      setFormulario(false);
                      setErrorOperacion('');
                    }
                  }}
                />
              ) : (
                <p role="status">Sin rutas coincidentes</p>
              )}
            </>
          )}
        </section>
        <div>
          {formulario ? (
            <RutaFormulario
              key={inicial?.id ?? 'nueva'}
              inicial={inicial}
              ocupado={ocupado}
              error={errorOperacion}
              guardar={guardar}
              cerrar={() => setFormulario(false)}
            />
          ) : datos.detalle.error ? (
            <p role="alert">
              {datos.detalle.error}
              <button className="secondary-button" type="button" onClick={datos.refrescar}>
                Reintentar detalle
              </button>
            </p>
          ) : datos.detalle.cargando ? (
            <p role="status">Cargando detalle…</p>
          ) : datos.detalle.datos ? (
            <RutaDetallePanel
              datos={datos.detalle.datos}
              editar={() => abrirFormulario(datos.detalle.datos)}
              eliminar={() => {
                origenBaja.current = document.activeElement as HTMLElement;
                setBaja(true);
                setErrorOperacion('');
              }}
            />
          ) : (
            <p role="status">Selecciona una ruta para ver el detalle.</p>
          )}
        </div>
      </div>
      {baja && datos.detalle.datos && (
        <DialogoBaja
          entidad="ruta"
          nombre={datos.detalle.datos.nombre}
          cerrar={cerrarBaja}
          confirmar={confirmarBaja}
          eliminando={ocupado}
          error={errorOperacion}
        />
      )}
    </PaginaModulo>
  );
}
