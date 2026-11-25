import { useCallback, useEffect, useRef, useState } from 'react';
import {
  abrirCiclo,
  actualizarCiclo,
  actualizarCompetencia,
  cerrarCiclo,
  crearCiclo,
  crearCompetencia,
  eliminarCompetencia,
} from './api';
import { CicloDetallePanel } from './CicloDetallePanel';
import { CicloFormulario } from './CicloFormulario';
import { CiclosFiltros } from './CiclosFiltros';
import { CiclosTabla } from './CiclosTabla';
import { CompetenciasSeccion, type FormularioCompetencia } from './CompetenciasSeccion';
import { DialogoConfirmacion } from './DialogoConfirmacion';
import { esConflicto, mensajeError, TEXTO_SOLO_RRHH } from './presentacion';
import { ResultadosCiclo } from './ResultadosCiclo';
import type { Competencia, EstadoCiclo, FiltrosCiclos, GuardarCicloRequest, GuardarCompetenciaRequest } from './tipos';
import { useCicloDetalle, useCiclosData } from './useCiclosData';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosCiclos = { busqueda: '', estado: '', pagina: 1, tamanoPagina: TAMANO_PAGINA };

type ModoFormulario = 'cerrado' | 'nuevo' | 'editar';
type Dialogo = { tipo: 'ninguno' } | { tipo: 'cerrar' } | { tipo: 'quitar'; competencia: Competencia };

export function CiclosVista({ puedeEscribir }: { puedeEscribir: boolean }) {
  const [busqueda, setBusqueda] = useState('');
  const [filtros, setFiltros] = useState<FiltrosCiclos>(INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [modo, setModo] = useState<ModoFormulario>('cerrado');
  const [formularioCompetencia, setFormularioCompetencia] = useState<FormularioCompetencia>(null);
  const [dialogo, setDialogo] = useState<Dialogo>({ tipo: 'ninguno' });
  const [abriendo, setAbriendo] = useState(false);
  const [errorAccion, setErrorAccion] = useState('');
  const tituloDetalleRef = useRef<HTMLHeadingElement>(null);
  const data = useCiclosData(filtros);
  const detalle = useCicloDetalle(seleccionadoId, data.revision);

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

  function cambiarEstado(estado: '' | EstadoCiclo): void {
    setFiltros((actual) => ({ ...actual, estado, pagina: 1 }));
  }

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setModo('cerrado');
    setFormularioCompetencia(null);
    setErrorAccion('');
  }

  const cerrarDialogo = useCallback(() => {
    setDialogo({ tipo: 'ninguno' });
    window.requestAnimationFrame(() => tituloDetalleRef.current?.focus());
  }, []);

  // Un 409 indica que el ciclo cambió por otra vía: se pone al día la pantalla y el formulario conserva lo escrito.
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

  async function guardarCiclo(request: GuardarCicloRequest): Promise<void> {
    await ejecutar(async () => {
      if (modo === 'editar' && detalle.detalle !== null) {
        await actualizarCiclo(detalle.detalle.id, request);
      } else {
        const resultado = await crearCiclo(request);
        setSeleccionadoId(resultado.id);
      }
      setModo('cerrado');
    });
  }

  async function guardarCompetencia(request: GuardarCompetenciaRequest): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    await ejecutar(async () => {
      if (typeof formularioCompetencia === 'number') {
        await actualizarCompetencia(seleccionadoId, formularioCompetencia, request);
      } else {
        await crearCompetencia(seleccionadoId, request);
      }
      setFormularioCompetencia(null);
    });
  }

  async function abrir(): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    setAbriendo(true);
    setErrorAccion('');
    try {
      await abrirCiclo(seleccionadoId);
    } catch (error: unknown) {
      setErrorAccion(mensajeError(error, 'No se pudo abrir el ciclo.'));
    } finally {
      setAbriendo(false);
      data.recargar();
    }
  }

  async function confirmar(): Promise<void> {
    const cicloId = seleccionadoId;
    if (cicloId === null) {
      return;
    }
    const actual = dialogo;
    await ejecutar(async () => {
      if (actual.tipo === 'cerrar') {
        await cerrarCiclo(cicloId);
      } else if (actual.tipo === 'quitar') {
        await eliminarCompetencia(cicloId, actual.competencia.id);
        setFormularioCompetencia(null);
      }
      setDialogo({ tipo: 'ninguno' });
    });
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const cargandoDetalle = seleccionadoId !== null && detalle.detalle === null && detalle.errorDetalle === '';
  const ciclo = detalle.detalle;

  return (
    <>
      <section aria-labelledby="ciclos-list-title" className="clientes-list-panel evaluacion-desempeno-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">CICLOS</div>
            <h2 id="ciclos-list-title">Ciclos de evaluación</h2>
          </div>
          <button
            aria-describedby={puedeEscribir ? undefined : 'ciclos-nuevo-solo-rrhh'}
            className="primary-button"
            disabled={!puedeEscribir}
            onClick={() => setModo('nuevo')}
            type="button"
          >
            Nuevo ciclo
          </button>
        </div>
        {!puedeEscribir && <p className="evaluacion-desempeno-aviso" id="ciclos-nuevo-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        <CiclosFiltros busqueda={busqueda} cambiarBusqueda={setBusqueda} cambiarEstado={cambiarEstado} estado={filtros.estado} />
        <CiclosTabla
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
      {modo === 'nuevo' && puedeEscribir && <CicloFormulario cancelar={() => setModo('cerrado')} ciclo={null} guardar={guardarCiclo} key="nuevo" />}
      {modo === 'editar' && puedeEscribir && ciclo !== null && (
        <CicloFormulario cancelar={() => setModo('cerrado')} ciclo={ciclo} guardar={guardarCiclo} key={ciclo.id} />
      )}
      <CicloDetallePanel
        abrir={() => void abrir()}
        abriendo={abriendo}
        cargando={cargandoDetalle}
        cerrar={() => setDialogo({ tipo: 'cerrar' })}
        ciclo={ciclo}
        editar={() => setModo('editar')}
        error={detalle.errorDetalle}
        errorAccion={errorAccion}
        puedeEscribir={puedeEscribir}
        tituloRef={tituloDetalleRef}
      >
        {ciclo !== null && (
          <>
            <CompetenciasSeccion
              cambiarFormulario={setFormularioCompetencia}
              ciclo={ciclo}
              formulario={formularioCompetencia}
              guardar={guardarCompetencia}
              pedirEliminar={(competencia) => setDialogo({ tipo: 'quitar', competencia })}
              puedeEscribir={puedeEscribir}
            />
            {ciclo.estado !== 'Configuracion' && <ResultadosCiclo error={detalle.errorResultados} resultados={detalle.resultados} />}
          </>
        )}
      </CicloDetallePanel>
      {dialogo.tipo === 'cerrar' && puedeEscribir && ciclo !== null && (
        <DialogoConfirmacion
          advertencia="El ciclo cerrado no admite cambios y las evaluaciones en borrador quedan congeladas."
          cerrar={cerrarDialogo}
          confirmar={confirmar}
          descripcion={`Se cerrará el ciclo ${ciclo.codigo} y se cerrarán sus evaluaciones enviadas.`}
          eyebrow="CICLOS"
          mensajeFallo="No se pudo cerrar el ciclo."
          textoConfirmar="Cerrar ciclo"
          textoEnviando="Cerrando…"
          titulo="Cerrar ciclo"
        />
      )}
      {dialogo.tipo === 'quitar' && puedeEscribir && (
        <DialogoConfirmacion
          advertencia="La competencia dejará de contar en la suma de pesos."
          cerrar={cerrarDialogo}
          confirmar={confirmar}
          descripcion={`Se quitará la competencia ${dialogo.competencia.nombre} del ciclo.`}
          eyebrow="CICLOS"
          mensajeFallo="No se pudo quitar la competencia."
          textoConfirmar="Quitar competencia"
          textoEnviando="Quitando…"
          titulo="Quitar competencia"
        />
      )}
    </>
  );
}
