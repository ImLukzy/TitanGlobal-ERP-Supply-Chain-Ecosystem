import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError } from './presentacion';
import type { EmpleadoDetalle, GuardarEmpleadoRequest, TipoDocumento } from './tipos';
import { validarEmpleado, type CamposEmpleado, type ErroresEmpleado } from './validacionEmpleado';

interface Props {
  empleado: EmpleadoDetalle | null;
  cancelar: () => void;
  guardar: (request: GuardarEmpleadoRequest) => Promise<void>;
}

function camposIniciales(empleado: EmpleadoDetalle | null): CamposEmpleado {
  return {
    tipoDocumento: empleado?.tipoDocumento ?? 'DNI',
    numeroDocumento: empleado?.numeroDocumento ?? '',
    nombres: empleado?.nombres ?? '',
    apellidos: empleado?.apellidos ?? '',
    fechaNacimiento: empleado?.fechaNacimiento ?? '',
    fechaIngreso: empleado?.fechaIngreso ?? '',
    cargo: empleado?.cargo ?? '',
    area: empleado?.area ?? '',
    email: empleado?.email ?? '',
    telefono: empleado?.telefono ?? '',
  };
}

// El select solo ofrece los tres tipos; cualquier otro valor cae en DNI.
function aTipoDocumento(valor: string): TipoDocumento {
  if (valor === 'CE' || valor === 'Pasaporte') {
    return valor;
  }
  return 'DNI';
}

// Los opcionales vacíos viajan como null, no como cadena vacía.
function valorOpcional(valor: string): string | null {
  const limpio = valor.trim();
  if (limpio === '') {
    return null;
  }
  return limpio;
}

export function EmpleadoFormulario({ empleado, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposEmpleado>(() => camposIniciales(empleado));
  const [errores, setErrores] = useState<ErroresEmpleado>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const titulo = empleado === null ? 'Nuevo empleado' : 'Editar empleado';

  function cambiarCampo(clave: keyof CamposEmpleado, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarEmpleado(campos);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    const request: GuardarEmpleadoRequest = {
      tipoDocumento: aTipoDocumento(campos.tipoDocumento),
      numeroDocumento: campos.numeroDocumento.trim(),
      nombres: campos.nombres.trim(),
      apellidos: campos.apellidos.trim(),
      fechaNacimiento: campos.fechaNacimiento,
      fechaIngreso: campos.fechaIngreso,
      cargo: campos.cargo.trim(),
      area: campos.area.trim(),
      email: valorOpcional(campos.email.toLowerCase()),
      telefono: valorOpcional(campos.telefono),
    };
    setGuardando(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el empleado.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="empleado-form-title" className="clientes-form-panel empleados-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">EMPLEADOS</div>
          <h2 id="empleado-form-title">{titulo}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="empleado-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Tipo de documento" htmlFor="empleado-tipo-documento" error={errores.tipoDocumento} idError="empleado-tipo-documento-error">
          <select
            aria-describedby={errores.tipoDocumento ? 'empleado-tipo-documento-error' : undefined}
            aria-invalid={Boolean(errores.tipoDocumento)}
            id="empleado-tipo-documento"
            onChange={(event) => cambiarCampo('tipoDocumento', event.currentTarget.value)}
            value={campos.tipoDocumento}
          >
            <option value="DNI">DNI</option>
            <option value="CE">CE</option>
            <option value="Pasaporte">Pasaporte</option>
          </select>
        </CampoConError>
        <CampoConError etiqueta="Número de documento" htmlFor="empleado-numero-documento" error={errores.numeroDocumento} idError="empleado-numero-documento-error">
          <input
            aria-describedby={errores.numeroDocumento ? 'empleado-numero-documento-error' : undefined}
            aria-invalid={Boolean(errores.numeroDocumento)}
            autoComplete="off"
            id="empleado-numero-documento"
            maxLength={20}
            onChange={(event) => cambiarCampo('numeroDocumento', event.currentTarget.value)}
            value={campos.numeroDocumento}
          />
        </CampoConError>
        <CampoConError etiqueta="Nombres" htmlFor="empleado-nombres" error={errores.nombres} idError="empleado-nombres-error">
          <input
            aria-describedby={errores.nombres ? 'empleado-nombres-error' : undefined}
            aria-invalid={Boolean(errores.nombres)}
            autoComplete="off"
            id="empleado-nombres"
            maxLength={100}
            onChange={(event) => cambiarCampo('nombres', event.currentTarget.value)}
            value={campos.nombres}
          />
        </CampoConError>
        <CampoConError etiqueta="Apellidos" htmlFor="empleado-apellidos" error={errores.apellidos} idError="empleado-apellidos-error">
          <input
            aria-describedby={errores.apellidos ? 'empleado-apellidos-error' : undefined}
            aria-invalid={Boolean(errores.apellidos)}
            autoComplete="off"
            id="empleado-apellidos"
            maxLength={100}
            onChange={(event) => cambiarCampo('apellidos', event.currentTarget.value)}
            value={campos.apellidos}
          />
        </CampoConError>
        <CampoConError etiqueta="Fecha de nacimiento" htmlFor="empleado-fecha-nacimiento" error={errores.fechaNacimiento} idError="empleado-fecha-nacimiento-error">
          <input
            aria-describedby={errores.fechaNacimiento ? 'empleado-fecha-nacimiento-error' : undefined}
            aria-invalid={Boolean(errores.fechaNacimiento)}
            id="empleado-fecha-nacimiento"
            onChange={(event) => cambiarCampo('fechaNacimiento', event.currentTarget.value)}
            type="date"
            value={campos.fechaNacimiento}
          />
        </CampoConError>
        <CampoConError etiqueta="Fecha de ingreso" htmlFor="empleado-fecha-ingreso" error={errores.fechaIngreso} idError="empleado-fecha-ingreso-error">
          <input
            aria-describedby={errores.fechaIngreso ? 'empleado-fecha-ingreso-error' : undefined}
            aria-invalid={Boolean(errores.fechaIngreso)}
            id="empleado-fecha-ingreso"
            onChange={(event) => cambiarCampo('fechaIngreso', event.currentTarget.value)}
            type="date"
            value={campos.fechaIngreso}
          />
        </CampoConError>
        <CampoConError etiqueta="Cargo" htmlFor="empleado-cargo" error={errores.cargo} idError="empleado-cargo-error">
          <input
            aria-describedby={errores.cargo ? 'empleado-cargo-error' : undefined}
            aria-invalid={Boolean(errores.cargo)}
            autoComplete="off"
            id="empleado-cargo"
            maxLength={100}
            onChange={(event) => cambiarCampo('cargo', event.currentTarget.value)}
            value={campos.cargo}
          />
        </CampoConError>
        <CampoConError etiqueta="Área" htmlFor="empleado-area" error={errores.area} idError="empleado-area-error">
          <input
            aria-describedby={errores.area ? 'empleado-area-error' : undefined}
            aria-invalid={Boolean(errores.area)}
            autoComplete="off"
            id="empleado-area"
            maxLength={100}
            onChange={(event) => cambiarCampo('area', event.currentTarget.value)}
            value={campos.area}
          />
        </CampoConError>
        <CampoConError etiqueta="Email" htmlFor="empleado-email" error={errores.email} idError="empleado-email-error">
          <input
            aria-describedby={errores.email ? 'empleado-email-error' : undefined}
            aria-invalid={Boolean(errores.email)}
            autoComplete="email"
            id="empleado-email"
            maxLength={150}
            onChange={(event) => cambiarCampo('email', event.currentTarget.value)}
            type="email"
            value={campos.email}
          />
        </CampoConError>
        <CampoConError etiqueta="Teléfono" htmlFor="empleado-telefono" error={errores.telefono} idError="empleado-telefono-error">
          <input
            aria-describedby={errores.telefono ? 'empleado-telefono-error' : undefined}
            aria-invalid={Boolean(errores.telefono)}
            autoComplete="tel"
            id="empleado-telefono"
            maxLength={20}
            onChange={(event) => cambiarCampo('telefono', event.currentTarget.value)}
            value={campos.telefono}
          />
        </CampoConError>
        <div className="empleado-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar empleado'}
          </button>
        </div>
      </form>
    </section>
  );
}
