import { textoEstado } from './presentacion';
import type { EstadoContrato } from './tipos';

// El estado se muestra con texto, nunca solo con color.
export function ContratoLaboralEstadoPastilla({ estado }: { estado: EstadoContrato }) {
  return (
    <span className={`contratos-laborales-estado is-${estado.toLowerCase()}`}>
      {textoEstado(estado)}
    </span>
  );
}
