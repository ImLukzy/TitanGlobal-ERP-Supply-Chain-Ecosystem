import { useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { buscarProductos, buscarProveedoresActivos, crearInspeccionInsumo, editarInspeccionInsumo } from './api';
import { SelectorCatalogo } from './SelectorCatalogo';
import type { InspeccionInsumoDetalle } from './tipos';
import { hoyPeru, validarInspeccion, type FormularioInspeccion } from './validacion';

interface FormularioProps {
  inspeccion: InspeccionInsumoDetalle | null;
  cancelar: () => void;
  guardado: (id: number) => void;
}

type CampoTexto = Exclude<keyof FormularioInspeccion, 'producto' | 'proveedor'>;

function valoresIniciales(inspeccion: InspeccionInsumoDetalle | null, hoy: string): FormularioInspeccion {
  if (inspeccion === null) {
    return {
      producto: null,
      proveedor: null,
      lote: '',
      fechaInspeccion: hoy,
      cantidadRecibida: '',
      cantidadMuestra: '',
      unidadMedida: '',
      tolerancia: '',
      inspector: '',
    };
  }
  return {
    producto: { id: inspeccion.productoId, nombre: inspeccion.productoNombre },
    proveedor: { id: inspeccion.proveedorId, nombre: inspeccion.proveedorRazonSocial },
    lote: inspeccion.lote,
    fechaInspeccion: inspeccion.fechaInspeccion,
    cantidadRecibida: String(inspeccion.cantidadRecibida),
    cantidadMuestra: String(inspeccion.cantidadMuestra),
    unidadMedida: inspeccion.unidadMedida,
    tolerancia: String(inspeccion.toleranciaDefectosPorcentaje),
    inspector: inspeccion.inspector,
  };
}

export function InspeccionInsumoFormulario({ inspeccion, cancelar, guardado }: FormularioProps) {
  const titulo = useRef<HTMLHeadingElement>(null);
  const [hoy] = useState(() => hoyPeru());
  const [valores, setValores] = useState<FormularioInspeccion>(() => valoresIniciales(inspeccion, hoy));
  const [errores, setErrores] = useState<Partial<Record<string, string>>>({});
  const [errorApi, setErrorApi] = useState('');
  const [guardando, setGuardando] = useState(false);

  useEffect(() => {
    titulo.current?.focus();
  }, []);

  function cambiar(cambios: Partial<FormularioInspeccion>): void {
    setValores((actuales) => ({ ...actuales, ...cambios }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (guardando) {
      return;
    }
    const resultado = validarInspeccion(valores, hoy);
    setErrores(resultado.errores);
    setErrorApi('');
    if (resultado.solicitud === undefined) {
      return;
    }
    setGuardando(true);
    try {
      const respuesta = inspeccion === null
        ? await crearInspeccionInsumo(resultado.solicitud)
        : await editarInspeccionInsumo(inspeccion.id, resultado.solicitud);
      guardado(respuesta.id);
    } catch (causa: unknown) {
      // Conserva lo escrito y muestra el mensaje literal de la API.
      setErrorApi(causa instanceof Error ? causa.message : 'No se pudo guardar la inspección.');
    } finally {
      setGuardando(false);
    }
  }

  function campo(nombre: CampoTexto, etiqueta: string, control: (props: ControlProps) => ReactNode): ReactNode {
    const id = `control-calidad-insumos-${nombre}`;
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
    <section aria-labelledby="control-calidad-insumos-formulario" className="control-calidad-insumos-tarjeta">
      <h2 id="control-calidad-insumos-formulario" ref={titulo} tabIndex={-1}>
        {inspeccion === null ? 'Nueva inspección' : `Editar inspección ${inspeccion.numero}`}
      </h2>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <fieldset className="control-calidad-insumos-campos" disabled={guardando}>
          <SelectorCatalogo
            buscar={buscarProductos}
            cambiar={(producto) => cambiar({ producto })}
            error={errores.producto}
            etiqueta="Insumo"
            seleccionado={valores.producto}
            textoBoton="Seleccionar insumo"
          />
          <SelectorCatalogo
            buscar={buscarProveedoresActivos}
            cambiar={(proveedor) => cambiar({ proveedor })}
            error={errores.proveedor}
            etiqueta="Proveedor"
            seleccionado={valores.proveedor}
            textoBoton="Seleccionar proveedor"
          />
          {campo('lote', 'Lote', (props) => (
            <input {...props} maxLength={60} onChange={(event) => cambiar({ lote: event.currentTarget.value.toUpperCase() })} />
          ))}
          {campo('fechaInspeccion', 'Fecha de inspección', (props) => <input {...props} max={hoy} type="date" />)}
          {campo('cantidadRecibida', 'Cantidad recibida', (props) => <input {...props} inputMode="decimal" />)}
          {campo('cantidadMuestra', 'Cantidad de la muestra', (props) => <input {...props} inputMode="decimal" />)}
          {campo('unidadMedida', 'Unidad de medida', (props) => <input {...props} maxLength={30} />)}
          {campo('tolerancia', 'Tolerancia de defectos (%)', (props) => <input {...props} inputMode="decimal" />)}
          {campo('inspector', 'Inspector', (props) => <input {...props} maxLength={120} />)}
        </fieldset>
        {errorApi !== '' && <p className="field-error" role="alert">{errorApi}</p>}
        <div className="control-calidad-insumos-acciones">
          <button className="secondary-button" disabled={guardando} onClick={cancelar} type="button">Cancelar</button>
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar inspección'}</button>
        </div>
      </form>
    </section>
  );
}

interface ControlProps {
  'aria-describedby': string | undefined;
  'aria-invalid': boolean;
  id: string;
  onChange: (event: { currentTarget: HTMLInputElement }) => void;
  value: string;
}
