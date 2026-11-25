import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError, useDialogoModal } from './presentacion';
import type { AdendaContratoRequest } from './tipos';
import {
  LIMITE_CARGO,
  sueldoComoNumero,
  validarAdenda,
  type ErroresAdenda,
} from './validacionContratoLaboral';

interface Props {
  numero: string;
  cerrar: () => void;
  agregar: (request: AdendaContratoRequest) => Promise<void>;
}

// Diálogo modal: solo se envían los campos con valor; lo que queda vacío no cambia.
export function DialogoAdenda({ numero, cerrar, agregar }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const primerCampoRef = useRef<HTMLInputElement>(null);
  const [fechaVigencia, setFechaVigencia] = useState('');
  const [cargo, setCargo] = useState('');
  const [sueldoBasico, setSueldoBasico] = useState('');
  const [errores, setErrores] = useState<ErroresAdenda>({});
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    primerCampoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarAdenda({ fechaVigencia, cargo, sueldoBasico });
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    const request: AdendaContratoRequest = { fechaVigencia };
    if (cargo.trim() !== '') {
      request.cargo = cargo.trim();
    }
    const sueldo = sueldoComoNumero(sueldoBasico);
    if (sueldo !== null) {
      request.sueldoBasico = sueldo;
    }
    setEnviando(true);
    try {
      await agregar(request);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo agregar la adenda.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="contrato-adenda-dialog-title"
        aria-modal="true"
        className="clientes-dialog contratos-laborales-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">ADENDA</div>
        <h2 id="contrato-adenda-dialog-title">Agregar adenda</h2>
        <p>Contrato: <strong>{numero}</strong></p>
        <p className="contratos-laborales-aviso">Deja vacío lo que no cambia.</p>
        <form className="contratos-laborales-dialog-form" noValidate onSubmit={(event) => void enviar(event)}>
          <CampoConError etiqueta="Fecha de vigencia" htmlFor="adenda-fecha-vigencia" error={errores.fechaVigencia} idError="adenda-fecha-vigencia-error">
            <input
              aria-describedby={errores.fechaVigencia ? 'adenda-fecha-vigencia-error' : undefined}
              aria-invalid={Boolean(errores.fechaVigencia)}
              id="adenda-fecha-vigencia"
              onChange={(event) => setFechaVigencia(event.currentTarget.value)}
              ref={primerCampoRef}
              type="date"
              value={fechaVigencia}
            />
          </CampoConError>
          <CampoConError etiqueta="Cargo" htmlFor="adenda-cargo" error={errores.cargo} idError="adenda-cargo-error">
            <input
              aria-describedby={errores.cargo ? 'adenda-cargo-error' : undefined}
              aria-invalid={Boolean(errores.cargo)}
              autoComplete="off"
              id="adenda-cargo"
              maxLength={LIMITE_CARGO}
              onChange={(event) => setCargo(event.currentTarget.value)}
              value={cargo}
            />
          </CampoConError>
          <CampoConError etiqueta="Sueldo básico (PEN)" htmlFor="adenda-sueldo" error={errores.sueldoBasico} idError="adenda-sueldo-error">
            <input
              aria-describedby={errores.sueldoBasico ? 'adenda-sueldo-error' : undefined}
              aria-invalid={Boolean(errores.sueldoBasico)}
              autoComplete="off"
              id="adenda-sueldo"
              inputMode="decimal"
              onChange={(event) => setSueldoBasico(event.currentTarget.value)}
              value={sueldoBasico}
            />
          </CampoConError>
          {errores.general !== undefined && (
            <p className="clientes-error clientes-dialog-error contratos-laborales-form-wide" id="adenda-general-error" role="alert">
              <span aria-hidden="true">!</span>
              {errores.general}
            </p>
          )}
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
              {enviando ? 'Guardando…' : 'Guardar adenda'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
