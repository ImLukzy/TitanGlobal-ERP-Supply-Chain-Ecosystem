import { useEffect, useRef, useState } from 'react';
import { activarTurno, actualizarTurno, crearTurno, desactivarTurno } from './api';
import { esConflicto, mensajeError, TEXTO_SOLO_RRHH } from './presentacion';
import type { FiltroActivo, FiltrosTurnos, GuardarTurnoRequest, TipoTurno } from './tipos';
import { TurnoDetallePanel } from './TurnoDetallePanel';
import { TurnoFormulario } from './TurnoFormulario';
import { TurnosFiltros } from './TurnosFiltros';
import { TurnosResumen } from './TurnosResumen';
import { TurnosTabla } from './TurnosTabla';
import { useTurnosData } from './useTurnosData';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosTurnos = { busqueda: '', tipo: '', activo: 'Todos', pagina: 1, tamanoPagina: TAMANO_PAGINA };

type ModoFormulario = 'cerrado' | 'nuevo' | 'editar';

interface MensajePorTurno {
  turnoId: number;
  texto: string;
}

export function TurnosVista({ puedeEscribir }: { puedeEscribir: boolean }) {
  const [busqueda, setBusqueda] = useState('');
  const [filtros, setFiltros] = useState<FiltrosTurnos>(INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [modo, setModo] = useState<ModoFormulario>('cerrado');
  const [enCurso, setEnCurso] = useState(false);
  const [mensajeAccion, setMensajeAccion] = useState<MensajePorTurno | null>(null);
  const tituloDetalleRef = useRef<HTMLHeadingElement>(null);
  const data = useTurnosData(filtros, seleccionadoId);

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

  function cambiarTipo(tipo: '' | TipoTurno): void {
    setFiltros((actual) => ({ ...actual, tipo, pagina: 1 }));
  }

  function cambiarActivo(activo: FiltroActivo): void {
    setFiltros((actual) => ({ ...actual, activo, pagina: 1 }));
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actual) => ({ ...actual, pagina }));
  }

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setModo('cerrado');
  }

  // Un 409 indica que el turno cambió por otra vía: se pone al día la pantalla y el formulario conserva lo escrito.
  async function guardarTurno(request: GuardarTurnoRequest): Promise<void> {
    try {
      if (modo === 'editar' && data.turno !== null) {
        await actualizarTurno(data.turno.id, request);
      } else {
        const resultado = await crearTurno(request);
        setSeleccionadoId(resultado.id);
      }
      setModo('cerrado');
      data.recargar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  async function alternarActivo(): Promise<void> {
    if (data.turno === null) {
      return;
    }
    const turno = data.turno;
    setEnCurso(true);
    setMensajeAccion(null);
    try {
      if (turno.activo) {
        await desactivarTurno(turno.id);
      } else {
        await activarTurno(turno.id);
      }
      data.recargar();
    } catch (error: unknown) {
      setMensajeAccion({ turnoId: turno.id, texto: mensajeError(error, 'No se pudo cambiar el estado del turno.') });
      if (esConflicto(error)) {
        data.recargar();
      }
    } finally {
      setEnCurso(false);
    }
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const cargandoDetalle = seleccionadoId !== null && data.turno === null && data.errorDetalle === '';
  const textoAccion = mensajeAccion !== null && mensajeAccion.turnoId === seleccionadoId ? mensajeAccion.texto : '';

  return (
    <>
      <TurnosResumen datos={data.resumen} error={data.errorResumen} />
      <div className="turnos-horarios-work-area">
        <section aria-labelledby="turnos-list-title" className="clientes-list-panel turnos-horarios-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">TURNOS</div>
              <h2 id="turnos-list-title">Todos los turnos</h2>
            </div>
            <button
              aria-describedby={puedeEscribir ? undefined : 'turnos-solo-rrhh'}
              className="primary-button"
              disabled={!puedeEscribir}
              onClick={() => setModo('nuevo')}
              type="button"
            >
              Nuevo turno
            </button>
          </div>
          {!puedeEscribir && <p className="turnos-horarios-aviso" id="turnos-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
          <TurnosFiltros
            activo={filtros.activo}
            busqueda={busqueda}
            cambiarActivo={cambiarActivo}
            cambiarBusqueda={setBusqueda}
            cambiarTipo={cambiarTipo}
            tipo={filtros.tipo}
          />
          <TurnosTabla
            cambiarPagina={cambiarPagina}
            cargando={data.lista === null}
            error={data.errorLista}
            items={data.lista?.items ?? []}
            pagina={filtros.pagina}
            seleccionadoId={seleccionadoId}
            seleccionar={seleccionar}
            totalPaginas={paginas}
          />
        </section>
        <TurnoDetallePanel
          cambiarActivo={() => void alternarActivo()}
          cargando={cargandoDetalle}
          editar={() => setModo('editar')}
          enCurso={enCurso}
          error={data.errorDetalle}
          mensajeAccion={textoAccion}
          puedeEscribir={puedeEscribir}
          tituloRef={tituloDetalleRef}
          turno={data.turno}
        />
      </div>
      {modo === 'nuevo' && puedeEscribir && (
        <TurnoFormulario cancelar={() => setModo('cerrado')} guardar={guardarTurno} key="nuevo" turno={null} />
      )}
      {modo === 'editar' && puedeEscribir && data.turno !== null && (
        <TurnoFormulario cancelar={() => setModo('cerrado')} guardar={guardarTurno} key={data.turno.id} turno={data.turno} />
      )}
    </>
  );
}
