import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError } from './presentacion';
import type { GuardarUsuarioRequest, RolOpcion, UsuarioDetalle } from './tipos';
import { validarUsuario, type ErroresUsuario } from './validacionUsuario';

interface Props {
  usuario: UsuarioDetalle | null;
  roles: RolOpcion[];
  cancelar: () => void;
  guardar: (request: GuardarUsuarioRequest) => Promise<void>;
}

export function UsuarioFormulario({ usuario, roles, cancelar, guardar }: Props) {
  const [nombreUsuario, setNombreUsuario] = useState(usuario?.nombreUsuario ?? '');
  const [nombreCompleto, setNombreCompleto] = useState(usuario?.nombreCompleto ?? '');
  const [email, setEmail] = useState(usuario?.email ?? '');
  const [activo, setActivo] = useState(usuario?.activo ?? true);
  const [rolIds, setRolIds] = useState<number[]>(usuario?.rolIds ?? []);
  const [errores, setErrores] = useState<ErroresUsuario>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  function alternarRol(id: number, marcado: boolean): void {
    setRolIds((actual) => {
      if (marcado) {
        return actual.includes(id) ? actual : [...actual, id];
      }
      return actual.filter((item) => item !== id);
    });
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarUsuario({ nombreUsuario, nombreCompleto, email });
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    const request: GuardarUsuarioRequest = {
      nombreUsuario: nombreUsuario.trim().toLowerCase(),
      nombreCompleto: nombreCompleto.trim(),
      email: email.trim().toLowerCase(),
      activo,
      rolIds: [...rolIds].sort((a, b) => a - b),
    };
    setGuardando(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el usuario.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="usuario-form-title" className="clientes-form-panel usuarios-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">USUARIOS</div>
          <h2 id="usuario-form-title">{usuario === null ? 'Nuevo usuario' : 'Editar usuario'}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="usuario-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Nombre de usuario" htmlFor="usuario-nombre-usuario" error={errores.nombreUsuario} idError="usuario-nombre-usuario-error">
          <input
            aria-describedby={errores.nombreUsuario ? 'usuario-nombre-usuario-error' : undefined}
            aria-invalid={Boolean(errores.nombreUsuario)}
            autoComplete="off"
            id="usuario-nombre-usuario"
            maxLength={40}
            onChange={(event) => setNombreUsuario(event.currentTarget.value.toLowerCase())}
            value={nombreUsuario}
          />
        </CampoConError>
        <CampoConError etiqueta="Nombre completo" htmlFor="usuario-nombre-completo" error={errores.nombreCompleto} idError="usuario-nombre-completo-error">
          <input
            aria-describedby={errores.nombreCompleto ? 'usuario-nombre-completo-error' : undefined}
            aria-invalid={Boolean(errores.nombreCompleto)}
            autoComplete="name"
            id="usuario-nombre-completo"
            maxLength={120}
            onChange={(event) => setNombreCompleto(event.currentTarget.value)}
            value={nombreCompleto}
          />
        </CampoConError>
        <CampoConError etiqueta="Email" htmlFor="usuario-email" error={errores.email} idError="usuario-email-error">
          <input
            aria-describedby={errores.email ? 'usuario-email-error' : undefined}
            aria-invalid={Boolean(errores.email)}
            autoComplete="email"
            id="usuario-email"
            maxLength={160}
            onChange={(event) => setEmail(event.currentTarget.value)}
            type="email"
            value={email}
          />
        </CampoConError>
        <label className="usuario-active-field">
          <input checked={activo} onChange={(event) => setActivo(event.currentTarget.checked)} type="checkbox" />
          Activo
        </label>
        <fieldset className="usuario-roles-field">
          <legend>Roles</legend>
          {roles.length === 0 ? (
            <p role="status">No hay roles disponibles.</p>
          ) : (
            <div className="usuario-roles-list">
              {roles.map((rol) => (
                <label className="usuario-check" key={rol.id}>
                  <input
                    checked={rolIds.includes(rol.id)}
                    onChange={(event) => alternarRol(rol.id, event.currentTarget.checked)}
                    type="checkbox"
                  />
                  <span>{rol.codigo} · {rol.nombre}</span>
                </label>
              ))}
            </div>
          )}
        </fieldset>
        <div className="usuarios-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar usuario'}
          </button>
        </div>
      </form>
    </section>
  );
}
