import { cleanup, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { expect, vi } from "vitest";
import { ListasPreciosPage } from "./ListasPreciosPage";
import type { ListaDetalle, ListaListado } from "./tipos";

export const listasMock: ListaDetalle[] = [
  {
    id: 1,
    codigo: "VIP-2026",
    nombre: "Precios VIP 2026",
    segmentoId: 1,
    segmentoCodigo: "VIP",
    prioridad: 1,
    fechaInicio: "2026-01-01T00:00:00Z",
    fechaFin: "2026-12-31T00:00:00Z",
    activa: true,
    vigente: true,
    productos: 2,
    items: [
      {
        productoId: 1,
        productoNombre: "Laptop Pro",
        precioBase: 2500,
        precio: 2300,
        diferenciaPorcentaje: -8,
      },
      {
        productoId: 2,
        productoNombre: "Monitor 27",
        precioBase: 900,
        precio: 820,
        diferenciaPorcentaje: -8.89,
      },
    ],
  },
  {
    id: 2,
    codigo: "GENERAL-2026",
    nombre: "Precios generales 2026",
    segmentoId: null,
    segmentoCodigo: null,
    prioridad: 10,
    fechaInicio: "2026-01-01T00:00:00Z",
    fechaFin: null,
    activa: true,
    vigente: true,
    productos: 2,
    items: [
      {
        productoId: 1,
        productoNombre: "Laptop Pro",
        precioBase: 2500,
        precio: 2450,
        diferenciaPorcentaje: -2,
      },
      {
        productoId: 3,
        productoNombre: "Mouse Óptico",
        precioBase: 50,
        precio: 45,
        diferenciaPorcentaje: -10,
      },
    ],
  },
  {
    id: 3,
    codigo: "PROMO-2025",
    nombre: "Promoción antigua",
    segmentoId: null,
    segmentoCodigo: null,
    prioridad: 5,
    fechaInicio: "2025-01-01T00:00:00Z",
    fechaFin: "2025-12-31T00:00:00Z",
    activa: true,
    vigente: false,
    productos: 1,
    items: [
      {
        productoId: 1,
        productoNombre: "Laptop Pro",
        precioBase: 2500,
        precio: 2000,
        diferenciaPorcentaje: -20,
      },
    ],
  },
];
export interface Llamada {
  url: string;
  init: RequestInit;
}
export function prepararApiListas(
  opciones: {
    listaVacia?: boolean;
    errorLista?: boolean;
    errorCrear?: boolean;
    errorPrecio?: boolean;
  } = {},
) {
  const llamadas: Llamada[] = [];
  const fetchMock = vi.fn(
    async (input: RequestInfo | URL, init: RequestInit = {}) => {
      const url = String(input);
      const parsed = new URL(url, "http://localhost");
      const method = init.method ?? "GET";
      llamadas.push({ url, init });
      if (parsed.pathname.endsWith("/listas-precios/resumen"))
        return Response.json({
          total: 3,
          activas: 3,
          vigentes: 2,
          deSegmento: 1,
          generales: 2,
          productosConPrecioEspecial: 3,
        });
      if (parsed.pathname.endsWith("/productos") && method === "GET")
        return Response.json({
          items: [
            { id: 1, nombre: "Laptop Pro", precio: 2500 },
            { id: 2, nombre: "Monitor 27", precio: 900 },
            { id: 3, nombre: "Mouse Óptico", precio: 50 },
            { id: 4, nombre: "Teclado Mecánico", precio: 200 },
          ],
          total: 4,
          pagina: 1,
          tamanoPagina: 100,
        });
      if (parsed.pathname.endsWith("/clientes"))
        return Response.json({
          items: [
            { id: 1, nombre: "Acme Industrial" },
            { id: 2, nombre: "Beta Comercio" },
          ],
        });
      if (parsed.pathname.endsWith("/segmentos-clientes"))
        return Response.json({
          items: [{ id: 1, codigo: "VIP", nombre: "VIP" }],
        });
      if (parsed.pathname.endsWith("/listas-precios/precio")) {
        if (opciones.errorPrecio)
          return Response.json(
            { StatusCode: 400, Message: "El cliente 9999 no existe." },
            { status: 400 },
          );
        const productId = Number(parsed.searchParams.get("productoId"));
        const clientId = Number(parsed.searchParams.get("clienteId"));
        const origen =
          productId === 4 ? "Base" : clientId === 1 ? "Segmento" : "General";
        const price = productId === 4 ? 200 : clientId === 1 ? 2300 : 2450;
        return Response.json({
          productoId: productId,
          productoNombre: productId === 4 ? "Teclado Mecánico" : "Laptop Pro",
          clienteId: clientId,
          clienteNombre: clientId === 1 ? "Acme Industrial" : "Beta Comercio",
          segmentoCodigo: clientId === 1 ? "VIP" : null,
          fecha: parsed.searchParams.get("fecha"),
          precio: price,
          precioBase: productId === 4 ? 200 : 2500,
          origen,
          listaId: origen === "Base" ? null : 1,
          listaCodigo:
            origen === "Segmento"
              ? "VIP-2026"
              : origen === "General"
                ? "GENERAL-2026"
                : null,
        });
      }
      if (parsed.pathname.endsWith("/listas-precios") && method === "GET") {
        if (opciones.errorLista)
          return Response.json(
            { StatusCode: 500, Message: "Falló el listado." },
            { status: 500 },
          );
        const found = opciones.listaVacia
          ? []
          : listasMock.map(
              (item): ListaListado => ({
                id: item.id,
                codigo: item.codigo,
                nombre: item.nombre,
                segmentoId: item.segmentoId,
                segmentoCodigo: item.segmentoCodigo,
                prioridad: item.prioridad,
                fechaInicio: item.fechaInicio,
                fechaFin: item.fechaFin,
                activa: item.activa,
                vigente: item.vigente,
                productos: item.productos,
              }),
            );
        return Response.json({
          items: found,
          total: found.length,
          pagina: 1,
          tamanoPagina: 10,
        });
      }
      if (parsed.pathname.endsWith("/listas-precios") && method === "POST") {
        if (opciones.errorCrear)
          return Response.json(
            {
              StatusCode: 409,
              Message: "Ya existe una lista de precios con el código NUEVA-01.",
            },
            { status: 409 },
          );
        return Response.json({ id: 9 });
      }
      const detail = listasMock.find((item) =>
        parsed.pathname.endsWith(`/listas-precios/${item.id}`),
      );
      if (detail) return Response.json(detail);
      if (method === "PUT") return Response.json({ id: 1 });
      if (method === "DELETE") return new Response(null, { status: 204 });
      return Response.json({});
    },
  );
  vi.stubGlobal("fetch", fetchMock);
  return { fetchMock, llamadas };
}
export function renderListas(): void {
  render(
    <MemoryRouter initialEntries={["/modulos/06-listas-precios"]}>
      <ListasPreciosPage />
    </MemoryRouter>,
  );
}
export async function esperarCargaInicial(
  fetchMock: ReturnType<typeof vi.fn>,
): Promise<void> {
  await screen.findByRole("button", { name: "VIP-2026" });
  await screen.findByText("Productos con precio especial");
  await screen.findByRole("combobox", { name: "Segmento" });
  await screen.findByRole("option", { name: "Laptop Pro" });
  await screen.findByRole("option", { name: "Acme Industrial" });
  await screen.findByRole("option", { name: "VIP" });
  await waitFor(() =>
    expect(
      fetchMock.mock.calls.filter((call) =>
        String(call[0]).includes("/listas-precios?"),
      ),
    ).toHaveLength(1),
  );
}
export function limpiarListas(): void {
  cleanup();
  vi.useRealTimers();
  vi.unstubAllGlobals();
}
