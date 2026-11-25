import type { PaginaEmpleados } from '../empleados/tipos';
import { aOrigenFiltro, aTipoFiltro, ORIGENES_MARCACION, TIPOS_MARCACION } from './presentacion';
import type { OrigenMarcacion, TipoMarcacion } from './tipos';

interface Props {
  empleados: PaginaEmpleados | null;
  empleadoId: number | null;
  desde: string;
  hasta: string;
  tipo: '' | TipoMarcacion;
  origen: '' | OrigenMarcacion;
  cambiarEmpleado: (id: number | null) => void;
  cambiarDesde: (fecha: string) => void;
  cambiarHasta: (fecha: string) => void;
  cambiarTipo: (tipo: '' | TipoMarcacion) => void;
  cambiarOrigen: (origen: '' | OrigenMarcacion) => void;
}

export function MarcacionesFiltros({
  empleados,
  empleadoId,
  desde,
  hasta,
  tipo,
  origen,
  cambiarEmpleado,
  cambiarDesde,
  cambiarHasta,
  cambiarTipo,
  cambiarOrigen,
}: Props) {
  return (
    <div className="solicitudes-filters asistencia-filters">
      <div>
        <label htmlFor="marcaciones-empleado">Empleado</label>
        <select
          id="marcaciones-empleado"
          onChange={(event) => cambiarEmpleado(event.currentTarget.value === '' ? null : Number(event.currentTarget.value))}
          value={empleadoId === null ? '' : String(empleadoId)}
        >
          <option value="">Todos</option>
          {(empleados?.items ?? []).map((empleado) => (
            <option key={empleado.id} value={String(empleado.id)}>
              {`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}
            </option>
          ))}
        </select>
      </div>
      <div>
        <label htmlFor="marcaciones-desde">Desde</label>
        <input id="marcaciones-desde" onChange={(event) => cambiarDesde(event.currentTarget.value)} type="date" value={desde} />
      </div>
      <div>
        <label htmlFor="marcaciones-hasta">Hasta</label>
        <input id="marcaciones-hasta" onChange={(event) => cambiarHasta(event.currentTarget.value)} type="date" value={hasta} />
      </div>
      <div>
        <label htmlFor="marcaciones-tipo">Tipo</label>
        <select id="marcaciones-tipo" onChange={(event) => cambiarTipo(aTipoFiltro(event.currentTarget.value))} value={tipo}>
          <option value="">Todos</option>
          {TIPOS_MARCACION.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="marcaciones-origen">Origen</label>
        <select id="marcaciones-origen" onChange={(event) => cambiarOrigen(aOrigenFiltro(event.currentTarget.value))} value={origen}>
          <option value="">Todos</option>
          {ORIGENES_MARCACION.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
      </div>
    </div>
  );
}
