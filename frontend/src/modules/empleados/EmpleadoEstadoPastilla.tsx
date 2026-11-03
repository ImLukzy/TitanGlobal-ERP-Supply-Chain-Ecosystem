import { textoEstado } from './presentacion';
import type { EstadoEmpleado } from './tipos';

// El estado se muestra con texto, nunca solo con color.
export function EmpleadoEstadoPastilla({ estado }: { estado: EstadoEmpleado }) {
  return (
    <span className={`empleado-estado${estado === 'Activo' ? ' is-activo' : ' is-cesado'}`}>
      {textoEstado(estado)}
    </span>
  );
}
