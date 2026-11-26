import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { PaginaEmpleados } from '../empleados/tipos';
import { mensajeError } from './presentacion';
import type { GuardarAsignacionTurnoRequest, PaginaTurnos } from './tipos';
import { MAXIMO_EMPLEADOS_SELECTOR } from './useAsignacionesTurnoData';
import { construirRequestAsignacion, validarAsignacion, type CamposAsignacion, type ErroresAsignacion } from './validacionTurno';

interface Props {
  empleados: PaginaEmpleados | null;
  turnos: PaginaTurnos | null;
  errorEmpleados: string;
  errorTurnos: string;
  cancelar: () => void;
  guardar: (request: GuardarAsignacionTurnoRequest) => Promise<void>;
}

const CAMPOS_INICIALES: CamposAsignacion = { empleadoId: '', turnoId: '', fechaInicio: '', fechaFin: '' };

export function AsignacionFormulario({ empleados, turnos, errorEmpleados, errorTurnos, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposAsignacion>(CAMPOS_INICIALES);
  const [errores, setErrores] = useState<ErroresAsignacion>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  function cambiarCampo(clave: keyof CamposAsignacion, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarAsignacion(campos);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(construirRequestAsignacion(campos));
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar la asignación.'));
    } finally {
      setGuardando(false);
    }
  }

  const turnosActivos = (turnos?.items ?? []).filter((turno) => turno.activo);
  const hayMas = empleados !== null && empleados.total > MAXIMO_EMPLEADOS_SELECTOR;

  return (
    <section aria-labelledby="asignacion-form-title" className="clientes-form-panel turnos-horarios-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">ASIGNACIONES</div>
          <h2 id="asignacion-form-title">Nueva asignación</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="turnos-horarios-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Empleado" htmlFor="asignacion-empleado" error={errores.empleadoId} idError="asignacion-empleado-error">
          <select
            aria-describedby={errores.empleadoId ? 'asignacion-empleado-error' : undefined}
            aria-invalid={Boolean(errores.empleadoId)}
            id="asignacion-empleado"
            onChange={(event) => cambiarCampo('empleadoId', event.currentTarget.value)}
            value={campos.empleadoId}
          >
            <option value="">{empleados === null && errorEmpleados === '' ? 'Cargando empleados…' : 'Selecciona un empleado'}</option>
            {(empleados?.items ?? []).map((empleado) => (
              <option key={empleado.id} value={String(empleado.id)}>
                {`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}
              </option>
            ))}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Turno" htmlFor="asignacion-turno" error={errores.turnoId} idError="asignacion-turno-error">
          <select
            aria-describedby={errores.turnoId ? 'asignacion-turno-error' : undefined}
            aria-invalid={Boolean(errores.turnoId)}
            id="asignacion-turno"
            onChange={(event) => cambiarCampo('turnoId', event.currentTarget.value)}
            value={campos.turnoId}
          >
            <option value="">{turnos === null && errorTurnos === '' ? 'Cargando turnos…' : 'Selecciona un turno'}</option>
            {turnosActivos.map((turno) => (
              <option key={turno.id} value={String(turno.id)}>{`${turno.codigo} · ${turno.nombre}`}</option>
            ))}
          </select>
        </CampoConError>
        {errorEmpleados !== '' && <p className="clientes-error turnos-horarios-form-wide" role="alert">{errorEmpleados}</p>}
        {errorTurnos !== '' && <p className="clientes-error turnos-horarios-form-wide" role="alert">{errorTurnos}</p>}
        {hayMas && <p className="turnos-horarios-aviso turnos-horarios-form-wide">Se muestran los primeros 100 empleados activos.</p>}
        <CampoConError etiqueta="Fecha de inicio" htmlFor="asignacion-fecha-inicio" error={errores.fechaInicio} idError="asignacion-fecha-inicio-error">
          <input
            aria-describedby={errores.fechaInicio ? 'asignacion-fecha-inicio-error' : undefined}
            aria-invalid={Boolean(errores.fechaInicio)}
            id="asignacion-fecha-inicio"
            onChange={(event) => cambiarCampo('fechaInicio', event.currentTarget.value)}
            type="date"
            value={campos.fechaInicio}
          />
        </CampoConError>
        <CampoConError etiqueta="Fecha de fin (opcional)" htmlFor="asignacion-fecha-fin" error={errores.fechaFin} idError="asignacion-fecha-fin-error">
          <input
            aria-describedby={errores.fechaFin ? 'asignacion-fecha-fin-error' : undefined}
            aria-invalid={Boolean(errores.fechaFin)}
            id="asignacion-fecha-fin"
            onChange={(event) => cambiarCampo('fechaFin', event.currentTarget.value)}
            type="date"
            value={campos.fechaFin}
          />
        </CampoConError>
        <p className="turnos-horarios-aviso turnos-horarios-form-wide">
          Si el empleado tiene una asignación abierta anterior, se cerrará el día antes de la fecha de inicio.
        </p>
        <div className="turnos-horarios-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar asignación'}
          </button>
        </div>
      </form>
    </section>
  );
}
