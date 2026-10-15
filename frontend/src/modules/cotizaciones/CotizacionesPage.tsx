import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import {
  actualizarCotizacion,
  aceptarCotizacion,
  crearCotizacion,
  eliminarCotizacion,
  rechazarCotizacion,
} from './api';
import { CotizacionDecisionDialog } from './CotizacionDecisionDialog';
import { CotizacionDetallePanel } from './CotizacionDetallePanel';
import { CotizacionFormulario } from './CotizacionFormulario';
import { CotizacionesFiltros } from './CotizacionesFiltros';
import { CotizacionesResumen } from './CotizacionesResumen';
import { CotizacionesTabla } from './CotizacionesTabla';
import { useCotizacionesData } from './useCotizacionesData';
import type {
  CotizacionListado,
  FiltrosCotizacion,
  GuardarCotizacionRequest,
} from './tipos';

const filtrosIniciales: FiltrosCotizacion = { pagina: 1, tamanoPagina: 10 };

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function CotizacionesPage() {
  const [filtros, setFiltros] = useState(filtrosIniciales);
  const [busqueda, setBusqueda] = useState('');
  const [errorFecha, setErrorFecha] = useState('');
  const [seleccionadaId, setSeleccionadaId] = useState<number | null>(null);
  const [formulario, setFormulario] = useState<'crear' | 'editar' | null>(null);
  const [decision, setDecision] = useState<'aceptar' | 'rechazar' | null>(null);
  const [dialogoBaja, setDialogoBaja] = useState(false);
  const [errorBaja, setErrorBaja] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const botonAccionRef = useRef<HTMLButtonElement>(null);
  const filtrosRef = useRef(filtros);
  filtrosRef.current = filtros;
  const data = useCotizacionesData(filtros, seleccionadaId);

  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      if ((filtrosRef.current.busqueda ?? '') === busqueda) {
        return;
      }
      setFiltros((actuales) => {
        if ((actuales.busqueda ?? '') === busqueda) {
          return actuales;
        }
        return { ...actuales, busqueda, pagina: 1 };
      });
      setSeleccionadaId(null);
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarFiltros(cambios: Partial<FiltrosCotizacion>): void {
    const siguientes = { ...filtros, ...cambios, pagina: 1 };
    const desde = siguientes.fechaDesde ?? '';
    const hasta = siguientes.fechaHasta ?? '';
    if (desde && hasta && desde > hasta) {
      setErrorFecha('La fecha desde no puede ser posterior a la fecha hasta.');
      return;
    }
    setErrorFecha('');
    setFiltros(siguientes);
    setSeleccionadaId(null);
  }

  function seleccionar(item: CotizacionListado): void {
    setSeleccionadaId(item.id);
    setFormulario(null);
  }

  function cerrarDecision(): void {
    setDecision(null);
    window.requestAnimationFrame(() => botonAccionRef.current?.focus());
  }

  function abrirDecision(
    tipo: 'aceptar' | 'rechazar',
    boton: HTMLButtonElement,
  ): void {
    botonAccionRef.current = boton;
    setDecision(tipo);
  }

  function cerrarBaja(): void {
    setDialogoBaja(false);
    setErrorBaja('');
    window.requestAnimationFrame(() => botonAccionRef.current?.focus());
  }

  async function guardar(request: GuardarCotizacionRequest): Promise<void> {
    if (formulario === 'editar' && data.detalle !== null) {
      await actualizarCotizacion(data.detalle.id, request);
    } else {
      const creada = await crearCotizacion(request);
      setSeleccionadaId(creada.id);
    }
    setFormulario(null);
    data.recargar();
  }

  async function aceptar(): Promise<void> {
    if (data.detalle === null) {
      return;
    }
    await aceptarCotizacion(data.detalle.id);
    setDecision(null);
    data.recargar();
  }

  async function rechazar(motivo: string): Promise<void> {
    if (data.detalle === null) {
      return;
    }
    await rechazarCotizacion(data.detalle.id, { motivo });
    setDecision(null);
    data.recargar();
  }

  async function eliminar(): Promise<void> {
    if (data.detalle === null) {
      return;
    }
    setEliminando(true);
    try {
      await eliminarCotizacion(data.detalle.id);
      cerrarBaja();
      setSeleccionadaId(null);
      data.recargar();
    } catch (error: unknown) {
      setErrorBaja(mensaje(error, 'No se pudo eliminar la cotización.'));
    } finally {
      setEliminando(false);
    }
  }

  return (
    <PaginaModulo
      className="solicitudes-page cotizaciones-page"
      modulo="03-cotizaciones"
    >
      <p className="module-intro">
        Prepara cotizaciones con descuentos por línea y gestiona su aceptación o
        rechazo.
      </p>
      <CotizacionesResumen datos={data.resumen} error={data.errorResumen} />
      {data.errorCatalogos && (
        <p className="solicitudes-error" role="alert">
          {data.errorCatalogos}
        </p>
      )}
      {formulario ? (
        <CotizacionFormulario
          cotizacion={formulario === 'editar' ? data.detalle : null}
          clientes={data.clientes}
          productos={data.productos}
          cancelar={() => setFormulario(null)}
          guardar={guardar}
        />
      ) : (
        <div className="solicitudes-work-area cotizaciones-work-area">
          <section
            aria-labelledby="cotizaciones-list-title"
            className="solicitudes-panel"
          >
            <div className="solicitudes-panel-heading">
              <div>
                <span className="page-eyebrow">COMERCIAL</span>
                <h2 id="cotizaciones-list-title">Cotizaciones</h2>
              </div>
              <button
                className="primary-button"
                onClick={() => setFormulario('crear')}
                type="button"
              >
                Nueva cotización
              </button>
            </div>
            <CotizacionesFiltros
              busqueda={busqueda}
              filtros={filtros}
              clientes={data.clientes}
              errorFecha={errorFecha}
              cambiarBusqueda={setBusqueda}
              cambiarFiltros={cambiarFiltros}
            />
            <CotizacionesTabla
              lista={data.lista}
              error={data.errorLista}
              seleccionadaId={seleccionadaId}
              seleccionar={seleccionar}
              pagina={filtros.pagina ?? 1}
              cambiarPagina={(pagina) =>
                setFiltros((actuales) => ({ ...actuales, pagina }))
              }
            />
          </section>
          <CotizacionDetallePanel
            cotizacion={data.detalle}
            cargando={data.cargandoDetalle}
            editar={() => setFormulario('editar')}
            aceptar={(boton) => abrirDecision('aceptar', boton)}
            rechazar={(boton) => abrirDecision('rechazar', boton)}
            eliminar={(boton) => {
              botonAccionRef.current = boton;
              setDialogoBaja(true);
            }}
          />
        </div>
      )}
      {data.errorDetalle && (
        <p className="solicitudes-error" role="alert">
          {data.errorDetalle}
        </p>
      )}
      {decision && data.detalle && (
        <CotizacionDecisionDialog
          numero={data.detalle.numero}
          tipo={decision}
          cerrar={cerrarDecision}
          confirmarAceptacion={aceptar}
          confirmarRechazo={rechazar}
        />
      )}
      {dialogoBaja && data.detalle && (
        <DialogoBaja
          entidad="cotización"
          nombre={data.detalle.numero}
          cerrar={cerrarBaja}
          confirmar={() => void eliminar()}
          eliminando={eliminando}
          error={errorBaja}
        />
      )}
    </PaginaModulo>
  );
}
