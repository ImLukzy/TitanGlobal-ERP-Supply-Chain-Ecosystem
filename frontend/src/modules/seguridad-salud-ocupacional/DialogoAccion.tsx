import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError, useDialogoModal } from './presentacion';
import type { GuardarAccionRequest } from './tipos';
import { construirRequestAccion, validarAccion, type CamposAccion, type Errores } from './validacionSst';

interface Props {
  numero: string;
  cerrar: () => void;
  guardar: (request: GuardarAccionRequest) => Promise<void>;
}

// Diálogo modal propio: agrega una acción correctiva al incidente abierto (mensaje 28 de la spec backend).
export function DialogoAccion({ numero, cerrar, guardar }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const campoRef = useRef<HTMLTextAreaElement>(null);
  const [campos, setCampos] = useState<CamposAccion>({ descripcion: '', responsable: '', fechaCompromiso: '' });
  const [errores, setErrores] = useState<Errores<CamposAccion>>({});
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    campoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  function cambiarCampo(clave: keyof CamposAccion, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    setMensaje('');
    const nuevos = validarAccion(campos);
    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) {
      return;
    }
    setEnviando(true);
    try {
      await guardar(construirRequestAccion(campos));
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo agregar la acción correctiva.'));
    } finally {
      setEnviando(false);
    }
  }

  function describir(clave: keyof CamposAccion): string | undefined {
    return errores[clave] ? `accion-${clave}-error` : undefined;
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div aria-labelledby="accion-dialog-title" aria-modal="true" className="clientes-dialog sst-dialog" ref={dialogoRef} role="dialog" tabIndex={-1}>
        <div className="page-eyebrow">{numero}</div>
        <h2 id="accion-dialog-title">Agregar acción correctiva</h2>
        <form className="sst-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
          <CampoConError etiqueta="Descripción" htmlFor="accion-descripcion" error={errores.descripcion} idError="accion-descripcion-error">
            <textarea aria-describedby={describir('descripcion')} aria-invalid={Boolean(errores.descripcion)} id="accion-descripcion" onChange={(event) => cambiarCampo('descripcion', event.currentTarget.value)} ref={campoRef} rows={3} value={campos.descripcion} />
          </CampoConError>
          <CampoConError etiqueta="Responsable" htmlFor="accion-responsable" error={errores.responsable} idError="accion-responsable-error">
            <input aria-describedby={describir('responsable')} aria-invalid={Boolean(errores.responsable)} autoComplete="off" id="accion-responsable" onChange={(event) => cambiarCampo('responsable', event.currentTarget.value)} type="text" value={campos.responsable} />
          </CampoConError>
          <CampoConError etiqueta="Fecha de compromiso" htmlFor="accion-compromiso" error={errores.fechaCompromiso} idError="accion-fechaCompromiso-error">
            <input aria-describedby={describir('fechaCompromiso')} aria-invalid={Boolean(errores.fechaCompromiso)} id="accion-compromiso" onChange={(event) => cambiarCampo('fechaCompromiso', event.currentTarget.value)} type="date" value={campos.fechaCompromiso} />
          </CampoConError>
          {mensaje !== '' && (
            <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
              <span aria-hidden="true">!</span>
              {mensaje}
            </p>
          )}
          <div className="clientes-dialog-actions">
            <button className="secondary-button" disabled={enviando} onClick={cerrar} type="button">Cancelar</button>
            <button className="primary-button" disabled={enviando} type="submit">{enviando ? 'Guardando…' : 'Agregar acción'}</button>
          </div>
        </form>
      </div>
    </div>
  );
}
