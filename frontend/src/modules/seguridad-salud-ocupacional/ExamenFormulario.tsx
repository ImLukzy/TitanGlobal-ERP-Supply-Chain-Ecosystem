import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { EmpleadoListado } from '../empleados/tipos';
import { hoyIso, mensajeError, RESULTADOS_EXAMEN, textoValor, TIPOS_EXAMEN } from './presentacion';
import type { RegistrarExamenRequest } from './tipos';
import { construirRequestExamen, LIMITE_CLINICA, LIMITE_RESTRICCIONES, validarExamen, type CamposExamen, type Errores } from './validacionSst';

interface Props {
  empleados: EmpleadoListado[];
  cancelar: () => void;
  guardar: (request: RegistrarExamenRequest) => Promise<void>;
}

const INICIALES: CamposExamen = { empleadoId: '', tipo: '', fecha: '', resultado: '', restricciones: '', fechaVencimiento: '', clinica: '' };

// Formulario de examen (mensajes 8, 11 y 13 a 19). Las restricciones solo existen con AptoConRestricciones
// y el vencimiento no existe en los exámenes de retiro; el backend valida el estado del empleado (mensaje 18).
export function ExamenFormulario({ empleados, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposExamen>(INICIALES);
  const [errores, setErrores] = useState<Errores<CamposExamen>>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const hoy = hoyIso();

  function cambiarCampo(clave: keyof CamposExamen, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevos = validarExamen(campos, hoy);
    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(construirRequestExamen(campos));
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo registrar el examen médico.'));
    } finally {
      setGuardando(false);
    }
  }

  function describir(clave: keyof CamposExamen): string | undefined {
    return errores[clave] ? `examen-${clave}-error` : undefined;
  }

  return (
    <section aria-labelledby="examen-form-title" className="clientes-form-panel sst-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">EXÁMENES MÉDICOS</div>
          <h2 id="examen-form-title">Nuevo examen médico</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="sst-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Empleado" htmlFor="examen-empleado" error={errores.empleadoId} idError="examen-empleadoId-error">
          <select aria-describedby={describir('empleadoId')} aria-invalid={Boolean(errores.empleadoId)} id="examen-empleado" onChange={(event) => cambiarCampo('empleadoId', event.currentTarget.value)} value={campos.empleadoId}>
            <option value="">Selecciona un empleado</option>
            {empleados.map((empleado) => <option key={empleado.id} value={String(empleado.id)}>{`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Tipo de examen" htmlFor="examen-tipo" error={errores.tipo} idError="examen-tipo-error">
          <select aria-describedby={describir('tipo')} aria-invalid={Boolean(errores.tipo)} id="examen-tipo" onChange={(event) => cambiarCampo('tipo', event.currentTarget.value)} value={campos.tipo}>
            <option value="">Selecciona un tipo</option>
            {TIPOS_EXAMEN.map((tipo) => <option key={tipo} value={tipo}>{textoValor(tipo)}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Fecha del examen" htmlFor="examen-fecha" error={errores.fecha} idError="examen-fecha-error">
          <input aria-describedby={describir('fecha')} aria-invalid={Boolean(errores.fecha)} id="examen-fecha" max={hoy} onChange={(event) => cambiarCampo('fecha', event.currentTarget.value)} type="date" value={campos.fecha} />
        </CampoConError>
        <CampoConError etiqueta="Resultado" htmlFor="examen-resultado" error={errores.resultado} idError="examen-resultado-error">
          <select aria-describedby={describir('resultado')} aria-invalid={Boolean(errores.resultado)} id="examen-resultado" onChange={(event) => cambiarCampo('resultado', event.currentTarget.value)} value={campos.resultado}>
            <option value="">Selecciona el resultado</option>
            {RESULTADOS_EXAMEN.map((resultado) => <option key={resultado} value={resultado}>{textoValor(resultado)}</option>)}
          </select>
        </CampoConError>
        {campos.tipo !== 'Retiro' && (
          <CampoConError etiqueta="Fecha de vencimiento" htmlFor="examen-vencimiento" error={errores.fechaVencimiento} idError="examen-fechaVencimiento-error">
            <input aria-describedby={describir('fechaVencimiento')} aria-invalid={Boolean(errores.fechaVencimiento)} id="examen-vencimiento" onChange={(event) => cambiarCampo('fechaVencimiento', event.currentTarget.value)} type="date" value={campos.fechaVencimiento} />
          </CampoConError>
        )}
        <CampoConError etiqueta="Clínica (opcional)" htmlFor="examen-clinica" error={errores.clinica} idError="examen-clinica-error">
          <input aria-describedby={describir('clinica')} aria-invalid={Boolean(errores.clinica)} autoComplete="off" id="examen-clinica" maxLength={LIMITE_CLINICA + 20} onChange={(event) => cambiarCampo('clinica', event.currentTarget.value)} type="text" value={campos.clinica} />
        </CampoConError>
        {campos.resultado === 'AptoConRestricciones' && (
          <div className="sst-form-wide">
            <CampoConError etiqueta="Restricciones" htmlFor="examen-restricciones" error={errores.restricciones} idError="examen-restricciones-error">
              <textarea aria-describedby={describir('restricciones')} aria-invalid={Boolean(errores.restricciones)} id="examen-restricciones" maxLength={LIMITE_RESTRICCIONES + 50} onChange={(event) => cambiarCampo('restricciones', event.currentTarget.value)} rows={3} value={campos.restricciones} />
            </CampoConError>
          </div>
        )}
        <div className="sst-form-actions">
          {mensaje !== '' && <p aria-live="polite" className="clientes-error" role="alert">{mensaje}</p>}
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Registrar examen'}</button>
        </div>
      </form>
    </section>
  );
}
