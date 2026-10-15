import type { FiltrosAuditoria } from './tipos';

interface Props {
  entidades: string[];
  filtros: FiltrosAuditoria;
  cambiar: (cambios: Partial<FiltrosAuditoria>) => void;
  errorEntidades: string;
}

export function AuditoriaFiltros({ entidades, filtros, cambiar, errorEntidades }: Props) {
  return (
    <form className="solicitudes-filters auditoria-filtros" onSubmit={(evento) => evento.preventDefault()}>
      <div>
        <label htmlFor="auditoria-entidad">Entidad</label>
        <select id="auditoria-entidad" onChange={(evento) => cambiar({ entidad: evento.currentTarget.value })} value={filtros.entidad}>
          <option value="">Todas</option>
          {entidades.map((entidad) => <option key={entidad} value={entidad}>{entidad}</option>)}
        </select>
        {errorEntidades !== '' && <small role="alert">{errorEntidades}</small>}
      </div>
      <div>
        <label htmlFor="auditoria-accion">Acción</label>
        <select id="auditoria-accion" onChange={(evento) => cambiar({ accion: evento.currentTarget.value as FiltrosAuditoria['accion'] })} value={filtros.accion}>
          <option value="">Todas</option>
          <option value="Alta">Alta</option>
          <option value="Modificacion">Modificación</option>
          <option value="Baja">Baja</option>
        </select>
      </div>
      <div>
        <label htmlFor="auditoria-usuario">Usuario</label>
        <input id="auditoria-usuario" onChange={(evento) => cambiar({ usuario: evento.currentTarget.value })} type="search" value={filtros.usuario} />
      </div>
      <div>
        <label htmlFor="auditoria-entidad-id">Id del registro</label>
        <input id="auditoria-entidad-id" inputMode="numeric" onChange={(evento) => cambiar({ entidadId: evento.currentTarget.value })} type="text" value={filtros.entidadId} />
      </div>
      <div>
        <label htmlFor="auditoria-fecha-desde">Fecha desde</label>
        <input id="auditoria-fecha-desde" onChange={(evento) => cambiar({ fechaDesde: evento.currentTarget.value })} type="date" value={filtros.fechaDesde} />
      </div>
      <div>
        <label htmlFor="auditoria-fecha-hasta">Fecha hasta</label>
        <input id="auditoria-fecha-hasta" onChange={(evento) => cambiar({ fechaHasta: evento.currentTarget.value })} type="date" value={filtros.fechaHasta} />
      </div>
    </form>
  );
}
