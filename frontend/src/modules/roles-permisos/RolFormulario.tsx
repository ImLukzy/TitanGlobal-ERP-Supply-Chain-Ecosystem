import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { actualizarRol, crearRol } from './api';
import { mensajeError } from './errores';
import type { GuardarRolRequest, RolDetalle } from './tipos';
import { validarRol, type ErroresRol } from './validacion';

interface RolFormularioProps {
  rol: RolDetalle | null;
  cancelar: () => void;
  guardado: () => void;
}

export function RolFormulario({ rol, cancelar, guardado }: RolFormularioProps) {
  const [codigo, setCodigo] = useState(rol?.codigo ?? '');
  const [nombre, setNombre] = useState(rol?.nombre ?? '');
  const [descripcion, setDescripcion] = useState(rol?.descripcion ?? '');
  const [errores, setErrores] = useState<ErroresRol>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const codigoBloqueado = rol?.esSistema === true;

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const request: GuardarRolRequest = {
      codigo: codigo.trim().toUpperCase(),
      nombre: nombre.trim(),
    };
    if (descripcion.trim() !== '') {
      request.descripcion = descripcion.trim();
    }
    const nuevosErrores = validarRol(request);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }

    setGuardando(true);
    try {
      if (rol === null) {
        await crearRol(request);
      } else {
        await actualizarRol(rol.id, request);
      }
      guardado();
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el rol.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="rol-form-title" className="clientes-form-panel roles-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">ROLES Y PERMISOS</div>
          <h2 id="rol-form-title">{rol === null ? 'Nuevo rol' : 'Editar rol'}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="rol-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Código" htmlFor="rol-codigo" error={errores.codigo} idError="rol-codigo-error">
          <input
            aria-describedby={errores.codigo ? 'rol-codigo-error' : undefined}
            aria-invalid={Boolean(errores.codigo)}
            autoComplete="off"
            disabled={codigoBloqueado}
            id="rol-codigo"
            maxLength={30}
            onChange={(event) => setCodigo(event.currentTarget.value.toUpperCase())}
            value={codigo}
          />
        </CampoConError>
        <CampoConError etiqueta="Nombre" htmlFor="rol-nombre" error={errores.nombre} idError="rol-nombre-error">
          <input
            aria-describedby={errores.nombre ? 'rol-nombre-error' : undefined}
            aria-invalid={Boolean(errores.nombre)}
            autoComplete="off"
            id="rol-nombre"
            maxLength={120}
            onChange={(event) => setNombre(event.currentTarget.value)}
            value={nombre}
          />
        </CampoConError>
        {codigoBloqueado && (
          <p className="rol-form-nota">El código de un rol de sistema no se puede cambiar.</p>
        )}
        <div className="rol-campo-ancho">
          <CampoConError etiqueta="Descripción" htmlFor="rol-descripcion" error={errores.descripcion} idError="rol-descripcion-error">
            <textarea
              aria-describedby={errores.descripcion ? 'rol-descripcion-error' : undefined}
              aria-invalid={Boolean(errores.descripcion)}
              id="rol-descripcion"
              maxLength={320}
              onChange={(event) => setDescripcion(event.currentTarget.value)}
              rows={3}
              value={descripcion}
            />
          </CampoConError>
        </div>
        <div className="roles-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar rol'}
          </button>
        </div>
      </form>
    </section>
  );
}
