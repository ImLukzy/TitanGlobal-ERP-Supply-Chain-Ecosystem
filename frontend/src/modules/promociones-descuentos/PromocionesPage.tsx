import { useEffect, useMemo, useState } from 'react';
import { ApiError } from '../../api/client';
import { DialogoBaja } from '../../components/DialogoBaja';
import { PaginaModulo } from '../../components/PaginaModulo';
import { actualizarPromocion, crearPromocion, eliminarPromocion } from './api';
import { EvaluadorPromociones } from './EvaluadorPromociones';
import { PromocionDetallePanel } from './PromocionDetallePanel';
import { PromocionFormulario } from './PromocionFormulario';
import { PromocionesFiltros } from './PromocionesFiltros';
import { PromocionesResumen } from './PromocionesResumen';
import { PromocionesTabla } from './PromocionesTabla';
import { usePromocionesData } from './usePromocionesData';
import type { FiltrosPromociones, GuardarPromocionRequest } from './tipos';

const filtrosIniciales: FiltrosPromociones = { pagina: 1, tamanoPagina: 10, vigentes: false };

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

export function PromocionesPage() {
  const [filtros, setFiltros] = useState<FiltrosPromociones>(filtrosIniciales);
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [formulario, setFormulario] = useState(false);
  const [dialogoBaja, setDialogoBaja] = useState(false);
  const [errorBaja, setErrorBaja] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const consulta = useMemo(() => ({ ...filtros, busqueda: busquedaAplicada }), [filtros, busquedaAplicada]);
  const data = usePromocionesData(consulta, seleccionadoId);

  useEffect(() => {
    const temporizador = window.setTimeout(() => setBusquedaAplicada((actual) => actual === busqueda ? actual : busqueda), 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setFiltros((actuales) => actuales.pagina === 1 ? actuales : { ...actuales, pagina: 1 });
    setSeleccionadoId(null);
  }

  function cambiarFiltro(cambios: Partial<FiltrosPromociones>): void {
    setFiltros((actuales) => ({ ...actuales, ...cambios, pagina: 1 }));
    setSeleccionadoId(null);
  }

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setFormulario(false);
  }

  async function guardar(request: GuardarPromocionRequest): Promise<void> {
    if (data.promocion !== null && formulario) await actualizarPromocion(data.promocion.id, request);
    else setSeleccionadoId((await crearPromocion(request)).id);
    setFormulario(false);
    data.recargar();
  }

  async function confirmarBaja(): Promise<void> {
    if (data.promocion === null) return;
    setEliminando(true);
    setErrorBaja('');
    try {
      await eliminarPromocion(data.promocion.id);
      setDialogoBaja(false);
      setSeleccionadoId(null);
      data.recargar();
    } catch (error: unknown) {
      setErrorBaja(mensaje(error, 'No se pudo eliminar la promoción.'));
    } finally {
      setEliminando(false);
    }
  }

  const items = data.lista?.items ?? [];
  return <PaginaModulo className="promociones-page" modulo="07-promociones-descuentos">
    <p className="module-intro">Administra campañas y simula los descuentos aplicables a una compra.</p>
    <PromocionesResumen datos={data.resumen} error={data.errorResumen} />
    <EvaluadorPromociones clientes={data.clientes} productos={data.productos} errorCatalogos={data.errorCatalogos} />
    <div className="contratos-work-area promociones-work-area">
      <section aria-labelledby="promociones-list-title" className="clientes-list-panel contratos-list-panel">
        <div className="clientes-panel-heading"><div><span className="page-eyebrow">COMERCIAL</span><h2 id="promociones-list-title">Promociones</h2></div><button className="primary-button" onClick={() => { setSeleccionadoId(null); setFormulario(true); }} type="button">Nueva promoción</button></div>
        <PromocionesFiltros busqueda={busqueda} cambiarBusqueda={cambiarBusqueda} cambiarFiltro={cambiarFiltro} filtros={filtros} segmentos={data.segmentos} />
        <PromocionesTabla cambiarPagina={(pagina) => cambiarFiltro({ pagina })} cargando={data.lista === null && data.errorLista === ''} error={data.errorLista} items={items} pagina={filtros.pagina ?? 1} seleccionar={seleccionar} seleccionadoId={seleccionadoId} total={data.lista?.total ?? 0} />
      </section>
      <PromocionDetallePanel cargando={seleccionadoId !== null && data.promocion === null && data.errorDetalle === ''} editar={() => setFormulario(true)} eliminar={() => { setErrorBaja(''); setDialogoBaja(true); }} error={data.errorDetalle} promocion={data.promocion} />
    </div>
    {formulario && <PromocionFormulario cancelar={() => setFormulario(false)} guardar={guardar} productos={data.productos} promocion={data.promocion} segmentos={data.segmentos} key={data.promocion?.id ?? 'nueva'} />}
    {dialogoBaja && data.promocion !== null && <DialogoBaja cerrar={() => setDialogoBaja(false)} confirmar={() => void confirmarBaja()} eliminando={eliminando} entidad="promoción" error={errorBaja} nombre={data.promocion.codigo} />}
  </PaginaModulo>;
}
