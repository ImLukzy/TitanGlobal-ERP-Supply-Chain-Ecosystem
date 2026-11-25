import { useCallback, useEffect, useRef, useState } from 'react';
import { cerrarEvaluacion, crearEvaluacion, enviarEvaluacion, guardarCalificaciones } from './api';
import { CalificacionesEditor } from './CalificacionesEditor';
import { DialogoConfirmacion } from './DialogoConfirmacion';
import { DialogoNuevaEvaluacion } from './DialogoNuevaEvaluacion';
import { EvaluacionDetallePanel } from './EvaluacionDetallePanel';
import { EvaluacionesFiltros } from './EvaluacionesFiltros';
import { EvaluacionesTabla } from './EvaluacionesTabla';
import { esConflicto, TEXTO_SOLO_RRHH } from './presentacion';
import type { CrearEvaluacionRequest, EstadoEvaluacion, FiltrosEvaluaciones, GuardarCalificacionesRequest } from './tipos';
import { useEvaluacionDetalle, useEvaluacionesData, useSelectoresEvaluacion } from './useEvaluacionesData';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosEvaluaciones = { cicloId: null, estado: '', busqueda: '', pagina: 1, tamanoPagina: TAMANO_PAGINA };

type Dialogo = 'ninguno' | 'nueva' | 'cerrar';

export function EvaluacionesVista({ puedeEscribir }: { puedeEscribir: boolean }) {
  const [busqueda, setBusqueda] = useState('');
  const [filtros, setFiltros] = useState<FiltrosEvaluaciones>(INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [dialogo, setDialogo] = useState<Dialogo>('ninguno');
  const tituloDetalleRef = useRef<HTMLHeadingElement>(null);
  const data = useEvaluacionesData(filtros);
  const detalle = useEvaluacionDetalle(seleccionadoId, data.revision);
  const selectores = useSelectoresEvaluacion();

  // La búsqueda se aplica 300 ms después del último cambio y vuelve a la página 1 solo si el texto cambia.
  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      setFiltros((actual) => {
        const termino = busqueda.trim();
        if (actual.busqueda === termino) {
          return actual;
        }
        return { ...actual, busqueda: termino, pagina: 1 };
      });
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  const cerrarDialogo = useCallback(() => {
    setDialogo('ninguno');
    window.requestAnimationFrame(() => tituloDetalleRef.current?.focus());
  }, []);

  // Un 409 indica que la evaluación cambió por otra vía: se pone al día la pantalla y el mensaje se muestra donde se pidió la acción.
  async function ejecutar(accion: () => Promise<void>): Promise<void> {
    try {
      await accion();
      data.recargar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  async function crear(request: CrearEvaluacionRequest): Promise<void> {
    await ejecutar(async () => {
      const resultado = await crearEvaluacion(request);
      setSeleccionadoId(resultado.id);
      setDialogo('ninguno');
    });
  }

  async function guardar(request: GuardarCalificacionesRequest): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    const id = seleccionadoId;
    await ejecutar(async () => {
      await guardarCalificaciones(id, request);
    });
  }

  // «Enviar» guarda primero lo que hay en pantalla: si faltan calificaciones, el servidor responde con su mensaje 28.
  async function enviar(request: GuardarCalificacionesRequest): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    const id = seleccionadoId;
    await ejecutar(async () => {
      await guardarCalificaciones(id, request);
      await enviarEvaluacion(id);
    });
  }

  async function cerrar(): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    const id = seleccionadoId;
    await ejecutar(async () => {
      await cerrarEvaluacion(id);
      setDialogo('ninguno');
    });
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const evaluacion = detalle.detalle;
  const cargandoDetalle = seleccionadoId !== null && evaluacion === null && detalle.error === '';
  const cargandoSelectores = selectores.ciclos === null || selectores.empleados === null;

  return (
    <>
      <section aria-labelledby="evaluaciones-list-title" className="clientes-list-panel evaluacion-desempeno-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">EVALUACIONES</div>
            <h2 id="evaluaciones-list-title">Evaluaciones de desempeño</h2>
          </div>
          <button
            aria-describedby={puedeEscribir ? undefined : 'evaluaciones-nueva-solo-rrhh'}
            className="primary-button"
            disabled={!puedeEscribir}
            onClick={() => setDialogo('nueva')}
            type="button"
          >
            Nueva evaluación
          </button>
        </div>
        {!puedeEscribir && <p className="evaluacion-desempeno-aviso" id="evaluaciones-nueva-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        {selectores.errorSelectores !== '' && <p className="clientes-error" role="alert">{selectores.errorSelectores}</p>}
        <EvaluacionesFiltros
          busqueda={busqueda}
          cambiarBusqueda={setBusqueda}
          cambiarCiclo={(cicloId) => setFiltros((actual) => ({ ...actual, cicloId, pagina: 1 }))}
          cambiarEstado={(estado: '' | EstadoEvaluacion) => setFiltros((actual) => ({ ...actual, estado, pagina: 1 }))}
          cicloId={filtros.cicloId}
          ciclos={selectores.ciclos?.items ?? []}
          estado={filtros.estado}
        />
        <EvaluacionesTabla
          cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))}
          cargando={data.lista === null}
          error={data.errorLista}
          items={data.lista?.items ?? []}
          pagina={filtros.pagina}
          seleccionadoId={seleccionadoId}
          seleccionar={setSeleccionadoId}
          totalPaginas={paginas}
        />
      </section>
      <EvaluacionDetallePanel
        cargando={cargandoDetalle}
        cerrar={() => setDialogo('cerrar')}
        error={detalle.error}
        evaluacion={evaluacion}
        puedeEscribir={puedeEscribir}
        tituloRef={tituloDetalleRef}
      >
        {evaluacion !== null && (
          <CalificacionesEditor enviar={enviar} evaluacion={evaluacion} guardar={guardar} key={evaluacion.id} puedeEscribir={puedeEscribir} />
        )}
      </EvaluacionDetallePanel>
      {dialogo === 'nueva' && puedeEscribir && (
        <DialogoNuevaEvaluacion
          cargando={cargandoSelectores}
          ciclos={selectores.ciclos?.items ?? []}
          cerrar={cerrarDialogo}
          crear={crear}
          empleados={selectores.empleados?.items ?? []}
        />
      )}
      {dialogo === 'cerrar' && puedeEscribir && evaluacion !== null && (
        <DialogoConfirmacion
          advertencia="La evaluación cerrada no se puede modificar."
          cerrar={cerrarDialogo}
          confirmar={cerrar}
          descripcion={`Se cerrará la evaluación de ${evaluacion.empleadoNombre} del ciclo ${evaluacion.cicloCodigo}.`}
          eyebrow="EVALUACIONES"
          mensajeFallo="No se pudo cerrar la evaluación."
          textoConfirmar="Cerrar evaluación"
          textoEnviando="Cerrando…"
          titulo="Cerrar evaluación"
        />
      )}
    </>
  );
}
