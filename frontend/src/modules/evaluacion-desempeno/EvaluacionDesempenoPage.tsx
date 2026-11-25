import { useSearchParams } from 'react-router';
import type { KeyboardEvent } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { useSesionOpcional } from '../../layout/SesionContext';
import { CiclosVista } from './CiclosVista';
import { EvaluacionesVista } from './EvaluacionesVista';

type Vista = 'ciclos' | 'evaluaciones';

const VISTAS: Vista[] = ['ciclos', 'evaluaciones'];
const ROLES_ESCRITURA = ['RRHH', 'ADMIN'];

function resolverVista(valor: string | null): Vista {
  return valor === 'evaluaciones' ? 'evaluaciones' : 'ciclos';
}

export function EvaluacionDesempenoPage() {
  const sesion = useSesionOpcional();
  const roles = sesion?.sesion?.roles ?? [];
  const puedeEscribir = roles.some((rol) => ROLES_ESCRITURA.includes(rol));
  const [searchParams, setSearchParams] = useSearchParams();
  const vista = resolverVista(searchParams.get('vista'));

  function cambiarVista(siguiente: Vista): void {
    setSearchParams(siguiente === 'ciclos' ? {} : { vista: siguiente });
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
      window.requestAnimationFrame(() => document.getElementById(`tab-evaluacion-desempeno-${proximaVista}`)?.focus());
    }
  }

  return (
    <PaginaModulo className="evaluacion-desempeno-page" modulo="68-evaluacion-desempeno">
      <p className="module-intro">Configura los ciclos de evaluación con sus competencias y pesos, registra y califica las evaluaciones de cada empleado y consulta los resultados del ciclo.</p>
      <div aria-label="Vistas de evaluación de desempeño" className="segment-tabs evaluacion-desempeno-tabs" onKeyDown={manejarTeclado} role="tablist">
        <button aria-controls="panel-evaluacion-desempeno-ciclos" aria-selected={vista === 'ciclos'} className="segment-tab" id="tab-evaluacion-desempeno-ciclos" onClick={() => cambiarVista('ciclos')} role="tab" tabIndex={vista === 'ciclos' ? 0 : -1} type="button">Ciclos</button>
        <button aria-controls="panel-evaluacion-desempeno-evaluaciones" aria-selected={vista === 'evaluaciones'} className="segment-tab" id="tab-evaluacion-desempeno-evaluaciones" onClick={() => cambiarVista('evaluaciones')} role="tab" tabIndex={vista === 'evaluaciones' ? 0 : -1} type="button">Evaluaciones</button>
      </div>
      <div aria-labelledby={`tab-evaluacion-desempeno-${vista}`} className="segment-tab-panel evaluacion-desempeno-tab-panel" id={`panel-evaluacion-desempeno-${vista}`} role="tabpanel" tabIndex={0}>
        {vista === 'ciclos' && <CiclosVista puedeEscribir={puedeEscribir} />}
        {vista === 'evaluaciones' && <EvaluacionesVista puedeEscribir={puedeEscribir} />}
      </div>
    </PaginaModulo>
  );
}
