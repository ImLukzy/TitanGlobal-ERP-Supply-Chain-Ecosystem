import { useCallback, useRef, useState } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { DialogoBaja } from '../../components/DialogoBaja';
import * as api from './api';
import { useSiniestrosData } from './useSiniestrosData';
import { SiniestrosResumen } from './SiniestrosResumen';
import { SiniestrosFiltros } from './SiniestrosFiltros';
import { SiniestrosTabla } from './SiniestrosTabla';
import { SiniestroDetallePanel } from './SiniestroDetallePanel';
import { SiniestroFormulario } from './SiniestroFormulario';
import { DialogoReclamo } from './DialogoReclamo';
import { DialogoCierre } from './DialogoCierre';
import { DialogoAnularSiniestro } from './DialogoAnularSiniestro';
import { mensajeError } from './presentacion';
import type { SiniestroDetalle, GuardarSiniestro, RegistrarReclamo, CerrarReclamo } from './tipos';
type Dialogo = 'baja' | 'reclamo' | 'cierre' | 'anulacion' | null;
export function SiniestrosFlotaPage() {
  const datos = useSiniestrosData();
  const [formulario, setFormulario] = useState(false),
    [inicial, setInicial] = useState<SiniestroDetalle | null>(null),
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
  function abrir(r: SiniestroDetalle | null) {
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
  async function guardar(d: GuardarSiniestro) {
    await mutar(
      async () => {
        const r = inicial
          ? await api.actualizarSiniestro(inicial.id, d)
          : await api.crearSiniestro(d);
        datos.seleccionar(r.id);
      },
      () => setFormulario(false),
    );
  }
  function reclamar(d: RegistrarReclamo) {
    if (!datos.detalle.datos) return;
    const id = datos.detalle.datos.id;
    void mutar(
      () => api.reclamarSiniestro(id, d),
      () => setDialogo(null),
    );
  }
  function cerrarReclamo(d: CerrarReclamo) {
    if (!datos.detalle.datos) return;
    const id = datos.detalle.datos.id;
    void mutar(
      () => api.cerrarSiniestro(id, d),
      () => setDialogo(null),
    );
  }
  function anular(motivo: string) {
    if (!datos.detalle.datos) return;
    const id = datos.detalle.datos.id;
    void mutar(
      () => api.anularSiniestro(id, motivo),
      () => setDialogo(null),
    );
  }
  function eliminar() {
    if (!datos.detalle.datos) return;
    const id = datos.detalle.datos.id;
    void mutar(
      () => api.eliminarSiniestro(id),
      () => {
        setDialogo(null);
        datos.seleccionar(null);
        requestAnimationFrame(() => nuevo.current?.focus());
      },
    );
  }
  return (
    <PaginaModulo modulo={50} className="siniestros-flota-page">
      <p>Incidentes de flota, referencias de póliza y seguimiento manual de reclamos.</p>
      {datos.resumen.error ? (
        <p role="alert">
          {datos.resumen.error}
          <button onClick={datos.refrescar}>Reintentar resumen</button>
        </p>
      ) : datos.resumen.datos ? (
        <SiniestrosResumen datos={datos.resumen.datos} />
      ) : (
        <p role="status">Cargando resumen…</p>
      )}
      <div className="siniestros-flota-toolbar">
        <h2>Siniestros de flota</h2>
        <button
          ref={nuevo}
          type="button"
          className="primary-button"
          disabled={ocupado}
          onClick={() => abrir(null)}
        >
          Nuevo siniestro
        </button>
      </div>
      <SiniestrosFiltros
        busqueda={datos.busqueda}
        estado={datos.estado}
        tipo={datos.tipo}
        cambiarBusqueda={datos.setBusqueda}
        cambiarEstado={datos.cambiarEstado}
        cambiarTipo={datos.cambiarTipo}
      />
      <div className="siniestros-flota-layout">
        <section className="siniestros-flota-card" aria-label="Listado de siniestros">
          {datos.lista.cargando && <p role="status">Cargando siniestros…</p>}
          {datos.lista.error && (
            <p role="alert">
              {datos.lista.error}
              <button onClick={datos.refrescar}>Reintentar listado</button>
            </p>
          )}
          {datos.lista.datos && (
            <>
              <p>{datos.lista.datos.total} siniestros</p>
              {datos.lista.datos.items.length ? (
                <SiniestrosTabla
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
                <p role="status">Sin siniestros coincidentes</p>
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
            <SiniestroFormulario
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
            <SiniestroDetallePanel
              datos={datos.detalle.datos}
              ocupado={ocupado}
              editar={() => abrir(datos.detalle.datos)}
              eliminar={() => mostrar('baja')}
              reclamar={() => mostrar('reclamo')}
              cerrar={() => mostrar('cierre')}
              anular={() => mostrar('anulacion')}
            />
          ) : (
            <p role="status">Selecciona un siniestro para ver el detalle.</p>
          )}
        </div>
      </div>
      {dialogo === 'baja' && datos.detalle.datos && (
        <DialogoBaja
          entidad="siniestro"
          nombre={datos.detalle.datos.codigo}
          cerrar={cerrarBaja}
          confirmar={eliminar}
          eliminando={ocupado}
          error={error}
        />
      )}
      {dialogo === 'reclamo' && (
        <DialogoReclamo ocupado={ocupado} error={error} cerrar={cerrar} confirmar={reclamar} />
      )}
      {dialogo === 'cierre' && datos.detalle.datos && (
        <DialogoCierre
          maximo={datos.detalle.datos.montoReclamado!}
          moneda={datos.detalle.datos.moneda!}
          ocupado={ocupado}
          error={error}
          cerrar={cerrar}
          confirmar={cerrarReclamo}
        />
      )}
      {dialogo === 'anulacion' && (
        <DialogoAnularSiniestro
          ocupado={ocupado}
          error={error}
          cerrar={cerrar}
          confirmar={anular}
        />
      )}
    </PaginaModulo>
  );
}
