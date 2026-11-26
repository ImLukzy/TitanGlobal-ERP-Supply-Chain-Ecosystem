import { useEffect, useId, useState } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { listarProductos } from '../productos/api';
import type { PaginaProductos } from '../productos/tipos';
import type { ProductoSeleccionado } from './tipos';

interface SelectorProductoProps {
  etiqueta: string;
  seleccionado: ProductoSeleccionado | null;
  cambiar: (producto: ProductoSeleccionado | null) => void;
  error?: string;
}

export function SelectorProducto({ etiqueta, seleccionado, cambiar, error }: SelectorProductoProps) {
  const id = useId();
  const [abierto, setAbierto] = useState(false);

  return (
    <CampoConError etiqueta={etiqueta} htmlFor={id} error={error} idError={`${id}-error`}>
      <div className="listas-materiales-selector-acciones">
        <button
          aria-describedby={error ? `${id}-error` : undefined}
          aria-expanded={abierto}
          className="secondary-button"
          id={id}
          onClick={() => setAbierto(!abierto)}
          type="button"
        >
          {seleccionado?.nombre ?? 'Seleccionar producto'}
        </button>
        {seleccionado && <button className="secondary-button" onClick={() => cambiar(null)} type="button">Limpiar</button>}
      </div>
      {abierto && (
        <CatalogoProductos etiqueta={etiqueta} elegir={(producto) => {
          cambiar(producto);
          setAbierto(false);
          document.getElementById(id)?.focus();
        }} />
      )}
    </CampoConError>
  );
}

function CatalogoProductos({ etiqueta, elegir }: { etiqueta: string; elegir: (producto: ProductoSeleccionado) => void }) {
  const id = useId();
  const [entrada, setEntrada] = useState('');
  const [consulta, setConsulta] = useState({ busqueda: '', pagina: 1, intento: 0 });
  const [respuesta, setRespuesta] = useState<{ consulta: typeof consulta; datos?: PaginaProductos; error?: string }>();
  const resultado = respuesta?.consulta === consulta ? respuesta : undefined;

  useEffect(() => {
    let vigente = true;
    void listarProductos({ busqueda: consulta.busqueda, pagina: consulta.pagina, tamanoPagina: 10, categoria: '', precioMin: '', precioMax: '' })
      .then((datos) => {
        if (vigente) setRespuesta({ consulta, datos });
      })
      .catch((cause: unknown) => {
        if (vigente) setRespuesta({ consulta, error: cause instanceof Error ? cause.message : 'No se pudieron cargar los productos.' });
      });
    return () => { vigente = false; };
  }, [consulta]);

  return (
    <div className="listas-materiales-catalogo">
      <div className="listas-materiales-busqueda">
        <label className="form-field" htmlFor={id}>
          Buscar por nombre
          <input id={id} onChange={(event) => setEntrada(event.currentTarget.value)} onKeyDown={(event) => {
            if (event.key === 'Enter') {
              event.preventDefault();
              setConsulta({ busqueda: entrada.trim(), pagina: 1, intento: consulta.intento + 1 });
            }
          }} type="search" value={entrada} />
        </label>
        <button className="secondary-button" onClick={() => setConsulta({ busqueda: entrada.trim(), pagina: 1, intento: consulta.intento + 1 })} type="button">Buscar</button>
      </div>
      {resultado === undefined && <p role="status">Cargando productos…</p>}
      {resultado?.error && <p className="field-error" role="alert">{resultado.error}</p>}
      {resultado?.datos?.items.length === 0 && <p role="status">No se encontraron productos.</p>}
      {resultado?.datos && resultado.datos.items.length > 0 && (
        <>
          <ul aria-label={`Opciones de ${etiqueta.toLowerCase()}`} className="listas-materiales-opciones">
            {resultado.datos.items.map((producto) => (
              <li key={producto.id}>
                <button className="secondary-button" onClick={() => elegir(producto)} type="button">{producto.nombre} · #{producto.id}</button>
              </li>
            ))}
          </ul>
          <nav aria-label={`Paginación de ${etiqueta.toLowerCase()}`} className="listas-materiales-acciones">
            <button className="secondary-button" disabled={consulta.pagina === 1} onClick={() => setConsulta({ ...consulta, pagina: consulta.pagina - 1 })} type="button">Anterior</button>
            <span>Página {consulta.pagina}</span>
            <button className="secondary-button" disabled={consulta.pagina * resultado.datos.tamanoPagina >= resultado.datos.total} onClick={() => setConsulta({ ...consulta, pagina: consulta.pagina + 1 })} type="button">Siguiente</button>
          </nav>
        </>
      )}
    </div>
  );
}
