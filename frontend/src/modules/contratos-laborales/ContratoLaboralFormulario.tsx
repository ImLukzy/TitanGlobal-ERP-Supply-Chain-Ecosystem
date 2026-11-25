import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { PaginaEmpleados } from '../empleados/tipos';
import { aModalidad, MODALIDADES, mensajeError, textoModalidad } from './presentacion';
import type { GuardarContratoLaboralRequest } from './tipos';
import { MAXIMO_EMPLEADOS_SELECTOR } from './useContratosLaboralesData';
import {
  LIMITE_CARGO,
  sueldoComoNumero,
  validarContrato,
  type CamposContrato,
  type ErroresContrato,
} from './validacionContratoLaboral';

interface Props {
  empleados: PaginaEmpleados | null;
  errorEmpleados: string;
  cancelar: () => void;
  guardar: (request: GuardarContratoLaboralRequest) => Promise<void>;
}

const CAMPOS_INICIALES: CamposContrato = {
  empleadoId: '',
  modalidad: 'Indeterminado',
  fechaInicio: '',
  fechaFin: '',
  sueldoBasico: '',
  cargo: '',
};

export function ContratoLaboralFormulario({ empleados, errorEmpleados, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposContrato>(CAMPOS_INICIALES);
  const [errores, setErrores] = useState<ErroresContrato>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const indeterminado = campos.modalidad === 'Indeterminado';

  function cambiarCampo(clave: keyof CamposContrato, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  // Con «Indeterminado» la fecha de fin se vacía y se deshabilita.
  function cambiarModalidad(valor: string): void {
    setCampos((actual) => ({
      ...actual,
      modalidad: valor,
      fechaFin: valor === 'Indeterminado' ? '' : actual.fechaFin,
    }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarContrato(campos);
    setErrores(nuevosErrores);
    const sueldo = sueldoComoNumero(campos.sueldoBasico);
    if (Object.keys(nuevosErrores).length > 0 || sueldo === null) {
      return;
    }
    const request: GuardarContratoLaboralRequest = {
      empleadoId: Number(campos.empleadoId),
      modalidad: aModalidad(campos.modalidad),
      fechaInicio: campos.fechaInicio,
      fechaFin: campos.fechaFin === '' ? null : campos.fechaFin,
      sueldoBasico: sueldo,
      cargo: campos.cargo.trim(),
    };
    setGuardando(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el contrato.'));
    } finally {
      setGuardando(false);
    }
  }

  const descripcionCargo = errores.cargo ? 'contrato-cargo-contador contrato-cargo-error' : 'contrato-cargo-contador';
  const hayMas = empleados !== null && empleados.total > MAXIMO_EMPLEADOS_SELECTOR;

  return (
    <section aria-labelledby="contrato-laboral-form-title" className="clientes-form-panel contratos-laborales-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">CONTRATOS LABORALES</div>
          <h2 id="contrato-laboral-form-title">Nuevo contrato</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="contratos-laborales-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Empleado" htmlFor="contrato-empleado" error={errores.empleadoId} idError="contrato-empleado-error">
          <select
            aria-describedby={errores.empleadoId ? 'contrato-empleado-error' : undefined}
            aria-invalid={Boolean(errores.empleadoId)}
            id="contrato-empleado"
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
        {errorEmpleados !== '' && (
          <p className="clientes-error contratos-laborales-form-wide" role="alert">{errorEmpleados}</p>
        )}
        {hayMas && (
          <p className="contratos-laborales-aviso contratos-laborales-form-wide">Se muestran los primeros 100 empleados activos.</p>
        )}
        <CampoConError etiqueta="Modalidad" htmlFor="contrato-modalidad" error={errores.modalidad} idError="contrato-modalidad-error">
          <select
            aria-describedby={errores.modalidad ? 'contrato-modalidad-error' : undefined}
            aria-invalid={Boolean(errores.modalidad)}
            id="contrato-modalidad"
            onChange={(event) => cambiarModalidad(event.currentTarget.value)}
            value={campos.modalidad}
          >
            {MODALIDADES.map((modalidad) => <option key={modalidad} value={modalidad}>{textoModalidad(modalidad)}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Fecha de inicio" htmlFor="contrato-fecha-inicio" error={errores.fechaInicio} idError="contrato-fecha-inicio-error">
          <input
            aria-describedby={errores.fechaInicio ? 'contrato-fecha-inicio-error' : undefined}
            aria-invalid={Boolean(errores.fechaInicio)}
            id="contrato-fecha-inicio"
            onChange={(event) => cambiarCampo('fechaInicio', event.currentTarget.value)}
            type="date"
            value={campos.fechaInicio}
          />
        </CampoConError>
        <CampoConError etiqueta="Fecha de fin" htmlFor="contrato-fecha-fin" error={errores.fechaFin} idError="contrato-fecha-fin-error">
          <input
            aria-describedby={errores.fechaFin ? 'contrato-fecha-fin-error' : undefined}
            aria-invalid={Boolean(errores.fechaFin)}
            disabled={indeterminado}
            id="contrato-fecha-fin"
            onChange={(event) => cambiarCampo('fechaFin', event.currentTarget.value)}
            type="date"
            value={campos.fechaFin}
          />
        </CampoConError>
        <CampoConError etiqueta="Sueldo básico (PEN)" htmlFor="contrato-sueldo" error={errores.sueldoBasico} idError="contrato-sueldo-error">
          <input
            aria-describedby={errores.sueldoBasico ? 'contrato-sueldo-error' : undefined}
            aria-invalid={Boolean(errores.sueldoBasico)}
            autoComplete="off"
            id="contrato-sueldo"
            inputMode="decimal"
            onChange={(event) => cambiarCampo('sueldoBasico', event.currentTarget.value)}
            value={campos.sueldoBasico}
          />
        </CampoConError>
        <div className="contratos-laborales-cargo-campo">
          <CampoConError etiqueta="Cargo" htmlFor="contrato-cargo" error={errores.cargo} idError="contrato-cargo-error">
            <input
              aria-describedby={descripcionCargo}
              aria-invalid={Boolean(errores.cargo)}
              autoComplete="off"
              id="contrato-cargo"
              maxLength={LIMITE_CARGO}
              onChange={(event) => cambiarCampo('cargo', event.currentTarget.value)}
              value={campos.cargo}
            />
          </CampoConError>
          <p className="contratos-laborales-contador" id="contrato-cargo-contador">{`${campos.cargo.length}/${LIMITE_CARGO}`}</p>
        </div>
        <div className="contratos-laborales-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar contrato'}
          </button>
        </div>
      </form>
    </section>
  );
}
