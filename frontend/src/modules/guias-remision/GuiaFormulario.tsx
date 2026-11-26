import { useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { camposCabecera, nuevaLinea, validarGuia, type Errores } from './validacion';
import { fechaParaApi, fechaParaInput } from './presentacion';
import { LineasGuiaEditor } from './LineasGuiaEditor';
import type { GuiaDetalle, GuardarGuia, ValoresGuia } from './tipos';
export function GuiaFormulario({
  inicial,
  ocupado,
  error,
  guardar,
  cerrar,
}: {
  inicial: GuiaDetalle | null;
  ocupado: boolean;
  error: string;
  guardar: (d: GuardarGuia) => void;
  cerrar: () => void;
}) {
  const [datos, setDatos] = useState<ValoresGuia>(
    inicial
      ? {
          ...inicial,
          numero: String(inicial.numero),
          fechaInicioTraslado: fechaParaInput(inicial.fechaInicioTraslado),
          observaciones: inicial.observaciones ?? '',
          lineas: inicial.lineas.map((l) => ({
            ...l,
            clave: `existente-${l.id}`,
          })),
        }
      : {
          serie: '',
          numero: '',
          fechaInicioTraslado: '',
          pesoBrutoKg: '',
          observaciones: '',
          remitenteNombre: '',
          remitenteDocumento: '',
          destinatarioNombre: '',
          destinatarioDocumento: '',
          direccionOrigen: '',
          direccionDestino: '',
          motivoTraslado: '',
          transportista: '',
          conductor: '',
          placa: '',
          lineas: [nuevaLinea()],
        },
  );
  const [errores, setErrores] = useState<Errores>({});
  const ref = useRef<HTMLFormElement>(null);
  function enviar(e: FormEvent) {
    e.preventDefault();
    if (ocupado) return;
    const fallos = validarGuia(datos);
    setErrores(fallos);
    if (Object.keys(fallos).length) {
      requestAnimationFrame(() =>
        ref.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      );
      return;
    }
    guardar({
      serie: datos.serie.trim().toUpperCase(),
      numero: Number(datos.numero),
      fechaInicioTraslado: fechaParaApi(datos.fechaInicioTraslado),
      pesoBrutoKg: Number(datos.pesoBrutoKg),
      observaciones: datos.observaciones.trim() || undefined,
      remitenteNombre: datos.remitenteNombre.trim(),
      remitenteDocumento: datos.remitenteDocumento.trim(),
      destinatarioNombre: datos.destinatarioNombre.trim(),
      destinatarioDocumento: datos.destinatarioDocumento.trim(),
      direccionOrigen: datos.direccionOrigen.trim(),
      direccionDestino: datos.direccionDestino.trim(),
      motivoTraslado: datos.motivoTraslado.trim(),
      transportista: datos.transportista.trim(),
      conductor: datos.conductor.trim(),
      placa: datos.placa.trim(),
      lineas: datos.lineas.map((l) => ({
        sku: l.sku.trim(),
        descripcion: l.descripcion.trim(),
        unidadMedida: l.unidadMedida.trim(),
        cantidad: Number(l.cantidad),
      })),
    });
  }
  const campos = [
    ['serie', 'Serie', 8],
    ['numero', 'Número', undefined],
    ['fechaInicioTraslado', 'Inicio del traslado (UTC)', undefined],
    ...camposCabecera.map(([c, l, m]) => [c, l, m] as const),
    ['pesoBrutoKg', 'Peso bruto (kg)', undefined],
    ['observaciones', 'Observaciones', 500],
  ] as const;
  return (
    <section className="guias-remision-card" aria-labelledby="guia-form-title">
      <h2 id="guia-form-title">{inicial ? 'Editar guía' : 'Nueva guía'}</h2>
      <p>Documento interno de transporte. Serie y número manuales.</p>
      <form ref={ref} onSubmit={enviar} noValidate>
        <fieldset disabled={ocupado} className="guias-remision-form">
          <legend className="sr-only">Datos del traslado</legend>
          {campos.map(([campo, label, maximo], i) => (
            <CampoConError
              key={campo}
              etiqueta={label}
              htmlFor={`guia-${campo}`}
              error={errores[campo]}
              idError={`guia-${campo}-error`}
            >
              <input
                autoFocus={i === 0}
                id={`guia-${campo}`}
                type={campo === 'fechaInicioTraslado' ? 'datetime-local' : 'text'}
                step={campo === 'fechaInicioTraslado' ? '0.001' : undefined}
                inputMode={
                  campo === 'numero' ? 'numeric' : campo === 'pesoBrutoKg' ? 'decimal' : undefined
                }
                value={datos[campo]}
                maxLength={maximo}
                aria-invalid={!!errores[campo]}
                aria-describedby={errores[campo] ? `guia-${campo}-error` : undefined}
                onChange={(e) => setDatos({ ...datos, [campo]: e.target.value })}
              />
            </CampoConError>
          ))}
          <LineasGuiaEditor
            lineas={datos.lineas}
            errores={errores}
            cambiar={(lineas) => {
              setDatos({ ...datos, lineas });
              setErrores({});
            }}
          />
        </fieldset>
        {error && (
          <p role="alert" className="field-error">
            {error}
          </p>
        )}
        <div className="guias-remision-actions">
          <button className="primary-button" disabled={ocupado} type="submit">
            {ocupado ? 'Guardando…' : 'Guardar borrador'}
          </button>
          <button className="secondary-button" disabled={ocupado} type="button" onClick={cerrar}>
            Volver al detalle
          </button>
        </div>
      </form>
    </section>
  );
}
