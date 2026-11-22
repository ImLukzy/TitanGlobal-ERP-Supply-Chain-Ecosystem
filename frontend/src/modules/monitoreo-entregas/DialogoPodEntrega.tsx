import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { DialogoEntrega } from './DialogoEntrega';
import { camposPod, validarPod, type Errores } from './validacion';
import type { RegistrarPod } from './tipos';
export function DialogoPodEntrega({
  ocupado,
  error,
  cerrar,
  confirmar,
}: {
  ocupado: boolean;
  error: string;
  cerrar: () => void;
  confirmar: (d: RegistrarPod) => void;
}) {
  const [datos, setDatos] = useState({
    receptor: '',
    documentoReceptor: '',
    referenciaConstancia: '',
    observaciones: '',
  });
  const [errores, setErrores] = useState<Errores>({});
  const ref = useRef<HTMLFormElement>(null);
  function enviar(e: FormEvent) {
    e.preventDefault();
    if (ocupado) return;
    const f = validarPod(datos);
    setErrores(f);
    if (Object.keys(f).length) {
      ref.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus();
      requestAnimationFrame(() =>
        ref.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    confirmar({
      receptor: datos.receptor.trim(),
      documentoReceptor: datos.documentoReceptor.trim(),
      referenciaConstancia: datos.referenciaConstancia.trim(),
      observaciones: datos.observaciones.trim() || undefined,
    });
  }
  const campos = [
    ...camposPod.map(([c, l, m]) => [c, l, m] as const),
    ['observaciones', 'Observaciones de entrega', 500],
  ] as const;
  return (
    <DialogoEntrega
      titulo="Registrar recepción manual"
      id="pod-entrega-title"
      ocupado={ocupado}
      error={error}
      cerrar={cerrar}
    >
      <p>Constancia de recepción textual. El despacho quedará de solo lectura.</p>
      <form ref={ref} onSubmit={enviar} noValidate>
        <fieldset disabled={ocupado} className="monitoreo-entregas-form">
          <legend className="sr-only">Datos de recepción</legend>
          {campos.map(([campo, label, maximo]) => (
            <CampoConError
              key={campo}
              etiqueta={label}
              htmlFor={`pod-${campo}`}
              error={errores[campo]}
              idError={`pod-${campo}-error`}
            >
              <input
                id={`pod-${campo}`}
                value={datos[campo]}
                maxLength={maximo}
                aria-invalid={!!errores[campo]}
                aria-describedby={errores[campo] ? `pod-${campo}-error` : undefined}
                onChange={(e) => setDatos({ ...datos, [campo]: e.target.value })}
              />
            </CampoConError>
          ))}
        </fieldset>
        <div className="monitoreo-entregas-actions">
          <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
            Volver
          </button>
          <button className="primary-button" type="submit" disabled={ocupado}>
            Confirmar recepción
          </button>
        </div>
      </form>
    </DialogoEntrega>
  );
}
