import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError } from './presentacion';
import type { CeseEmpleadoRequest } from './tipos';
import { LIMITE_MOTIVO_CESE, validarCese, type ErroresCese } from './validacionEmpleado';

interface Props {
  nombre: string;
  fechaIngreso: string;
  cerrar: () => void;
  registrar: (request: CeseEmpleadoRequest) => Promise<void>;
}

// Diálogo modal propio: el de baja de DialogoBaja borra y el cese no borra.
export function DialogoCese({ nombre, fechaIngreso, cerrar, registrar }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const primerCampoRef = useRef<HTMLInputElement>(null);
  const [fechaCese, setFechaCese] = useState('');
  const [motivo, setMotivo] = useState('');
  const [errores, setErrores] = useState<ErroresCese>({});
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    primerCampoRef.current?.focus();
  }, []);

  useEffect(() => {
    function manejarTeclado(event: globalThis.KeyboardEvent): void {
      if (event.key === 'Escape') {
        event.preventDefault();
        cerrar();
      }
      if (event.key === 'Tab' && dialogoRef.current !== null) {
        const elementos = Array.from(
          dialogoRef.current.querySelectorAll<HTMLElement>('input:not(:disabled), textarea:not(:disabled), button:not(:disabled)'),
        );
        const primero = elementos[0];
        const ultimo = elementos[elementos.length - 1];
        if (primero !== undefined && ultimo !== undefined) {
          if (event.shiftKey && document.activeElement === primero) {
            event.preventDefault();
            ultimo.focus();
          } else if (!event.shiftKey && document.activeElement === ultimo) {
            event.preventDefault();
            primero.focus();
          }
        }
      }
    }
    document.addEventListener('keydown', manejarTeclado);
    return () => document.removeEventListener('keydown', manejarTeclado);
  }, [cerrar]);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarCese({ fechaCese, motivo, fechaIngreso });
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setEnviando(true);
    try {
      await registrar({ fechaCese, motivo: motivo.trim() });
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo registrar el cese.'));
    } finally {
      setEnviando(false);
    }
  }

  const descripcionMotivo = errores.motivo
    ? 'cese-motivo-contador cese-motivo-error'
    : 'cese-motivo-contador';

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="empleado-cese-dialog-title"
        aria-modal="true"
        className="clientes-dialog empleado-cese-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">CESE</div>
        <h2 id="empleado-cese-dialog-title">Registrar cese</h2>
        <p>Empleado: <strong>{nombre}</strong></p>
        <form className="empleado-cese-form" noValidate onSubmit={(event) => void enviar(event)}>
          <CampoConError etiqueta="Fecha de cese" htmlFor="cese-fecha" error={errores.fechaCese} idError="cese-fecha-error">
            <input
              aria-describedby={errores.fechaCese ? 'cese-fecha-error' : undefined}
              aria-invalid={Boolean(errores.fechaCese)}
              id="cese-fecha"
              onChange={(event) => setFechaCese(event.currentTarget.value)}
              ref={primerCampoRef}
              type="date"
              value={fechaCese}
            />
          </CampoConError>
          <CampoConError etiqueta="Motivo de cese" htmlFor="cese-motivo" error={errores.motivo} idError="cese-motivo-error">
            <textarea
              aria-describedby={descripcionMotivo}
              aria-invalid={Boolean(errores.motivo)}
              id="cese-motivo"
              maxLength={LIMITE_MOTIVO_CESE}
              onChange={(event) => setMotivo(event.currentTarget.value)}
              rows={4}
              value={motivo}
            />
          </CampoConError>
          <p className="empleado-contador" id="cese-motivo-contador">{`${motivo.length}/${LIMITE_MOTIVO_CESE}`}</p>
          {mensaje !== '' && (
            <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
              <span aria-hidden="true">!</span>
              {mensaje}
            </p>
          )}
          <div className="clientes-dialog-actions">
            <button className="secondary-button" disabled={enviando} onClick={cerrar} type="button">
              Cancelar
            </button>
            <button className="danger-button" disabled={enviando} type="submit">
              {enviando ? 'Confirmando…' : 'Confirmar cese'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
