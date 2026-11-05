import { useState, type FormEvent } from 'react';
import { iniciarSesion } from '../api/autenticacion';
import { ApiError } from '../api/client';
import { CampoConError } from '../components/CampoConError';
import { useSesion } from '../layout/SesionContext';
import './inicio-sesion.css';

const MENSAJE_OBLIGATORIOS = 'El usuario y la contraseña son obligatorios.';
const MENSAJE_GENERICO = 'No se pudo iniciar sesión.';

export function InicioSesion() {
  const { guardarSesion } = useSesion();
  const [nombreUsuario, setNombreUsuario] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (nombreUsuario.trim() === '' || contrasena === '') {
      setError(MENSAJE_OBLIGATORIOS);
      return;
    }
    setError(null);
    setEnviando(true);
    try {
      guardarSesion(await iniciarSesion(nombreUsuario.trim(), contrasena));
    } catch (fallo: unknown) {
      setError(fallo instanceof ApiError ? fallo.message : MENSAJE_GENERICO);
      setEnviando(false);
    }
  }

  return (
    <main className="inicio-sesion">
      <section className="inicio-sesion-tarjeta" aria-labelledby="titulo-inicio-sesion">
        <div className="inicio-sesion-marca">
          <span className="brand-mark" aria-hidden="true">T</span>
          <span>TitanGlobal ERP</span>
        </div>
        <h1 id="titulo-inicio-sesion">Iniciar sesión</h1>
        <form className="inicio-sesion-formulario" noValidate onSubmit={(event) => void enviar(event)}>
          <CampoConError etiqueta="Usuario" htmlFor="inicio-usuario" idError="inicio-usuario-error">
            <input
              autoComplete="username"
              autoFocus
              id="inicio-usuario"
              name="usuario"
              onChange={(event) => setNombreUsuario(event.currentTarget.value)}
              type="text"
              value={nombreUsuario}
            />
          </CampoConError>
          <CampoConError etiqueta="Contraseña" htmlFor="inicio-contrasena" idError="inicio-contrasena-error">
            <input
              autoComplete="current-password"
              id="inicio-contrasena"
              name="contrasena"
              onChange={(event) => setContrasena(event.currentTarget.value)}
              type="password"
              value={contrasena}
            />
          </CampoConError>
          {error !== null && <p className="inicio-sesion-error" role="alert">{error}</p>}
          <button className="primary-button inicio-sesion-boton" disabled={enviando} type="submit">
            {enviando ? 'Entrando…' : 'Iniciar sesión'}
          </button>
        </form>
        <p className="inicio-sesion-aviso">Usuario de demostración: invitado · Contraseña: Demo2026!</p>
      </section>
    </main>
  );
}
