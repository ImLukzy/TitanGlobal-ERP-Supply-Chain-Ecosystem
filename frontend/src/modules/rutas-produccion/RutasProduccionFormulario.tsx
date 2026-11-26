import { useEffect, useRef, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { buscarCentrosTrabajo, buscarProductos, crearRutaProduccion, editarRutaProduccion } from './api';
import { SelectorCatalogo } from './SelectorCatalogo';
import type { OpcionCatalogo, RutaProduccionDetalle } from './tipos';
import { validarFormularioRuta, type LineaOperacion } from './validacion';

interface FormularioProps {
  ruta: RutaProduccionDetalle | null;
  cancelar: () => void;
  guardado: (id: number) => void;
  consultar: () => void;
}

const maximoOperaciones = 100;

function lineaVacia(clave: number): LineaOperacion {
  return { clave, nombre: '', centro: null, minutosPreparacion: '0', minutosPorUnidad: '' };
}

function lineasIniciales(ruta: RutaProduccionDetalle | null): LineaOperacion[] {
  if (ruta === null) {
    return [lineaVacia(0)];
  }
  return [...ruta.operaciones]
    .sort((a, b) => a.secuencia - b.secuencia)
    .map((operacion, clave) => ({
      clave,
      nombre: operacion.nombre,
      centro: { id: operacion.centroTrabajoId, nombre: operacion.centroTrabajoNombre },
      minutosPreparacion: String(operacion.minutosPreparacion),
      minutosPorUnidad: String(operacion.minutosPorUnidad),
    }));
}

export function RutasProduccionFormulario({ ruta, cancelar, guardado, consultar }: FormularioProps) {
  const titulo = useRef<HTMLHeadingElement>(null);
  const [producto, setProducto] = useState<OpcionCatalogo | null>(null);
  const [version, setVersion] = useState('');
  const [lineas, setLineas] = useState<LineaOperacion[]>(() => lineasIniciales(ruta));
  const siguienteClave = useRef(lineas.length);
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [errorApi, setErrorApi] = useState<{ mensaje: string; consultar: boolean }>();
  const [guardando, setGuardando] = useState(false);
  const esAlta = ruta === null;

  useEffect(() => {
    titulo.current?.focus();
  }, []);

  function cambiarLinea(clave: number, cambios: Partial<LineaOperacion>): void {
    setLineas((actuales) => actuales.map((linea) => (linea.clave === clave ? { ...linea, ...cambios } : linea)));
  }

  function mover(indice: number, desplazamiento: -1 | 1): void {
    setLineas((actuales) => {
      const copia = [...actuales];
      const destino = indice + desplazamiento;
      [copia[indice], copia[destino]] = [copia[destino] as LineaOperacion, copia[indice] as LineaOperacion];
      return copia;
    });
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (guardando) {
      return;
    }
    const resultado = validarFormularioRuta({ producto, version, lineas }, esAlta);
    setErrores(resultado.errores);
    setErrorApi(undefined);
    if (resultado.operaciones === undefined) {
      return;
    }

    setGuardando(true);
    try {
      const respuesta = ruta === null
        ? await crearRutaProduccion({ productoId: producto?.id ?? 0, version: Number(version), operaciones: resultado.operaciones })
        : await editarRutaProduccion(ruta.id, { operaciones: resultado.operaciones });
      guardado(respuesta.id);
    } catch (causa: unknown) {
      // Conserva lo escrito y muestra el mensaje literal de la API.
      setErrorApi({
        mensaje: causa instanceof Error ? causa.message : 'No se pudo guardar la ruta de producción.',
        consultar: causa instanceof ApiError && [404, 409].includes(causa.statusCode),
      });
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="rutas-produccion-formulario" className="rutas-produccion-tarjeta">
      <h2 id="rutas-produccion-formulario" ref={titulo} tabIndex={-1}>{esAlta ? 'Nueva ruta de producción' : 'Editar borrador'}</h2>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <fieldset className="rutas-produccion-campos" disabled={guardando}>
          {ruta === null ? (
            <div className="rutas-produccion-cabecera-formulario">
              <SelectorCatalogo
                buscar={buscarProductos}
                cambiar={setProducto}
                error={errores.producto}
                etiqueta="Producto fabricado"
                seleccionado={producto}
                textoBoton="Seleccionar producto"
              />
              <CampoConError error={errores.version} etiqueta="Versión" htmlFor="rutas-produccion-version" idError="rutas-produccion-version-error">
                <input
                  aria-describedby={errores.version ? 'rutas-produccion-version-error' : undefined}
                  aria-invalid={Boolean(errores.version)}
                  id="rutas-produccion-version"
                  inputMode="numeric"
                  onChange={(event) => setVersion(event.currentTarget.value)}
                  value={version}
                />
              </CampoConError>
            </div>
          ) : (
            <p className="rutas-produccion-identidad"><strong>{ruta.productoNombre}</strong><span>Versión {ruta.version}</span></p>
          )}

          <h3>Operaciones</h3>
          <p className="rutas-produccion-ayuda">La secuencia sigue el orden de la lista. Usa Subir y Bajar para cambiarla.</p>
          {lineas.map((linea, indice) => {
            const numero = indice + 1;
            const id = `rutas-produccion-operacion-${linea.clave}`;
            const errorNombre = errores[`${linea.clave}-nombre`];
            const errorPreparacion = errores[`${linea.clave}-preparacion`];
            const errorUnidad = errores[`${linea.clave}-unidad`];
            return (
              <fieldset className="rutas-produccion-linea" key={linea.clave}>
                <legend>Operación {numero}</legend>
                <div className="rutas-produccion-linea-campos">
                  <CampoConError error={errorNombre} etiqueta={`Nombre de la operación ${numero}`} htmlFor={`${id}-nombre`} idError={`${id}-nombre-error`}>
                    <input
                      aria-describedby={errorNombre ? `${id}-nombre-error` : undefined}
                      aria-invalid={Boolean(errorNombre)}
                      id={`${id}-nombre`}
                      maxLength={140}
                      onChange={(event) => cambiarLinea(linea.clave, { nombre: event.currentTarget.value })}
                      value={linea.nombre}
                    />
                  </CampoConError>
                  <SelectorCatalogo
                    buscar={buscarCentrosTrabajo}
                    cambiar={(centro) => cambiarLinea(linea.clave, { centro })}
                    error={errores[`${linea.clave}-centro`]}
                    etiqueta={`Centro de trabajo ${numero}`}
                    seleccionado={linea.centro}
                    textoBoton="Seleccionar centro"
                  />
                  <CampoConError error={errorPreparacion} etiqueta={`Minutos de preparación ${numero}`} htmlFor={`${id}-preparacion`} idError={`${id}-preparacion-error`}>
                    <input
                      aria-describedby={errorPreparacion ? `${id}-preparacion-error` : undefined}
                      aria-invalid={Boolean(errorPreparacion)}
                      id={`${id}-preparacion`}
                      inputMode="decimal"
                      onChange={(event) => cambiarLinea(linea.clave, { minutosPreparacion: event.currentTarget.value })}
                      value={linea.minutosPreparacion}
                    />
                  </CampoConError>
                  <CampoConError error={errorUnidad} etiqueta={`Minutos por unidad ${numero}`} htmlFor={`${id}-unidad`} idError={`${id}-unidad-error`}>
                    <input
                      aria-describedby={errorUnidad ? `${id}-unidad-error` : undefined}
                      aria-invalid={Boolean(errorUnidad)}
                      id={`${id}-unidad`}
                      inputMode="decimal"
                      onChange={(event) => cambiarLinea(linea.clave, { minutosPorUnidad: event.currentTarget.value })}
                      value={linea.minutosPorUnidad}
                    />
                  </CampoConError>
                </div>
                <div className="rutas-produccion-acciones">
                  <button className="secondary-button" disabled={indice === 0} onClick={() => mover(indice, -1)} type="button">Subir operación {numero}</button>
                  <button className="secondary-button" disabled={indice === lineas.length - 1} onClick={() => mover(indice, 1)} type="button">Bajar operación {numero}</button>
                  <button className="secondary-button" onClick={() => setLineas(lineas.filter(({ clave }) => clave !== linea.clave))} type="button">Quitar operación {numero}</button>
                </div>
              </fieldset>
            );
          })}
          {errores.operaciones && <p className="field-error" role="alert">{errores.operaciones}</p>}
          <button
            className="secondary-button"
            disabled={lineas.length >= maximoOperaciones}
            onClick={() => {
              const nueva = lineaVacia(siguienteClave.current);
              siguienteClave.current += 1;
              setLineas([...lineas, nueva]);
            }}
            type="button"
          >
            Agregar operación
          </button>
        </fieldset>
        {errorApi && (
          <div className="rutas-produccion-error">
            <p className="field-error" role="alert">{errorApi.mensaje}</p>
            {errorApi.consultar && ruta !== null && <button className="secondary-button" onClick={consultar} type="button">Consultar estado actual</button>}
          </div>
        )}
        <div className="rutas-produccion-acciones">
          <button className="secondary-button" disabled={guardando} onClick={cancelar} type="button">Cancelar</button>
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar borrador'}</button>
        </div>
      </form>
    </section>
  );
}
