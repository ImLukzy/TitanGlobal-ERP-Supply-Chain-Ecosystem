import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearCantidad, formatearFecha } from '../../lib/formato';
import { PermisosEfectivos } from './PermisosEfectivos';
import { EstadoPastilla, RolesPastillas } from './UsuarioPastillas';
import type { PermisoEfectivo, UsuarioDetalle } from './tipos';

interface Props {
  usuario: UsuarioDetalle | null;
  permisos: PermisoEfectivo[] | null;
  cargando: boolean;
  error: string;
  errorPermisos: string;
}

export function UsuarioDetallePanel({ usuario, permisos, cargando, error, errorPermisos }: Props) {
  return (
    <PanelDetalle ariaLabelledBy="usuario-detail-title" className="clientes-detail-panel usuarios-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando usuario…</p>}
      {error === '' && !cargando && usuario === null && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona un usuario para ver sus permisos efectivos.</p>
      )}
      {usuario !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">FICHA DEL USUARIO</div>
              <h2 id="usuario-detail-title">{usuario.nombreCompleto}</h2>
            </div>
            <span aria-hidden="true" className="cliente-initials">
              {usuario.nombreCompleto.slice(0, 1).toLocaleUpperCase('es-PE')}
            </span>
          </div>
          <dl className="usuarios-detail-grid">
            <div>
              <dt>Usuario</dt>
              <dd>{usuario.nombreUsuario}</dd>
            </div>
            <div>
              <dt>Email</dt>
              <dd>{usuario.email}</dd>
            </div>
            <div>
              <dt>Fecha de alta</dt>
              <dd>{formatearFecha(usuario.fechaAlta)}</dd>
            </div>
            <div>
              <dt>Estado</dt>
              <dd><EstadoPastilla activo={usuario.activo} /></dd>
            </div>
            <div className="usuarios-detail-roles">
              <dt>Roles</dt>
              <dd><RolesPastillas codigos={usuario.roles} /></dd>
            </div>
          </dl>
          <section aria-labelledby="usuario-permisos-title" className="usuarios-permisos">
            <h3 id="usuario-permisos-title">Permisos efectivos</h3>
            <p className="usuarios-permisos-resumen">
              {formatearCantidad(usuario.modulosConLectura, 'módulo', 'módulos')} con lectura, {usuario.modulosConEscritura} con escritura
            </p>
            {errorPermisos !== '' && <p className="clientes-error" role="alert">{errorPermisos}</p>}
            {errorPermisos === '' && permisos === null && (
              <p aria-live="polite" className="clientes-status" role="status">Cargando permisos…</p>
            )}
            {permisos !== null && <PermisosEfectivos permisos={permisos} />}
          </section>
        </>
      )}
    </PanelDetalle>
  );
}
