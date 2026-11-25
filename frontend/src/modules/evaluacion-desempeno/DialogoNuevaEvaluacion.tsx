import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { EmpleadoListado } from '../empleados/tipos';
import { mensajeError, useDialogoModal } from './presentacion';
import type { CicloListado, CrearEvaluacionRequest } from './tipos';
import { validarNuevaEvaluacion, type CamposNuevaEvaluacion, type ErroresNuevaEvaluacion } from './validacionEvaluacion';

interface Props {
  ciclos: CicloListado[];
  empleados: EmpleadoListado[];
  cargando: boolean;
  cerrar: () => void;
  crear: (request: CrearEvaluacionRequest) => Promise<void>;
}

function nombreEmpleado(empleado: EmpleadoListado): string {
  return `${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`;
}

// Diálogo modal propio: registra una evaluación de un ciclo abierto (mensajes 17 y 18 de la spec backend).
export function DialogoNuevaEvaluacion({ ciclos, empleados, cargando, cerrar, crear }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const primerCampoRef = useRef<HTMLSelectElement>(null);
  const [campos, setCampos] = useState<CamposNuevaEvaluacion>({ cicloId: '', empleadoId: '', evaluadorId: '' });
  const [errores, setErrores] = useState<ErroresNuevaEvaluacion>({});
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);
  const abiertos = ciclos.filter((ciclo) => ciclo.estado === 'Abierto');

  useEffect(() => {
    primerCampoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  function cambiarCampo(clave: keyof CamposNuevaEvaluacion, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    setMensaje('');
    const nuevosErrores = validarNuevaEvaluacion(campos);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setEnviando(true);
    try {
      await crear({ cicloId: Number(campos.cicloId), empleadoId: Number(campos.empleadoId), evaluadorId: Number(campos.evaluadorId) });
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo registrar la evaluación.'));
    } finally {
      setEnviando(false);
    }
  }

  const textoCarga = cargando ? 'Cargando…' : 'Selecciona una opción';

  return (
    <div className="clientes-dialog-backdrop">
      <div aria-labelledby="evaluacion-nueva-dialog-title" aria-modal="true" className="clientes-dialog evaluacion-desempeno-dialog" ref={dialogoRef} role="dialog" tabIndex={-1}>
        <div className="page-eyebrow">EVALUACIONES</div>
        <h2 id="evaluacion-nueva-dialog-title">Nueva evaluación</h2>
        <form className="evaluacion-desempeno-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
          <CampoConError etiqueta="Ciclo abierto" htmlFor="evaluacion-nueva-ciclo" error={errores.cicloId} idError="evaluacion-nueva-ciclo-error">
            <select
              aria-describedby={errores.cicloId ? 'evaluacion-nueva-ciclo-error' : undefined}
              aria-invalid={Boolean(errores.cicloId)}
              id="evaluacion-nueva-ciclo"
              onChange={(event) => cambiarCampo('cicloId', event.currentTarget.value)}
              ref={primerCampoRef}
              value={campos.cicloId}
            >
              <option value="">{textoCarga}</option>
              {abiertos.map((ciclo) => <option key={ciclo.id} value={String(ciclo.id)}>{`${ciclo.codigo} · ${ciclo.nombre}`}</option>)}
            </select>
          </CampoConError>
          <CampoConError etiqueta="Empleado evaluado" htmlFor="evaluacion-nueva-empleado" error={errores.empleadoId} idError="evaluacion-nueva-empleado-error">
            <select
              aria-describedby={errores.empleadoId ? 'evaluacion-nueva-empleado-error' : undefined}
              aria-invalid={Boolean(errores.empleadoId)}
              id="evaluacion-nueva-empleado"
              onChange={(event) => cambiarCampo('empleadoId', event.currentTarget.value)}
              value={campos.empleadoId}
            >
              <option value="">{textoCarga}</option>
              {empleados.map((empleado) => <option key={empleado.id} value={String(empleado.id)}>{nombreEmpleado(empleado)}</option>)}
            </select>
          </CampoConError>
          <CampoConError etiqueta="Evaluador" htmlFor="evaluacion-nueva-evaluador" error={errores.evaluadorId} idError="evaluacion-nueva-evaluador-error">
            <select
              aria-describedby={errores.evaluadorId ? 'evaluacion-nueva-evaluador-error' : undefined}
              aria-invalid={Boolean(errores.evaluadorId)}
              id="evaluacion-nueva-evaluador"
              onChange={(event) => cambiarCampo('evaluadorId', event.currentTarget.value)}
              value={campos.evaluadorId}
            >
              <option value="">{textoCarga}</option>
              {empleados.map((empleado) => <option key={empleado.id} value={String(empleado.id)}>{nombreEmpleado(empleado)}</option>)}
            </select>
          </CampoConError>
          {!cargando && abiertos.length === 0 && <p className="evaluacion-desempeno-aviso">No hay ciclos abiertos: abre un ciclo para registrar evaluaciones.</p>}
          {mensaje !== '' && (
            <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
              <span aria-hidden="true">!</span>
              {mensaje}
            </p>
          )}
          <div className="clientes-dialog-actions">
            <button className="secondary-button" disabled={enviando} onClick={cerrar} type="button">Cancelar</button>
            <button className="primary-button" disabled={enviando} type="submit">{enviando ? 'Registrando…' : 'Registrar evaluación'}</button>
          </div>
        </form>
      </div>
    </div>
  );
}
