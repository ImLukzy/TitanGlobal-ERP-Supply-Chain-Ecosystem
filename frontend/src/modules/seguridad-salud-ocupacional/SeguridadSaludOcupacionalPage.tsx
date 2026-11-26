import { useCallback, useState, type KeyboardEvent } from 'react';
import { useSearchParams } from 'react-router';
import { PaginaModulo } from '../../components/PaginaModulo';
import { useSesionOpcional } from '../../layout/SesionContext';
import { EppVista } from './EppVista';
import { ExamenesVista } from './ExamenesVista';
import { IncidentesVista } from './IncidentesVista';
import { anioActual, aniosDisponibles } from './presentacion';
import { ResumenSst } from './ResumenSst';
import { useResumen } from './useIncidentesData';

type Vista = 'incidentes' | 'epp' | 'examenes';

const VISTAS: { id: Vista; etiqueta: string }[] = [
  { id: 'incidentes', etiqueta: 'Incidentes' },
  { id: 'epp', etiqueta: 'EPP' },
  { id: 'examenes', etiqueta: 'Exámenes médicos' },
];
const ROLES_ESCRITURA = ['RRHH', 'ADMIN'];

function resolverVista(valor: string | null): Vista {
  return VISTAS.find((vista) => vista.id === valor)?.id ?? 'incidentes';
}

export function SeguridadSaludOcupacionalPage() {
  const sesion = useSesionOpcional();
  const roles = sesion?.sesion?.roles ?? [];
  const puedeEscribir = roles.some((rol) => ROLES_ESCRITURA.includes(rol));
  const [searchParams, setSearchParams] = useSearchParams();
  const vista = resolverVista(searchParams.get('vista'));
  const [actual] = useState<number>(() => anioActual());
  const [anio, setAnio] = useState<number>(actual);
  const [revisionResumen, setRevisionResumen] = useState(0);
  const resumen = useResumen(anio, revisionResumen);
  // Cualquier escritura de las vistas actualiza el resumen del año.
  const alCambiar = useCallback(() => {
    setRevisionResumen((valor) => valor + 1);
  }, []);
  // Los enlaces del resumen cambian los parámetros: la vista se vuelve a montar con el filtro aplicado.
  const claveVista = searchParams.toString();

  function cambiarVista(siguiente: Vista): void {
    setSearchParams(siguiente === 'incidentes' ? {} : { vista: siguiente });
  }

  function manejarTeclado(evento: KeyboardEvent<HTMLDivElement>): void {
    if (evento.key !== 'ArrowLeft' && evento.key !== 'ArrowRight') {
      return;
    }
    evento.preventDefault();
    const posicion = VISTAS.findIndex((item) => item.id === vista);
    const desplazamiento = evento.key === 'ArrowRight' ? 1 : VISTAS.length - 1;
    const proxima = VISTAS[(posicion + desplazamiento) % VISTAS.length];
    if (proxima !== undefined) {
      cambiarVista(proxima.id);
      window.requestAnimationFrame(() => document.getElementById(`tab-sst-${proxima.id}`)?.focus());
    }
  }

  return (
    <PaginaModulo className="sst-page" modulo="70-seguridad-salud-ocupacional">
      <p className="module-intro">Registra incidentes y accidentes con sus acciones correctivas, gestiona el catálogo y las entregas de EPP con su vencimiento, y controla los exámenes médicos ocupacionales.</p>
      <div className="sst-anio">
        <label htmlFor="sst-anio">Año</label>
        <select id="sst-anio" onChange={(evento) => setAnio(Number(evento.currentTarget.value))} value={anio}>
          {aniosDisponibles(actual).map((valor) => <option key={valor} value={valor}>{valor}</option>)}
        </select>
      </div>
      <ResumenSst datos={resumen.resumen} error={resumen.error} />
      <div aria-label="Vistas de seguridad y salud ocupacional" className="segment-tabs sst-tabs" onKeyDown={manejarTeclado} role="tablist">
        {VISTAS.map((item) => (
          <button
            aria-controls={`panel-sst-${item.id}`}
            aria-selected={vista === item.id}
            className="segment-tab"
            id={`tab-sst-${item.id}`}
            key={item.id}
            onClick={() => cambiarVista(item.id)}
            role="tab"
            tabIndex={vista === item.id ? 0 : -1}
            type="button"
          >
            {item.etiqueta}
          </button>
        ))}
      </div>
      <div aria-labelledby={`tab-sst-${vista}`} className="segment-tab-panel sst-tab-panel" id={`panel-sst-${vista}`} role="tabpanel" tabIndex={0}>
        {vista === 'incidentes' && <IncidentesVista alCambiar={alCambiar} key={claveVista} parametros={searchParams} puedeEscribir={puedeEscribir} />}
        {vista === 'epp' && <EppVista alCambiar={alCambiar} key={claveVista} parametros={searchParams} puedeEscribir={puedeEscribir} />}
        {vista === 'examenes' && <ExamenesVista alCambiar={alCambiar} key={claveVista} parametros={searchParams} puedeEscribir={puedeEscribir} />}
      </div>
    </PaginaModulo>
  );
}
