import { act, cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, useLocation } from 'react-router';
import { expect, vi } from 'vitest';
import { NotificacionesAlertasPage } from './NotificacionesAlertasPage';
import type { Alerta, ResumenAlertas } from './tipos';

export const alertasMock: Alerta[] = [
  { clave: 'reclamo-vencido:3', tipo: 'ReclamoVencido', severidad: 'Alta', mensaje: 'El reclamo REC-DEV-003 de Gamma Manufactura superó su plazo de atención.', modulo: '09', registroId: 3, ruta: '/modulos/09-reclamos-postventa', fechaReferencia: '2026-10-14T00:00:00Z', descartada: false },
  { clave: 'orden-atrasada:2', tipo: 'OrdenAbastecimientoAtrasada', severidad: 'Alta', mensaje: 'La orden OA-DEV-002 de Andes Suministros S.A.C. está atrasada desde el 10/10/2026.', modulo: '15', registroId: 2, ruta: '/modulos/15-ordenes-abastecimiento', fechaReferencia: '2026-10-10T00:00:00Z', descartada: false },
  { clave: 'contrato-excedido:4', tipo: 'ContratoExcedido', severidad: 'Alta', mensaje: 'El contrato CC-DEV-004 de Delta Logística va al 125.00 % de su monto comprometido.', modulo: '05', registroId: 4, ruta: '/modulos/05-contratos-comerciales', fechaReferencia: '2026-10-09T00:00:00Z', descartada: false },
  { clave: 'almacen-lleno:1', tipo: 'AlmacenCasiLleno', severidad: 'Media', mensaje: 'El almacén ALM-LIM-01 tiene asignado el 90.00 % de su capacidad.', modulo: '23', registroId: 1, ruta: '/modulos/23-almacenes', fechaReferencia: '2026-10-15T00:00:00Z', descartada: false },
  { clave: 'contrato-por-vencer:1', tipo: 'ContratoPorVencer', severidad: 'Media', mensaje: 'El contrato CC-DEV-001 de Acme Industrial termina el 30/10/2026.', modulo: '05', registroId: 1, ruta: '/modulos/05-contratos-comerciales', fechaReferencia: '2026-10-30T00:00:00Z', descartada: false },
  { clave: 'cotizacion-vencida:2', tipo: 'CotizacionVencida', severidad: 'Baja', mensaje: 'La cotización COT-DEV-002 de Beta Comercio venció sin respuesta.', modulo: '03', registroId: 2, ruta: '/modulos/03-cotizaciones', fechaReferencia: '2026-10-12T00:00:00Z', descartada: false },
  { clave: 'solicitud-pendiente:1', tipo: 'SolicitudPendienteAntigua', severidad: 'Baja', mensaje: 'La solicitud SC-000001 lleva 8 días pendiente.', modulo: '13', registroId: 1, ruta: '/modulos/13-solicitudes-compra', fechaReferencia: '2026-10-07T00:00:00Z', descartada: false },
  { clave: 'reclamo-vencido:8', tipo: 'ReclamoVencido', severidad: 'Alta', mensaje: 'El reclamo REC-DEV-008 de Acme Industrial superó su plazo de atención.', modulo: '09', registroId: 8, ruta: '/modulos/09-reclamos-postventa', fechaReferencia: '2026-10-06T00:00:00Z', descartada: true },
];

const resumenMock: ResumenAlertas = {
  total: 7,
  altas: 3,
  medias: 2,
  bajas: 2,
  descartadas: 1,
  porTipo: [
    { tipo: 'ReclamoVencido', alertas: 1 },
    { tipo: 'OrdenAbastecimientoAtrasada', alertas: 1 },
    { tipo: 'ContratoExcedido', alertas: 1 },
    { tipo: 'AlmacenCasiLleno', alertas: 1 },
    { tipo: 'ContratoPorVencer', alertas: 1 },
    { tipo: 'CotizacionVencida', alertas: 1 },
    { tipo: 'SolicitudPendienteAntigua', alertas: 1 },
  ],
};

interface OpcionesApi {
  listaVacia?: boolean;
  errorLista?: boolean;
  errorResumen?: boolean;
  errorDescarte?: 404 | 409;
  errorRestauracion?: 404;
}

export function prepararApiAlertas(opciones: OpcionesApi = {}) {
  const llamadas: { url: string; init: RequestInit }[] = [];
  const alertas = alertasMock.map((alerta) => ({ ...alerta }));
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });
    if (parsed.pathname.endsWith('/alertas/resumen')) {
      if (opciones.errorResumen) {
        return Response.json({ StatusCode: 500, Message: 'Falló el resumen de alertas.' }, { status: 500 });
      }
      return Response.json(resumenMock);
    }
    if (parsed.pathname.endsWith('/alertas') && metodo === 'GET') {
      if (opciones.errorLista) {
        return Response.json({ StatusCode: 500, Message: 'Falló el listado de alertas.' }, { status: 500 });
      }
      const tipo = parsed.searchParams.get('tipo');
      const severidad = parsed.searchParams.get('severidad')?.toLowerCase();
      const incluirDescartadas = parsed.searchParams.get('incluirDescartadas') === 'true';
      const items = opciones.listaVacia ? [] : alertas.filter((alerta) => (
        (incluirDescartadas || !alerta.descartada)
        && (tipo === null || alerta.tipo === tipo)
        && (severidad === undefined || alerta.severidad.toLowerCase() === severidad)
      ));
      return Response.json({ items, total: items.length, pagina: Number(parsed.searchParams.get('pagina') ?? 1), tamanoPagina: 20 });
    }
    if (parsed.pathname.endsWith('/alertas/descartar') && metodo === 'POST') {
      const body = JSON.parse(String(init.body ?? '{}')) as { clave: string };
      if (opciones.errorDescarte !== undefined) {
        const status = opciones.errorDescarte;
        const message = status === 404 ? 'La alerta reclamo-vencido:3 no existe o ya no está activa.' : 'La alerta reclamo-vencido:3 ya está descartada.';
        return Response.json({ StatusCode: status, Message: message }, { status });
      }
      const alerta = alertas.find((item) => item.clave === body.clave);
      if (alerta) {
        alerta.descartada = true;
      }
      return new Response(null, { status: 204 });
    }
    if (parsed.pathname.endsWith('/alertas/restaurar') && metodo === 'POST') {
      const body = JSON.parse(String(init.body ?? '{}')) as { clave: string };
      if (opciones.errorRestauracion) {
        return Response.json({ StatusCode: 404, Message: 'La alerta reclamo-vencido:8 no está descartada.' }, { status: 404 });
      }
      const alerta = alertas.find((item) => item.clave === body.clave);
      if (alerta) {
        alerta.descartada = false;
      }
      return new Response(null, { status: 204 });
    }
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { llamadas, fetchMock };
}

function LocationProbe() {
  const location = useLocation();
  return <output data-testid="ruta-actual">{location.pathname}{location.search}</output>;
}

export function renderAlertas(path = '/modulos/78-notificaciones-alertas') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <LocationProbe />
      <NotificacionesAlertasPage />
    </MemoryRouter>,
  );
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof prepararApiAlertas>['fetchMock']): Promise<void> {
  await waitFor(() => {
    expect(screen.queryByText('Cargando alertas…')).not.toBeInTheDocument();
    expect(screen.queryByText('Cargando resumen de alertas…')).not.toBeInTheDocument();
    expect(fetchMock.mock.calls.filter((llamada) => String(llamada[0]).includes('/alertas?'))).toHaveLength(1);
    expect(fetchMock.mock.calls.filter((llamada) => String(llamada[0]).endsWith('/alertas/resumen'))).toHaveLength(1);
  });
}

export async function montarAlertasCargadas(fetchMock: ReturnType<typeof prepararApiAlertas>['fetchMock'], path?: string): Promise<void> {
  vi.useFakeTimers();
  renderAlertas(path);
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
    await vi.advanceTimersByTimeAsync(1);
  });
  vi.useRealTimers();
  await esperarCargaInicial(fetchMock);
}

export function limpiarAlertas(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
}
