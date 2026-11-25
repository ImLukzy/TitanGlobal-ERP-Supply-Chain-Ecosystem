import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { formatearFecha } from '../../lib/formato';
import { aModalidad, MODALIDADES, mensajeError, textoModalidad, useDialogoModal } from './presentacion';
import type { ContratoLaboralDetalle, RenovarContratoLaboralRequest } from './tipos';
import {
  diaSiguiente,
  LIMITE_CARGO,
  sueldoComoNumero,
  validarRenovacion,
  type ErroresRenovacion,
} from './validacionContratoLaboral';

interface Props {
  contrato: ContratoLaboralDetalle;
  cerrar: () => void;
  renovar: (request: RenovarContratoLaboralRequest) => Promise<void>;
}

// Diálogo modal: el contrato nuevo empieza el día siguiente al fin del actual; modalidad, sueldo y cargo se prellenan con los vigentes.
export function DialogoRenovacion({ contrato, cerrar, renovar }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const primerCampoRef = useRef<HTMLSelectElement>(null);
  const [modalidad, setModalidad] = useState<string>(contrato.modalidad);
  const [fechaFin, setFechaFin] = useState('');
  const [sueldoBasico, setSueldoBasico] = useState(String(contrato.sueldoVigente));
  const [cargo, setCargo] = useState(contrato.cargoVigente);
  const [errores, setErrores] = useState<ErroresRenovacion>({});
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);
  const fechaInicio = contrato.fechaFin === null ? '' : diaSiguiente(contrato.fechaFin);
  const indeterminado = modalidad === 'Indeterminado';

  useEffect(() => {
    primerCampoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  function cambiarModalidad(valor: string): void {
    setModalidad(valor);
    if (valor === 'Indeterminado') {
      setFechaFin('');
    }
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarRenovacion({ modalidad, fechaInicio, fechaFin, sueldoBasico, cargo });
    setErrores(nuevosErrores);
    const sueldo = sueldoComoNumero(sueldoBasico);
    if (Object.keys(nuevosErrores).length > 0 || sueldo === null) {
      return;
    }
    setEnviando(true);
    try {
      await renovar({
        modalidad: aModalidad(modalidad),
        fechaFin: fechaFin === '' ? null : fechaFin,
        sueldoBasico: sueldo,
        cargo: cargo.trim(),
      });
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo renovar el contrato.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="contrato-renovacion-dialog-title"
        aria-modal="true"
        className="clientes-dialog contratos-laborales-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">RENOVACIÓN</div>
        <h2 id="contrato-renovacion-dialog-title">Renovar contrato</h2>
        <p>Contrato: <strong>{contrato.numero}</strong></p>
        {fechaInicio !== '' && (
          <p className="contratos-laborales-aviso">{`Empieza el ${formatearFecha(fechaInicio)}`}</p>
        )}
        <form className="contratos-laborales-dialog-form" noValidate onSubmit={(event) => void enviar(event)}>
          <CampoConError etiqueta="Modalidad" htmlFor="renovacion-modalidad" error={errores.modalidad} idError="renovacion-modalidad-error">
            <select
              aria-describedby={errores.modalidad ? 'renovacion-modalidad-error' : undefined}
              aria-invalid={Boolean(errores.modalidad)}
              id="renovacion-modalidad"
              onChange={(event) => cambiarModalidad(event.currentTarget.value)}
              ref={primerCampoRef}
              value={modalidad}
            >
              {MODALIDADES.map((item) => <option key={item} value={item}>{textoModalidad(item)}</option>)}
            </select>
          </CampoConError>
          <CampoConError etiqueta="Fecha de fin" htmlFor="renovacion-fecha-fin" error={errores.fechaFin} idError="renovacion-fecha-fin-error">
            <input
              aria-describedby={errores.fechaFin ? 'renovacion-fecha-fin-error' : undefined}
              aria-invalid={Boolean(errores.fechaFin)}
              disabled={indeterminado}
              id="renovacion-fecha-fin"
              onChange={(event) => setFechaFin(event.currentTarget.value)}
              type="date"
              value={fechaFin}
            />
          </CampoConError>
          <CampoConError etiqueta="Sueldo básico (PEN)" htmlFor="renovacion-sueldo" error={errores.sueldoBasico} idError="renovacion-sueldo-error">
            <input
              aria-describedby={errores.sueldoBasico ? 'renovacion-sueldo-error' : undefined}
              aria-invalid={Boolean(errores.sueldoBasico)}
              autoComplete="off"
              id="renovacion-sueldo"
              inputMode="decimal"
              onChange={(event) => setSueldoBasico(event.currentTarget.value)}
              value={sueldoBasico}
            />
          </CampoConError>
          <CampoConError etiqueta="Cargo" htmlFor="renovacion-cargo" error={errores.cargo} idError="renovacion-cargo-error">
            <input
              aria-describedby={errores.cargo ? 'renovacion-cargo-error' : undefined}
              aria-invalid={Boolean(errores.cargo)}
              autoComplete="off"
              id="renovacion-cargo"
              maxLength={LIMITE_CARGO}
              onChange={(event) => setCargo(event.currentTarget.value)}
              value={cargo}
            />
          </CampoConError>
          {mensaje !== '' && (
            <p aria-live="polite" className="clientes-error clientes-dialog-error contratos-laborales-form-wide" role="alert">
              <span aria-hidden="true">!</span>
              {mensaje}
            </p>
          )}
          <div className="clientes-dialog-actions contratos-laborales-form-wide">
            <button className="secondary-button" disabled={enviando} onClick={cerrar} type="button">
              Cancelar
            </button>
            <button className="primary-button" disabled={enviando} type="submit">
              {enviando ? 'Renovando…' : 'Confirmar renovación'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
