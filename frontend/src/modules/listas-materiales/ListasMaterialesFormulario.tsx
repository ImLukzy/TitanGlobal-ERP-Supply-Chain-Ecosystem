import { useEffect, useRef, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { crearListaMaterial, editarListaMaterial } from './api';
import { SelectorProducto } from './SelectorProducto';
import type { ComponenteListaRequest, ListaMaterialDetalle, ProductoSeleccionado } from './tipos';
import { validarCantidad, versionValida } from './validacion';

interface Linea {
  clave: number;
  producto: ProductoSeleccionado | null;
  cantidad: string;
  unidadMedida: string;
}

interface FormularioProps {
  lista: ListaMaterialDetalle | null;
  cancelar: () => void;
  guardado: (id: number) => void;
  consultar: () => void;
}

function lineaVacia(clave: number): Linea {
  return { clave, producto: null, cantidad: '', unidadMedida: '' };
}

export function ListasMaterialesFormulario({ lista, cancelar, guardado, consultar }: FormularioProps) {
  const titulo = useRef<HTMLHeadingElement>(null);
  const [producto, setProducto] = useState<ProductoSeleccionado | null>(null);
  const [version, setVersion] = useState('');
  const [lineas, setLineas] = useState<Linea[]>(() => lista?.componentes.map((componente, clave) => ({
    clave,
    producto: { id: componente.componenteProductoId, nombre: componente.componenteProductoNombre },
    cantidad: String(componente.cantidad),
    unidadMedida: componente.unidadMedida,
  })) ?? [lineaVacia(0)]);
  const siguienteClave = useRef(lineas.length);
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [errorApi, setErrorApi] = useState<{ mensaje: string; consultar: boolean }>();
  const [guardando, setGuardando] = useState(false);
  const idFabricado = lista?.productoId ?? producto?.id;

  useEffect(() => { titulo.current?.focus(); }, []);

  function cambiarLinea(clave: number, cambios: Partial<Linea>): void {
    setLineas((actuales) => actuales.map((linea) => linea.clave === clave ? { ...linea, ...cambios } : linea));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (guardando) return;

    const encontrados: Record<string, string> = {};
    if (idFabricado === undefined) encontrados.producto = 'El identificador del producto debe ser mayor que 0.';
    if (lista === null && !versionValida(version)) encontrados.version = 'La versión debe ser mayor que 0.';
    if (lineas.length < 1 || lineas.length > 100) encontrados.lineas = 'La lista de materiales debe tener entre 1 y 100 componentes.';

    const vistos = new Set<number>();
    const componentes: ComponenteListaRequest[] = [];
    for (const { clave, producto: componente, cantidad, unidadMedida } of lineas) {
      if (componente === null) {
        encontrados[`${clave}-producto`] = 'El identificador del componente debe ser mayor que 0.';
      } else if (componente.id === idFabricado) {
        encontrados[`${clave}-producto`] = 'El producto fabricado no puede ser componente de su propia lista.';
      } else if (vistos.has(componente.id)) {
        encontrados[`${clave}-producto`] = `El componente ${componente.id} está repetido.`;
      }
      if (componente !== null) vistos.add(componente.id);

      const decimal = validarCantidad(cantidad);
      if ('error' in decimal) encontrados[`${clave}-cantidad`] = decimal.error;
      const unidad = unidadMedida.trim();
      if (unidad === '') encontrados[`${clave}-unidad`] = 'La unidad de medida es obligatoria.';
      if (unidad.length > 20) encontrados[`${clave}-unidad`] = 'La unidad de medida no puede superar 20 caracteres.';
      if (componente !== null && 'cantidad' in decimal) {
        componentes.push({ componenteProductoId: componente.id, cantidad: decimal.cantidad, unidadMedida: unidad });
      }
    }

    setErrores(encontrados);
    setErrorApi(undefined);
    if (Object.keys(encontrados).length > 0 || idFabricado === undefined) return;
    setGuardando(true);
    try {
      const resultado = lista === null
        ? await crearListaMaterial({ productoId: idFabricado, version: Number(version), componentes })
        : await editarListaMaterial(lista.id, { componentes });
      guardado(resultado.id);
    } catch (cause: unknown) {
      setErrorApi({ mensaje: cause instanceof Error ? cause.message : 'No se pudo guardar la lista de materiales.', consultar: cause instanceof ApiError && [404, 409].includes(cause.statusCode) });
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="listas-materiales-formulario" className="listas-materiales-tarjeta">
      <h2 id="listas-materiales-formulario" ref={titulo} tabIndex={-1}>{lista === null ? 'Nueva lista de materiales' : 'Editar borrador'}</h2>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <fieldset className="listas-materiales-campos" disabled={guardando}>
          {lista === null ? (
            <div className="listas-materiales-cabecera-formulario">
              <SelectorProducto cambiar={setProducto} error={errores.producto} etiqueta="Producto fabricado" seleccionado={producto} />
              <CampoConError error={errores.version} etiqueta="Versión" htmlFor="listas-materiales-version" idError="listas-materiales-version-error">
                <input aria-describedby={errores.version ? 'listas-materiales-version-error' : undefined} aria-invalid={Boolean(errores.version)} id="listas-materiales-version" inputMode="numeric" onChange={(event) => setVersion(event.currentTarget.value)} value={version} />
              </CampoConError>
            </div>
          ) : <p>{lista.productoNombre} · Versión {lista.version}</p>}

          <h3>Componentes</h3>
          {lineas.map((linea, indice) => {
            const id = `listas-materiales-linea-${linea.clave}`;
            const errorCantidad = errores[`${linea.clave}-cantidad`];
            const errorUnidad = errores[`${linea.clave}-unidad`];
            return (
              <fieldset className="listas-materiales-linea" key={linea.clave}>
                <legend>Línea {indice + 1}</legend>
                <div className="listas-materiales-linea-campos">
                  <SelectorProducto cambiar={(seleccion) => cambiarLinea(linea.clave, { producto: seleccion })} error={errores[`${linea.clave}-producto`]} etiqueta={`Componente ${indice + 1}`} seleccionado={linea.producto} />
                  <CampoConError error={errorCantidad} etiqueta="Cantidad" htmlFor={`${id}-cantidad`} idError={`${id}-cantidad-error`}>
                    <input aria-describedby={errorCantidad ? `${id}-cantidad-error` : undefined} aria-invalid={Boolean(errorCantidad)} id={`${id}-cantidad`} inputMode="decimal" onChange={(event) => cambiarLinea(linea.clave, { cantidad: event.currentTarget.value })} value={linea.cantidad} />
                  </CampoConError>
                  <CampoConError error={errorUnidad} etiqueta="Unidad de medida" htmlFor={`${id}-unidad`} idError={`${id}-unidad-error`}>
                    <input aria-describedby={errorUnidad ? `${id}-unidad-error` : undefined} aria-invalid={Boolean(errorUnidad)} id={`${id}-unidad`} onChange={(event) => cambiarLinea(linea.clave, { unidadMedida: event.currentTarget.value })} value={linea.unidadMedida} />
                  </CampoConError>
                  <button className="secondary-button" onClick={() => setLineas(lineas.filter(({ clave }) => clave !== linea.clave))} type="button">Quitar línea {indice + 1}</button>
                </div>
              </fieldset>
            );
          })}
          {errores.lineas && <p className="field-error" role="alert">{errores.lineas}</p>}
          <button className="secondary-button" disabled={lineas.length >= 100} onClick={() => {
            const nueva = lineaVacia(siguienteClave.current);
            siguienteClave.current += 1;
            setLineas([...lineas, nueva]);
          }} type="button">Agregar componente</button>
        </fieldset>
        {errorApi && (
          <div className="listas-materiales-error">
            <p className="field-error" role="alert">{errorApi.mensaje}</p>
            {errorApi.consultar && lista !== null && <button className="secondary-button" onClick={consultar} type="button">Consultar estado actual</button>}
          </div>
        )}
        <div className="listas-materiales-acciones">
          <button className="secondary-button" disabled={guardando} onClick={cancelar} type="button">Cancelar</button>
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar borrador'}</button>
        </div>
      </form>
    </section>
  );
}
