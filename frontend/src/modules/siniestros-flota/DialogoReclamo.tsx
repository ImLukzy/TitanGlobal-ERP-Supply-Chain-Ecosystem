import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { DialogoSiniestro } from './DialogoSiniestro';
import { camposReclamo, validarReclamo } from './validacion';
import type { ValoresReclamo, RegistrarReclamo } from './tipos';
export function DialogoReclamo({
  ocupado,
  error,
  cerrar,
  confirmar,
}: {
  ocupado: boolean;
  error: string;
  cerrar: () => void;
  confirmar: (d: RegistrarReclamo) => void;
}) {
  const [datos, setDatos] = useState<ValoresReclamo>({
    numeroPoliza: '',
    aseguradora: '',
    numeroReclamo: '',
    moneda: '',
    montoReclamado: '',
  });
  const [errores, setErrores] = useState<Partial<Record<keyof ValoresReclamo, string>>>({});
  const ref = useRef<HTMLFormElement>(null);
  function enviar(e: FormEvent) {
    e.preventDefault();
    if (ocupado) return;
    const f = validarReclamo(datos);
    setErrores(f);
    if (Object.keys(f).length) {
      requestAnimationFrame(() =>
        ref.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    confirmar({
      numeroPoliza: datos.numeroPoliza.trim(),
      aseguradora: datos.aseguradora.trim(),
      numeroReclamo: datos.numeroReclamo.trim(),
      moneda: datos.moneda.trim().toUpperCase(),
      montoReclamado: Number(datos.montoReclamado),
    });
  }
  const campos = [
    ...camposReclamo.map(([c, l, m]) => [c, l, m] as const),
    ['moneda', 'Moneda (3 letras)', 3],
    ['montoReclamado', 'Monto reclamado', undefined],
  ] as const;
  return (
    <DialogoSiniestro
      titulo="Abrir reclamo"
      id="reclamo-title"
      ocupado={ocupado}
      error={error}
      cerrar={cerrar}
    >
      <p>Registra las referencias manuales de póliza y reclamo.</p>
      <form ref={ref} onSubmit={enviar} noValidate>
        {campos.map(([c, l, m]) => (
          <CampoConError
            key={c}
            etiqueta={l}
            htmlFor={`reclamo-${c}`}
            error={errores[c]}
            idError={`reclamo-${c}-error`}
          >
            <input
              id={`reclamo-${c}`}
              value={datos[c]}
              maxLength={m}
              inputMode={c === 'montoReclamado' ? 'decimal' : undefined}
              disabled={ocupado}
              onChange={(e) => setDatos((p) => ({ ...p, [c]: e.target.value }))}
              aria-invalid={!!errores[c]}
              aria-describedby={errores[c] ? `reclamo-${c}-error` : undefined}
            />
          </CampoConError>
        ))}
        <div className="siniestros-flota-actions">
          <button type="button" className="secondary-button" disabled={ocupado} onClick={cerrar}>
            Volver
          </button>
          <button type="submit" className="primary-button" disabled={ocupado}>
            Confirmar reclamo
          </button>
        </div>
      </form>
    </DialogoSiniestro>
  );
}
