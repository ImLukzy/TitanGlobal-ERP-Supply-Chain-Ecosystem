import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { hoyUtc, MESES, mensajeError, TIPOS_PLANILLA, useDialogoModal } from './presentacion';
import type { CrearPlanillaRequest } from './tipos';
import { construirRequestPlanilla, validarPlanilla, type CamposPlanilla, type ErroresPlanilla } from './validacionPlanilla';

interface Props {
  cerrar: () => void;
  crear: (request: CrearPlanillaRequest) => Promise<void>;
}

function camposIniciales(): CamposPlanilla {
  return { anio: hoyUtc().slice(0, 4), mes: '', tipo: 'Mensual' };
}

// Diálogo modal propio: crea una planilla Mensual o Complementaria de un periodo (mensajes 1 a 6 de la spec backend).
export function DialogoNuevaPlanilla({ cerrar, crear }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const primerCampoRef = useRef<HTMLInputElement>(null);
  const [campos, setCampos] = useState<CamposPlanilla>(camposIniciales);
  const [errores, setErrores] = useState<ErroresPlanilla>({});
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    primerCampoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  function cambiarCampo(clave: keyof CamposPlanilla, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    setMensaje('');
    const nuevosErrores = validarPlanilla(campos);
    setErrores(nuevosErrores);
    const request = construirRequestPlanilla(campos);
    if (Object.keys(nuevosErrores).length > 0 || request === null) {
      return;
    }
    setEnviando(true);
    try {
      await crear(request);
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo crear la planilla.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="planilla-nueva-dialog-title"
        aria-modal="true"
        className="clientes-dialog planillas-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">PLANILLAS</div>
        <h2 id="planilla-nueva-dialog-title">Nueva planilla</h2>
        <form className="planillas-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
          <CampoConError etiqueta="Año" htmlFor="planilla-nueva-anio" error={errores.anio} idError="planilla-nueva-anio-error">
            <input
              aria-describedby={errores.anio ? 'planilla-nueva-anio-error' : undefined}
              aria-invalid={Boolean(errores.anio)}
              autoComplete="off"
              id="planilla-nueva-anio"
              inputMode="numeric"
              onChange={(event) => cambiarCampo('anio', event.currentTarget.value)}
              ref={primerCampoRef}
              type="text"
              value={campos.anio}
            />
          </CampoConError>
          <CampoConError etiqueta="Mes" htmlFor="planilla-nueva-mes" error={errores.mes} idError="planilla-nueva-mes-error">
            <select
              aria-describedby={errores.mes ? 'planilla-nueva-mes-error' : undefined}
              aria-invalid={Boolean(errores.mes)}
              id="planilla-nueva-mes"
              onChange={(event) => cambiarCampo('mes', event.currentTarget.value)}
              value={campos.mes}
            >
              <option value="">Selecciona un mes</option>
              {MESES.map((nombre, indice) => <option key={nombre} value={String(indice + 1)}>{nombre}</option>)}
            </select>
          </CampoConError>
          <CampoConError etiqueta="Tipo" htmlFor="planilla-nueva-tipo" error={errores.tipo} idError="planilla-nueva-tipo-error">
            <select
              aria-describedby={errores.tipo ? 'planilla-nueva-tipo-error' : undefined}
              aria-invalid={Boolean(errores.tipo)}
              id="planilla-nueva-tipo"
              onChange={(event) => cambiarCampo('tipo', event.currentTarget.value)}
              value={campos.tipo}
            >
              {TIPOS_PLANILLA.map((tipo) => <option key={tipo} value={tipo}>{tipo}</option>)}
            </select>
          </CampoConError>
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
            <button className="primary-button" disabled={enviando} type="submit">
              {enviando ? 'Creando…' : 'Crear planilla'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
