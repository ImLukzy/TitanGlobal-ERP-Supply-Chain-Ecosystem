import { useEffect, useMemo, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { PaginaModulo } from '../../components/PaginaModulo';
import { activarContrato, actualizarContrato, cancelarContrato, crearContrato, eliminarContrato, finalizarContrato } from './api';
import { ContratoAccionDialog } from './ContratoAccionDialog';
import { ContratoDetallePanel } from './ContratoDetallePanel';
import { ContratoFormulario } from './ContratoFormulario';
import { ContratosFiltros } from './ContratosFiltros';
import { ContratosResumen } from './ContratosResumen';
import { ContratosTabla } from './ContratosTabla';
import { useContratosData } from './useContratosData';
import type { FiltrosContrato, GuardarContratoRequest } from './tipos';

const iniciales: FiltrosContrato = { pagina: 1, tamanoPagina: 10, vencidos: false };
function mensaje(error: unknown, respaldo: string): string { return error instanceof ApiError ? error.message : respaldo; }

export function ContratosComercialesPage() {
  const [filtros, setFiltros] = useState<FiltrosContrato>(iniciales);
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [formulario, setFormulario] = useState(false);
  const [accion, setAccion] = useState<'activar' | 'finalizar' | 'cancelar' | 'eliminar' | null>(null);
  const [errorAccion, setErrorAccion] = useState('');
  const botonRef = useRef<HTMLButtonElement>(null);
  const filtrosConsulta = useMemo(() => ({ ...filtros, busqueda: busquedaAplicada }), [filtros, busquedaAplicada]);
  const data = useContratosData(filtrosConsulta, seleccionadoId);

  useEffect(() => {
    const timer = window.setTimeout(() => setBusquedaAplicada((actual) => actual === busqueda ? actual : busqueda), 300);
    return () => window.clearTimeout(timer);
  }, [busqueda]);

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setFiltros((actual) => actual.pagina === 1 ? actual : { ...actual, pagina: 1 });
    setSeleccionadoId(null);
  }

  function cambiarFiltro(cambios: Partial<FiltrosContrato>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: 1 }));
    setSeleccionadoId(null);
  }

  function seleccionar(id: number): void { setSeleccionadoId(id); setFormulario(false); }

  function abrirAccion(tipo: 'activar' | 'finalizar' | 'cancelar' | 'eliminar', boton: HTMLButtonElement): void {
    botonRef.current = boton;
    setErrorAccion('');
    setAccion(tipo);
  }

  function cerrarAccion(): void {
    setAccion(null);
    setErrorAccion('');
    window.setTimeout(() => botonRef.current?.focus(), 0);
  }

  async function guardar(request: GuardarContratoRequest): Promise<void> {
    if (data.contrato?.estado === 'Borrador' && formulario) await actualizarContrato(data.contrato.id, request);
    else {
      const creado = await crearContrato(request);
      setSeleccionadoId(creado.id);
    }
    setFormulario(false);
    data.recargar();
  }

  async function confirmarAccion(motivo?: string): Promise<void> {
    if (!data.contrato || !accion) return;
    try {
      if (accion === 'activar') await activarContrato(data.contrato.id);
      if (accion === 'finalizar') await finalizarContrato(data.contrato.id);
      if (accion === 'cancelar') await cancelarContrato(data.contrato.id, motivo ?? '');
      if (accion === 'eliminar') {
        await eliminarContrato(data.contrato.id);
        setSeleccionadoId(null);
      }
      cerrarAccion();
      data.recargar();
    } catch (error: unknown) {
      setErrorAccion(mensaje(error, 'No se pudo actualizar el contrato.'));
    }
  }

  const lista = data.lista?.items ?? [];
  return (
    <PaginaModulo className="contratos-page" modulo="05-contratos-comerciales">
      <p className="module-intro">Administra los acuerdos comerciales y el consumo de órdenes por periodo.</p>
      <ContratosResumen datos={data.resumen} error={data.errorResumen} />
      {data.errorClientes && <p className="clientes-error" role="alert">{data.errorClientes}</p>}
      <div className="contratos-work-area">
        <section aria-labelledby="contratos-list-title" className="clientes-list-panel contratos-list-panel">
          <div className="clientes-panel-heading"><div><span className="page-eyebrow">COMERCIAL</span><h2 id="contratos-list-title">Contratos comerciales</h2></div><button className="primary-button" onClick={() => setFormulario(true)} type="button">Nuevo contrato</button></div>
          <ContratosFiltros busqueda={busqueda} cambiarBusqueda={cambiarBusqueda} cambiarFiltro={cambiarFiltro} clientes={data.clientes} filtros={filtros} />
          <ContratosTabla cambiarPagina={(pagina) => cambiarFiltro({ pagina })} cargando={data.lista === null && data.errorLista === ''} error={data.errorLista} items={lista} pagina={filtros.pagina ?? 1} seleccionar={seleccionar} seleccionadoId={seleccionadoId} total={data.lista?.total ?? 0} />
        </section>
        <ContratoDetallePanel accion={abrirAccion} cargando={seleccionadoId !== null && data.contrato === null && data.errorDetalle === ''} contrato={data.contrato} editar={() => setFormulario(true)} error={data.errorDetalle} />
      </div>
      {formulario && <ContratoFormulario cancelar={() => setFormulario(false)} clientes={data.clientes} contrato={data.contrato?.estado === 'Borrador' ? data.contrato : null} guardar={guardar} key={data.contrato?.id ?? 'nuevo'} />}
      {accion && data.contrato && <ContratoAccionDialog accion={accion} cerrar={cerrarAccion} confirmar={confirmarAccion} error={errorAccion} numero={data.contrato.numero} />}
    </PaginaModulo>
  );
}
