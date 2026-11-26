import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { PaginaEmpleados } from '../empleados/tipos';
import { mensajeError, TIPOS_MARCACION } from './presentacion';
import type { RegistrarMarcacionRequest } from './tipos';
import { MAXIMO_EMPLEADOS_SELECTOR } from './useJornadasData';
import { construirRequestMarcacion, validarMarcacion, type CamposMarcacion, type ErroresMarcacion } from './validacionMarcacion';

interface Props {
  empleados: PaginaEmpleados | null;
  errorEmpleados: string;
  cancelar: () => void;
  guardar: (request: RegistrarMarcacionRequest) => Promise<void>;
}

const CAMPOS_INICIALES: CamposMarcacion = { empleadoId: '', tipo: '', fecha: '', hora: '', observacion: '' };

export function MarcacionFormulario({ empleados, errorEmpleados, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposMarcacion>(CAMPOS_INICIALES);
  const [errores, setErrores] = useState<ErroresMarcacion>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  function cambiarCampo(clave: keyof CamposMarcacion, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarMarcacion(campos);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(construirRequestMarcacion(campos));
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo registrar la marcación.'));
    } finally {
      setGuardando(false);
    }
  }

  const hayMas = empleados !== null && empleados.total > MAXIMO_EMPLEADOS_SELECTOR;

  return (
    <section aria-labelledby="marcacion-form-title" className="clientes-form-panel asistencia-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">MARCACIONES</div>
          <h2 id="marcacion-form-title">Registrar marcación</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="asistencia-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Empleado" htmlFor="marcacion-empleado" error={errores.empleadoId} idError="marcacion-empleado-error">
          <select
            aria-describedby={errores.empleadoId ? 'marcacion-empleado-error' : undefined}
            aria-invalid={Boolean(errores.empleadoId)}
            id="marcacion-empleado"
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
        <CampoConError etiqueta="Tipo" htmlFor="marcacion-tipo" error={errores.tipo} idError="marcacion-tipo-error">
          <select
            aria-describedby={errores.tipo ? 'marcacion-tipo-error' : undefined}
            aria-invalid={Boolean(errores.tipo)}
            id="marcacion-tipo"
            onChange={(event) => cambiarCampo('tipo', event.currentTarget.value)}
            value={campos.tipo}
          >
            <option value="">Selecciona el tipo</option>
            {TIPOS_MARCACION.map((tipo) => <option key={tipo} value={tipo}>{tipo}</option>)}
          </select>
        </CampoConError>
        {errorEmpleados !== '' && <p className="clientes-error asistencia-form-wide" role="alert">{errorEmpleados}</p>}
        {hayMas && <p className="asistencia-aviso asistencia-form-wide">Se muestran los primeros 100 empleados.</p>}
        <CampoConError etiqueta="Fecha (hora de Perú)" htmlFor="marcacion-fecha" error={errores.fecha} idError="marcacion-fecha-error">
          <input
            aria-describedby={errores.fecha ? 'marcacion-fecha-error' : undefined}
            aria-invalid={Boolean(errores.fecha)}
            id="marcacion-fecha"
            onChange={(event) => cambiarCampo('fecha', event.currentTarget.value)}
            type="date"
            value={campos.fecha}
          />
        </CampoConError>
        <CampoConError etiqueta="Hora (hora de Perú)" htmlFor="marcacion-hora" error={errores.hora} idError="marcacion-hora-error">
          <input
            aria-describedby={errores.hora ? 'marcacion-hora-error' : undefined}
            aria-invalid={Boolean(errores.hora)}
            id="marcacion-hora"
            onChange={(event) => cambiarCampo('hora', event.currentTarget.value)}
            type="time"
            value={campos.hora}
          />
        </CampoConError>
        <div className="asistencia-form-wide">
          <CampoConError etiqueta="Observación" htmlFor="marcacion-observacion" error={errores.observacion} idError="marcacion-observacion-error">
            <input
              aria-describedby={errores.observacion ? 'marcacion-observacion-error' : undefined}
              aria-invalid={Boolean(errores.observacion)}
              id="marcacion-observacion"
              onChange={(event) => cambiarCampo('observacion', event.currentTarget.value)}
              type="text"
              value={campos.observacion}
            />
          </CampoConError>
        </div>
        <div className="asistencia-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Registrar marcación'}
          </button>
        </div>
      </form>
    </section>
  );
}
