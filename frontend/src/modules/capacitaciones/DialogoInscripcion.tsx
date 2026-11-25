import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { EmpleadoListado } from '../empleados/tipos';
import { mensajeError, useDialogoModal } from './presentacion';
import type { ProgramacionDetalle } from './tipos';

interface Props {
  programacion: ProgramacionDetalle;
  empleados: EmpleadoListado[];
  cargando: boolean;
  cerrar: () => void;
  inscribir: (empleadoId: number) => Promise<void>;
}

// Diálogo modal propio: inscribe a un empleado activo (mensajes 16 a 19 de la spec backend).
// No ofrece a quien ya está inscrito; un empleado retirado sí puede volver a inscribirse.
export function DialogoInscripcion({ programacion, empleados, cargando, cerrar, inscribir }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const campoRef = useRef<HTMLSelectElement>(null);
  const [empleadoId, setEmpleadoId] = useState('');
  const [error, setError] = useState('');
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);
  const ocupados = new Set(programacion.inscripciones.filter((inscripcion) => inscripcion.estado !== 'Retirado').map((inscripcion) => inscripcion.empleadoId));
  const disponibles = empleados.filter((empleado) => !ocupados.has(empleado.id));

  useEffect(() => {
    campoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    setMensaje('');
    if (empleadoId === '') {
      setError('Selecciona un empleado.');
      return;
    }
    setError('');
    setEnviando(true);
    try {
      await inscribir(Number(empleadoId));
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo inscribir al empleado.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div aria-labelledby="inscripcion-dialog-title" aria-modal="true" className="clientes-dialog capacitaciones-dialog" ref={dialogoRef} role="dialog" tabIndex={-1}>
        <div className="page-eyebrow">PROGRAMACIONES</div>
        <h2 id="inscripcion-dialog-title">Inscribir empleado</h2>
        <form className="capacitaciones-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
          <CampoConError etiqueta="Empleado" htmlFor="inscripcion-empleado" error={error} idError="inscripcion-empleado-error">
            <select aria-describedby={error === '' ? undefined : 'inscripcion-empleado-error'} aria-invalid={error !== ''} id="inscripcion-empleado" onChange={(event) => setEmpleadoId(event.currentTarget.value)} ref={campoRef} value={empleadoId}>
              <option value="">{cargando ? 'Cargando…' : 'Selecciona un empleado'}</option>
              {disponibles.map((empleado) => <option key={empleado.id} value={String(empleado.id)}>{`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}</option>)}
            </select>
          </CampoConError>
          {mensaje !== '' && (
            <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
              <span aria-hidden="true">!</span>
              {mensaje}
            </p>
          )}
          <div className="clientes-dialog-actions">
            <button className="secondary-button" disabled={enviando} onClick={cerrar} type="button">Cancelar</button>
            <button className="primary-button" disabled={enviando} type="submit">{enviando ? 'Inscribiendo…' : 'Inscribir empleado'}</button>
          </div>
        </form>
      </div>
    </div>
  );
}
