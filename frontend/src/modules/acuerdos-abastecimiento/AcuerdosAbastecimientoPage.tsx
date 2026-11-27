import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import {
  actualizarAcuerdo,
  cancelarAcuerdo,
  crearAcuerdo,
  crearLineaAcuerdo,
  eliminarAcuerdo,
  eliminarLineaAcuerdo,
  reactivarAcuerdo,
  registrarConsumo,
  suspenderAcuerdo,
} from './api';
import { AcuerdoDetallePanel } from './AcuerdoDetallePanel';
import { AcuerdoFormulario } from './AcuerdoFormulario';
import { AcuerdosFiltros } from './AcuerdosFiltros';
import { AcuerdosResumen } from './AcuerdosResumen';
import { AcuerdosTabla } from './AcuerdosTabla';
import { DialogoConfirmarAcuerdo, DialogoConsumo, type TipoConfirmacion } from './AcuerdosDialogos';
import { LineaFormulario } from './LineaFormulario';
import { useAcuerdosData } from './useAcuerdosData';
import type { AcuerdoListado, CrearAcuerdoRequest, CrearLineaRequest, FiltrosAcuerdo, LineaAcuerdo } from './tipos';

const filtrosIniciales: FiltrosAcuerdo = { pagina: 1, tamanoPagina: 10 };

type Dialogo =
  | { tipo: TipoConfirmacion; linea: LineaAcuerdo | null }
  | { tipo: 'consumo'; linea: LineaAcuerdo }
  | { tipo: 'baja' };

export function AcuerdosAbastecimientoPage() {
  const [filtros, setFiltros] = useState<FiltrosAcuerdo>(filtrosIniciales);
  const [busqueda, setBusqueda] = useState('');
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [formulario, setFormulario] = useState<'crear' | 'editar' | 'linea' | null>(null);
  const [dialogo, setDialogo] = useState<Dialogo | null>(null);
  const [errorBaja, setErrorBaja] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const botonOrigenRef = useRef<HTMLButtonElement | null>(null);
  const primeraBusquedaRef = useRef(true);
  const { lista, resumen, proveedores, productos, errorLista, errorResumen, errorOpciones, acuerdo, errorDetalle, cargandoDetalle, recargar } =
    useAcuerdosData(filtros, seleccionadoId);

  useEffect(() => {
    if (primeraBusquedaRef.current) {
      primeraBusquedaRef.current = false;
      return undefined;
    }
    const temporizador = window.setTimeout(() => {
      setFiltros((actuales) => ({ ...actuales, busqueda: busqueda.trim(), pagina: 1 }));
      setSeleccionadoId(null);
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarFiltros(cambios: Partial<FiltrosAcuerdo>): void {
    setFiltros((actuales) => ({ ...actuales, ...cambios, pagina: 1 }));
    setSeleccionadoId(null);
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actuales) => ({ ...actuales, pagina }));
  }

  function seleccionar(item: AcuerdoListado): void {
    setSeleccionadoId(item.id);
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

  async function guardarAcuerdo(datos: CrearAcuerdoRequest): Promise<void> {
    let id: number;
    if (formulario === 'editar' && acuerdo !== null) {
      const cambios = { fechaInicio: datos.fechaInicio, fechaFin: datos.fechaFin, montoMaximo: datos.montoMaximo };
      id = (await actualizarAcuerdo(acuerdo.id, datos.descripcion === undefined ? cambios : { ...cambios, descripcion: datos.descripcion })).id;
    } else {
      id = (await crearAcuerdo(datos)).id;
    }
    setFormulario(null);
    setSeleccionadoId(id);
    recargar();
  }

  async function guardarLinea(datos: CrearLineaRequest): Promise<void> {
    if (acuerdo === null) {
      return;
    }
    await crearLineaAcuerdo(acuerdo.id, datos);
    setFormulario(null);
    recargar();
  }

  async function accionDialogo(accion: () => Promise<unknown>): Promise<void> {
    await accion();
    setDialogo(null);
    recargar();
  }

  async function confirmarBaja(): Promise<void> {
    if (acuerdo === null) {
      return;
    }
    setErrorBaja('');
    setEliminando(true);
    try {
      await eliminarAcuerdo(acuerdo.id);
      setDialogo(null);
      setSeleccionadoId(null);
      recargar();
    } catch (error: unknown) {
      setErrorBaja(error instanceof ApiError ? error.message : 'No se pudo eliminar el acuerdo.');
    } finally {
      setEliminando(false);
    }
  }

  function ejecutarConfirmacion(tipo: TipoConfirmacion, linea: LineaAcuerdo | null): Promise<unknown> {
    if (acuerdo === null) {
      return Promise.resolve();
    }
    if (tipo === 'suspender') {
      return suspenderAcuerdo(acuerdo.id);
    }
    if (tipo === 'reactivar') {
      return reactivarAcuerdo(acuerdo.id);
    }
    if (tipo === 'cancelar') {
      return cancelarAcuerdo(acuerdo.id);
    }
    return linea === null ? Promise.resolve() : eliminarLineaAcuerdo(acuerdo.id, linea.id);
  }

  return (
    <PaginaModulo className="acuerdos-page" modulo="20-acuerdos-abastecimiento">
      <p className="module-intro">Gestiona acuerdos marco con proveedores: líneas, consumo y estado.</p>
      <AcuerdosResumen datos={resumen} error={errorResumen} />
      {errorOpciones && <p className="acuerdos-error" role="alert">{errorOpciones}</p>}
      <div className="acuerdos-work-area">
        <section aria-labelledby="acuerdos-list-title" className="acuerdos-panel acuerdos-list-panel">
          <div className="acuerdos-panel-heading">
            <div>
              <span className="page-eyebrow">COMPRAS</span>
              <h2 id="acuerdos-list-title">Acuerdos de abastecimiento</h2>
            </div>
            <button className="primary-button" onClick={() => setFormulario('crear')} type="button">Nuevo acuerdo</button>
          </div>
          <AcuerdosFiltros busqueda={busqueda} cambiarBusqueda={setBusqueda} cambiarFiltros={cambiarFiltros} filtros={filtros} proveedores={proveedores} />
          <AcuerdosTabla
            cambiarPagina={cambiarPagina}
            error={errorLista}
            lista={lista}
            pagina={filtros.pagina ?? 1}
            seleccionadoId={seleccionadoId}
            seleccionar={seleccionar}
          />
        </section>
        <AcuerdoDetallePanel
          acciones={{
            editar: () => setFormulario('editar'),
            agregarLinea: () => setFormulario('linea'),
            consumir: (linea, boton) => abrirDialogo({ tipo: 'consumo', linea }, boton),
            quitarLinea: (linea, boton) => abrirDialogo({ tipo: 'quitar', linea }, boton),
            suspender: (boton) => abrirDialogo({ tipo: 'suspender', linea: null }, boton),
            reactivar: (boton) => abrirDialogo({ tipo: 'reactivar', linea: null }, boton),
            cancelar: (boton) => abrirDialogo({ tipo: 'cancelar', linea: null }, boton),
            eliminar: (boton) => abrirDialogo({ tipo: 'baja' }, boton),
          }}
          acuerdo={acuerdo}
          cargando={cargandoDetalle}
        />
      </div>
      {errorDetalle && <p className="acuerdos-error" role="alert">{errorDetalle}</p>}
      {(formulario === 'crear' || (formulario === 'editar' && acuerdo !== null)) && (
        <AcuerdoFormulario
          acuerdo={formulario === 'editar' ? acuerdo : null}
          cancelar={() => setFormulario(null)}
          guardar={guardarAcuerdo}
          key={formulario === 'editar' ? `editar-${acuerdo?.id ?? 0}` : 'crear-acuerdo'}
          proveedores={proveedores}
        />
      )}
      {formulario === 'linea' && acuerdo !== null && (
        <LineaFormulario
          acuerdo={acuerdo}
          cancelar={() => setFormulario(null)}
          guardar={guardarLinea}
          key={`linea-${acuerdo.id}`}
          productos={productos}
        />
      )}
      {acuerdo !== null && dialogo !== null && dialogo.tipo !== 'consumo' && dialogo.tipo !== 'baja' && (
        <DialogoConfirmarAcuerdo
          acuerdo={acuerdo}
          cerrar={cerrarDialogo}
          confirmar={() => accionDialogo(() => ejecutarConfirmacion(dialogo.tipo, dialogo.linea))}
          linea={dialogo.linea}
          tipo={dialogo.tipo}
        />
      )}
      {acuerdo !== null && dialogo?.tipo === 'consumo' && (
        <DialogoConsumo
          cerrar={cerrarDialogo}
          confirmar={(cantidad) => accionDialogo(() => registrarConsumo(acuerdo.id, dialogo.linea.id, { cantidad }))}
          linea={dialogo.linea}
        />
      )}
      {acuerdo !== null && dialogo?.tipo === 'baja' && (
        <DialogoBaja
          cerrar={cerrarDialogo}
          confirmar={() => void confirmarBaja()}
          eliminando={eliminando}
          entidad="acuerdo"
          error={errorBaja}
          nombre={`${acuerdo.numero} · ${acuerdo.proveedorNombre}`}
        />
      )}
    </PaginaModulo>
  );
}
