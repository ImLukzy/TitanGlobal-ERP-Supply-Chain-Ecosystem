import { useCallback, useState } from 'react';
import { EntregasPanel } from './EntregasPanel';
import { EquiposPanel } from './EquiposPanel';
import { useEquiposSelector } from './useEppData';
import { useEmpleados } from './useIncidentesData';

interface Props {
  puedeEscribir: boolean;
  alCambiar: () => void;
  parametros: URLSearchParams;
}

// Vista EPP: catálogo de equipos y entregas. Los cambios del catálogo actualizan los selectores de las entregas.
export function EppVista({ puedeEscribir, alCambiar, parametros }: Props) {
  const [revisionCatalogo, setRevisionCatalogo] = useState(0);
  const selector = useEquiposSelector(revisionCatalogo);
  const empleados = useEmpleados();
  const alCambiarCatalogo = useCallback(() => {
    setRevisionCatalogo((valor) => valor + 1);
    alCambiar();
  }, [alCambiar]);
  const error = selector.errorEquipos !== '' ? selector.errorEquipos : empleados.errorEmpleados;

  return (
    <>
      {error !== '' && <p aria-live="polite" className="clientes-error" role="alert">{error}</p>}
      <EntregasPanel alCambiar={alCambiar} empleados={empleados.empleados ?? []} equipos={selector.equipos ?? []} puedeEscribir={puedeEscribir} vencidasInicial={parametros.get('vencidas') === 'true'} />
      <EquiposPanel alCambiar={alCambiarCatalogo} puedeEscribir={puedeEscribir} />
    </>
  );
}
