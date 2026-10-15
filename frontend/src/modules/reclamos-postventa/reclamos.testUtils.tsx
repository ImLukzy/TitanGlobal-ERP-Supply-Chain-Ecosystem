import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { ReclamosPostventaPage } from './ReclamosPostventaPage';
import type { ReclamoDetalle, ReclamoListado } from './tipos';

export const reclamosMock: ReclamoDetalle[] = [
  { id: 1, numero: 'REC-DEV-001', fecha: '2026-01-01T00:00:00Z', clienteId: 1, clienteNombre: 'Acme Industrial', ordenCompraId: 1, numeroOrden: 'OC-001', productoNombre: 'Laptop Pro', tipo: 'Defecto', prioridad: 'Alta', estado: 'Abierto', fechaLimite: '2026-01-03T00:00:00Z', vencido: true, descripcion: 'La laptop presenta una falla de encendido.', resolucion: null, motivoRechazo: null, fechaInicio: null, fechaResolucion: null, fechaCierre: null, atendidoATiempo: null },
  { id: 2, numero: 'REC-DEV-002', fecha: '2026-10-12T00:00:00Z', clienteId: 1, clienteNombre: 'Acme Industrial', ordenCompraId: 2, numeroOrden: 'OC-002', productoNombre: 'Mouse Óptico', tipo: 'Faltante', prioridad: 'Media', estado: 'EnProceso', fechaLimite: '2026-10-17T00:00:00Z', vencido: false, descripcion: 'Falta un accesorio en el envío.', resolucion: null, motivoRechazo: null, fechaInicio: '2026-10-13T00:00:00Z', fechaResolucion: null, fechaCierre: null, atendidoATiempo: null },
  { id: 3, numero: 'REC-DEV-003', fecha: '2026-10-10T00:00:00Z', clienteId: 2, clienteNombre: 'Beta Comercio', ordenCompraId: 5, numeroOrden: 'OC-005', productoNombre: null, tipo: 'Retraso', prioridad: 'Baja', estado: 'Resuelto', fechaLimite: '2026-10-20T00:00:00Z', vencido: false, descripcion: 'El pedido demoró en llegar.', resolucion: 'Se coordinó una nueva entrega.', motivoRechazo: null, fechaInicio: '2026-10-11T00:00:00Z', fechaResolucion: '2026-10-12T00:00:00Z', fechaCierre: null, atendidoATiempo: true },
  { id: 4, numero: 'REC-DEV-004', fecha: '2026-09-20T00:00:00Z', clienteId: 3, clienteNombre: 'Gamma Manufactura', ordenCompraId: 7, numeroOrden: 'OC-007', productoNombre: 'Silla Ejecutiva', tipo: 'Facturacion', prioridad: 'Media', estado: 'Cerrado', fechaLimite: '2026-09-25T00:00:00Z', vencido: false, descripcion: 'Consulta por la factura.', resolucion: 'Factura corregida.', motivoRechazo: null, fechaInicio: '2026-09-21T00:00:00Z', fechaResolucion: '2026-09-26T00:00:00Z', fechaCierre: '2026-09-27T00:00:00Z', atendidoATiempo: false },
  { id: 5, numero: 'REC-DEV-005', fecha: '2026-10-01T00:00:00Z', clienteId: 4, clienteNombre: 'Delta Logística', ordenCompraId: 8, numeroOrden: 'OC-008', productoNombre: 'Monitor 27', tipo: 'Otro', prioridad: 'Baja', estado: 'Rechazado', fechaLimite: '2026-10-11T00:00:00Z', vencido: false, descripcion: 'Solicitud fuera de cobertura.', resolucion: null, motivoRechazo: 'El plazo de garantía terminó.', fechaInicio: null, fechaResolucion: null, fechaCierre: '2026-10-02T00:00:00Z', atendidoATiempo: null },
];

export function listado(reclamo: ReclamoDetalle): ReclamoListado {
  return {
    id: reclamo.id,
    numero: reclamo.numero,
    fecha: reclamo.fecha,
    clienteId: reclamo.clienteId,
    clienteNombre: reclamo.clienteNombre,
    ordenCompraId: reclamo.ordenCompraId,
    numeroOrden: reclamo.numeroOrden,
    productoNombre: reclamo.productoNombre,
    tipo: reclamo.tipo,
    prioridad: reclamo.prioridad,
    estado: reclamo.estado,
    fechaLimite: reclamo.fechaLimite,
    vencido: reclamo.vencido,
  };
}

export function prepararApi(opciones: { errorLista?: boolean; vacia?: boolean; errorCrear?: boolean; errorAccion?: boolean } = {}) {
  const llamadas: Array<{ url: string; init: RequestInit }> = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(input);
    const parsed = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });
    if (parsed.pathname.endsWith('/reclamos-postventa/resumen')) return Response.json({ total: 5, abiertos: 1, enProceso: 1, resueltos: 1, cerrados: 1, rechazados: 1, vencidos: 1, porTipo: [{ tipo: 'Defecto', reclamos: 1 }, { tipo: 'Faltante', reclamos: 1 }, { tipo: 'Retraso', reclamos: 1 }, { tipo: 'Facturacion', reclamos: 1 }, { tipo: 'Otro', reclamos: 1 }], horasPromedioResolucion: 36, cumplimientoPlazo: 80 });
    if (parsed.pathname.endsWith('/clientes')) return Response.json({ items: [{ id: 1, nombre: 'Acme Industrial' }, { id: 2, nombre: 'Beta Comercio' }, { id: 3, nombre: 'Gamma Manufactura' }, { id: 4, nombre: 'Delta Logística' }] });
    if (parsed.pathname.endsWith('/reclamos-postventa') && metodo === 'GET') {
      if (opciones.errorLista) return Response.json({ StatusCode: 500, Message: 'Falló la bandeja.' }, { status: 500 });
      const items = opciones.vacia ? [] : reclamosMock.map(listado);
      return Response.json({ items, total: items.length, pagina: 1, tamanoPagina: 10 });
    }
    if (parsed.pathname.endsWith('/reclamos-postventa') && metodo === 'POST') {
      if (opciones.errorCrear) return Response.json({ StatusCode: 400, Message: 'El cliente 9999 no existe.' }, { status: 400 });
      return Response.json({ id: 6, numero: 'REC-000006' });
    }
    if (/\/reclamos-postventa\/\d+\/iniciar$/.test(parsed.pathname) && opciones.errorAccion) return Response.json({ StatusCode: 409, Message: 'El reclamo REC-DEV-001 está cerrado.' }, { status: 409 });
    if (/\/reclamos-postventa\/\d+\/(iniciar|resolver|cerrar|rechazar)$/.test(parsed.pathname)) return Response.json(reclamosMock[0]);
    if (metodo === 'PUT') return Response.json({ id: 2, numero: 'REC-DEV-002' });
    const detalle = reclamosMock.find((item) => parsed.pathname.endsWith(`/reclamos-postventa/${item.id}`));
    if (detalle) return Response.json(detalle);
    if (parsed.pathname.endsWith('/ordenes-compra/clientes/1/jerarquia')) return Response.json([{ ordenId: 1, numeroOrden: 'OC-001', fecha: '2026-01-02T00:00:00Z', nombreCliente: 'Acme Industrial', detalles: [{ productoId: 1, nombreProducto: 'Laptop Pro' }, { productoId: 5, nombreProducto: 'Mouse Óptico' }] }]);
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

export function renderPagina(initialEntries = ['/modulos/09-reclamos-postventa']) {
  return render(<MemoryRouter initialEntries={initialEntries}><ReclamosPostventaPage /></MemoryRouter>);
}

export async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await screen.findByRole('button', { name: 'REC-DEV-001' });
  await screen.findByText('80.00 %');
  await screen.findByRole('combobox', { name: 'Cliente' });
  await screen.findByRole('option', { name: 'Acme Industrial' });
  await waitFor(() => expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/reclamos-postventa?')).length).toBe(1));
}

export function limpiar(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
}
