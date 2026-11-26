import { useEffect, useRef, useState } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { listarListasMateriales, obtenerListaMaterial } from './api';
import { ListaMaterialDecisionDialog } from './ListaMaterialDecisionDialog';
import { ListasMaterialesFormulario } from './ListasMaterialesFormulario';
import { SelectorProducto } from './SelectorProducto';
import { estadosLista, type DecisionLista, type EstadoLista, type ListaMaterialDetalle, type PaginaListasMateriales, type ProductoSeleccionado } from './tipos';

const tamanoPagina = 20;
const columnas = [
  { id: 'producto', etiqueta: 'Producto' },
  { id: 'version', etiqueta: 'Versión' },
  { id: 'estado', etiqueta: 'Estado' },
  { id: 'componentes', etiqueta: 'Componentes' },
];

type Resultado<T> = { datos: T; error?: never } | { datos?: never; error: string };

function consultar<T>(peticion: Promise<T>, recibir: (resultado: Resultado<T>) => void): () => void {
  let vigente = true;
  void peticion.then((datos) => {
    if (vigente) recibir({ datos });
  }).catch((cause: unknown) => {
    if (vigente) recibir({ error: cause instanceof Error ? cause.message : 'No se pudo cargar la información.' });
  });
  return () => { vigente = false; };
}

export function ListasMaterialesPage() {
  const [producto, setProducto] = useState<ProductoSeleccionado | null>(null);
  const [estado, setEstado] = useState<EstadoLista | ''>('');
  const [pagina, setPagina] = useState(1);
  const [seleccion, setSeleccion] = useState<number | null>(null);
  const [revision, setRevision] = useState(0);
  const [lectura, setLectura] = useState<{ clave: string; resultado: Resultado<PaginaListasMateriales> }>();
  const [detalle, setDetalle] = useState<{ clave: string; resultado: Resultado<ListaMaterialDetalle> }>();
  const [formulario, setFormulario] = useState<{ lista: ListaMaterialDetalle | null }>();
  const [decision, setDecision] = useState<{ accion: DecisionLista; lista: ListaMaterialDetalle }>();
  const [aviso, setAviso] = useState('');
  const focoPendiente = useRef<string | null>(null);
  const idProducto = producto?.id;
  const claveLectura = `${idProducto ?? ''}/${estado}/${pagina}/${revision}`;
  const claveDetalle = `${seleccion ?? ''}/${revision}`;
  const resultadoLista = lectura?.clave === claveLectura ? lectura.resultado : undefined;
  const resultadoDetalle = detalle?.clave === claveDetalle ? detalle.resultado : undefined;
  const datos = resultadoLista?.datos;
  const lista = resultadoDetalle?.datos;

  useEffect(() => consultar(listarListasMateriales({ productoId: idProducto, estado, pagina, tamanoPagina }), (resultado) => {
    const ultima = Math.max(1, Math.ceil((resultado.datos?.total ?? 0) / tamanoPagina));
    if (resultado.datos && pagina > ultima) {
      setPagina(ultima);
    } else {
      setLectura({ clave: claveLectura, resultado });
    }
  }), [claveLectura, estado, idProducto, pagina]);

  useEffect(() => {
    if (seleccion === null) return;
    return consultar(obtenerListaMaterial(seleccion), (resultado) => setDetalle({ clave: claveDetalle, resultado }));
  }, [claveDetalle, seleccion]);

  useEffect(() => {
    if (focoPendiente.current !== null && decision === undefined && formulario === undefined) {
      (document.getElementById(focoPendiente.current) ?? document.getElementById('listas-materiales-listado'))?.focus();
      focoPendiente.current = null;
    }
  }, [decision, formulario]);

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
    <PaginaModulo className="listas-materiales-page" modulo="31-listas-materiales">
      <div inert={decision !== undefined}>
        {aviso && <p className="listas-materiales-aviso" role="status">{aviso}</p>}
        {formulario ? (
          <ListasMaterialesFormulario
            cancelar={() => cerrarFormulario(formulario.lista ? 'listas-materiales-editar' : 'listas-materiales-nueva')}
            consultar={() => { cerrarFormulario('listas-materiales-detalle'); actualizar(); }}
            guardado={(id) => {
              setSeleccion(id);
              setAviso(formulario.lista ? 'Borrador actualizado.' : 'Borrador creado.');
              cerrarFormulario('listas-materiales-detalle');
              actualizar();
            }}
            lista={formulario.lista}
          />
        ) : (
          <div className="listas-materiales-area">
            <section aria-labelledby="listas-materiales-listado" className="listas-materiales-tarjeta">
              <div className="listas-materiales-encabezado">
                <h2 id="listas-materiales-listado" tabIndex={-1}>Versiones</h2>
                <button className="primary-button" id="listas-materiales-nueva" onClick={() => { setAviso(''); setFormulario({ lista: null }); }} type="button">Nueva lista</button>
              </div>
              <div className="listas-materiales-filtros">
                <SelectorProducto cambiar={(seleccionado) => { setProducto(seleccionado); setPagina(1); }} etiqueta="Filtrar por producto" seleccionado={producto} />
                <CampoConError etiqueta="Filtrar por estado" htmlFor="listas-materiales-estado" idError="listas-materiales-estado-error">
                  <select id="listas-materiales-estado" onChange={(event) => {
                    setEstado(event.currentTarget.value as EstadoLista | '');
                    setPagina(1);
                  }} value={estado}>
                    <option value="">Todos los estados</option>
                    {estadosLista.map((valor) => <option key={valor}>{valor}</option>)}
                  </select>
                </CampoConError>
              </div>
              {resultadoLista === undefined && <p role="status">Cargando listas de materiales…</p>}
              {resultadoLista?.error && <div className="listas-materiales-error"><p className="field-error" role="alert">{resultadoLista.error}</p><button className="secondary-button" onClick={actualizar} type="button">Reintentar listado</button></div>}
              {datos?.items.length === 0 && <p role="status">No se encontraron listas de materiales.</p>}
              {datos && datos.items.length > 0 && (
                <TablaPaginada
                  cambiarPagina={setPagina}
                  caption="Versiones de listas de materiales"
                  className="listas-materiales-tabla"
                  columnas={columnas}
                  elementos={datos.items}
                  etiquetaRegion="Listas de materiales"
                  obtenerClave={(fila) => fila.id}
                  pagina={datos.pagina}
                  renderizarFila={(fila) => (
                    <tr className={fila.id === seleccion ? 'listas-materiales-seleccion' : undefined}>
                      <th scope="row"><button aria-label={`Ver ${fila.productoNombre}, versión ${fila.version}`} aria-pressed={fila.id === seleccion} className="listas-materiales-enlace" onClick={() => {
                        setSeleccion(fila.id);
                        document.getElementById('listas-materiales-detalle')?.focus();
                      }} type="button">{fila.productoNombre}</button></th>
                      <td>{fila.version}</td><td>{fila.estado}</td><td>{fila.totalComponentes}</td>
                    </tr>
                  )}
                  totalPaginas={Math.ceil(datos.total / datos.tamanoPagina)}
                />
              )}
            </section>

            <PanelDetalle ariaLabelledBy="listas-materiales-detalle" className="listas-materiales-tarjeta">
              <h2 id="listas-materiales-detalle" tabIndex={-1}>Detalle de la lista</h2>
              {seleccion === null && <p>Selecciona una versión para ver sus componentes.</p>}
              {seleccion !== null && resultadoDetalle === undefined && <p role="status">Cargando detalle…</p>}
              {resultadoDetalle?.error && <div className="listas-materiales-error"><p className="field-error" role="alert">{resultadoDetalle.error}</p><button className="secondary-button" onClick={actualizar} type="button">Reintentar detalle</button></div>}
              {lista && (
                <>
                  <p className="listas-materiales-identidad"><strong>{lista.productoNombre}</strong><span>Versión {lista.version} · {lista.estado}</span></p>
                  <h3>Componentes</h3>
                  <ul className="listas-materiales-componentes">
                    {lista.componentes.map((componente) => (
                      <li key={componente.componenteProductoId}>
                        <span>{componente.componenteProductoNombre}</span>
                        <span>{componente.cantidad} {componente.unidadMedida}</span>
                      </li>
                    ))}
                  </ul>
                  <div className="listas-materiales-acciones">
                    {lista.estado === 'Borrador' && (
                      <>
                        <button className="secondary-button" id="listas-materiales-editar" onClick={() => { setAviso(''); setFormulario({ lista }); }} type="button">Editar</button>
                        <button className="primary-button" id="listas-materiales-activar" onClick={() => { setAviso(''); setDecision({ accion: 'activar', lista }); }} type="button">Activar</button>
                      </>
                    )}
                    {lista.estado === 'Activa' && <button className="danger-button" id="listas-materiales-retirar" onClick={() => { setAviso(''); setDecision({ accion: 'retirar', lista }); }} type="button">Retirar</button>}
                  </div>
                </>
              )}
            </PanelDetalle>
          </div>
        )}
      </div>
      {decision && (
        <ListaMaterialDecisionDialog
          accion={decision.accion}
          cerrar={() => cerrarDecision(`listas-materiales-${decision.accion}`)}
          confirmado={() => {
            setAviso(decision.accion === 'activar' ? 'Versión activada.' : 'Versión retirada. Sigue disponible en el historial.');
            cerrarDecision('listas-materiales-detalle');
            actualizar();
          }}
          consultar={() => { cerrarDecision('listas-materiales-detalle'); actualizar(); }}
          lista={decision.lista}
        />
      )}
    </PaginaModulo>
  );
}
