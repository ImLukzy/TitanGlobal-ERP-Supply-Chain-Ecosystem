import { useSearchParams } from 'react-router';
import type { KeyboardEvent } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { useSesionOpcional } from '../../layout/SesionContext';
import { AsignacionesVista } from './AsignacionesVista';
import { TurnosVista } from './TurnosVista';

type Vista = 'turnos' | 'asignaciones';

const VISTAS: Vista[] = ['turnos', 'asignaciones'];
const ROLES_ESCRITURA = ['RRHH', 'ADMIN'];

function resolverVista(valor: string | null): Vista {
  if (valor === 'asignaciones') {
    return valor;
  }
  return 'turnos';
}

export function TurnosHorariosPage() {
  const sesion = useSesionOpcional();
  const roles = sesion?.sesion?.roles ?? [];
  const puedeEscribir = roles.some((rol) => ROLES_ESCRITURA.includes(rol));
  const [searchParams, setSearchParams] = useSearchParams();
  const vista = resolverVista(searchParams.get('vista'));

  function cambiarVista(siguiente: Vista): void {
    setSearchParams(siguiente === 'turnos' ? {} : { vista: siguiente });
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
      window.requestAnimationFrame(() => document.getElementById(`tab-turnos-${proximaVista}`)?.focus());
    }
  }

  return (
    <PaginaModulo className="turnos-horarios-page" modulo="64-turnos-horarios">
      <p className="module-intro">Administra los turnos de trabajo y asigna a cada empleado el turno que le corresponde.</p>
      <div aria-label="Vistas de turnos y horarios" className="segment-tabs turnos-horarios-tabs" onKeyDown={manejarTeclado} role="tablist">
        <button aria-controls="panel-turnos-turnos" aria-selected={vista === 'turnos'} className="segment-tab" id="tab-turnos-turnos" onClick={() => cambiarVista('turnos')} role="tab" tabIndex={vista === 'turnos' ? 0 : -1} type="button">Turnos</button>
        <button aria-controls="panel-turnos-asignaciones" aria-selected={vista === 'asignaciones'} className="segment-tab" id="tab-turnos-asignaciones" onClick={() => cambiarVista('asignaciones')} role="tab" tabIndex={vista === 'asignaciones' ? 0 : -1} type="button">Asignaciones</button>
      </div>
      <div aria-labelledby={`tab-turnos-${vista}`} className="segment-tab-panel turnos-horarios-tab-panel" id={`panel-turnos-${vista}`} role="tabpanel" tabIndex={0}>
        {vista === 'turnos' && <TurnosVista puedeEscribir={puedeEscribir} />}
        {vista === 'asignaciones' && <AsignacionesVista puedeEscribir={puedeEscribir} />}
      </div>
    </PaginaModulo>
  );
}
