import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { PaginaEmpleados } from '../empleados/tipos';
import { hoyUtc, MAXIMO_EMPLEADOS_SELECTOR, MESES, mensajeError, TIPOS_PLANILLA } from './presentacion';
import type { GuardarBonoRequest } from './tipos';
import { construirRequestBono, LIMITE_CONCEPTO, validarBono, type CamposBono, type ErroresBono } from './validacionPlanilla';

interface Props {
  empleados: PaginaEmpleados | null;
  errorEmpleados: string;
  cancelar: () => void;
  guardar: (request: GuardarBonoRequest) => Promise<void>;
}

function camposIniciales(): CamposBono {
  const hoy = hoyUtc();
  return { empleadoId: '', anio: hoy.slice(0, 4), mes: String(Number(hoy.slice(5, 7))), tipoPlanilla: 'Mensual', concepto: '', monto: '' };
}

export function BonoFormulario({ empleados, errorEmpleados, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposBono>(camposIniciales);
  const [errores, setErrores] = useState<ErroresBono>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const hayMas = empleados !== null && empleados.total > MAXIMO_EMPLEADOS_SELECTOR;

  function cambiarCampo(clave: keyof CamposBono, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarBono(campos);
    setErrores(nuevosErrores);
    const request = construirRequestBono(campos);
    if (Object.keys(nuevosErrores).length > 0 || request === null) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo registrar el bono.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="bono-form-title" className="clientes-form-panel planillas-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">BONOS</div>
          <h2 id="bono-form-title">Nuevo bono</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="planillas-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Empleado" htmlFor="bono-empleado" error={errores.empleadoId} idError="bono-empleado-error">
          <select
            aria-describedby={errores.empleadoId ? 'bono-empleado-error' : undefined}
            aria-invalid={Boolean(errores.empleadoId)}
            id="bono-empleado"
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
        {hayMas && <p className="planillas-aviso planillas-form-wide">Se muestran los primeros 100 empleados activos.</p>}
        <CampoConError etiqueta="Tipo de planilla" htmlFor="bono-tipo" error={errores.tipoPlanilla} idError="bono-tipo-error">
          <select
            aria-describedby={errores.tipoPlanilla ? 'bono-tipo-error' : undefined}
            aria-invalid={Boolean(errores.tipoPlanilla)}
            id="bono-tipo"
            onChange={(event) => cambiarCampo('tipoPlanilla', event.currentTarget.value)}
            value={campos.tipoPlanilla}
          >
            {TIPOS_PLANILLA.map((tipo) => <option key={tipo} value={tipo}>{tipo}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Año" htmlFor="bono-anio" error={errores.anio} idError="bono-anio-error">
          <input
            aria-describedby={errores.anio ? 'bono-anio-error' : undefined}
            aria-invalid={Boolean(errores.anio)}
            autoComplete="off"
            id="bono-anio"
            inputMode="numeric"
            onChange={(event) => cambiarCampo('anio', event.currentTarget.value)}
            type="text"
            value={campos.anio}
          />
        </CampoConError>
        <CampoConError etiqueta="Mes" htmlFor="bono-mes" error={errores.mes} idError="bono-mes-error">
          <select
            aria-describedby={errores.mes ? 'bono-mes-error' : undefined}
            aria-invalid={Boolean(errores.mes)}
            id="bono-mes"
            onChange={(event) => cambiarCampo('mes', event.currentTarget.value)}
            value={campos.mes}
          >
            <option value="">Selecciona un mes</option>
            {MESES.map((nombre, indice) => <option key={nombre} value={String(indice + 1)}>{nombre}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Concepto" htmlFor="bono-concepto" error={errores.concepto} idError="bono-concepto-error">
          <input
            aria-describedby={errores.concepto ? 'bono-concepto-error' : undefined}
            aria-invalid={Boolean(errores.concepto)}
            autoComplete="off"
            id="bono-concepto"
            onChange={(event) => cambiarCampo('concepto', event.currentTarget.value)}
            placeholder={`Hasta ${LIMITE_CONCEPTO} caracteres`}
            type="text"
            value={campos.concepto}
          />
        </CampoConError>
        <CampoConError etiqueta="Monto" htmlFor="bono-monto" error={errores.monto} idError="bono-monto-error">
          <input
            aria-describedby={errores.monto ? 'bono-monto-error' : undefined}
            aria-invalid={Boolean(errores.monto)}
            autoComplete="off"
            id="bono-monto"
            inputMode="decimal"
            onChange={(event) => cambiarCampo('monto', event.currentTarget.value)}
            type="text"
            value={campos.monto}
          />
        </CampoConError>
        <div className="planillas-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Registrar bono'}
          </button>
        </div>
      </form>
    </section>
  );
}
