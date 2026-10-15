import { afterEach, describe, expect, it, vi } from "vitest";
import {
  actualizarLista,
  crearLista,
  eliminarLista,
  listarListas,
  obtenerLista,
  obtenerResumenListas,
  resolverPrecio,
} from "./api";
import { limpiarListas, prepararApiListas } from "./listas.testUtils";

afterEach(limpiarListas);
describe("API de listas de precios", () => {
  it("A2 usa rutas, métodos, consultas y cuerpo del contrato", async () => {
    const { llamadas } = prepararApiListas();
    await listarListas({
      busqueda: "VIP",
      segmentoId: 1,
      vigentes: true,
      pagina: 2,
      tamanoPagina: 10,
    });
    await obtenerLista(1);
    const request = {
      codigo: "X-1",
      nombre: "Lista",
      prioridad: 1,
      fechaInicio: "2026-01-01",
      activa: true,
      items: [{ productoId: 1, precio: 25 }],
    };
    await crearLista(request);
    await actualizarLista(1, request);
    await eliminarLista(1);
    await resolverPrecio(1, 2, "2026-10-15");
    await obtenerResumenListas();
    expect(llamadas[0]?.url).toContain(
      "/listas-precios?busqueda=VIP&segmentoId=1&vigentes=true&pagina=2&tamanoPagina=10",
    );
    expect(llamadas[1]?.url).toBe("/api/v1/listas-precios/1");
    expect(llamadas[2]?.init.method).toBe("POST");
    expect(llamadas[2]?.init.body).toBe(JSON.stringify(request));
    expect(llamadas[3]?.init.method).toBe("PUT");
    expect(llamadas[4]?.init.method).toBe("DELETE");
    expect(llamadas[5]?.url).toContain(
      "/listas-precios/precio?productoId=1&clienteId=2&fecha=2026-10-15",
    );
    expect(llamadas[6]?.url).toBe("/api/v1/listas-precios/resumen");
    expect(vi.mocked(fetch)).toHaveBeenCalledTimes(7);
  });
});
