import { useEffect, useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router';
import { ApiError } from '../../api/client';
import { PaginaModulo } from '../../components/PaginaModulo';
import { AlertaConfirmacionDialog } from './AlertaConfirmacionDialog';
import { AlertaDetallePanel } from './AlertaDetallePanel';
import { AlertasFiltros } from './AlertasFiltros';
import { AlertasResumen } from './AlertasResumen';
import { AlertasTabla } from './AlertasTabla';
import { descartarAlerta, restaurarAlerta } from './api';
import { useAlertasData } from './useAlertasData';
import type { Alerta, FiltrosAlerta, FiltroSeveridad, TipoAlerta } from './tipos';
import './notificaciones-alertas.css';

const tiposValidos: TipoAlerta[] = [
  'ReclamoVencido', 'OrdenAbastecimientoAtrasada', 'ContratoExcedido',
  'AlmacenCasiLleno', 'ContratoPorVencer', 'CotizacionVencida', 'SolicitudPendienteAntigua',
];
const severidadesValidas: FiltroSeveridad[] = ['', 'alta', 'media', 'baja'];

function leerFiltros(parametros: URLSearchParams): FiltrosAlerta {
  const tipo = parametros.get('tipo') ?? '';
  const severidad = (parametros.get('severidad') ?? '').toLowerCase() as FiltroSeveridad;
  const pagina = Number(parametros.get('pagina') ?? 1);
  return {
    tipo: tiposValidos.includes(tipo as TipoAlerta) ? tipo as TipoAlerta : '',
    severidad: severidadesValidas.includes(severidad) ? severidad : '',
    incluirDescartadas: parametros.get('incluirDescartadas') === 'true',
    pagina: Number.isInteger(pagina) && pagina > 0 ? pagina : 1,
    tamanoPagina: 20,
  };
}

function mensajeError(error: unknown): string {
  return error instanceof ApiError ? error.message : 'No se pudo completar la acción sobre la alerta.';
}

export function NotificacionesAlertasPage() {
  const [parametros, establecerParametros] = useSearchParams();
  const filtros = useMemo(() => leerFiltros(parametros), [parametros]);
  const datos = useAlertasData(filtros);
  const [seleccionadaClave, setSeleccionadaClave] = useState<string | null>(null);
  const [confirmacion, setConfirmacion] = useState<Alerta | null>(null);
  const [errorAccion, setErrorAccion] = useState('');
  const [procesando, setProcesando] = useState(false);
  const [restaurarFoco, setRestaurarFoco] = useState(false);
  const botonAccionRef = useRef<HTMLButtonElement | null>(null);
  const alertas = datos.lista?.items ?? [];
  const seleccionada = alertas.find((alerta) => alerta.clave === seleccionadaClave) ?? null;

  useEffect(() => {
    if (restaurarFoco && confirmacion === null) {
      botonAccionRef.current?.focus();
      setRestaurarFoco(false);
    }
  }, [confirmacion, restaurarFoco]);

  function cambiarFiltros(cambios: Partial<FiltrosAlerta>): void {
    const nuevos = { ...filtros, ...cambios, pagina: cambios.pagina ?? 1 };
    const consulta = new URLSearchParams(parametros);
    if (nuevos.tipo === '') {
      consulta.delete('tipo');
    } else {
      consulta.set('tipo', nuevos.tipo);
    }
    if (nuevos.severidad === '') {
      consulta.delete('severidad');
    } else {
      consulta.set('severidad', nuevos.severidad);
    }
    if (nuevos.incluirDescartadas) {
      consulta.set('incluirDescartadas', 'true');
    } else {
      consulta.delete('incluirDescartadas');
    }
    if (nuevos.pagina === 1) {
      consulta.delete('pagina');
    } else {
      consulta.set('pagina', String(nuevos.pagina));
    }
    establecerParametros(consulta);
    setSeleccionadaClave(null);
  }

  function abrirConfirmacion(alerta: Alerta, boton: HTMLButtonElement): void {
    botonAccionRef.current = boton;
    setErrorAccion('');
    setConfirmacion(alerta);
    setSeleccionadaClave(alerta.clave);
  }

  function cerrarConfirmacion(): void {
    setConfirmacion(null);
    setErrorAccion('');
    setRestaurarFoco(true);
  }

  async function confirmarAccion(): Promise<void> {
    if (confirmacion === null) {
      return;
    }
    setProcesando(true);
    setErrorAccion('');
    try {
      if (confirmacion.descartada) {
        await restaurarAlerta(confirmacion.clave);
      } else {
        await descartarAlerta(confirmacion.clave);
      }
      setConfirmacion(null);
      setSeleccionadaClave(null);
      setRestaurarFoco(true);
      datos.recargar();
      window.dispatchEvent(new Event('alertas-resumen-refrescar'));
    } catch (error: unknown) {
      setErrorAccion(mensajeError(error));
    } finally {
      setProcesando(false);
    }
  }

  return (
    <PaginaModulo className="notificaciones-alertas-page" modulo="78-notificaciones-alertas">
      <p className="module-intro">Consulta y administra las alertas que requieren atención en los módulos del ERP.</p>
      <AlertasResumen datos={datos.resumen} error={datos.errorResumen} />
      <div className="alertas-work-area">
        <section aria-labelledby="alertas-list-title" className="clientes-list-panel alertas-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <span className="page-eyebrow">GOBIERNO Y AUDITORÍA</span>
              <h2 id="alertas-list-title">Centro de alertas</h2>
            </div>
          </div>
          <AlertasFiltros cambiar={cambiarFiltros} filtros={filtros} />
          <AlertasTabla
            alertas={alertas}
            actuar={abrirConfirmacion}
            cambiarPagina={(pagina) => cambiarFiltros({ pagina })}
            cargando={datos.lista === null && datos.errorLista === ''}
            error={datos.errorLista}
            pagina={filtros.pagina}
            seleccionar={(alerta) => setSeleccionadaClave(alerta.clave)}
            seleccionadaClave={seleccionadaClave}
            total={datos.lista?.total ?? 0}
          />
        </section>
        <AlertaDetallePanel alerta={seleccionada} />
      </div>
      {confirmacion !== null && (
        <AlertaConfirmacionDialog
          alerta={confirmacion}
          cerrar={cerrarConfirmacion}
          confirmar={() => void confirmarAccion()}
          error={errorAccion}
          procesando={procesando}
        />
      )}
    </PaginaModulo>
  );
}
