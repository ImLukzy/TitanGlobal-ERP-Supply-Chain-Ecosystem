import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import {
  actualizarLicitacion,
  adjudicarLicitacion,
  cancelarLicitacion,
  cerrarLicitacion,
  crearLicitacion,
  crearOferta,
  eliminarLicitacion,
  mejorarOferta,
} from './api';
import { DialogoAdjudicarLicitacion, DialogoConfirmarLicitacion, DialogoMejorarOferta } from './LicitacionesDialogos';
import { LicitacionDetallePanel } from './LicitacionDetallePanel';
import { LicitacionFormulario } from './LicitacionFormulario';
import { LicitacionesFiltros } from './LicitacionesFiltros';
import { LicitacionesResumen } from './LicitacionesResumen';
import { LicitacionesTabla } from './LicitacionesTabla';
import { OfertaFormulario } from './OfertaFormulario';
import { useLicitacionesData } from './useLicitacionesData';
import type { CrearLicitacionRequest, CrearOfertaRequest, FiltrosLicitacion, LicitacionListado, OfertaLicitacion } from './tipos';

const filtrosIniciales: FiltrosLicitacion = { pagina: 1, tamanoPagina: 10 };

type Dialogo = { tipo: 'cerrar' | 'cancelar' | 'adjudicar' | 'baja' } | { tipo: 'mejorar'; oferta: OfertaLicitacion };

export function LicitacionesPage() {
  const [filtros, setFiltros] = useState<FiltrosLicitacion>(filtrosIniciales);
  const [busqueda, setBusqueda] = useState('');
  const [seleccionadaId, setSeleccionadaId] = useState<number | null>(null);
  const [formulario, setFormulario] = useState<'crear' | 'editar' | 'oferta' | null>(null);
  const [dialogo, setDialogo] = useState<Dialogo | null>(null);
  const [errorBaja, setErrorBaja] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const botonOrigenRef = useRef<HTMLButtonElement | null>(null);
  const primeraBusquedaRef = useRef(true);
  const { lista, resumen, proveedores, errorLista, errorResumen, errorProveedores, licitacion, errorDetalle, cargandoDetalle, recargar } =
    useLicitacionesData(filtros, seleccionadaId);

  useEffect(() => {
    if (primeraBusquedaRef.current) {
      primeraBusquedaRef.current = false;
      return undefined;
    }
    const temporizador = window.setTimeout(() => {
      setFiltros((actuales) => ({ ...actuales, busqueda: busqueda.trim(), pagina: 1 }));
      setSeleccionadaId(null);
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarFiltros(cambios: Partial<FiltrosLicitacion>): void {
    setFiltros((actuales) => ({ ...actuales, ...cambios, pagina: 1 }));
    setSeleccionadaId(null);
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actuales) => ({ ...actuales, pagina }));
  }

  function seleccionar(item: LicitacionListado): void {
    setSeleccionadaId(item.id);
    setFormulario(null);
  }

  function abrirDialogo(siguiente: Dialogo, boton: HTMLButtonElement): void {
    botonOrigenRef.current = boton;
    setErrorBaja('');
    setDialogo(siguiente);
  }

  function cerrarDialogo(): void {
    setDialogo(null);
    setErrorBaja('');
    window.requestAnimationFrame(() => botonOrigenRef.current?.focus());
  }

  async function guardarLicitacion(datos: CrearLicitacionRequest): Promise<void> {
    let id: number;
    if (formulario === 'editar' && licitacion !== null) {
      const cambios = { titulo: datos.titulo, presupuestoReferencial: datos.presupuestoReferencial, fechaCierre: datos.fechaCierre };
      id = (await actualizarLicitacion(licitacion.id, datos.descripcion === undefined ? cambios : { ...cambios, descripcion: datos.descripcion })).id;
    } else {
      id = (await crearLicitacion(datos)).id;
    }
    setFormulario(null);
    setSeleccionadaId(id);
    recargar();
  }

  async function guardarOferta(datos: CrearOfertaRequest): Promise<void> {
    if (licitacion === null) {
      return;
    }
    await crearOferta(licitacion.id, datos);
    setFormulario(null);
    recargar();
  }

  async function accionDialogo(accion: () => Promise<unknown>): Promise<void> {
    await accion();
    setDialogo(null);
    recargar();
  }

  async function confirmarBaja(): Promise<void> {
    if (licitacion === null) {
      return;
    }
    setErrorBaja('');
    setEliminando(true);
    try {
      await eliminarLicitacion(licitacion.id);
      setDialogo(null);
      setSeleccionadaId(null);
      recargar();
    } catch (error: unknown) {
      setErrorBaja(error instanceof ApiError ? error.message : 'No se pudo eliminar la licitación.');
    } finally {
      setEliminando(false);
    }
  }

  return (
    <PaginaModulo className="licitaciones-page" modulo="14-licitaciones">
      <p className="module-intro">Publica licitaciones, compara las ofertas de los proveedores y adjudica la mejor.</p>
      <LicitacionesResumen datos={resumen} error={errorResumen} />
      {errorProveedores && <p className="licitaciones-error" role="alert">{errorProveedores}</p>}
      <div className="licitaciones-work-area">
        <section aria-labelledby="licitaciones-list-title" className="licitaciones-panel licitaciones-list-panel">
          <div className="licitaciones-panel-heading">
            <div>
              <span className="page-eyebrow">COMPRAS</span>
              <h2 id="licitaciones-list-title">Licitaciones</h2>
            </div>
            <button className="primary-button" onClick={() => setFormulario('crear')} type="button">Nueva licitación</button>
          </div>
          <LicitacionesFiltros busqueda={busqueda} cambiarBusqueda={setBusqueda} cambiarFiltros={cambiarFiltros} filtros={filtros} />
          <LicitacionesTabla
            cambiarPagina={cambiarPagina}
            error={errorLista}
            lista={lista}
            pagina={filtros.pagina ?? 1}
            seleccionadaId={seleccionadaId}
            seleccionar={seleccionar}
          />
        </section>
        <LicitacionDetallePanel
          acciones={{
            editar: () => setFormulario('editar'),
            ofertar: (boton) => { botonOrigenRef.current = boton; setFormulario('oferta'); },
            mejorar: (oferta, boton) => abrirDialogo({ tipo: 'mejorar', oferta }, boton),
            cerrar: (boton) => abrirDialogo({ tipo: 'cerrar' }, boton),
            adjudicar: (boton) => abrirDialogo({ tipo: 'adjudicar' }, boton),
            cancelar: (boton) => abrirDialogo({ tipo: 'cancelar' }, boton),
            eliminar: (boton) => abrirDialogo({ tipo: 'baja' }, boton),
          }}
          cargando={cargandoDetalle}
          licitacion={licitacion}
        />
      </div>
      {errorDetalle && <p className="licitaciones-error" role="alert">{errorDetalle}</p>}
      {(formulario === 'crear' || (formulario === 'editar' && licitacion !== null)) && (
        <LicitacionFormulario
          cancelar={() => setFormulario(null)}
          guardar={guardarLicitacion}
          key={formulario === 'editar' ? `editar-${licitacion?.id ?? 0}` : 'crear-licitacion'}
          licitacion={formulario === 'editar' ? licitacion : null}
        />
      )}
      {formulario === 'oferta' && licitacion !== null && (
        <OfertaFormulario
          cancelar={() => setFormulario(null)}
          guardar={guardarOferta}
          key={`oferta-${licitacion.id}`}
          licitacion={licitacion}
          proveedores={proveedores}
        />
      )}
      {licitacion !== null && dialogo !== null && (dialogo.tipo === 'cerrar' || dialogo.tipo === 'cancelar') && (
        <DialogoConfirmarLicitacion
          cerrar={cerrarDialogo}
          confirmar={() => accionDialogo(() => (dialogo.tipo === 'cerrar' ? cerrarLicitacion(licitacion.id) : cancelarLicitacion(licitacion.id)))}
          licitacion={licitacion}
          tipo={dialogo.tipo}
        />
      )}
      {licitacion !== null && dialogo?.tipo === 'adjudicar' && (
        <DialogoAdjudicarLicitacion
          cerrar={cerrarDialogo}
          confirmar={(ofertaId) => accionDialogo(() => adjudicarLicitacion(licitacion.id, { ofertaId }))}
          licitacion={licitacion}
        />
      )}
      {licitacion !== null && dialogo?.tipo === 'mejorar' && (
        <DialogoMejorarOferta
          cerrar={cerrarDialogo}
          confirmar={(monto) => accionDialogo(() => mejorarOferta(licitacion.id, dialogo.oferta.id, { monto }))}
          oferta={dialogo.oferta}
        />
      )}
      {licitacion !== null && dialogo?.tipo === 'baja' && (
        <DialogoBaja
          cerrar={cerrarDialogo}
          confirmar={() => void confirmarBaja()}
          eliminando={eliminando}
          entidad="licitación"
          error={errorBaja}
          nombre={`${licitacion.numero} · ${licitacion.titulo}`}
        />
      )}
    </PaginaModulo>
  );
}
