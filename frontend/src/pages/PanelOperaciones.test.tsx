import { cleanup, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { dominios, modulos } from '../modules/catalogo';
import { pantallas } from '../modules/pantallas';
import { Inicio } from './Inicio';
import { barrasVendidos, construirProcesos, type DatosPanel } from './panelOperaciones';

const respuestas: Record<string, unknown> = {
  '/api/v1/sistema/salud': { estado: 'OK' },
  '/api/v1/clientes/resumen': {
    totalClientes: 5,
    clientesConOrdenes: 4,
    clientesSinDescripcion: 3,
    topClientesPorMonto: [
      { clienteId: 2, nombre: 'Beta Comercio', montoTotal: 2000, totalOrdenes: 2 },
      { clienteId: 1, nombre: 'Acme Industrial', montoTotal: 10000, totalOrdenes: 4 },
    ],
  },
  '/api/v1/productos/resumen': {
    totalProductos: 10,
    productosSinDescripcion: 3,
    precioPromedio: 994.5,
    porCategoria: [{ categoria: 'Computo', totalProductos: 3, precioPromedio: 2400, precioMaximo: 3200 }],
    masVendidos: [
      { productoId: 3, nombre: 'Mouse Óptico', unidadesVendidas: 4, montoVendido: 200 },
      { productoId: 7, nombre: 'Laptop Air', unidadesVendidas: 1, montoVendido: 3200 },
    ],
  },
  '/api/v1/contratos-comerciales/resumen': {
    total: 3, borradores: 1, vigentes: 1, vencidos: 0, finalizados: 0, cancelados: 1,
    montoComprometidoVigente: 20000, montoConsumidoVigente: 9000, porcentajeConsumoVigente: 45, porCondicion: [],
  },
  '/api/v1/cotizaciones/resumen': {
    total: 4, emitidas: 2, vencidas: 1, aceptadas: 1, rechazadas: 1, montoVigente: 180, montoAceptado: 80, tasaAceptacion: 50, topClientes: [],
  },
  '/api/v1/ordenes-abastecimiento/resumen': {
    total: 4, emitidas: 2, recibidas: 1, anuladas: 1, atrasadas: 2, montoPorRecibir: 9700, montoRecibido: 250, topProveedores: [],
  },
  '/api/v1/solicitudes-compra/resumen': {
    total: 5, pendientes: 2, aprobadas: 2, rechazadas: 1, montoPendiente: 16760, montoAprobado: 10200, topProductosSolicitados: [],
  },
  '/api/v1/reclamos-postventa/resumen': {
    total: 5, abiertos: 1, enProceso: 1, resueltos: 1, cerrados: 1, rechazados: 1, vencidos: 2, porTipo: [], horasPromedioResolucion: 60, cumplimientoPlazo: 50,
  },
  '/api/v1/alertas/resumen': { total: 8, altas: 4, medias: 1, bajas: 3, descartadas: 0, porTipo: [] },
};

function simularApi(cambios: Record<string, unknown> = {}, fallidas: string[] = []) {
  vi.stubGlobal('fetch', vi.fn((input: RequestInfo | URL) => {
    const url = String(input);
    if (fallidas.includes(url)) {
      return Promise.resolve(Response.json({ StatusCode: 500, Message: 'Error' }, { status: 500 }));
    }
    return Promise.resolve(Response.json({ ...respuestas, ...cambios }[url] ?? {}));
  }));
}

function renderInicio() {
  return render(<MemoryRouter><Inicio /></MemoryRouter>);
}

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe('panel de operaciones', () => {
  it('muestra los indicadores con su valor y enlace al módulo', async () => {
    simularApi();
    renderInicio();

    const indicadores = await screen.findByRole('region', { name: 'Indicadores principales' });
    const clientes = within(indicadores).getByRole('link', { name: /Clientes/ });
    expect(clientes).toHaveAttribute('href', '/modulos/01-clientes');
    expect(clientes).toHaveTextContent('5');
    expect(clientes).toHaveTextContent('4 con órdenes de compra');
    expect(within(indicadores).getByRole('link', { name: /Productos en catálogo/ })).toHaveAttribute('href', '/modulos/21-productos');
    expect(within(indicadores).getByRole('link', { name: /Contratos vigentes/ })).toHaveTextContent('45 % consumido');
  });

  it('lista primero lo urgente y omite lo que está en cero', async () => {
    simularApi({ '/api/v1/reclamos-postventa/resumen': { ...(respuestas['/api/v1/reclamos-postventa/resumen'] as object), vencidos: 0 } });
    renderInicio();

    const seccion = await screen.findByRole('region', { name: 'Requiere atención' });
    const filas = within(seccion).getAllByRole('listitem').map((fila) => fila.textContent);
    expect(filas).toEqual([
      'UrgenteAlertas de prioridad alta4',
      'UrgenteÓrdenes de abastecimiento atrasadas2',
      'AvisoCotizaciones vencidas1',
      'AvisoSolicitudes de compra por aprobar2',
    ]);
  });

  it('dice que no hay pendientes solo si comprobó todas las fuentes', async () => {
    const enCero = {
      '/api/v1/alertas/resumen': { altas: 0 },
      '/api/v1/ordenes-abastecimiento/resumen': { ...(respuestas['/api/v1/ordenes-abastecimiento/resumen'] as object), atrasadas: 0 },
      '/api/v1/reclamos-postventa/resumen': { ...(respuestas['/api/v1/reclamos-postventa/resumen'] as object), vencidos: 0 },
      '/api/v1/cotizaciones/resumen': { ...(respuestas['/api/v1/cotizaciones/resumen'] as object), vencidas: 0 },
      '/api/v1/solicitudes-compra/resumen': { ...(respuestas['/api/v1/solicitudes-compra/resumen'] as object), pendientes: 0 },
    };
    simularApi(enCero);
    const { unmount } = renderInicio();
    expect(await screen.findByText('Nada pendiente por ahora.')).toBeInTheDocument();
    unmount();

    simularApi(enCero, ['/api/v1/alertas/resumen']);
    renderInicio();
    expect(await screen.findByText('No se pudo comprobar todo; revisa cada módulo.')).toBeInTheDocument();
    expect(screen.queryByText('Nada pendiente por ahora.')).not.toBeInTheDocument();
  });

  it('si falla una fuente conserva las demás y lo avisa', async () => {
    simularApi({}, ['/api/v1/clientes/resumen']);
    renderInicio();

    const indicadores = await screen.findByRole('region', { name: 'Indicadores principales' });
    expect(within(indicadores).getByRole('link', { name: /Clientes/ })).toHaveTextContent('No disponible');
    expect(within(indicadores).getByRole('link', { name: /Productos en catálogo/ })).toHaveTextContent('10');
    expect(screen.getByText('Algunos indicadores no están disponibles en este momento.')).toBeInTheDocument();
    expect(within(screen.getByRole('region', { name: 'Compras por cliente' })).getByText('No disponible')).toBeInTheDocument();
  });

  it('trata una respuesta incompleta como no disponible en lugar de romper la portada', async () => {
    simularApi({ '/api/v1/productos/resumen': { totalProductos: 10 } });
    renderInicio();

    const indicadores = await screen.findByRole('region', { name: 'Indicadores principales' });
    expect(within(indicadores).getByRole('link', { name: /Productos en catálogo/ })).toHaveTextContent('No disponible');
  });

  it('si fallan todas las fuentes muestra un error y mantiene los dominios', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('offline')));
    renderInicio();

    expect(await screen.findByText('No se pudieron cargar los indicadores.')).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Indicadores principales' })).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Dominios del negocio' })).toBeInTheDocument();
  });

  it('ordena las barras por valor aunque la API ordene por unidades', () => {
    const datos = respuestas['/api/v1/productos/resumen'] as Parameters<typeof barrasVendidos>[0];
    expect(barrasVendidos(datos).map((fila) => fila.etiqueta)).toEqual(['Laptop Air', 'Mouse Óptico']);
  });

  it('agrupa cada proceso sin perder ningún registro', () => {
    const datos = {
      cotizaciones: respuestas['/api/v1/cotizaciones/resumen'],
      solicitudes: respuestas['/api/v1/solicitudes-compra/resumen'],
      abastecimiento: respuestas['/api/v1/ordenes-abastecimiento/resumen'],
      reclamos: respuestas['/api/v1/reclamos-postventa/resumen'],
    } as DatosPanel;
    const totales = [datos.cotizaciones?.total, datos.solicitudes?.total, datos.abastecimiento?.total, datos.reclamos?.total];
    expect(construirProcesos(datos).map((proceso) => proceso.completado + proceso.enCurso + proceso.descartado)).toEqual(totales);
  });
});

describe('dominios de la portada', () => {
  it('cuenta las pantallas registradas y solo enlaza los dominios que tienen alguna', async () => {
    simularApi();
    renderInicio();
    await screen.findByRole('region', { name: 'Indicadores principales' });

    const registradas = modulos.filter((modulo) => pantallas[modulo.slug] !== undefined).length;
    expect(screen.getByText(`${registradas} de 80 pantallas`)).toBeInTheDocument();
    dominios.forEach((dominio) => {
      const primera = dominio.modulos.find((modulo) => pantallas[modulo.slug] !== undefined);
      const encabezado = screen.getByRole('heading', { name: dominio.nombre });
      const enlace = encabezado.closest('a');
      if (primera === undefined) {
        expect(enlace).toBeNull();
      } else {
        expect(enlace).toHaveAttribute('href', `/modulos/${primera.slug}`);
      }
    });
  });
});
