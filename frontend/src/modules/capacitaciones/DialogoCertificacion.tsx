import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError, useDialogoModal } from './presentacion';
import type { EmitirCertificacionRequest, Inscripcion, ProgramacionDetalle } from './tipos';
import { construirRequestCertificacion, LIMITE_CODIGO_CERTIFICACION, validarCertificacion, type CamposCertificacion, type ErroresCertificacion } from './validacionCapacitacion';

interface Props {
  programacion: ProgramacionDetalle;
  inscripcion: Inscripcion;
  cerrar: () => void;
  emitir: (request: EmitirCertificacionRequest) => Promise<void>;
}

// Diálogo modal propio: emite la certificación de un inscrito aprobado (mensajes 25 a 30 de la spec backend).
export function DialogoCertificacion({ programacion, inscripcion, cerrar, emitir }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const campoRef = useRef<HTMLInputElement>(null);
  const [campos, setCampos] = useState<CamposCertificacion>({ codigo: '', fechaEmision: programacion.fechaFin, fechaVencimiento: '' });
  const [errores, setErrores] = useState<ErroresCertificacion>({});
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    campoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  function cambiarCampo(clave: keyof CamposCertificacion, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    setMensaje('');
    const nuevosErrores = validarCertificacion(campos, programacion.fechaFin);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setEnviando(true);
    try {
      await emitir(construirRequestCertificacion(campos));
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo emitir la certificación.'));
    } finally {
      setEnviando(false);
    }
  }

  function describir(clave: keyof CamposCertificacion): string | undefined {
    return errores[clave] ? `certificacion-${clave}-error` : undefined;
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div aria-labelledby="certificacion-dialog-title" aria-modal="true" className="clientes-dialog capacitaciones-dialog" ref={dialogoRef} role="dialog" tabIndex={-1}>
        <div className="page-eyebrow">PROGRAMACIONES</div>
        <h2 id="certificacion-dialog-title">Emitir certificado</h2>
        <p>{`${inscripcion.empleadoNombre} · ${programacion.cursoNombre}`}</p>
        <form className="capacitaciones-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
          <CampoConError etiqueta="Código de certificación" htmlFor="certificacion-codigo" error={errores.codigo} idError="certificacion-codigo-error">
            <input aria-describedby={describir('codigo')} aria-invalid={Boolean(errores.codigo)} autoComplete="off" id="certificacion-codigo" maxLength={LIMITE_CODIGO_CERTIFICACION + 10} onChange={(event) => cambiarCampo('codigo', event.currentTarget.value)} ref={campoRef} type="text" value={campos.codigo} />
          </CampoConError>
          <CampoConError etiqueta="Fecha de emisión" htmlFor="certificacion-fechaEmision" error={errores.fechaEmision} idError="certificacion-fechaEmision-error">
            <input aria-describedby={describir('fechaEmision')} aria-invalid={Boolean(errores.fechaEmision)} id="certificacion-fechaEmision" onChange={(event) => cambiarCampo('fechaEmision', event.currentTarget.value)} type="date" value={campos.fechaEmision} />
          </CampoConError>
          <CampoConError etiqueta="Fecha de vencimiento (opcional)" htmlFor="certificacion-fechaVencimiento" error={errores.fechaVencimiento} idError="certificacion-fechaVencimiento-error">
            <input aria-describedby={describir('fechaVencimiento')} aria-invalid={Boolean(errores.fechaVencimiento)} id="certificacion-fechaVencimiento" onChange={(event) => cambiarCampo('fechaVencimiento', event.currentTarget.value)} type="date" value={campos.fechaVencimiento} />
          </CampoConError>
          {mensaje !== '' && (
            <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
              <span aria-hidden="true">!</span>
              {mensaje}
            </p>
          )}
          <div className="clientes-dialog-actions">
            <button className="secondary-button" disabled={enviando} onClick={cerrar} type="button">Cancelar</button>
            <button className="primary-button" disabled={enviando} type="submit">{enviando ? 'Emitiendo…' : 'Emitir certificación'}</button>
          </div>
        </form>
      </div>
    </div>
  );
}
