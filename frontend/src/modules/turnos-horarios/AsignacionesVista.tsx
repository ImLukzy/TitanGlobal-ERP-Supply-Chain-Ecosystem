import { useCallback, useRef, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import { crearAsignacionTurno, eliminarAsignacionTurno, finalizarAsignacionTurno } from './api';
import { AsignacionDetallePanel } from './AsignacionDetallePanel';
import { AsignacionFormulario } from './AsignacionFormulario';
import { AsignacionesFiltros } from './AsignacionesFiltros';
import { AsignacionesTabla } from './AsignacionesTabla';
import { DialogoFinalizacion } from './DialogoFinalizacion';
import { esConflicto, hoyUtc, mensajeError, TEXTO_SOLO_RRHH } from './presentacion';
import type {
  AsignacionTurnoListado,
  EstadoAsignacion,
  FiltrosAsignaciones,
  FinalizarAsignacionTurnoRequest,
  GuardarAsignacionTurnoRequest,
} from './tipos';
import { useAsignacionesTurnoData } from './useAsignacionesTurnoData';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosAsignaciones = { empleadoId: null, turnoId: null, estado: '', pagina: 1, tamanoPagina: TAMANO_PAGINA };

type ModoFormulario = 'cerrado' | 'nuevo';
type Dialogo = 'ninguno' | 'finalizar' | 'eliminar';

export function AsignacionesVista({ puedeEscribir }: { puedeEscribir: boolean }) {
  const [filtros, setFiltros] = useState<FiltrosAsignaciones>(INICIALES);
  const [seleccion, setSeleccion] = useState<AsignacionTurnoListado | null>(null);
  const [modo, setModo] = useState<ModoFormulario>('cerrado');
  const [dialogo, setDialogo] = useState<Dialogo>('ninguno');
  const [errorEliminar, setErrorEliminar] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const botonFijarFinRef = useRef<HTMLButtonElement>(null);
  const botonEliminarRef = useRef<HTMLButtonElement>(null);
  const tituloDetalleRef = useRef<HTMLHeadingElement>(null);
  const data = useAsignacionesTurnoData(filtros, seleccion === null ? null : seleccion.empleadoId);

  // La ficha toma la fila actual de la lista (ya recargada) y, si la fila no está en la página, la última conocida.
  const asignacion = seleccion === null ? null : (data.lista?.items.find((item) => item.id === seleccion.id) ?? seleccion);

  function cambiarEmpleado(empleadoId: number | null): void {
    setFiltros((actual) => ({ ...actual, empleadoId, pagina: 1 }));
  }

  function cambiarTurno(turnoId: number | null): void {
    setFiltros((actual) => ({ ...actual, turnoId, pagina: 1 }));
  }

  function cambiarEstado(estado: '' | EstadoAsignacion): void {
    setFiltros((actual) => ({ ...actual, estado, pagina: 1 }));
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actual) => ({ ...actual, pagina }));
  }

  function seleccionar(item: AsignacionTurnoListado): void {
    setSeleccion(item);
    setModo('cerrado');
  }

  async function guardarAsignacion(request: GuardarAsignacionTurnoRequest): Promise<void> {
    try {
      await crearAsignacionTurno(request);
      setModo('cerrado');
      setSeleccion(null);
      data.recargar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  async function finalizarAsignacion(request: FinalizarAsignacionTurnoRequest): Promise<void> {
    if (asignacion === null) {
      return;
    }
    try {
      await finalizarAsignacionTurno(asignacion.id, request);
      const estado: EstadoAsignacion = request.fechaFin < hoyUtc() ? 'Finalizada' : asignacion.estado;
      setSeleccion({ ...asignacion, fechaFin: request.fechaFin, estado });
      setDialogo('ninguno');
      tituloDetalleRef.current?.focus();
      data.recargar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  async function confirmarEliminacion(): Promise<void> {
    if (asignacion === null) {
      return;
    }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await eliminarAsignacionTurno(asignacion.id);
      setDialogo('ninguno');
      setSeleccion(null);
      data.recargar();
    } catch (error: unknown) {
      setErrorEliminar(mensajeError(error, 'No se pudo eliminar la asignación.'));
      if (esConflicto(error)) {
        data.recargar();
      }
    } finally {
      setEliminando(false);
    }
  }

  const cerrarEliminacion = useCallback(() => {
    setDialogo('ninguno');
    setErrorEliminar('');
    window.requestAnimationFrame(() => botonEliminarRef.current?.focus());
  }, []);

  // Al cancelar o con Escape el foco vuelve al botón «Fijar fin», o a la ficha si ya no está disponible.
  function cerrarFinalizacion(): void {
    setDialogo('ninguno');
    if (botonFijarFinRef.current !== null && !botonFijarFinRef.current.disabled) {
      botonFijarFinRef.current.focus();
    } else {
      tituloDetalleRef.current?.focus();
    }
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const descripcion = asignacion === null ? '' : `${asignacion.turnoCodigo} de ${asignacion.empleadoNombre}`;

  return (
    <>
      <div className="turnos-horarios-work-area">
        <section aria-labelledby="asignaciones-list-title" className="clientes-list-panel turnos-horarios-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">ASIGNACIONES</div>
              <h2 id="asignaciones-list-title">Todas las asignaciones</h2>
            </div>
            <button
              aria-describedby={puedeEscribir ? undefined : 'asignaciones-solo-rrhh'}
              className="primary-button"
              disabled={!puedeEscribir}
              onClick={() => setModo('nuevo')}
              type="button"
            >
              Nueva asignación
            </button>
          </div>
          {!puedeEscribir && <p className="turnos-horarios-aviso" id="asignaciones-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
          <AsignacionesFiltros
            cambiarEmpleado={cambiarEmpleado}
            cambiarEstado={cambiarEstado}
            cambiarTurno={cambiarTurno}
            empleadoId={filtros.empleadoId}
            empleados={data.empleados}
            estado={filtros.estado}
            turnoId={filtros.turnoId}
            turnos={data.turnos}
          />
          <AsignacionesTabla
            cambiarPagina={cambiarPagina}
            cargando={data.lista === null}
            error={data.errorLista}
            items={data.lista?.items ?? []}
            pagina={filtros.pagina}
            seleccionadoId={seleccion === null ? null : seleccion.id}
            seleccionar={seleccionar}
            totalPaginas={paginas}
          />
        </section>
        <AsignacionDetallePanel
          asignacion={asignacion}
          botonEliminarRef={botonEliminarRef}
          botonFijarFinRef={botonFijarFinRef}
          eliminar={() => {
            setErrorEliminar('');
            setDialogo('eliminar');
          }}
          errorProgramacion={data.errorProgramacion}
          fijarFin={() => setDialogo('finalizar')}
          programacion={data.programacion}
          puedeEscribir={puedeEscribir}
          tituloRef={tituloDetalleRef}
        />
      </div>
      {modo === 'nuevo' && puedeEscribir && (
        <AsignacionFormulario
          cancelar={() => setModo('cerrado')}
          empleados={data.empleados}
          errorEmpleados={data.errorEmpleados}
          errorTurnos={data.errorTurnos}
          guardar={guardarAsignacion}
          turnos={data.turnos}
        />
      )}
      {dialogo === 'finalizar' && asignacion !== null && (
        <DialogoFinalizacion
          cerrar={cerrarFinalizacion}
          descripcion={descripcion}
          fechaInicio={asignacion.fechaInicio}
          finalizar={finalizarAsignacion}
        />
      )}
      {dialogo === 'eliminar' && asignacion !== null && (
        <DialogoBaja
          cerrar={cerrarEliminacion}
          confirmar={() => void confirmarEliminacion()}
          eliminando={eliminando}
          entidad="asignación"
          error={errorEliminar}
          nombre={descripcion}
        />
      )}
    </>
  );
}
