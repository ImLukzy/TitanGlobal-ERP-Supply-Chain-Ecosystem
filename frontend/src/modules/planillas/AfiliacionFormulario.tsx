import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { PaginaEmpleados } from '../empleados/tipos';
import { hoyUtc, MAXIMO_EMPLEADOS_SELECTOR, mensajeError } from './presentacion';
import type { AfiliacionPension, GuardarAfiliacionRequest, ParametrosPlanilla } from './tipos';
import {
  construirRequestAfiliacion,
  interpretarEntero,
  validarAfiliacion,
  type CamposAfiliacion,
  type ErroresAfiliacion,
} from './validacionPlanilla';

interface Props {
  empleados: PaginaEmpleados | null;
  errorEmpleados: string;
  parametros: ParametrosPlanilla | null;
  errorParametros: string;
  empleadoId: number | null;
  afiliacion: AfiliacionPension | null;
  cancelar: () => void;
  guardar: (empleadoId: number, request: GuardarAfiliacionRequest) => Promise<void>;
}

function camposIniciales(empleadoId: number | null, afiliacion: AfiliacionPension | null): CamposAfiliacion {
  return {
    empleadoId: empleadoId === null ? '' : String(empleadoId),
    codigoPension: afiliacion?.codigoPension ?? '',
    cuspp: afiliacion?.cuspp ?? '',
    fechaAfiliacion: afiliacion?.fechaAfiliacion ?? hoyUtc(),
  };
}

// Formulario de alta o cambio: el CUSPP solo se pide, valida y envía cuando el sistema elegido es una AFP.
export function AfiliacionFormulario({ empleados, errorEmpleados, parametros, errorParametros, empleadoId, afiliacion, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposAfiliacion>(() => camposIniciales(empleadoId, afiliacion));
  const [errores, setErrores] = useState<ErroresAfiliacion>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const pensiones = parametros?.pensiones ?? [];
  const esAfp = pensiones.find((pension) => pension.codigo === campos.codigoPension)?.sistema === 'AFP';
  const hayMas = empleados !== null && empleados.total > MAXIMO_EMPLEADOS_SELECTOR;

  function cambiarCampo(clave: keyof CamposAfiliacion, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarAfiliacion(campos, esAfp);
    setErrores(nuevosErrores);
    const id = interpretarEntero(campos.empleadoId);
    if (Object.keys(nuevosErrores).length > 0 || id === null) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(id, construirRequestAfiliacion(campos, esAfp));
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar la afiliación.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="afiliacion-form-title" className="clientes-form-panel planillas-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">AFILIACIONES</div>
          <h2 id="afiliacion-form-title">{afiliacion === null ? 'Nueva afiliación' : 'Cambiar afiliación'}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="planillas-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Empleado" htmlFor="afiliacion-empleado" error={errores.empleadoId} idError="afiliacion-empleado-error">
          <select
            aria-describedby={errores.empleadoId ? 'afiliacion-empleado-error' : undefined}
            aria-invalid={Boolean(errores.empleadoId)}
            id="afiliacion-empleado"
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
        {errorEmpleados !== '' && <p className="clientes-error planillas-form-wide" role="alert">{errorEmpleados}</p>}
        {errorParametros !== '' && <p className="clientes-error planillas-form-wide" role="alert">{errorParametros}</p>}
        {hayMas && <p className="planillas-aviso planillas-form-wide">Se muestran los primeros 100 empleados activos.</p>}
        <CampoConError etiqueta="Sistema de pensiones" htmlFor="afiliacion-sistema" error={errores.codigoPension} idError="afiliacion-sistema-error">
          <select
            aria-describedby={errores.codigoPension ? 'afiliacion-sistema-error' : undefined}
            aria-invalid={Boolean(errores.codigoPension)}
            id="afiliacion-sistema"
            onChange={(event) => cambiarCampo('codigoPension', event.currentTarget.value)}
            value={campos.codigoPension}
          >
            <option value="">{parametros === null && errorParametros === '' ? 'Cargando sistemas…' : 'Selecciona un sistema'}</option>
            {pensiones.map((pension) => (
              <option key={pension.codigo} value={pension.codigo}>{`${pension.codigo} (${pension.sistema})`}</option>
            ))}
          </select>
        </CampoConError>
        {esAfp && (
          <CampoConError etiqueta="CUSPP" htmlFor="afiliacion-cuspp" error={errores.cuspp} idError="afiliacion-cuspp-error">
            <input
              aria-describedby={errores.cuspp ? 'afiliacion-cuspp-error' : undefined}
              aria-invalid={Boolean(errores.cuspp)}
              autoComplete="off"
              id="afiliacion-cuspp"
              maxLength={12}
              onChange={(event) => cambiarCampo('cuspp', event.currentTarget.value)}
              type="text"
              value={campos.cuspp}
            />
          </CampoConError>
        )}
        <CampoConError etiqueta="Fecha de afiliación" htmlFor="afiliacion-fecha" error={errores.fechaAfiliacion} idError="afiliacion-fecha-error">
          <input
            aria-describedby={errores.fechaAfiliacion ? 'afiliacion-fecha-error' : undefined}
            aria-invalid={Boolean(errores.fechaAfiliacion)}
            id="afiliacion-fecha"
            onChange={(event) => cambiarCampo('fechaAfiliacion', event.currentTarget.value)}
            type="date"
            value={campos.fechaAfiliacion}
          />
        </CampoConError>
        <div className="planillas-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar afiliación'}
          </button>
        </div>
      </form>
    </section>
  );
}
