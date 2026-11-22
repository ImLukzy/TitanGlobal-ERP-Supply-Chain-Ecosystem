import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { DialogoRetorno } from './DialogoRetorno';
import { errorCantidad } from './validacion';
import type { RegistrarCantidad } from './tipos';
export function DialogoCantidadRetorno({
  etapa,
  maximo,
  unidad,
  ocupado,
  error,
  cerrar,
  confirmar,
}: {
  etapa: 'recogida' | 'recepcion';
  maximo: string;
  unidad: string;
  ocupado: boolean;
  error: string;
  cerrar: () => void;
  confirmar: (d: RegistrarCantidad) => void;
}) {
  const [cantidad, setCantidad] = useState(''),
    [responsable, setResponsable] = useState(''),
    [errores, setErrores] = useState<{ cantidad?: string; responsable?: string }>({});
  const ref = useRef<HTMLFormElement>(null);
  const titulo = etapa === 'recogida' ? 'Registrar recogida' : 'Registrar recepción';
  function enviar(e: FormEvent) {
    e.preventDefault();
    if (ocupado) return;
    const f: { cantidad?: string; responsable?: string } = {};
    const c = errorCantidad(cantidad);
    if (c) f.cantidad = c;
    else if (Number(cantidad) > Number(maximo))
      f.cantidad =
        etapa === 'recogida'
          ? 'La cantidad recogida no puede superar la programada.'
          : 'La cantidad recibida no puede superar la recogida.';
    if (!responsable.trim()) f.responsable = 'El campo responsable es obligatorio.';
    else if (responsable.trim().length > 100)
      f.responsable = 'El campo responsable no puede superar 100 caracteres.';
    setErrores(f);
    if (Object.keys(f).length) {
      requestAnimationFrame(() =>
        ref.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    confirmar({ cantidad: Number(cantidad), responsable: responsable.trim() });
  }
  return (
    <DialogoRetorno
      titulo={titulo}
      id="cantidad-retorno-title"
      ocupado={ocupado}
      error={error}
      cerrar={cerrar}
    >
      <p>
        Máximo de esta etapa:{' '}
        <span className="logistica-inversa-number">
          {maximo} {unidad}
        </span>
        . El registro cerrará esta etapa.
      </p>
      <form ref={ref} onSubmit={enviar} noValidate>
        <CampoConError
          etiqueta="Cantidad"
          htmlFor="etapa-cantidad"
          error={errores.cantidad}
          idError="etapa-cantidad-error"
        >
          <input
            id="etapa-cantidad"
            inputMode="decimal"
            value={cantidad}
            disabled={ocupado}
            onChange={(e) => setCantidad(e.target.value)}
            aria-invalid={!!errores.cantidad}
            aria-describedby={errores.cantidad ? 'etapa-cantidad-error' : undefined}
          />
        </CampoConError>
        <CampoConError
          etiqueta="Responsable"
          htmlFor="etapa-responsable"
          error={errores.responsable}
          idError="etapa-responsable-error"
        >
          <input
            id="etapa-responsable"
            maxLength={100}
            value={responsable}
            disabled={ocupado}
            onChange={(e) => setResponsable(e.target.value)}
            aria-invalid={!!errores.responsable}
            aria-describedby={errores.responsable ? 'etapa-responsable-error' : undefined}
          />
        </CampoConError>
        <div className="logistica-inversa-actions">
          <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
            Volver
          </button>
          <button className="primary-button" type="submit" disabled={ocupado}>
            {etapa === 'recogida' ? 'Confirmar recogida' : 'Confirmar recepción'}
          </button>
        </div>
      </form>
    </DialogoRetorno>
  );
}
