import { useSearchParams } from 'react-router';
import type { KeyboardEvent } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { useSesionOpcional } from '../../layout/SesionContext';
import { JornadasVista } from './JornadasVista';
import { MarcacionesVista } from './MarcacionesVista';

type Vista = 'jornadas' | 'marcaciones';

const VISTAS: Vista[] = ['jornadas', 'marcaciones'];
const ROLES_ESCRITURA = ['RRHH', 'ADMIN'];

function resolverVista(valor: string | null): Vista {
  if (valor === 'marcaciones') {
    return valor;
  }
  return 'jornadas';
}

export function AsistenciaBiometricaPage() {
  const sesion = useSesionOpcional();
  const roles = sesion?.sesion?.roles ?? [];
  const puedeEscribir = roles.some((rol) => ROLES_ESCRITURA.includes(rol));
  const [searchParams, setSearchParams] = useSearchParams();
  const vista = resolverVista(searchParams.get('vista'));

  function cambiarVista(siguiente: Vista): void {
    setSearchParams(siguiente === 'jornadas' ? {} : { vista: siguiente });
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
      window.requestAnimationFrame(() => document.getElementById(`tab-asistencia-${proximaVista}`)?.focus());
    }
  }

  return (
    <PaginaModulo className="asistencia-page" modulo="63-asistencia-biometrica">
      <p className="module-intro">Consulta la asistencia de cada empleado, registra o importa marcaciones y decide las horas extra.</p>
      <div aria-label="Vistas de asistencia biométrica" className="segment-tabs asistencia-tabs" onKeyDown={manejarTeclado} role="tablist">
        <button aria-controls="panel-asistencia-jornadas" aria-selected={vista === 'jornadas'} className="segment-tab" id="tab-asistencia-jornadas" onClick={() => cambiarVista('jornadas')} role="tab" tabIndex={vista === 'jornadas' ? 0 : -1} type="button">Jornadas</button>
        <button aria-controls="panel-asistencia-marcaciones" aria-selected={vista === 'marcaciones'} className="segment-tab" id="tab-asistencia-marcaciones" onClick={() => cambiarVista('marcaciones')} role="tab" tabIndex={vista === 'marcaciones' ? 0 : -1} type="button">Marcaciones</button>
      </div>
      <div aria-labelledby={`tab-asistencia-${vista}`} className="segment-tab-panel asistencia-tab-panel" id={`panel-asistencia-${vista}`} role="tabpanel" tabIndex={0}>
        {vista === 'jornadas' && <JornadasVista puedeEscribir={puedeEscribir} />}
        {vista === 'marcaciones' && <MarcacionesVista puedeEscribir={puedeEscribir} />}
      </div>
    </PaginaModulo>
  );
}
