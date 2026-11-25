import { useCallback, useEffect, useRef, useState } from 'react';
import { aprobarPlanilla, calcularPlanilla, crearPlanilla, pagarPlanilla } from './api';
import { DialogoBoleta } from './DialogoBoleta';
import { DialogoConfirmacion } from './DialogoConfirmacion';
import { DialogoNuevaPlanilla } from './DialogoNuevaPlanilla';
import { DialogoPago } from './DialogoPago';
import { PanelParametros } from './PanelParametros';
import { PlanillaDetallePanel } from './PlanillaDetallePanel';
import { PlanillaEmpleadosTabla } from './PlanillaEmpleadosTabla';
import { PlanillasFiltros } from './PlanillasFiltros';
import { PlanillasResumen } from './PlanillasResumen';
import { PlanillasTabla } from './PlanillasTabla';
import { esConflicto, interpretarAnioFiltro, mensajeError, TEXTO_SOLO_RRHH } from './presentacion';
import type { CrearPlanillaRequest, EstadoPlanilla, FiltrosDetalles, FiltrosPlanillas, TipoPlanilla } from './tipos';
import { usePlanillaDetalle, usePlanillasData } from './usePlanillasData';

const TAMANO_PAGINA = 10;
const FILTROS_INICIALES: FiltrosPlanillas = { anio: null, tipo: '', estado: '', pagina: 1, tamanoPagina: TAMANO_PAGINA };
const DETALLES_INICIALES: FiltrosDetalles = { busqueda: '', soloIncidencias: false, pagina: 1, tamanoPagina: TAMANO_PAGINA };

type Dialogo = 'ninguno' | 'nueva' | 'aprobar' | 'pago' | 'boleta';

export function PlanillasVista({ puedeEscribir }: { puedeEscribir: boolean }) {
  const [anioTexto, setAnioTexto] = useState('');
  const [filtros, setFiltros] = useState<FiltrosPlanillas>(FILTROS_INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [filtrosDetalle, setFiltrosDetalle] = useState<FiltrosDetalles>(DETALLES_INICIALES);
  const [busquedaDetalle, setBusquedaDetalle] = useState('');
  const [dialogo, setDialogo] = useState<Dialogo>('ninguno');
  const [boletaId, setBoletaId] = useState<number | null>(null);
  const [parametrosAbiertos, setParametrosAbiertos] = useState(false);
  const [calculando, setCalculando] = useState(false);
  const [resultadoCalculo, setResultadoCalculo] = useState('');
  const [errorAccion, setErrorAccion] = useState('');
  const tituloDetalleRef = useRef<HTMLHeadingElement>(null);
  const data = usePlanillasData(filtros);
  const detalle = usePlanillaDetalle(seleccionadoId, filtrosDetalle, data.revision);

  // La búsqueda por empleado se aplica 300 ms después del último cambio y vuelve a la página 1 solo si el texto cambia.
  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      setFiltrosDetalle((actual) => {
        const termino = busquedaDetalle.trim();
        if (actual.busqueda === termino) {
          return actual;
        }
        return { ...actual, busqueda: termino, pagina: 1 };
      });
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busquedaDetalle]);

  function cambiarAnio(texto: string): void {
    setAnioTexto(texto);
    const anio = interpretarAnioFiltro(texto);
    if (anio !== undefined) {
      setFiltros((actual) => ({ ...actual, anio, pagina: 1 }));
    }
  }

  function cambiarTipo(tipo: '' | TipoPlanilla): void {
    setFiltros((actual) => ({ ...actual, tipo, pagina: 1 }));
  }

  function cambiarEstado(estado: '' | EstadoPlanilla): void {
    setFiltros((actual) => ({ ...actual, estado, pagina: 1 }));
  }

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setFiltrosDetalle(DETALLES_INICIALES);
    setBusquedaDetalle('');
    setResultadoCalculo('');
    setErrorAccion('');
    setParametrosAbiertos(false);
  }

  const cerrarDialogo = useCallback(() => {
    setDialogo('ninguno');
    window.requestAnimationFrame(() => tituloDetalleRef.current?.focus());
  }, []);

  async function crear(request: CrearPlanillaRequest): Promise<void> {
    try {
      const resultado = await crearPlanilla(request);
      seleccionar(resultado.id);
      setDialogo('ninguno');
      data.recargar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  async function calcular(): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    setCalculando(true);
    setResultadoCalculo('');
    setErrorAccion('');
    try {
      const resultado = await calcularPlanilla(seleccionadoId);
      setResultadoCalculo(`Cálculo terminado: ${resultado.empleados} empleados y ${resultado.incidencias} incidencias.`);
    } catch (error: unknown) {
      setErrorAccion(mensajeError(error, 'No se pudo calcular la planilla.'));
    } finally {
      setCalculando(false);
      data.recargar();
    }
  }

  // Un 409 indica que la planilla cambió por otra vía: se pone al día la pantalla y el mensaje se muestra dentro del diálogo.
  async function aprobar(): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    try {
      await aprobarPlanilla(seleccionadoId);
      setDialogo('ninguno');
      setResultadoCalculo('');
      data.recargar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  async function pagar(fechaPago: string): Promise<void> {
    if (seleccionadoId === null) {
      return;
    }
    try {
      await pagarPlanilla(seleccionadoId, { fechaPago });
      setDialogo('ninguno');
      data.recargar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const paginasDetalle = Math.max(1, Math.ceil((detalle.empleados?.total ?? 0) / TAMANO_PAGINA));
  if (detalle.empleados !== null && detalle.empleados.pagina === filtrosDetalle.pagina && filtrosDetalle.pagina > paginasDetalle) {
    setFiltrosDetalle({ ...filtrosDetalle, pagina: paginasDetalle });
  }
  const cargandoDetalle = seleccionadoId !== null && detalle.detalle === null && detalle.errorDetalle === '';
  const numeroSeleccionado = detalle.detalle?.numero ?? '';

  return (
    <>
      <PlanillasResumen datos={data.resumen} error={data.errorResumen} />
      <section aria-labelledby="planillas-list-title" className="clientes-list-panel planillas-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">PLANILLAS</div>
            <h2 id="planillas-list-title">Todas las planillas</h2>
          </div>
          <div className="planillas-heading-actions">
            <button aria-expanded={parametrosAbiertos} className="secondary-button" onClick={() => setParametrosAbiertos((actual) => !actual)} type="button">
              Parámetros
            </button>
            <button
              aria-describedby={puedeEscribir ? undefined : 'planillas-nueva-solo-rrhh'}
              className="primary-button"
              disabled={!puedeEscribir}
              onClick={() => setDialogo('nueva')}
              type="button"
            >
              Nueva planilla
            </button>
          </div>
        </div>
        {!puedeEscribir && <p className="planillas-aviso" id="planillas-nueva-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        <PlanillasFiltros
          anioTexto={anioTexto}
          cambiarAnio={cambiarAnio}
          cambiarEstado={cambiarEstado}
          cambiarTipo={cambiarTipo}
          estado={filtros.estado}
          tipo={filtros.tipo}
        />
        <PlanillasTabla
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
      {parametrosAbiertos && <PanelParametros cerrar={() => setParametrosAbiertos(false)} />}
      <PlanillaDetallePanel
        aprobar={() => setDialogo('aprobar')}
        calcular={() => void calcular()}
        calculando={calculando}
        cargando={cargandoDetalle}
        error={detalle.errorDetalle}
        errorAccion={errorAccion}
        pagar={() => setDialogo('pago')}
        planilla={detalle.detalle}
        puedeEscribir={puedeEscribir}
        resultadoCalculo={resultadoCalculo}
        tituloRef={tituloDetalleRef}
      >
        <PlanillaEmpleadosTabla
          busqueda={busquedaDetalle}
          cambiarBusqueda={setBusquedaDetalle}
          cambiarPagina={(pagina) => setFiltrosDetalle((actual) => ({ ...actual, pagina }))}
          cambiarSoloIncidencias={(soloIncidencias) => setFiltrosDetalle((actual) => ({ ...actual, soloIncidencias, pagina: 1 }))}
          cargando={detalle.empleados === null}
          error={detalle.errorEmpleados}
          items={detalle.empleados?.items ?? []}
          pagina={filtrosDetalle.pagina}
          soloIncidencias={filtrosDetalle.soloIncidencias}
          totalPaginas={paginasDetalle}
          verBoleta={(empleado) => {
            setBoletaId(empleado.id);
            setDialogo('boleta');
          }}
        />
      </PlanillaDetallePanel>
      {dialogo === 'nueva' && puedeEscribir && <DialogoNuevaPlanilla cerrar={cerrarDialogo} crear={crear} />}
      {dialogo === 'aprobar' && puedeEscribir && (
        <DialogoConfirmacion
          advertencia="La planilla aprobada no se puede modificar."
          cerrar={cerrarDialogo}
          confirmar={aprobar}
          descripcion={`Se aprobará la planilla ${numeroSeleccionado} y se numerarán las boletas de pago.`}
          eyebrow="PLANILLAS"
          mensajeFallo="No se pudo aprobar la planilla."
          textoConfirmar="Aprobar planilla"
          textoEnviando="Aprobando…"
          titulo="Aprobar planilla"
        />
      )}
      {dialogo === 'pago' && puedeEscribir && <DialogoPago cerrar={cerrarDialogo} numero={numeroSeleccionado} pagar={pagar} />}
      {dialogo === 'boleta' && boletaId !== null && <DialogoBoleta cerrar={cerrarDialogo} detalleId={boletaId} />}
    </>
  );
}
