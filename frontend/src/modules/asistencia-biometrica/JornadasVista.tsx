import { useCallback, useRef, useState } from 'react';
import { formatearFecha } from '../../lib/formato';
import { aprobarHorasExtra, calcularJornadas, rechazarHorasExtra } from './api';
import { DialogoCalculo } from './DialogoCalculo';
import { DialogoRechazo } from './DialogoRechazo';
import { JornadaDetallePanel } from './JornadaDetallePanel';
import { JornadasFiltros } from './JornadasFiltros';
import { JornadasResumen } from './JornadasResumen';
import { JornadasTabla } from './JornadasTabla';
import { esConflicto, mensajeError, semanaActual, TEXTO_SOLO_RRHH } from './presentacion';
import type {
  CalcularJornadasRequest,
  EstadoHorasExtra,
  EstadoJornada,
  FiltrosJornadas,
  JornadaAsistencia,
  ResultadoCalculo,
} from './tipos';
import { useJornadasData } from './useJornadasData';

const TAMANO_PAGINA = 10;

type Dialogo = 'ninguno' | 'calculo' | 'rechazo';

// Decisión ya aceptada por la API que la lista todavía no refleja: evita mostrar los botones de una jornada ya decidida.
interface DecisionLocal {
  jornadaId: number;
  estadoHorasExtra: EstadoHorasExtra;
  motivoRechazo: string | null;
}

function filtrosIniciales(): FiltrosJornadas {
  const { desde, hasta } = semanaActual();
  return { empleadoId: null, desde, hasta, estado: '', estadoHorasExtra: '', pagina: 1, tamanoPagina: TAMANO_PAGINA };
}

export function JornadasVista({ puedeEscribir }: { puedeEscribir: boolean }) {
  const [filtros, setFiltros] = useState<FiltrosJornadas>(filtrosIniciales);
  const [seleccion, setSeleccion] = useState<JornadaAsistencia | null>(null);
  const [decision, setDecision] = useState<DecisionLocal | null>(null);
  const [dialogo, setDialogo] = useState<Dialogo>('ninguno');
  const [errorAccion, setErrorAccion] = useState('');
  const [procesando, setProcesando] = useState(false);
  const botonCalcularRef = useRef<HTMLButtonElement>(null);
  const botonRechazarRef = useRef<HTMLButtonElement>(null);
  const tituloDetalleRef = useRef<HTMLHeadingElement>(null);
  const data = useJornadasData(
    filtros,
    seleccion === null ? null : { id: seleccion.id, empleadoId: seleccion.empleadoId, fecha: seleccion.fecha },
  );

  // La ficha toma la fila actual de la lista (ya recargada) y, si la fila no está en la página, la última conocida.
  const base = seleccion === null ? null : (data.lista?.items.find((item) => item.id === seleccion.id) ?? seleccion);
  let jornada = base;
  if (base !== null && decision !== null && decision.jornadaId === base.id && base.estadoHorasExtra === 'Pendiente') {
    jornada = { ...base, estadoHorasExtra: decision.estadoHorasExtra, motivoRechazo: decision.motivoRechazo };
  }

  function cambiarFiltros(cambio: Partial<FiltrosJornadas>): void {
    setFiltros((actual) => ({ ...actual, ...cambio, pagina: 1 }));
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actual) => ({ ...actual, pagina }));
  }

  function seleccionar(item: JornadaAsistencia): void {
    setSeleccion(item);
    setDecision(null);
    setErrorAccion('');
  }

  async function aprobar(): Promise<void> {
    if (jornada === null) {
      return;
    }
    setProcesando(true);
    setErrorAccion('');
    try {
      await aprobarHorasExtra(jornada.id);
      setDecision({ jornadaId: jornada.id, estadoHorasExtra: 'Aprobada', motivoRechazo: null });
      data.recargar();
    } catch (error: unknown) {
      setErrorAccion(mensajeError(error, 'No se pudieron aprobar las horas extra.'));
      if (esConflicto(error)) {
        data.recargar();
      }
    } finally {
      setProcesando(false);
    }
  }

  async function rechazar(motivo: string): Promise<void> {
    if (jornada === null) {
      return;
    }
    try {
      await rechazarHorasExtra(jornada.id, { motivo: motivo.trim() });
      setDecision({ jornadaId: jornada.id, estadoHorasExtra: 'Rechazada', motivoRechazo: motivo.trim() });
      setErrorAccion('');
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

  // Recalcular regenera las jornadas: las filas seleccionadas pueden cambiar de id, así que se limpia la selección.
  async function calcular(request: CalcularJornadasRequest): Promise<ResultadoCalculo> {
    const resultado = await calcularJornadas(request);
    setSeleccion(null);
    setDecision(null);
    data.recargar();
    return resultado;
  }

  const cerrarCalculo = useCallback(() => {
    setDialogo('ninguno');
    window.requestAnimationFrame(() => botonCalcularRef.current?.focus());
  }, []);

  function cerrarRechazo(): void {
    setDialogo('ninguno');
    if (botonRechazarRef.current !== null && !botonRechazarRef.current.disabled) {
      botonRechazarRef.current.focus();
    } else {
      tituloDetalleRef.current?.focus();
    }
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const descripcion = jornada === null ? '' : `${jornada.empleadoNombre}, ${formatearFecha(jornada.fecha)}`;

  return (
    <>
      <JornadasResumen datos={data.resumen} error={data.errorResumen} />
      <div className="asistencia-work-area">
        <section aria-labelledby="jornadas-list-title" className="clientes-list-panel asistencia-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">JORNADAS</div>
              <h2 id="jornadas-list-title">Jornadas de asistencia</h2>
            </div>
            <button
              aria-describedby={puedeEscribir ? undefined : 'jornadas-solo-rrhh'}
              className="primary-button"
              disabled={!puedeEscribir}
              onClick={() => setDialogo('calculo')}
              ref={botonCalcularRef}
              type="button"
            >
              Calcular jornadas
            </button>
          </div>
          {!puedeEscribir && <p className="asistencia-aviso" id="jornadas-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
          <JornadasFiltros
            cambiarDesde={(desde) => cambiarFiltros({ desde })}
            cambiarEmpleado={(empleadoId) => cambiarFiltros({ empleadoId })}
            cambiarEstado={(estado: '' | EstadoJornada) => cambiarFiltros({ estado })}
            cambiarEstadoHorasExtra={(estadoHorasExtra: '' | EstadoHorasExtra) => cambiarFiltros({ estadoHorasExtra })}
            cambiarHasta={(hasta) => cambiarFiltros({ hasta })}
            desde={filtros.desde}
            empleadoId={filtros.empleadoId}
            empleados={data.empleados}
            estado={filtros.estado}
            estadoHorasExtra={filtros.estadoHorasExtra}
            hasta={filtros.hasta}
          />
          <JornadasTabla
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
        <JornadaDetallePanel
          aprobar={() => void aprobar()}
          botonRechazarRef={botonRechazarRef}
          errorAccion={errorAccion}
          errorMarcaciones={data.errorMarcaciones}
          jornada={jornada}
          marcaciones={data.marcaciones}
          procesando={procesando}
          puedeEscribir={puedeEscribir}
          rechazar={() => setDialogo('rechazo')}
          tituloRef={tituloDetalleRef}
        />
      </div>
      {dialogo === 'calculo' && puedeEscribir && (
        <DialogoCalculo
          calcular={calcular}
          cerrar={cerrarCalculo}
          desde={filtros.desde}
          empleados={data.empleados}
          hasta={filtros.hasta}
        />
      )}
      {dialogo === 'rechazo' && puedeEscribir && jornada !== null && (
        <DialogoRechazo cerrar={cerrarRechazo} descripcion={descripcion} rechazar={rechazar} />
      )}
    </>
  );
}
