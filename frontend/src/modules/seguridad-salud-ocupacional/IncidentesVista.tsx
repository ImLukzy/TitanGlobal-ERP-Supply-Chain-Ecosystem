import { useEffect, useRef, useState } from 'react';
import { agregarAccion, cerrarIncidente, completarAccion, registrarIncidente } from './api';
import { DialogoAccion } from './DialogoAccion';
import { DialogoCompletarAccion } from './DialogoCompletarAccion';
import { IncidenteDetallePanel } from './IncidenteDetallePanel';
import { IncidenteFormulario } from './IncidenteFormulario';
import { IncidentesFiltros } from './IncidentesFiltros';
import { IncidentesTabla } from './IncidentesTabla';
import { aSeveridad, aTipoIncidente, esConflicto, mensajeError, TAMANO_PAGINA, TEXTO_SOLO_RRHH } from './presentacion';
import type { AccionCorrectiva, FiltrosIncidentes, GuardarAccionRequest, RegistrarIncidenteRequest } from './tipos';
import { useEmpleados, useIncidenteDetalle, useIncidentesData } from './useIncidentesData';

interface Props {
  puedeEscribir: boolean;
  alCambiar: () => void;
  parametros: URLSearchParams;
}

type Dialogo = { tipo: 'ninguno' } | { tipo: 'accion' } | { tipo: 'completar'; accion: AccionCorrectiva };

// Los filtros iniciales vienen de los enlaces del resumen (?estado=Abierto, ?desde=…).
function filtrosIniciales(parametros: URLSearchParams): FiltrosIncidentes {
  const estado = parametros.get('estado');
  return {
    busqueda: '',
    tipo: aTipoIncidente(parametros.get('tipo') ?? ''),
    severidad: aSeveridad(parametros.get('severidad') ?? ''),
    estado: estado === 'Abierto' || estado === 'Cerrado' ? estado : '',
    desde: parametros.get('desde') ?? '',
    hasta: parametros.get('hasta') ?? '',
    pagina: 1,
    tamanoPagina: TAMANO_PAGINA,
  };
}

export function IncidentesVista({ puedeEscribir, alCambiar, parametros }: Props) {
  const [busqueda, setBusqueda] = useState('');
  const [filtros, setFiltros] = useState<FiltrosIncidentes>(() => filtrosIniciales(parametros));
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [formulario, setFormulario] = useState(false);
  const [dialogo, setDialogo] = useState<Dialogo>({ tipo: 'ninguno' });
  const [enCurso, setEnCurso] = useState(false);
  const [errorAccion, setErrorAccion] = useState<{ incidenteId: number; texto: string } | null>(null);
  const tituloRef = useRef<HTMLHeadingElement>(null);
  const data = useIncidentesData(filtros);
  const detalle = useIncidenteDetalle(seleccionadoId, data.revision);
  const empleados = useEmpleados();
  const incidente = detalle.detalle;

  // La búsqueda se aplica 300 ms después del último cambio y vuelve a la página 1 solo si el texto cambia.
  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      setFiltros((actual) => {
        const termino = busqueda.trim();
        return actual.busqueda === termino ? actual : { ...actual, busqueda: termino, pagina: 1 };
      });
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarFiltros(cambios: Partial<FiltrosIncidentes>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: 1 }));
  }

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setFormulario(false);
    setErrorAccion(null);
  }

  async function registrar(request: RegistrarIncidenteRequest): Promise<void> {
    const creado = await registrarIncidente(request);
    setSeleccionadoId(creado.id);
    setFormulario(false);
    data.recargar();
    alCambiar();
  }

  // Un 409 indica que el incidente cambió por otra vía: se pone al día la pantalla y el diálogo conserva lo escrito.
  async function escribir(operacion: () => Promise<unknown>): Promise<void> {
    try {
      await operacion();
      setDialogo({ tipo: 'ninguno' });
      data.recargar();
      alCambiar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  async function guardarAccion(request: GuardarAccionRequest): Promise<void> {
    if (incidente !== null) {
      await escribir(() => agregarAccion(incidente.id, request));
    }
  }

  async function marcarCompletada(accion: AccionCorrectiva, fecha: string): Promise<void> {
    if (incidente !== null) {
      await escribir(() => completarAccion(incidente.id, accion.id, { fechaCompletada: fecha }));
    }
  }

  async function cerrar(): Promise<void> {
    if (incidente === null) {
      return;
    }
    const actual = incidente;
    setEnCurso(true);
    setErrorAccion(null);
    try {
      await cerrarIncidente(actual.id);
      alCambiar();
    } catch (error: unknown) {
      setErrorAccion({ incidenteId: actual.id, texto: mensajeError(error, 'No se pudo cerrar el incidente.') });
    } finally {
      setEnCurso(false);
      data.recargar();
    }
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const cargandoDetalle = seleccionadoId !== null && incidente === null && detalle.error === '';

  return (
    <>
      <section aria-labelledby="incidentes-list-title" className="clientes-list-panel sst-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">INCIDENTES</div>
            <h2 id="incidentes-list-title">Incidentes y accidentes</h2>
          </div>
          <button aria-describedby={puedeEscribir ? undefined : 'incidentes-nuevo-solo-rrhh'} className="primary-button" disabled={!puedeEscribir} onClick={() => setFormulario(true)} type="button">Nuevo incidente</button>
        </div>
        {!puedeEscribir && <p className="sst-aviso" id="incidentes-nuevo-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        <IncidentesFiltros busqueda={busqueda} cambiar={cambiarFiltros} cambiarBusqueda={setBusqueda} filtros={filtros} />
        {empleados.errorEmpleados !== '' && <p className="clientes-error" role="alert">{empleados.errorEmpleados}</p>}
        <IncidentesTabla
          cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))}
          cargando={data.lista === null}
          error={data.errorLista}
          items={data.lista?.items ?? []}
          pagina={filtros.pagina}
          seleccionadoId={seleccionadoId}
          seleccionar={seleccionar}
          totalPaginas={paginas}
        />
      </section>
      {formulario && puedeEscribir && <IncidenteFormulario cancelar={() => setFormulario(false)} empleados={empleados.empleados ?? []} guardar={registrar} />}
      <IncidenteDetallePanel
        agregarAccion={() => setDialogo({ tipo: 'accion' })}
        cargando={cargandoDetalle}
        cerrar={() => void cerrar()}
        completarAccion={(accion) => setDialogo({ tipo: 'completar', accion })}
        enCurso={enCurso}
        error={detalle.error}
        errorAccion={errorAccion !== null && errorAccion.incidenteId === seleccionadoId ? errorAccion.texto : ''}
        incidente={incidente}
        puedeEscribir={puedeEscribir}
        tituloRef={tituloRef}
      />
      {dialogo.tipo === 'accion' && incidente !== null && <DialogoAccion cerrar={() => setDialogo({ tipo: 'ninguno' })} guardar={guardarAccion} numero={incidente.numero} />}
      {dialogo.tipo === 'completar' && incidente !== null && (
        <DialogoCompletarAccion accion={dialogo.accion} cerrar={() => setDialogo({ tipo: 'ninguno' })} completar={(fecha) => marcarCompletada(dialogo.accion, fecha)} />
      )}
    </>
  );
}
