import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { crearMantenimiento, editarMantenimiento } from './api';
import { nombreCentro } from './presentacion';
import { SelectorCentro } from './SelectorCentro';
import type { MantenimientoDetalle } from './tipos';
import { hoyPeru, validarMantenimiento, type FormularioMantenimiento } from './validacion';

interface FormularioProps {
  mantenimiento: MantenimientoDetalle | null;
  cancelar: () => void;
  guardado: (id: number) => void;
}

type CampoTexto = Exclude<keyof FormularioMantenimiento, 'centro'>;

function valoresIniciales(mantenimiento: MantenimientoDetalle | null, hoy: string): FormularioMantenimiento {
  if (mantenimiento === null) {
    return { centro: null, descripcion: '', fechaProgramada: hoy, duracion: '', frecuencia: '', responsable: '' };
  }
  return {
    centro: { id: mantenimiento.centroTrabajoId, codigo: mantenimiento.centroTrabajoCodigo, nombre: mantenimiento.centroTrabajoNombre },
    descripcion: mantenimiento.descripcion,
    fechaProgramada: mantenimiento.fechaProgramada,
    duracion: String(mantenimiento.duracionHorasPlanificada),
    frecuencia: mantenimiento.frecuenciaDias === null ? '' : String(mantenimiento.frecuenciaDias),
    responsable: mantenimiento.responsable ?? '',
  };
}

export function MantenimientoFormulario({ mantenimiento, cancelar, guardado }: FormularioProps) {
  const titulo = useRef<HTMLHeadingElement>(null);
  const [hoy] = useState(() => hoyPeru());
  const [valores, setValores] = useState<FormularioMantenimiento>(() => valoresIniciales(mantenimiento, hoy));
  const [errores, setErrores] = useState<Partial<Record<string, string>>>({});
  const [errorApi, setErrorApi] = useState('');
  const [guardando, setGuardando] = useState(false);
  const esAlta = mantenimiento === null;

  useEffect(() => {
    titulo.current?.focus();
  }, []);

  function cambiar(cambios: Partial<FormularioMantenimiento>): void {
    setValores((actuales) => ({ ...actuales, ...cambios }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (guardando) {
      return;
    }
    const resultado = validarMantenimiento(valores, hoy, esAlta);
    setErrores(resultado.errores);
    setErrorApi('');
    if (resultado.solicitud === undefined) {
      return;
    }
    setGuardando(true);
    try {
      const respuesta = mantenimiento === null
        ? await crearMantenimiento({ centroTrabajoId: valores.centro?.id ?? 0, ...resultado.solicitud })
        : await editarMantenimiento(mantenimiento.id, resultado.solicitud);
      guardado(respuesta.id);
    } catch (causa: unknown) {
      // Conserva lo escrito y muestra el mensaje literal de la API.
      setErrorApi(causa instanceof Error ? causa.message : 'No se pudo guardar el mantenimiento.');
    } finally {
      setGuardando(false);
    }
  }

  function campo(nombre: CampoTexto, etiqueta: string, control: (props: ControlProps) => ReactNode): ReactNode {
    const id = `mantenimiento-preventivo-${nombre}`;
    const error = errores[nombre];
    return (
      <CampoConError error={error} etiqueta={etiqueta} htmlFor={id} idError={`${id}-error`}>
        {control({
          'aria-describedby': error ? `${id}-error` : undefined,
          'aria-invalid': Boolean(error),
          id,
          onChange: (event) => cambiar({ [nombre]: event.currentTarget.value }),
          value: valores[nombre],
        })}
      </CampoConError>
    );
  }

  return (
    <section aria-labelledby="mantenimiento-preventivo-formulario" className="mantenimiento-preventivo-tarjeta">
      <h2 id="mantenimiento-preventivo-formulario" ref={titulo} tabIndex={-1}>
        {mantenimiento === null ? 'Nuevo mantenimiento' : `Editar mantenimiento ${mantenimiento.numero}`}
      </h2>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <fieldset className="mantenimiento-preventivo-campos" disabled={guardando}>
          {esAlta ? (
            <SelectorCentro
              cambiar={(centro) => cambiar({ centro })}
              error={errores.centro}
              etiqueta="Centro de trabajo"
              seleccionado={valores.centro}
              soloActivos
              textoBoton="Seleccionar centro"
            />
          ) : (
            <div className="form-field">
              <span>Centro de trabajo</span>
              <p className="mantenimiento-preventivo-fijo">{valores.centro === null ? '' : nombreCentro(valores.centro)}</p>
            </div>
          )}
          {campo('descripcion', 'Descripción', (props) => <textarea {...props} maxLength={270} rows={2} />)}
          {campo('fechaProgramada', 'Fecha programada', (props) => <input {...props} min={hoy} type="date" />)}
          {campo('duracion', 'Duración planificada (horas)', (props) => <input {...props} inputMode="decimal" />)}
          {campo('frecuencia', 'Frecuencia en días (opcional)', (props) => <input {...props} inputMode="numeric" />)}
          {campo('responsable', 'Responsable (opcional)', (props) => <input {...props} maxLength={120} />)}
        </fieldset>
        {errorApi !== '' && <p className="field-error" role="alert">{errorApi}</p>}
        <div className="mantenimiento-preventivo-acciones">
          <button className="secondary-button" disabled={guardando} onClick={cancelar} type="button">Volver</button>
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar mantenimiento'}</button>
        </div>
      </form>
    </section>
  );
}

interface ControlProps {
  'aria-describedby': string | undefined;
  'aria-invalid': boolean;
  id: string;
  onChange: (event: { currentTarget: HTMLInputElement | HTMLTextAreaElement }) => void;
  value: string;
}
