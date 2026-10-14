import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  buscarProductos,
  crearOrdenCompra,
  obtenerCategoriasCliente,
  obtenerClientesCompradores,
  obtenerFrecuenciaClientes,
  obtenerJerarquiaCliente,
  obtenerProductoMasCaro,
  obtenerProductosSinDescripcion,
  obtenerPromedioCliente,
  obtenerSumaVentas,
} from './api';

afterEach(() => {
  vi.unstubAllGlobals();
});

async function comprobarLlamada<T>(
  accion: () => Promise<T>,
  urlEsperada: string,
  metodo: string,
  respuesta: T,
): Promise<void> {
  const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify(respuesta), { status: 200 }));
  vi.stubGlobal('fetch', fetchMock);

  await expect(accion()).resolves.toEqual(respuesta);
  expect(fetchMock).toHaveBeenCalledTimes(1);
  expect(fetchMock.mock.calls[0]?.[0]).toBe(urlEsperada);
  expect(fetchMock.mock.calls[0]?.[1]?.method).toBe(metodo);
}

describe('API de OrdenesCompra', () => {
  it('POST crea una orden y devuelve el identificador camelCase', async () => {
    const body = { numeroOrden: 'OC-001', clienteId: 1, total: 2500 };
    await comprobarLlamada(() => crearOrdenCompra(body), '/api/v1/ordenes-compra', 'POST', { id: 7 });
  });

  it('GET productos/filtro envía término y precio mínimo codificados', async () => {
    const productos = [{ id: 1, nombre: 'Laptop Pro', precio: 2500 }];
    await comprobarLlamada(() => buscarProductos('Laptop & Pro', 100), '/api/v1/ordenes-compra/productos/filtro?termino=Laptop+%26+Pro&minPrecio=100', 'GET', productos);
  });

  it('GET ventas/suma envía fechaInicio y recibe totalVentas', async () => {
    await comprobarLlamada(() => obtenerSumaVentas('2026-01-01'), '/api/v1/ordenes-compra/ventas/suma?fechaInicio=2026-01-01', 'GET', { totalVentas: 5000 });
  });

  it('GET productos/mas-caro devuelve el resumen en camelCase', async () => {
    const producto = { id: 1, nombre: 'Laptop Pro', precio: 2500 };
    await comprobarLlamada(obtenerProductoMasCaro, '/api/v1/ordenes-compra/productos/mas-caro', 'GET', producto);
  });

  it('GET clientes/{id}/promedio recibe promedioCompra', async () => {
    await comprobarLlamada(() => obtenerPromedioCliente(1), '/api/v1/ordenes-compra/clientes/1/promedio', 'GET', { clienteId: 1, promedioCompra: 2500 });
  });

  it('GET productos/sin-descripcion devuelve productos', async () => {
    const productos = [{ id: 3, nombre: 'Mouse', precio: 80 }];
    await comprobarLlamada(obtenerProductosSinDescripcion, '/api/v1/ordenes-compra/productos/sin-descripcion', 'GET', productos);
  });

  it('GET clientes/frecuencia recibe el ranking con nombres camelCase', async () => {
    const ranking = [{ clienteId: 1, totalOrdenes: 4 }, { clienteId: 2, totalOrdenes: 2 }];
    await comprobarLlamada(obtenerFrecuenciaClientes, '/api/v1/ordenes-compra/clientes/frecuencia', 'GET', ranking);
  });

  it('GET clientes/{id}/jerarquia devuelve orden y detalles camelCase', async () => {
    const ordenes = [{
      ordenId: 1,
      numeroOrden: 'OC-001',
      fecha: '2026-01-02T00:00:00Z',
      nombreCliente: 'Acme Industrial',
      detalles: [{ productoId: 1, nombreProducto: 'Laptop Pro', cantidad: 1, precioUnitario: 2500 }],
    }];
    await comprobarLlamada(() => obtenerJerarquiaCliente(1), '/api/v1/ordenes-compra/clientes/1/jerarquia', 'GET', ordenes);
  });

  it('GET clientes/{id}/categorias-unicas devuelve las categorías', async () => {
    await comprobarLlamada(() => obtenerCategoriasCliente(1), '/api/v1/ordenes-compra/clientes/1/categorias-unicas', 'GET', ['Computo', 'Pantallas', 'Perifericos']);
  });

  it('GET productos/{id}/clientes-compradores devuelve los nombres', async () => {
    await comprobarLlamada(() => obtenerClientesCompradores(1), '/api/v1/ordenes-compra/productos/1/clientes-compradores', 'GET', ['Acme Industrial', 'Beta Comercio']);
  });
});
