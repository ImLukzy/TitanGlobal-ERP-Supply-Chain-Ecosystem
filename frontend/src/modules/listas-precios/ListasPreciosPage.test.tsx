import { afterEach, describe, expect, it, vi } from "vitest";
import {
  act,
  fireEvent,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import {
  esperarCargaInicial,
  limpiarListas,
  prepararApiListas,
  renderListas,
} from "./listas.testUtils";

afterEach(limpiarListas);
async function montarCargado(
  fetchMock: ReturnType<typeof prepararApiListas>["fetchMock"],
): Promise<void> {
  vi.useFakeTimers();
  renderListas();
  await act(async () => {
    await vi.advanceTimersByTimeAsync(301);
    await Promise.resolve();
    await Promise.resolve();
  });
  vi.useRealTimers();
  await esperarCargaInicial(fetchMock);
}
describe("Pantalla de listas de precios", () => {
  it("A3 carga una sola lista y resumen con los datos semilla", async () => {
    const { fetchMock, llamadas } = prepararApiListas();
    await montarCargado(fetchMock);
    expect(screen.getByText("Precios VIP 2026")).toBeInTheDocument();
    expect(screen.getByText("Vigentes").parentElement).toHaveTextContent("2");
    expect(
      llamadas.filter((item) => item.url.includes("/listas-precios?")),
    ).toHaveLength(1);
  });

  it("A3 aplica búsqueda tras 300 ms y no hace una petición prematura", async () => {
    const { fetchMock, llamadas } = prepararApiListas();
    await montarCargado(fetchMock);
    vi.useFakeTimers();
    fireEvent.change(screen.getByLabelText("Buscar por código o nombre"), {
      target: { value: "VIP" },
    });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(299);
    });
    expect(
      llamadas.filter((item) => item.url.includes("busqueda=VIP")),
    ).toHaveLength(0);
    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });
    vi.useRealTimers();
    await waitFor(() =>
      expect(llamadas.some((item) => item.url.includes("busqueda=VIP"))).toBe(
        true,
      ),
    );
    expect(fetchMock).toHaveBeenCalled();
  });

  it("A3 filtra por segmento y vigencia y vuelve a página 1", async () => {
    const { fetchMock, llamadas } = prepararApiListas();
    await montarCargado(fetchMock);
    fireEvent.change(screen.getByLabelText("Segmento"), {
      target: { value: "1" },
    });
    fireEvent.click(screen.getByLabelText("Solo vigentes"));
    await waitFor(() =>
      expect(
        llamadas.some(
          (item) =>
            item.url.includes("segmentoId=1") &&
            item.url.includes("vigentes=true") &&
            item.url.includes("pagina=1"),
        ),
      ).toBe(true),
    );
  });

  it("A3 calcula precios con los tres orígenes solo al pulsar Calcular", async () => {
    const { fetchMock, llamadas } = prepararApiListas();
    await montarCargado(fetchMock);
    expect(
      llamadas.filter((item) => item.url.includes("/listas-precios/precio?")),
    ).toHaveLength(0);
    fireEvent.change(screen.getByLabelText("Producto"), {
      target: { value: "1" },
    });
    fireEvent.change(screen.getByLabelText("Cliente"), {
      target: { value: "1" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Calcular" }));
    expect(
      await screen.findByText(/Lista VIP-2026 \(segmento VIP\)/),
    ).toBeInTheDocument();
    expect(screen.getByText(/2,300\.00/)).toBeInTheDocument();
    expect(screen.getByText("−8.00 %")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Cliente"), {
      target: { value: "2" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Calcular" }));
    expect(await screen.findByText("Lista GENERAL-2026")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("Producto"), {
      target: { value: "4" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Calcular" }));
    expect(
      await screen.findByText("Precio base", { selector: "dd" }),
    ).toBeInTheDocument();
    expect(
      llamadas.filter((item) => item.url.includes("/listas-precios/precio?")),
    ).toHaveLength(3);
    expect(fetchMock).toHaveBeenCalled();
  });

  it("A3 muestra el error literal del simulador sin peticiones al escribir", async () => {
    const { fetchMock, llamadas } = prepararApiListas({ errorPrecio: true });
    await montarCargado(fetchMock);
    fireEvent.change(screen.getByLabelText("Producto"), {
      target: { value: "1" },
    });
    fireEvent.change(screen.getByLabelText("Cliente"), {
      target: { value: "2" },
    });
    expect(
      llamadas.filter((item) => item.url.includes("/listas-precios/precio?")),
    ).toHaveLength(0);
    fireEvent.click(screen.getByRole("button", { name: "Calcular" }));
    expect(
      await screen.findByText("El cliente 9999 no existe."),
    ).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalled();
  });

  it("A3 presenta los estados vacío y error del listado", async () => {
    const empty = prepararApiListas({ listaVacia: true });
    renderListas();
    expect(
      await screen.findByText("No hay listas que coincidan con los filtros."),
    ).toBeInTheDocument();
    limpiarListas();
    const failed = prepararApiListas({ errorLista: true });
    renderListas();
    expect(await screen.findByText("Falló el listado.")).toBeInTheDocument();
    expect(empty.fetchMock).toHaveBeenCalled();
    expect(failed.fetchMock).toHaveBeenCalled();
  });

  it("A4 valida localmente y presenta 409 conservando los campos", async () => {
    const { fetchMock, llamadas } = prepararApiListas({ errorCrear: true });
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole("button", { name: "Nueva lista" }));
    const dialog = within(screen.getByRole("dialog"));
    fireEvent.click(dialog.getByRole("button", { name: "Guardar lista" }));
    expect(screen.getByText("El código es obligatorio.")).toBeInTheDocument();
    expect(
      screen.getByText("El nombre de la lista es obligatorio."),
    ).toBeInTheDocument();
    expect(
      llamadas.filter(
        (item) =>
          item.init.method === "POST" && item.url.endsWith("/listas-precios"),
      ),
    ).toHaveLength(0);
    fireEvent.change(dialog.getByLabelText("Código"), {
      target: { value: "nueva-01" },
    });
    fireEvent.change(dialog.getByLabelText("Nombre"), {
      target: { value: "Lista nueva" },
    });
    fireEvent.change(
      dialog.getByLabelText("Producto", { selector: "select" }),
      { target: { value: "1" } },
    );
    fireEvent.change(dialog.getByLabelText("Precio"), {
      target: { value: "2300" },
    });
    fireEvent.click(dialog.getByRole("button", { name: "Guardar lista" }));
    expect(
      await screen.findByText(
        "Ya existe una lista de precios con el código NUEVA-01.",
      ),
    ).toBeInTheDocument();
    expect(dialog.getByLabelText("Código")).toHaveValue("NUEVA-01");
    expect(dialog.getByLabelText("Nombre")).toHaveValue("Lista nueva");
    expect(fetchMock).toHaveBeenCalled();
  });

  it("A4 valida formato, máximos, prioridad, fechas y precio antes de enviar", async () => {
    const { llamadas, fetchMock } = prepararApiListas();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole("button", { name: "Nueva lista" }));
    const dialog = within(screen.getByRole("dialog"));
    fireEvent.change(dialog.getByLabelText("Código"), {
      target: { value: "A" },
    });
    fireEvent.change(dialog.getByLabelText("Nombre"), {
      target: { value: "N".repeat(101) },
    });
    fireEvent.click(dialog.getByRole("button", { name: "Guardar lista" }));
    expect(
      screen.getByText(
        "El código solo admite letras, números y guiones (2 a 20 caracteres).",
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        "El nombre de la lista no puede superar 100 caracteres.",
      ),
    ).toBeInTheDocument();
    fireEvent.change(dialog.getByLabelText("Código"), {
      target: { value: "LISTA-01" },
    });
    fireEvent.change(dialog.getByLabelText("Nombre"), {
      target: { value: "Lista de prueba" },
    });
    fireEvent.change(dialog.getByLabelText("Prioridad"), {
      target: { value: "1000" },
    });
    fireEvent.change(dialog.getByLabelText("Fecha de inicio"), {
      target: { value: "2026-10-10" },
    });
    fireEvent.change(dialog.getByLabelText("Fecha de fin (opcional)"), {
      target: { value: "2026-10-01" },
    });
    fireEvent.change(
      dialog.getByLabelText("Producto", { selector: "select" }),
      {
        target: { value: "1" },
      },
    );
    fireEvent.change(dialog.getByLabelText("Precio"), {
      target: { value: "0" },
    });
    fireEvent.click(dialog.getByRole("button", { name: "Guardar lista" }));
    expect(
      screen.getByText("La prioridad debe estar entre 1 y 999."),
    ).toBeInTheDocument();
    expect(
      screen.getByText("La fecha de fin no puede ser anterior a la de inicio."),
    ).toBeInTheDocument();
    expect(
      screen.getByText("El precio debe ser mayor que 0."),
    ).toBeInTheDocument();
    expect(
      llamadas.filter(
        (item) =>
          item.init.method === "POST" && item.url.endsWith("/listas-precios"),
      ),
    ).toHaveLength(0);
    expect(fetchMock).toHaveBeenCalled();
  });

  it("A4 muestra detalle de lista y permite eliminar con Escape", async () => {
    const { llamadas, fetchMock } = prepararApiListas();
    await montarCargado(fetchMock);
    fireEvent.click(screen.getByRole("button", { name: "VIP-2026" }));
    expect(await screen.findByText("Precios VIP 2026")).toBeInTheDocument();
    expect(screen.getByText("DETALLE DE LISTA")).toBeInTheDocument();
    expect(
      document.querySelector(
        "section[aria-labelledby='lista-detail-title'] .contrato-status",
      ),
    ).toHaveTextContent("Vigente");
    expect(screen.getByText("−8.00 %")).toBeInTheDocument();
    fireEvent.click(await screen.findByRole("button", { name: "Eliminar" }));
    const antes = llamadas.length;
    fireEvent.keyDown(document, { key: "Escape" });
    await waitFor(() =>
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument(),
    );
    expect(llamadas).toHaveLength(antes);
    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Eliminar" })).toHaveFocus(),
    );
    fireEvent.click(screen.getByRole("button", { name: "VIP-2026" }));
    fireEvent.click(await screen.findByRole("button", { name: "Eliminar" }));
    fireEvent.click(
      screen.getByRole("button", { name: "Eliminar lista de precios" }),
    );
    await waitFor(() =>
      expect(
        llamadas.some(
          (item) =>
            item.init.method === "DELETE" &&
            item.url.endsWith("/listas-precios/1"),
        ),
      ).toBe(true),
    );
  });
});
