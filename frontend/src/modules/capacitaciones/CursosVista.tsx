import { useEffect, useRef, useState } from 'react';
import { activarCurso, actualizarCurso, crearCurso, desactivarCurso } from './api';
import { CursoDetallePanel } from './CursoDetallePanel';
import { CursoFormulario } from './CursoFormulario';
import { CursosFiltros } from './CursosFiltros';
import { CursosTabla } from './CursosTabla';
import { esConflicto, mensajeError, TEXTO_SOLO_RRHH } from './presentacion';
import type { FiltroActivo, FiltrosCursos, GuardarCursoRequest, TipoCurso } from './tipos';
import { useCursoDetalle, useCursosData } from './useCursosData';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosCursos = { busqueda: '', tipo: '', activo: 'Todos', pagina: 1, tamanoPagina: TAMANO_PAGINA };

type ModoFormulario = 'cerrado' | 'nuevo' | 'editar';

interface Props {
  puedeEscribir: boolean;
  alCambiar: () => void;
}

export function CursosVista({ puedeEscribir, alCambiar }: Props) {
  const [busqueda, setBusqueda] = useState('');
  const [filtros, setFiltros] = useState<FiltrosCursos>(INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [modo, setModo] = useState<ModoFormulario>('cerrado');
  const [enCurso, setEnCurso] = useState(false);
  const [errorAccion, setErrorAccion] = useState<{ cursoId: number; texto: string } | null>(null);
  const tituloRef = useRef<HTMLHeadingElement>(null);
  const data = useCursosData(filtros);
  const detalle = useCursoDetalle(seleccionadoId, data.revision);
  const curso = detalle.detalle;

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

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setModo('cerrado');
    setErrorAccion(null);
  }

  // Un 409 indica que el curso cambió por otra vía: se pone al día la pantalla y el formulario conserva lo escrito.
  async function guardarCurso(request: GuardarCursoRequest): Promise<void> {
    try {
      if (modo === 'editar' && curso !== null) {
        await actualizarCurso(curso.id, request);
      } else {
        const resultado = await crearCurso(request);
        setSeleccionadoId(resultado.id);
      }
      setModo('cerrado');
      data.recargar();
      alCambiar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  async function alternarActivo(): Promise<void> {
    if (curso === null) {
      return;
    }
    const actual = curso;
    setEnCurso(true);
    setErrorAccion(null);
    try {
      if (actual.activo) {
        await desactivarCurso(actual.id);
      } else {
        await activarCurso(actual.id);
      }
      alCambiar();
    } catch (error: unknown) {
      setErrorAccion({ cursoId: actual.id, texto: mensajeError(error, 'No se pudo cambiar el estado del curso.') });
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
  const cargandoDetalle = seleccionadoId !== null && curso === null && detalle.error === '';

  return (
    <>
      <section aria-labelledby="cursos-list-title" className="clientes-list-panel capacitaciones-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">CURSOS</div>
            <h2 id="cursos-list-title">Catálogo de cursos</h2>
          </div>
          <button aria-describedby={puedeEscribir ? undefined : 'cursos-nuevo-solo-rrhh'} className="primary-button" disabled={!puedeEscribir} onClick={() => setModo('nuevo')} type="button">
            Nuevo curso
          </button>
        </div>
        {!puedeEscribir && <p className="capacitaciones-aviso" id="cursos-nuevo-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        <CursosFiltros
          activo={filtros.activo}
          busqueda={busqueda}
          cambiarActivo={(activo: FiltroActivo) => setFiltros((actual) => ({ ...actual, activo, pagina: 1 }))}
          cambiarBusqueda={setBusqueda}
          cambiarTipo={(tipo: '' | TipoCurso) => setFiltros((actual) => ({ ...actual, tipo, pagina: 1 }))}
          tipo={filtros.tipo}
        />
        <CursosTabla
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
      {modo === 'nuevo' && puedeEscribir && <CursoFormulario cancelar={() => setModo('cerrado')} curso={null} guardar={guardarCurso} key="nuevo" />}
      {modo === 'editar' && puedeEscribir && curso !== null && <CursoFormulario cancelar={() => setModo('cerrado')} curso={curso} guardar={guardarCurso} key={curso.id} />}
      <CursoDetallePanel
        alternarActivo={() => void alternarActivo()}
        cargando={cargandoDetalle}
        curso={curso}
        editar={() => setModo('editar')}
        enCurso={enCurso}
        error={detalle.error}
        errorAccion={errorAccion !== null && errorAccion.cursoId === seleccionadoId ? errorAccion.texto : ''}
        puedeEscribir={puedeEscribir}
        tituloRef={tituloRef}
      />
    </>
  );
}
