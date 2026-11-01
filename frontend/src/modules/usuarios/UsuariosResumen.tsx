import { formatearCantidad } from '../../lib/formato';
import { RolesPastillas } from './UsuarioPastillas';
import type { ResumenUsuarios } from './tipos';

interface Props {
  datos: ResumenUsuarios | null;
  error: string;
}

export function UsuariosResumen({ datos, error }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen de usuarios…</p>;
  }
  return (
    <section aria-label="Resumen de usuarios" className="usuarios-summary">
      <article className="cliente-stat">
        <span>Total de usuarios</span>
        <strong>{datos.total}</strong>
      </article>
      <article className="cliente-stat">
        <span>Activos</span>
        <strong>{datos.activos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Inactivos</span>
        <strong>{datos.inactivos}</strong>
      </article>
      <article className="cliente-stat">
        <span>Sin rol</span>
        <strong>{datos.sinRol}</strong>
      </article>
      <section aria-labelledby="usuarios-roles-title" className="solicitudes-top-panel usuarios-roles-panel">
        <h2 id="usuarios-roles-title">Usuarios por rol</h2>
        {datos.porRol.length === 0 ? (
          <p role="status">Sin roles registrados</p>
        ) : (
          <ol>
            {datos.porRol.map((rol) => (
              <li key={rol.rolId}>
                <RolesPastillas codigos={[rol.codigo]} />
                <strong>{formatearCantidad(rol.usuarios, 'usuario', 'usuarios')}</strong>
              </li>
            ))}
          </ol>
        )}
      </section>
    </section>
  );
}
