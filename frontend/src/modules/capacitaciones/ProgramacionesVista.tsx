import { useCallback, useMemo, useRef, useState } from 'react';
import {
  cancelarProgramacion,
  crearProgramacion,
  emitirCertificacion,
  inscribirEmpleado,
  marcarRealizada,
  registrarResultados,
  retirarInscripcion,
} from './api';
import { DialogoCertificacion } from './DialogoCertificacion';
import { DialogoConfirmacion } from './DialogoConfirmacion';
import { DialogoInscripcion } from './DialogoInscripcion';
import { InscritosTabla } from './InscritosTabla';
import { esConflicto, mensajeError, TEXTO_SOLO_RRHH, textoFecha } from './presentacion';
import { ProgramacionDetallePanel } from './ProgramacionDetallePanel';
import { ProgramacionFormulario } from './ProgramacionFormulario';
import { ProgramacionesFiltros } from './ProgramacionesFiltros';
import { ProgramacionesTabla } from './ProgramacionesTabla';
import { ResultadosEditor } from './ResultadosEditor';
import type {
  EmitirCertificacionRequest,
  EstadoProgramacion,
  FiltrosProgramaciones,
  GuardarProgramacionRequest,
  Inscripcion,
  RegistrarResultadosRequest,
} from './tipos';
import { useProgramacionDetalle, useProgramacionesData, useSelectoresProgramacion } from './useProgramacionesData';

const TAMANO_PAGINA = 10;

type Dialogo = { tipo: 'ninguno' } | { tipo: 'inscribir' } | { tipo: 'cancelar' } | { tipo: 'certificar'; inscripcion: Inscripcion };

interface Props {
  anio: number;
  puedeEscribir: boolean;
  alCambiar: () => void;
}

export function ProgramacionesVista({ anio, puedeEscribir, alCambiar }: Props) {
  const [parcial, setParcial] = useState<{ cursoId: number | null; estado: '' | EstadoProgramacion; pagina: number }>({ cursoId: null, estado: '', pagina: 1 });
  const filtros = useMemo<FiltrosProgramaciones>(() => ({ anio, ...parcial, tamanoPagina: TAMANO_PAGINA }), [anio, parcial]);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [formularioAbierto, setFormularioAbierto] = useState(false);
  const [dialogo, setDialogo] = useState<Dialogo>({ tipo: 'ninguno' });
  const [enCurso, setEnCurso] = useState(false);
  const [resultadosGuardadosId, setResultadosGuardadosId] = useState<number | null>(null);
  const [errorAccion, setErrorAccion] = useState<{ programacionId: number; texto: string } | null>(null);
  const tituloRef = useRef<HTMLHeadingElement>(null);
  const data = useProgramacionesData(filtros);
  const detalle = useProgramacionDetalle(seleccionadoId, data.revision);
  const selectores = useSelectoresProgramacion();
  const programacion = detalle.detalle;

  const cerrarDialogo = useCallback(() => {
    setDialogo({ tipo: 'ninguno' });
    window.requestAnimationFrame(() => tituloRef.current?.focus());
  }, []);

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setFormularioAbierto(false);
    setErrorAccion(null);
    setResultadosGuardadosId(null);
  }

  // Un 409 indica que la programación cambió por otra vía: se pone al día la pantalla y el formulario conserva lo escrito.
  async function ejecutar(accion: () => Promise<void>): Promise<void> {
    try {
      await accion();
      data.recargar();
      alCambiar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  async function accionDirecta(accion: () => Promise<unknown>, fallo: string): Promise<void> {
    if (programacion === null) {
      return;
    }
    const id = programacion.id;
    setEnCurso(true);
    setErrorAccion(null);
    try {
      await ejecutar(async () => {
        await accion();
      });
    } catch (error: unknown) {
      setErrorAccion({ programacionId: id, texto: mensajeError(error, fallo) });
    } finally {
      setEnCurso(false);
    }
  }

  async function guardarProgramacion(request: GuardarProgramacionRequest): Promise<void> {
    await ejecutar(async () => {
      const resultado = await crearProgramacion(request);
      setSeleccionadoId(resultado.id);
      setFormularioAbierto(false);
    });
  }

  async function inscribir(empleadoId: number): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    const id = seleccionadoId;
    await ejecutar(async () => {
      await inscribirEmpleado(id, empleadoId);
      setDialogo({ tipo: 'ninguno' });
    });
  }

  async function cancelar(): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    const id = seleccionadoId;
    await ejecutar(async () => {
      await cancelarProgramacion(id);
      setDialogo({ tipo: 'ninguno' });
    });
  }

  async function guardarResultados(request: RegistrarResultadosRequest): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    const id = seleccionadoId;
    await ejecutar(async () => {
      await registrarResultados(id, request);
      setResultadosGuardadosId(id);
    });
  }

  async function emitir(inscripcionId: number, request: EmitirCertificacionRequest): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    const id = seleccionadoId;
    await ejecutar(async () => {
      await emitirCertificacion(id, inscripcionId, request);
      setDialogo({ tipo: 'ninguno' });
    });
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === parcial.pagina && parcial.pagina > paginas) {
    setParcial({ ...parcial, pagina: paginas });
  }
  const cargandoDetalle = seleccionadoId !== null && programacion === null && detalle.error === '';
  const claveResultados = programacion === null ? '' : `${programacion.id}|${programacion.inscripciones.map((item) => `${item.id}:${item.estado}:${item.nota ?? ''}:${item.certificacion?.id ?? ''}`).join(',')}`;

  return (
    <>
      <section aria-labelledby="programaciones-list-title" className="clientes-list-panel capacitaciones-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">PROGRAMACIONES</div>
            <h2 id="programaciones-list-title">Programaciones de cursos</h2>
          </div>
          <button aria-describedby={puedeEscribir ? undefined : 'programaciones-nueva-solo-rrhh'} className="primary-button" disabled={!puedeEscribir} onClick={() => setFormularioAbierto(true)} type="button">
            Nueva programación
          </button>
        </div>
        {!puedeEscribir && <p className="capacitaciones-aviso" id="programaciones-nueva-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        {selectores.errorSelectores !== '' && <p className="clientes-error" role="alert">{selectores.errorSelectores}</p>}
        <ProgramacionesFiltros
          cambiarCurso={(cursoId) => setParcial((actual) => ({ ...actual, cursoId, pagina: 1 }))}
          cambiarEstado={(estado) => setParcial((actual) => ({ ...actual, estado, pagina: 1 }))}
          cursoId={parcial.cursoId}
          cursos={selectores.cursos?.items ?? []}
          estado={parcial.estado}
        />
        <ProgramacionesTabla
          cambiarPagina={(pagina) => setParcial((actual) => ({ ...actual, pagina }))}
          cargando={data.lista === null}
          error={data.errorLista}
          items={data.lista?.items ?? []}
          pagina={parcial.pagina}
          seleccionadoId={seleccionadoId}
          seleccionar={seleccionar}
          totalPaginas={paginas}
        />
      </section>
      {formularioAbierto && puedeEscribir && (
        <ProgramacionFormulario cancelar={() => setFormularioAbierto(false)} cargando={selectores.cursos === null} cursos={selectores.cursos?.items ?? []} guardar={guardarProgramacion} />
      )}
      <ProgramacionDetallePanel
        cancelar={() => setDialogo({ tipo: 'cancelar' })}
        cargando={cargandoDetalle}
        enCurso={enCurso}
        error={detalle.error}
        errorAccion={errorAccion !== null && errorAccion.programacionId === seleccionadoId ? errorAccion.texto : ''}
        inscribir={() => setDialogo({ tipo: 'inscribir' })}
        programacion={programacion}
        puedeEscribir={puedeEscribir}
        realizar={() => void accionDirecta(() => marcarRealizada(programacion?.id ?? 0), 'No se pudo marcar la programación como realizada.')}
        tituloRef={tituloRef}
      >
        {programacion !== null && (
          <section className="capacitaciones-section">
            <h3>Inscritos</h3>
            {programacion.estado === 'Realizada' && puedeEscribir ? (
              <ResultadosEditor emitir={(inscripcion) => setDialogo({ tipo: 'certificar', inscripcion })} guardado={resultadosGuardadosId === programacion.id} guardar={guardarResultados} inscripciones={programacion.inscripciones} key={claveResultados} />
            ) : (
              <InscritosTabla
                emitir={(inscripcion) => setDialogo({ tipo: 'certificar', inscripcion })}
                enCurso={enCurso}
                programacion={programacion}
                puedeEscribir={puedeEscribir}
                retirar={(inscripcion) => void accionDirecta(() => retirarInscripcion(programacion.id, inscripcion.id), 'No se pudo retirar la inscripción.')}
              />
            )}
          </section>
        )}
      </ProgramacionDetallePanel>
      {dialogo.tipo === 'inscribir' && puedeEscribir && programacion !== null && (
        <DialogoInscripcion cargando={selectores.empleados === null} cerrar={cerrarDialogo} empleados={selectores.empleados?.items ?? []} inscribir={inscribir} programacion={programacion} />
      )}
      {dialogo.tipo === 'certificar' && puedeEscribir && programacion !== null && (
        <DialogoCertificacion cerrar={cerrarDialogo} emitir={(request) => emitir(dialogo.inscripcion.id, request)} inscripcion={dialogo.inscripcion} programacion={programacion} />
      )}
      {dialogo.tipo === 'cancelar' && puedeEscribir && programacion !== null && (
        <DialogoConfirmacion
          advertencia="Las inscripciones de la programación pasarán a Retirado y no se podrá revertir."
          cerrar={cerrarDialogo}
          confirmar={cancelar}
          descripcion={`Se cancelará la programación de ${programacion.cursoNombre} del ${textoFecha(programacion.fechaInicio)}.`}
          eyebrow="PROGRAMACIONES"
          mensajeFallo="No se pudo cancelar la programación."
          textoConfirmar="Confirmar cancelación"
          textoEnviando="Cancelando…"
          titulo="Cancelar programación"
        />
      )}
    </>
  );
}
