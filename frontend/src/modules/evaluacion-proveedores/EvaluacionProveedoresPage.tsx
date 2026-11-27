import { useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { actualizarEvaluacion, crearEvaluacion, eliminarEvaluacion } from './api';
import { EvaluacionDetallePanel } from './EvaluacionDetallePanel';
import { EvaluacionFormulario } from './EvaluacionFormulario';
import { EvaluacionesFiltros } from './EvaluacionesFiltros';
import { EvaluacionesResumen } from './EvaluacionesResumen';
import { EvaluacionesTabla } from './EvaluacionesTabla';
import { useEvaluacionesData } from './useEvaluacionesData';
import type { ActualizarEvaluacionRequest, CrearEvaluacionRequest, EvaluacionListado, FiltrosEvaluacion } from './tipos';

const filtrosIniciales: FiltrosEvaluacion = { pagina: 1, tamanoPagina: 10 };

function obtenerMensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function EvaluacionProveedoresPage() {
  const [filtros, setFiltros] = useState<FiltrosEvaluacion>(filtrosIniciales);
  const [seleccionadaId, setSeleccionadaId] = useState<number | null>(null);
  const [formulario, setFormulario] = useState<'crear' | 'editar' | null>(null);
  const [baja, setBaja] = useState(false);
  const [errorBaja, setErrorBaja] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const botonBajaRef = useRef<HTMLButtonElement>(null);
  const { lista, resumen, proveedores, errorLista, errorResumen, errorProveedores, evaluacion, errorDetalle, cargandoDetalle, recargar } = useEvaluacionesData(filtros, seleccionadaId);

  function cambiarFiltros(cambios: Partial<FiltrosEvaluacion>): void {
    setFiltros((actuales) => ({ ...actuales, ...cambios, pagina: 1 }));
    setSeleccionadaId(null);
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actuales) => ({ ...actuales, pagina }));
  }

  function seleccionar(item: EvaluacionListado): void {
    setSeleccionadaId(item.id);
    setFormulario(null);
  }

  async function guardar(datos: CrearEvaluacionRequest): Promise<void> {
    let id: number;
    if (formulario === 'editar' && evaluacion !== null) {
      const puntajes: ActualizarEvaluacionRequest = {
        puntajeCumplimiento: datos.puntajeCumplimiento,
        puntajeCalidad: datos.puntajeCalidad,
        puntajePlazos: datos.puntajePlazos,
      };
      if (datos.observaciones !== undefined) {
        puntajes.observaciones = datos.observaciones;
      }
      id = (await actualizarEvaluacion(evaluacion.id, puntajes)).id;
    } else {
      id = (await crearEvaluacion(datos)).id;
    }
    setFormulario(null);
    setSeleccionadaId(id);
    recargar();
  }

  function abrirBaja(boton: HTMLButtonElement): void {
    botonBajaRef.current = boton;
    setErrorBaja('');
    setBaja(true);
  }

  function cerrarBaja(): void {
    setBaja(false);
    setErrorBaja('');
    window.requestAnimationFrame(() => botonBajaRef.current?.focus());
  }

  async function confirmarBaja(): Promise<void> {
    if (evaluacion === null) {
      return;
    }
    setErrorBaja('');
    setEliminando(true);
    try {
      await eliminarEvaluacion(evaluacion.id);
      setBaja(false);
      setSeleccionadaId(null);
      recargar();
    } catch (error: unknown) {
      setErrorBaja(obtenerMensaje(error, 'No se pudo eliminar la evaluación.'));
    } finally {
      setEliminando(false);
    }
  }

  return (
    <PaginaModulo className="evaluaciones-page" modulo="12-evaluacion-proveedores">
      <p className="module-intro">Califica a los proveedores cada mes según cumplimiento, calidad y plazos.</p>
      <EvaluacionesResumen datos={resumen} error={errorResumen} />
      {errorProveedores && <p className="evaluaciones-error" role="alert">{errorProveedores}</p>}
      <div className="evaluaciones-work-area">
        <section aria-labelledby="evaluaciones-list-title" className="evaluaciones-panel evaluaciones-list-panel">
          <div className="evaluaciones-panel-heading">
            <div>
              <span className="page-eyebrow">COMPRAS</span>
              <h2 id="evaluaciones-list-title">Evaluaciones de proveedores</h2>
            </div>
            <button className="primary-button" onClick={() => setFormulario('crear')} type="button">Nueva evaluación</button>
          </div>
          <EvaluacionesFiltros cambiarFiltros={cambiarFiltros} filtros={filtros} proveedores={proveedores} />
          <EvaluacionesTabla
            cambiarPagina={cambiarPagina}
            error={errorLista}
            lista={lista}
            pagina={filtros.pagina ?? 1}
            seleccionadaId={seleccionadaId}
            seleccionar={seleccionar}
          />
        </section>
        <EvaluacionDetallePanel
          cargando={cargandoDetalle}
          editar={() => setFormulario('editar')}
          eliminar={abrirBaja}
          evaluacion={evaluacion}
        />
      </div>
      {errorDetalle && <p className="evaluaciones-error" role="alert">{errorDetalle}</p>}
      {formulario !== null && (
        <EvaluacionFormulario
          cancelar={() => setFormulario(null)}
          evaluacion={formulario === 'editar' ? evaluacion : null}
          guardar={guardar}
          key={formulario === 'editar' ? `editar-${evaluacion?.id ?? 0}` : 'crear-evaluacion'}
          proveedores={proveedores}
        />
      )}
      {baja && evaluacion !== null && (
        <DialogoBaja
          cerrar={cerrarBaja}
          confirmar={() => void confirmarBaja()}
          eliminando={eliminando}
          entidad="evaluación"
          error={errorBaja}
          nombre={`${evaluacion.proveedorNombre} · ${evaluacion.periodo}`}
        />
      )}
    </PaginaModulo>
  );
}
