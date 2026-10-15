import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, vi } from 'vitest';
import { PromocionesPage } from './PromocionesPage';

export const promo = { id: 1, codigo: 'LAPTOP-VIP-15', nombre: 'Laptop VIP', tipo: 'Producto', productoId: 1, productoNombre: 'Laptop Pro', montoMinimo: null, porcentajeDescuento: 15, segmentoId: 1, segmentoCodigo: 'VIP', fechaInicio: '2026-01-01T00:00:00Z', fechaFin: '2026-12-31T00:00:00Z', activa: true, vigente: true };
export const promocionCompra = { clienteId: 1, clienteNombre: 'Acme Industrial', segmentoCodigo: 'VIP', fecha: '2026-02-01T00:00:00Z', lineas: [{ productoId: 1, productoNombre: 'Laptop Pro', cantidad: 2, precioUnitario: 2500, subtotal: 5000, promocion: { id: 1, codigo: 'LAPTOP-VIP-15', nombre: 'Laptop VIP', porcentajeDescuento: 15 }, descuento: 750, importe: 4250 }], totalTrasLineas: 4250, promocionCompra: { id: 2, codigo: 'COMPRA-3000', nombre: 'Compra 3000', porcentajeDescuento: 5 }, descuentoCompra: 212.5, subtotal: 5000, descuentoTotal: 962.5, total: 4037.5 };

export interface Llamada { url: string; init: RequestInit; }
export interface Opciones { errorCrear?: boolean; errorEvaluar?: boolean; sinPromociones?: boolean; listaVacia?: boolean; }

export function prepararApi(opciones: Opciones = {}) {
  const llamadas: Llamada[] = [];
  const fetchMock = vi.fn(async (entrada: RequestInfo | URL, init: RequestInit = {}) => {
    const url = String(entrada);
    const parseada = new URL(url, 'http://localhost');
    const metodo = init.method ?? 'GET';
    llamadas.push({ url, init });
    if (parseada.pathname.endsWith('/promociones/resumen')) return Response.json({ total: 4, activas: 3, vigentes: 2, deProducto: 3, deMontoMinimo: 1, deSegmento: 1, porcentajePromedioVigentes: 12.5 });
    if (parseada.pathname.endsWith('/clientes')) return Response.json({ items: [{ id: 1, nombre: 'Acme Industrial' }, { id: 2, nombre: 'Beta Comercio' }] });
    if (parseada.pathname.endsWith('/productos')) return Response.json({ items: [{ id: 1, nombre: 'Laptop Pro', precio: 2500 }, { id: 2, nombre: 'Mouse Óptico', precio: 80 }] });
    if (parseada.pathname.endsWith('/segmentos-clientes')) return Response.json({ items: [{ id: 1, codigo: 'VIP', nombre: 'Clientes VIP' }] });
    if (parseada.pathname.endsWith('/promociones/evaluar')) {
      if (opciones.errorEvaluar) return Response.json({ StatusCode: 400, Message: 'No se pudo evaluar la compra.' }, { status: 400 });
      return Response.json(opciones.sinPromociones ? { ...promocionCompra, lineas: [{ ...promocionCompra.lineas[0], promocion: null, descuento: 0, importe: 5000 }], totalTrasLineas: 5000, promocionCompra: null, descuentoCompra: 0, subtotal: 5000, descuentoTotal: 0, total: 5000 } : promocionCompra);
    }
    if (parseada.pathname.endsWith('/promociones') && metodo === 'GET') {
      const items = opciones.listaVacia ? [] : [promo, { ...promo, id: 2, codigo: 'COMPRA-3000', tipo: 'MontoMinimo', productoId: null, productoNombre: null, montoMinimo: 3000, segmentoId: null, segmentoCodigo: null, vigente: false }, { ...promo, id: 3, codigo: 'INACTIVA', activa: false, vigente: false }];
      return Response.json({ items, total: items.length, pagina: Number(parseada.searchParams.get('pagina') ?? 1), tamanoPagina: 10 });
    }
    if (parseada.pathname.endsWith('/promociones') && metodo === 'POST') return opciones.errorCrear ? Response.json({ StatusCode: 409, Message: 'Ya existe una promoción con el código LAPTOP-10.' }, { status: 409 }) : Response.json({ id: 9 });
    if (metodo === 'PUT') return Response.json({ id: 1 });
    if (metodo === 'DELETE') return new Response(null, { status: 204 });
    if (/\/promociones\/\d+$/.test(parseada.pathname)) return Response.json(promo);
    return Response.json({});
  });
  vi.stubGlobal('fetch', fetchMock);
  return { fetchMock, llamadas };
}

export function renderPromociones() { return render(<MemoryRouter initialEntries={['/modulos/07-promociones-descuentos']}><PromocionesPage /></MemoryRouter>); }

export async function esperarCargaInicial(fetchMock: ReturnType<typeof vi.fn>): Promise<void> {
  await screen.findByRole('button', { name: 'LAPTOP-VIP-15' });
  await screen.findByRole('heading', { name: 'Promociones' });
  await screen.findByRole('combobox', { name: 'Segmento' });
  await waitFor(() => expect(fetchMock.mock.calls.filter((call) => String(call[0]).includes('/promociones?'))).toHaveLength(1));
}

export function limpiar(): void { cleanup(); vi.useRealTimers(); vi.unstubAllGlobals(); }
