import { useSearchParams } from 'react-router';
import type { KeyboardEvent } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { useSesionOpcional } from '../../layout/SesionContext';
import { AfiliacionesVista } from './AfiliacionesVista';
import { BonosVista } from './BonosVista';
import { PlanillasVista } from './PlanillasVista';

type Vista = 'planillas' | 'bonos' | 'afiliaciones';

const VISTAS: Vista[] = ['planillas', 'bonos', 'afiliaciones'];
const ROLES_ESCRITURA = ['RRHH', 'ADMIN'];

function resolverVista(valor: string | null): Vista {
  if (valor === 'bonos' || valor === 'afiliaciones') {
    return valor;
  }
  return 'planillas';
}

export function PlanillasPage() {
  const sesion = useSesionOpcional();
  const roles = sesion?.sesion?.roles ?? [];
  const puedeEscribir = roles.some((rol) => ROLES_ESCRITURA.includes(rol));
  const [searchParams, setSearchParams] = useSearchParams();
  const vista = resolverVista(searchParams.get('vista'));

  function cambiarVista(siguiente: Vista): void {
    setSearchParams(siguiente === 'planillas' ? {} : { vista: siguiente });
  }

  function manejarTeclado(evento: KeyboardEvent<HTMLDivElement>): void {
    if (evento.key !== 'ArrowLeft' && evento.key !== 'ArrowRight') {
      return;
    }
    evento.preventDefault();
    const posicion = VISTAS.indexOf(vista);
    const desplazamiento = evento.key === 'ArrowRight' ? 1 : VISTAS.length - 1;
    const proximaVista = VISTAS[(posicion + desplazamiento) % VISTAS.length];
    if (proximaVista !== undefined) {
      cambiarVista(proximaVista);
      window.requestAnimationFrame(() => document.getElementById(`tab-planillas-${proximaVista}`)?.focus());
    }
  }

  return (
    <PaginaModulo className="planillas-page" modulo="66-planillas">
      <p className="module-intro">Calcula, aprueba y paga la planilla de cada periodo, consulta el detalle por empleado, imprime las boletas y administra bonos y afiliaciones a pensiones.</p>
      <div aria-label="Vistas de planillas" className="segment-tabs planillas-tabs" onKeyDown={manejarTeclado} role="tablist">
        <button aria-controls="panel-planillas-planillas" aria-selected={vista === 'planillas'} className="segment-tab" id="tab-planillas-planillas" onClick={() => cambiarVista('planillas')} role="tab" tabIndex={vista === 'planillas' ? 0 : -1} type="button">Planillas</button>
        <button aria-controls="panel-planillas-bonos" aria-selected={vista === 'bonos'} className="segment-tab" id="tab-planillas-bonos" onClick={() => cambiarVista('bonos')} role="tab" tabIndex={vista === 'bonos' ? 0 : -1} type="button">Bonos</button>
        <button aria-controls="panel-planillas-afiliaciones" aria-selected={vista === 'afiliaciones'} className="segment-tab" id="tab-planillas-afiliaciones" onClick={() => cambiarVista('afiliaciones')} role="tab" tabIndex={vista === 'afiliaciones' ? 0 : -1} type="button">Afiliaciones</button>
      </div>
      <div aria-labelledby={`tab-planillas-${vista}`} className="segment-tab-panel planillas-tab-panel" id={`panel-planillas-${vista}`} role="tabpanel" tabIndex={0}>
        {vista === 'planillas' && <PlanillasVista puedeEscribir={puedeEscribir} />}
        {vista === 'bonos' && <BonosVista puedeEscribir={puedeEscribir} />}
        {vista === 'afiliaciones' && <AfiliacionesVista puedeEscribir={puedeEscribir} />}
      </div>
    </PaginaModulo>
  );
}
