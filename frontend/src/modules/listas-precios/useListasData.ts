import { useCallback, useEffect, useState } from "react";
import { ApiError, apiGet } from "../../api/client";
import { listarListas, obtenerLista, obtenerResumenListas } from "./api";
import type {
  FiltrosListas,
  ListaDetalle,
  PaginaListas,
  ResumenListas,
} from "./tipos";

function errorMessage(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.message : fallback;
}

export function useListasData(
  filtros: FiltrosListas,
  seleccionadoId: number | null,
) {
  const [lista, setLista] = useState<PaginaListas | null>(null);
  const [resumen, setResumen] = useState<ResumenListas | null>(null);
  const [detalle, setDetalle] = useState<ListaDetalle | null>(null);
  const [productos, setProductos] = useState<
    { id: number; nombre: string; precioBase: number }[]
  >([]);
  const [clientes, setClientes] = useState<{ id: number; nombre: string }[]>(
    [],
  );
  const [segmentos, setSegmentos] = useState<
    { id: number; codigo: string; nombre: string }[]
  >([]);
  const [errors, setErrors] = useState({
    lista: "",
    resumen: "",
    detalle: "",
    catalogos: "",
  });
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let activo = true;
    void listarListas(filtros)
      .then((data) => {
        if (activo) {
          setLista(data);
          setErrors((old) => ({ ...old, lista: "" }));
        }
      })
      .catch((error: unknown) => {
        if (activo)
          setErrors((old) => ({
            ...old,
            lista: errorMessage(
              error,
              "No se pudo cargar la lista de precios.",
            ),
          }));
      });
    return () => {
      activo = false;
    };
  }, [filtros, revision]);

  useEffect(() => {
    let activo = true;
    void obtenerResumenListas()
      .then((data) => {
        if (activo) setResumen(data);
      })
      .catch((error: unknown) => {
        if (activo)
          setErrors((old) => ({
            ...old,
            resumen: errorMessage(error, "No se pudo cargar el resumen."),
          }));
      });
    return () => {
      activo = false;
    };
  }, [revision]);

  useEffect(() => {
    let activo = true;
    void Promise.all([
      apiGet<{ items: { id: number; nombre: string; precio: number }[] }>(
        "/productos?tamanoPagina=100&pagina=1",
      ),
      apiGet<{ items: { id: number; nombre: string }[] }>(
        "/clientes?tamanoPagina=100&pagina=1",
      ),
      apiGet<{ items: { id: number; codigo: string; nombre: string }[] }>(
        "/segmentos-clientes?tamanoPagina=100&pagina=1",
      ),
    ])
      .then(([p, c, s]) => {
        if (!activo) return;
        setProductos(
          p.items.map((item) => ({
            id: item.id,
            nombre: item.nombre,
            precioBase: item.precio,
          })),
        );
        setClientes(c.items);
        setSegmentos(
          s.items.map((item) => ({
            id: item.id,
            codigo: item.codigo,
            nombre: item.nombre,
          })),
        );
      })
      .catch((error: unknown) => {
        if (activo)
          setErrors((old) => ({
            ...old,
            catalogos: errorMessage(
              error,
              "No se pudieron cargar los datos del formulario.",
            ),
          }));
      });
    return () => {
      activo = false;
    };
  }, []);

  useEffect(() => {
    if (seleccionadoId === null) return;
    let activo = true;
    void obtenerLista(seleccionadoId)
      .then((data) => {
        if (activo) {
          setDetalle(data);
          setErrors((old) => ({ ...old, detalle: "" }));
        }
      })
      .catch((error: unknown) => {
        if (activo)
          setErrors((old) => ({
            ...old,
            detalle: errorMessage(error, "No se pudo cargar el detalle."),
          }));
      });
    return () => {
      activo = false;
    };
  }, [seleccionadoId, revision]);

  const recargar = useCallback(() => setRevision((actual) => actual + 1), []);
  return {
    lista,
    resumen,
    detalle: detalle?.id === seleccionadoId ? detalle : null,
    productos,
    clientes,
    segmentos,
    errors,
    recargar,
  };
}
