import { useEffect, useMemo, useRef, useState } from "react";
import { ApiError } from "../../api/client";
import { DialogoBaja } from "../../components/DialogoBaja";
import { PaginaModulo } from "../../components/PaginaModulo";
import { actualizarLista, crearLista, eliminarLista } from "./api";
import { ListaDetallePanel } from "./ListaDetallePanel";
import { ListasFiltros } from "./ListasFiltros";
import { ListaFormulario } from "./ListaFormulario";
import { ListasResumen } from "./ListasResumen";
import { ListasTabla } from "./ListasTabla";
import { ListaSimulador } from "./ListaSimulador";
import { useListasData } from "./useListasData";
import "./listas-precios.css";
import type { FiltrosListas, GuardarLista } from "./tipos";

const iniciales: FiltrosListas = {
  pagina: 1,
  tamanoPagina: 10,
  vigentes: false,
};
function errorMessage(error: unknown): string {
  return error instanceof ApiError
    ? error.message
    : "No se pudo eliminar la lista de precios.";
}

export function ListasPreciosPage() {
  const [filtros, setFiltros] = useState<FiltrosListas>(iniciales);
  const [busqueda, setBusqueda] = useState("");
  const [busquedaAplicada, setBusquedaAplicada] = useState("");
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [formulario, setFormulario] = useState(false);
  const [dialogoBaja, setDialogoBaja] = useState(false);
  const [errorBaja, setErrorBaja] = useState("");
  const [ocupado, setOcupado] = useState(false);
  const botonBajaRef = useRef<HTMLButtonElement | null>(null);
  const filtrosConsulta = useMemo(
    () => ({ ...filtros, busqueda: busquedaAplicada }),
    [filtros, busquedaAplicada],
  );
  const data = useListasData(filtrosConsulta, seleccionadoId);

  useEffect(() => {
    const timer = window.setTimeout(
      () =>
        setBusquedaAplicada((actual) =>
          actual === busqueda ? actual : busqueda,
        ),
      300,
    );
    return () => window.clearTimeout(timer);
  }, [busqueda]);

  function cambiarBusqueda(value: string): void {
    setBusqueda(value);
    setFiltros((actual) =>
      actual.pagina === 1 ? actual : { ...actual, pagina: 1 },
    );
    setSeleccionadoId(null);
  }
  function cambiarFiltro(cambios: Partial<FiltrosListas>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: 1 }));
    setSeleccionadoId(null);
  }
  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setFormulario(false);
  }
  async function guardar(request: GuardarLista): Promise<void> {
    if (data.detalle && formulario)
      await actualizarLista(data.detalle.id, request);
    else {
      const respuesta = await crearLista(request);
      setSeleccionadoId(respuesta.id);
    }
    setFormulario(false);
    data.recargar();
  }
  async function confirmarBaja(): Promise<void> {
    if (seleccionadoId === null) return;
    setOcupado(true);
    setErrorBaja("");
    try {
      await eliminarLista(seleccionadoId);
      setSeleccionadoId(null);
      setDialogoBaja(false);
      data.recargar();
    } catch (error: unknown) {
      setErrorBaja(errorMessage(error));
    } finally {
      setOcupado(false);
    }
  }
  const lista = data.lista?.items ?? [];
  return (
    <PaginaModulo modulo="06-listas-precios">
      <p className="module-intro">
        Administra listas por segmento y simula el precio aplicable a cada
        cliente.
      </p>
      <ListasResumen resumen={data.resumen} />
      {data.errors.resumen && (
        <p className="clientes-error" role="alert">
          {data.errors.resumen}
        </p>
      )}
      {data.errors.catalogos && (
        <p className="clientes-error" role="alert">
          {data.errors.catalogos}
        </p>
      )}
      <ListaSimulador clientes={data.clientes} productos={data.productos} />
      <div className="listas-work-area">
        <section
          aria-labelledby="listas-title"
          className="clientes-list-panel listas-list-panel"
        >
          <div className="clientes-panel-heading">
            <div>
              <span className="page-eyebrow">VENTAS</span>
              <h2 id="listas-title">Listas de precios</h2>
            </div>
            <button
              className="primary-button"
              onClick={() => setFormulario(true)}
              type="button"
            >
              Nueva lista
            </button>
          </div>
          <ListasFiltros
            busqueda={busqueda}
            cambiarBusqueda={cambiarBusqueda}
            cambiarFiltro={cambiarFiltro}
            filtros={filtros}
            segmentos={data.segmentos}
          />
          <ListasTabla
            cambiarPagina={(pagina) => cambiarFiltro({ pagina })}
            cargando={data.lista === null && data.errors.lista === ""}
            error={data.errors.lista}
            items={lista}
            pagina={filtros.pagina ?? 1}
            seleccionar={seleccionar}
            seleccionadoId={seleccionadoId}
            total={data.lista?.total ?? 0}
          />
        </section>
        <ListaDetallePanel
          cargando={
            seleccionadoId !== null &&
            data.detalle === null &&
            data.errors.detalle === ""
          }
          editar={() => setFormulario(true)}
          eliminar={() => {
            setErrorBaja("");
            setDialogoBaja(true);
          }}
          error={data.errors.detalle}
          guardarBotonBaja={(element) => {
            botonBajaRef.current = element;
          }}
          lista={data.detalle}
        />
      </div>
      {formulario && (
        <ListaFormulario
          cerrar={() => setFormulario(false)}
          guardar={guardar}
          lista={data.detalle}
          productos={data.productos}
          segmentos={data.segmentos}
          key={data.detalle?.id ?? "nueva"}
        />
      )}
      {dialogoBaja && data.detalle && (
        <DialogoBaja
          cerrar={() => {
            setDialogoBaja(false);
            window.setTimeout(() => botonBajaRef.current?.focus(), 0);
          }}
          confirmar={() => void confirmarBaja()}
          eliminando={ocupado}
          entidad="lista de precios"
          error={errorBaja}
          nombre={data.detalle.codigo}
        />
      )}
    </PaginaModulo>
  );
}
