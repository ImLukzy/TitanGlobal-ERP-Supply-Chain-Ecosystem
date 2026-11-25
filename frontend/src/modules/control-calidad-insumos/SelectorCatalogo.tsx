import { useEffect, useId, useState } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { OpcionCatalogo, PaginaCatalogo } from './tipos';

interface SelectorCatalogoProps {
  etiqueta: string;
  textoBoton: string;
  seleccionado: OpcionCatalogo | null;
  cambiar: (opcion: OpcionCatalogo | null) => void;
  buscar: (busqueda: string, pagina: number) => Promise<PaginaCatalogo>;
  error?: string;
}

export function SelectorCatalogo({ etiqueta, textoBoton, seleccionado, cambiar, buscar, error }: SelectorCatalogoProps) {
  const id = useId();
  const [abierto, setAbierto] = useState(false);

  return (
    <CampoConError etiqueta={etiqueta} htmlFor={id} error={error} idError={`${id}-error`}>
      <div className="control-calidad-insumos-selector-acciones">
        <button
          aria-describedby={error ? `${id}-error` : undefined}
          aria-expanded={abierto}
          aria-invalid={Boolean(error)}
          className="secondary-button"
          id={id}
          onClick={() => setAbierto(!abierto)}
          type="button"
        >
          {seleccionado === null ? textoBoton : seleccionado.nombre}
        </button>
        {seleccionado !== null && <button className="secondary-button" onClick={() => cambiar(null)} type="button">Limpiar</button>}
      </div>
      {abierto && (
        <Catalogo
          buscar={buscar}
          elegir={(opcion) => {
            cambiar(opcion);
            setAbierto(false);
            document.getElementById(id)?.focus();
          }}
          etiqueta={etiqueta}
        />
      )}
    </CampoConError>
  );
}

interface CatalogoProps {
  etiqueta: string;
  buscar: (busqueda: string, pagina: number) => Promise<PaginaCatalogo>;
  elegir: (opcion: OpcionCatalogo) => void;
}

function Catalogo({ etiqueta, buscar, elegir }: CatalogoProps) {
  const id = useId();
  const [entrada, setEntrada] = useState('');
  const [consulta, setConsulta] = useState({ busqueda: '', pagina: 1, intento: 0 });
  const [respuesta, setRespuesta] = useState<{ consulta: typeof consulta; datos?: PaginaCatalogo; error?: string }>();
  const resultado = respuesta?.consulta === consulta ? respuesta : undefined;
  const etiquetaMinuscula = etiqueta.toLowerCase();

  useEffect(() => {
    let vigente = true;
    buscar(consulta.busqueda, consulta.pagina)
      .then((datos) => {
        if (vigente) setRespuesta({ consulta, datos });
      })
      .catch((causa: unknown) => {
        if (vigente) setRespuesta({ consulta, error: causa instanceof Error ? causa.message : 'No se pudo cargar el catálogo.' });
      });
    return () => {
      vigente = false;
    };
  }, [buscar, consulta]);

  function aplicarBusqueda(): void {
    setConsulta({ busqueda: entrada.trim(), pagina: 1, intento: consulta.intento + 1 });
  }

  return (
    <div className="control-calidad-insumos-catalogo">
      <div className="control-calidad-insumos-busqueda">
        <label className="form-field" htmlFor={id}>
          Buscar por nombre
          <input
            id={id}
            onChange={(event) => setEntrada(event.currentTarget.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault();
                aplicarBusqueda();
              }
            }}
            type="search"
            value={entrada}
          />
        </label>
        <button className="secondary-button" onClick={aplicarBusqueda} type="button">Buscar</button>
      </div>
      {resultado === undefined && <p role="status">Cargando opciones…</p>}
      {resultado?.error && <p className="field-error" role="alert">{resultado.error}</p>}
      {resultado?.datos?.items.length === 0 && <p role="status">No se encontraron opciones.</p>}
      {resultado?.datos && resultado.datos.items.length > 0 && (
        <>
          <ul aria-label={`Opciones de ${etiquetaMinuscula}`} className="control-calidad-insumos-opciones">
            {resultado.datos.items.map((opcion) => (
              <li key={opcion.id}>
                <button className="secondary-button" onClick={() => elegir(opcion)} type="button">
                  {opcion.nombre} · #{opcion.id}
                </button>
              </li>
            ))}
          </ul>
          <nav aria-label={`Paginación de ${etiquetaMinuscula}`} className="control-calidad-insumos-acciones">
            <button className="secondary-button" disabled={consulta.pagina === 1} onClick={() => setConsulta({ ...consulta, pagina: consulta.pagina - 1 })} type="button">Anterior</button>
            <span>Página {consulta.pagina}</span>
            <button
              className="secondary-button"
              disabled={consulta.pagina * resultado.datos.tamanoPagina >= resultado.datos.total}
              onClick={() => setConsulta({ ...consulta, pagina: consulta.pagina + 1 })}
              type="button"
            >
              Siguiente
            </button>
          </nav>
        </>
      )}
    </div>
  );
}
