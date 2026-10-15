import { useSearchParams } from 'react-router';
import { PaginaModulo } from '../../components/PaginaModulo';
import { AsignacionesTab } from './AsignacionesTab';
import { ComisionesTab } from './ComisionesTab';
import { VendedoresTab } from './VendedoresTab';
import type { KeyboardEvent } from 'react';

type Vista = 'vendedores' | 'asignacion' | 'comisiones';

function resolverVista(valor: string | null): Vista {
  if (valor === 'asignacion' || valor === 'comisiones') {
    return valor;
  }
  return 'vendedores';
}

export function ComisionesVendedoresPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const vista = resolverVista(searchParams.get('vista'));

  function cambiarVista(siguiente: Vista): void {
    setSearchParams(siguiente === 'vendedores' ? {} : { vista: siguiente });
  }

  function manejarTeclado(evento: KeyboardEvent<HTMLDivElement>): void {
    if (evento.key !== 'ArrowLeft' && evento.key !== 'ArrowRight') {
      return;
    }
    evento.preventDefault();
    const vistas: Vista[] = ['vendedores', 'asignacion', 'comisiones'];
    const posicion = vistas.indexOf(vista);
    const siguiente = evento.key === 'ArrowRight' ? (posicion + 1) % vistas.length : (posicion + vistas.length - 1) % vistas.length;
    const proximaVista = vistas[siguiente];
    if (proximaVista !== undefined) {
      cambiarVista(proximaVista);
      window.requestAnimationFrame(() => document.getElementById(`tab-comisiones-${proximaVista}`)?.focus());
    }
  }

  return (
    <PaginaModulo className="comisiones-page" modulo="08-comisiones-vendedores">
      <p className="module-intro">Gestiona vendedores, asigna órdenes de compra y liquida las comisiones de cada periodo.</p>
      <div aria-label="Vistas de comisiones de vendedores" className="segment-tabs comisiones-tabs" onKeyDown={manejarTeclado} role="tablist">
        <button aria-controls="panel-comisiones-vendedores" aria-selected={vista === 'vendedores'} className="segment-tab" id="tab-comisiones-vendedores" onClick={() => cambiarVista('vendedores')} role="tab" tabIndex={vista === 'vendedores' ? 0 : -1} type="button">Vendedores</button>
        <button aria-controls="panel-comisiones-asignacion" aria-selected={vista === 'asignacion'} className="segment-tab" id="tab-comisiones-asignacion" onClick={() => cambiarVista('asignacion')} role="tab" tabIndex={vista === 'asignacion' ? 0 : -1} type="button">Asignación de ventas</button>
        <button aria-controls="panel-comisiones-periodo" aria-selected={vista === 'comisiones'} className="segment-tab" id="tab-comisiones-comisiones" onClick={() => cambiarVista('comisiones')} role="tab" tabIndex={vista === 'comisiones' ? 0 : -1} type="button">Comisiones del mes</button>
      </div>
      <div aria-labelledby={`tab-comisiones-${vista}`} className="comisiones-tab-panel" id={`panel-comisiones-${vista}`} role="tabpanel" tabIndex={0}>
        {vista === 'vendedores' && <VendedoresTab activo />}
        {vista === 'asignacion' && <AsignacionesTab activo />}
        {vista === 'comisiones' && <ComisionesTab activo />}
      </div>
    </PaginaModulo>
  );
}
