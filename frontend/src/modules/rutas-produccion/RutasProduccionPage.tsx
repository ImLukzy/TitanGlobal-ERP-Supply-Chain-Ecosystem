import { useEffect, useRef, useState } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { buscarProductos, listarRutasProduccion, obtenerRutaProduccion } from './api';
import { RutaProduccionDecisionDialog } from './RutaProduccionDecisionDialog';
import { RutasProduccionFormulario } from './RutasProduccionFormulario';
import { SelectorCatalogo } from './SelectorCatalogo';
import {
  estadosRuta,
  type DecisionRuta,
  type EstadoRuta,
  type OpcionCatalogo,
  type PaginaRutasProduccion,
  type RutaProduccionDetalle,
} from './tipos';

const tamanoPagina = 20;
const columnas = [
  { id: 'producto', etiqueta: 'Producto' },
  { id: 'version', etiqueta: 'Versión' },
  { id: 'estado', etiqueta: 'Estado' },
  { id: 'operaciones', etiqueta: 'Operaciones' },
];

type Resultado<T> = { datos: T; error?: never } | { datos?: never; error: string };

function consultar<T>(peticion: Promise<T>, recibir: (resultado: Resultado<T>) => void): () => void {
  let vigente = true;
  peticion
    .then((datos) => {
      if (vigente) recibir({ datos });
    })
    .catch((causa: unknown) => {
      if (vigente) recibir({ error: causa instanceof Error ? causa.message : 'No se pudo cargar la información.' });
    });
  return () => {
    vigente = false;
  };
}

function minutos(valor: number): string {
  return valor.toFixed(2);
}

export function RutasProduccionPage() {
  const [producto, setProducto] = useState<OpcionCatalogo | null>(null);
  const [estado, setEstado] = useState<EstadoRuta | ''>('');
  const [pagina, setPagina] = useState(1);
  const [seleccion, setSeleccion] = useState<number | null>(null);
  const [revision, setRevision] = useState(0);
  const [lectura, setLectura] = useState<{ clave: string; resultado: Resultado<PaginaRutasProduccion> }>();
  const [detalle, setDetalle] = useState<{ clave: string; resultado: Resultado<RutaProduccionDetalle> }>();
  const [formulario, setFormulario] = useState<{ ruta: RutaProduccionDetalle | null }>();
  const [decision, setDecision] = useState<{ accion: DecisionRuta; ruta: RutaProduccionDetalle }>();
  const [aviso, setAviso] = useState('');
  const focoPendiente = useRef<string | null>(null);
  const idProducto = producto?.id;
  const claveLectura = `${idProducto ?? ''}/${estado}/${pagina}/${revision}`;
  const claveDetalle = `${seleccion ?? ''}/${revision}`;
  const resultadoLista = lectura?.clave === claveLectura ? lectura.resultado : undefined;
  const resultadoDetalle = detalle?.clave === claveDetalle ? detalle.resultado : undefined;
  const datos = resultadoLista?.datos;
  const ruta = resultadoDetalle?.datos;

  useEffect(() => consultar(listarRutasProduccion({ productoId: idProducto, estado, pagina, tamanoPagina }), (resultado) => {
    // Si un cambio de estado deja la página fuera del total, vuelve a la última página con datos.
    const ultima = Math.max(1, Math.ceil((resultado.datos?.total ?? 0) / tamanoPagina));
    if (resultado.datos && pagina > ultima) {
      setPagina(ultima);
    } else {
      setLectura({ clave: claveLectura, resultado });
    }
  }), [claveLectura, estado, idProducto, pagina]);

  useEffect(() => {
    if (seleccion === null) {
      return undefined;
    }
    return consultar(obtenerRutaProduccion(seleccion), (resultado) => setDetalle({ clave: claveDetalle, resultado }));
  }, [claveDetalle, seleccion]);

  useEffect(() => {
    if (focoPendiente.current !== null && decision === undefined && formulario === undefined) {
      const objetivo = focoPendiente.current;
      focoPendiente.current = null;
      window.requestAnimationFrame(() => {
        (document.getElementById(objetivo) ?? document.getElementById('rutas-produccion-detalle'))?.focus();
      });
    }
  }, [decision, formulario, ruta]);

  function actualizar(): void {
    setRevision((actual) => actual + 1);
  }

  function cerrarFormulario(objetivo: string): void {
    focoPendiente.current = objetivo;
    setFormulario(undefined);
  }

  function cerrarDecision(objetivo: string): void {
    focoPendiente.current = objetivo;
    setDecision(undefined);
  }

  return (
    <PaginaModulo className="rutas-produccion-page" modulo="32-rutas-produccion">
      <div inert={decision !== undefined}>
        <p className="module-intro">Define las operaciones, los centros de trabajo y los tiempos estándar con que se fabrica cada producto.</p>
        {aviso && <p className="rutas-produccion-aviso" role="status">{aviso}</p>}
        {formulario ? (
          <RutasProduccionFormulario
            cancelar={() => cerrarFormulario(formulario.ruta ? 'rutas-produccion-editar' : 'rutas-produccion-nueva')}
            consultar={() => {
              cerrarFormulario('rutas-produccion-detalle');
              actualizar();
            }}
            guardado={(id) => {
              setSeleccion(id);
              setAviso(formulario.ruta ? 'Borrador actualizado.' : 'Borrador creado.');
              cerrarFormulario('rutas-produccion-detalle');
              actualizar();
            }}
            ruta={formulario.ruta}
          />
        ) : (
          <div className="rutas-produccion-area">
            <section aria-labelledby="rutas-produccion-listado" className="rutas-produccion-tarjeta">
              <div className="rutas-produccion-encabezado">
                <h2 id="rutas-produccion-listado" tabIndex={-1}>Versiones</h2>
                <button
                  className="primary-button"
                  id="rutas-produccion-nueva"
                  onClick={() => {
                    setAviso('');
                    setFormulario({ ruta: null });
                  }}
                  type="button"
                >
                  Nueva ruta
                </button>
              </div>
              <div className="rutas-produccion-filtros">
                <SelectorCatalogo
                  buscar={buscarProductos}
                  cambiar={(seleccionado) => {
                    setProducto(seleccionado);
                    setPagina(1);
                  }}
                  etiqueta="Filtrar por producto"
                  seleccionado={producto}
                  textoBoton="Todos los productos"
                />
                <CampoConError etiqueta="Filtrar por estado" htmlFor="rutas-produccion-estado" idError="rutas-produccion-estado-error">
                  <select
                    id="rutas-produccion-estado"
                    onChange={(event) => {
                      setEstado(event.currentTarget.value as EstadoRuta | '');
                      setPagina(1);
                    }}
                    value={estado}
                  >
                    <option value="">Todos los estados</option>
                    {estadosRuta.map((valor) => <option key={valor}>{valor}</option>)}
                  </select>
                </CampoConError>
              </div>
              {resultadoLista === undefined && <p role="status">Cargando rutas de producción…</p>}
              {resultadoLista?.error && (
                <div className="rutas-produccion-error">
                  <p className="field-error" role="alert">{resultadoLista.error}</p>
                  <button className="secondary-button" onClick={actualizar} type="button">Reintentar listado</button>
                </div>
              )}
              {datos?.items.length === 0 && <p role="status">No se encontraron rutas de producción.</p>}
              {datos && datos.items.length > 0 && (
                <TablaPaginada
                  cambiarPagina={setPagina}
                  caption="Versiones de rutas de producción"
                  className="rutas-produccion-tabla"
                  classNameContenedor="rutas-produccion-tabla-scroll"
                  columnas={columnas}
                  elementos={datos.items}
                  etiquetaRegion="Rutas de producción"
                  obtenerClave={(fila) => fila.id}
                  pagina={datos.pagina}
                  renderizarFila={(fila) => (
                    <tr className={fila.id === seleccion ? 'is-selected' : undefined}>
                      <th scope="row">
                        <button
                          aria-label={`Ver ${fila.productoNombre}, versión ${fila.version}`}
                          aria-pressed={fila.id === seleccion}
                          className="rutas-produccion-enlace"
                          onClick={() => {
                            setSeleccion(fila.id);
                            document.getElementById('rutas-produccion-detalle')?.focus();
                          }}
                          type="button"
                        >
                          {fila.productoNombre}
                        </button>
                      </th>
                      <td className="rutas-produccion-cifra">{fila.version}</td>
                      <td><span className={`rutas-produccion-estado is-${fila.estado.toLowerCase()}`}>{fila.estado}</span></td>
                      <td className="rutas-produccion-cifra">{fila.totalOperaciones}</td>
                    </tr>
                  )}
                  totalPaginas={Math.max(1, Math.ceil(datos.total / datos.tamanoPagina))}
                />
              )}
            </section>

            <PanelDetalle ariaLabelledBy="rutas-produccion-detalle" className="rutas-produccion-tarjeta">
              <h2 id="rutas-produccion-detalle" tabIndex={-1}>Detalle de la ruta</h2>
              {seleccion === null && <p>Selecciona una versión para ver sus operaciones.</p>}
              {seleccion !== null && resultadoDetalle === undefined && <p role="status">Cargando detalle…</p>}
              {resultadoDetalle?.error && (
                <div className="rutas-produccion-error">
                  <p className="field-error" role="alert">{resultadoDetalle.error}</p>
                  <button className="secondary-button" onClick={actualizar} type="button">Reintentar detalle</button>
                </div>
              )}
              {ruta && (
                <>
                  <p className="rutas-produccion-identidad">
                    <strong>{ruta.productoNombre}</strong>
                    <span>Versión {ruta.version} · {ruta.estado}</span>
                  </p>
                  <div className="rutas-produccion-tabla-scroll" role="region" aria-label="Operaciones de la ruta" tabIndex={0}>
                    <table className="rutas-produccion-operaciones">
                      <caption>Operaciones en orden de secuencia</caption>
                      <thead>
                        <tr>
                          <th scope="col">#</th>
                          <th scope="col">Operación</th>
                          <th scope="col">Centro</th>
                          <th scope="col">Prep. (min)</th>
                          <th scope="col">Por unidad (min)</th>
                        </tr>
                      </thead>
                      <tbody>
                        {[...ruta.operaciones].sort((a, b) => a.secuencia - b.secuencia).map((operacion) => (
                          <tr key={operacion.secuencia}>
                            <td className="rutas-produccion-cifra">{operacion.secuencia}</td>
                            <td>{operacion.nombre}</td>
                            <td>{operacion.centroTrabajoNombre}</td>
                            <td className="rutas-produccion-cifra">{minutos(operacion.minutosPreparacion)}</td>
                            <td className="rutas-produccion-cifra">{minutos(operacion.minutosPorUnidad)}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                  <div className="rutas-produccion-acciones">
                    {ruta.estado === 'Borrador' && (
                      <>
                        <button
                          className="secondary-button"
                          id="rutas-produccion-editar"
                          onClick={() => {
                            setAviso('');
                            setFormulario({ ruta });
                          }}
                          type="button"
                        >
                          Editar
                        </button>
                        <button
                          className="primary-button"
                          id="rutas-produccion-activar"
                          onClick={() => {
                            setAviso('');
                            setDecision({ accion: 'activar', ruta });
                          }}
                          type="button"
                        >
                          Activar
                        </button>
                      </>
                    )}
                    {ruta.estado === 'Activa' && (
                      <button
                        className="danger-button"
                        id="rutas-produccion-retirar"
                        onClick={() => {
                          setAviso('');
                          setDecision({ accion: 'retirar', ruta });
                        }}
                        type="button"
                      >
                        Retirar
                      </button>
                    )}
                  </div>
                </>
              )}
            </PanelDetalle>
          </div>
        )}
      </div>
      {decision && (
        <RutaProduccionDecisionDialog
          accion={decision.accion}
          cerrar={() => cerrarDecision(`rutas-produccion-${decision.accion}`)}
          confirmado={() => {
            setAviso(decision.accion === 'activar' ? 'Versión activada.' : 'Versión retirada. Sigue disponible en el historial.');
            cerrarDecision('rutas-produccion-detalle');
            actualizar();
          }}
          consultar={() => {
            cerrarDecision('rutas-produccion-detalle');
            actualizar();
          }}
          ruta={decision.ruta}
        />
      )}
    </PaginaModulo>
  );
}
