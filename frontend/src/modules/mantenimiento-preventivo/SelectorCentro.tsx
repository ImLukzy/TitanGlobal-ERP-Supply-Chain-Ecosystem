import { useEffect, useId, useState } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { buscarCentros } from './api';
import { nombreCentro } from './presentacion';
import type { CentroOpcion, PaginaCentros } from './tipos';

interface SelectorCentroProps {
  etiqueta: string;
  textoBoton: string;
  seleccionado: CentroOpcion | null;
  cambiar: (centro: CentroOpcion | null) => void;
  soloActivos: boolean;
  error?: string;
}

export function SelectorCentro({ etiqueta, textoBoton, seleccionado, cambiar, soloActivos, error }: SelectorCentroProps) {
  const id = useId();
  const [abierto, setAbierto] = useState(false);

  return (
    <CampoConError etiqueta={etiqueta} htmlFor={id} error={error} idError={`${id}-error`}>
      <div className="mantenimiento-preventivo-selector-acciones">
        <button
          aria-describedby={error ? `${id}-error` : undefined}
          aria-expanded={abierto}
          aria-invalid={Boolean(error)}
          className="secondary-button"
          id={id}
          onClick={() => setAbierto(!abierto)}
          type="button"
        >
          {seleccionado === null ? textoBoton : nombreCentro(seleccionado)}
        </button>
        {seleccionado !== null && <button className="secondary-button" onClick={() => cambiar(null)} type="button">Limpiar</button>}
      </div>
      {abierto && (
        <Catalogo
          elegir={(centro) => {
            cambiar(centro);
            setAbierto(false);
            document.getElementById(id)?.focus();
          }}
          etiqueta={etiqueta}
          soloActivos={soloActivos}
        />
      )}
    </CampoConError>
  );
}

interface CatalogoProps {
  etiqueta: string;
  soloActivos: boolean;
  elegir: (centro: CentroOpcion) => void;
}

function Catalogo({ etiqueta, soloActivos, elegir }: CatalogoProps) {
  const id = useId();
  const [entrada, setEntrada] = useState('');
  const [consulta, setConsulta] = useState({ busqueda: '', intento: 0 });
  const [respuesta, setRespuesta] = useState<{ consulta: typeof consulta; datos?: PaginaCentros; error?: string }>();
  const resultado = respuesta?.consulta === consulta ? respuesta : undefined;

  useEffect(() => {
    let vigente = true;
    buscarCentros(consulta.busqueda, soloActivos)
      .then((datos) => {
        if (vigente) setRespuesta({ consulta, datos });
      })
      .catch((causa: unknown) => {
        if (vigente) setRespuesta({ consulta, error: causa instanceof Error ? causa.message : 'No se pudieron cargar los centros de trabajo.' });
      });
    return () => {
      vigente = false;
    };
  }, [consulta, soloActivos]);

  function aplicarBusqueda(): void {
    setConsulta({ busqueda: entrada.trim(), intento: consulta.intento + 1 });
  }

  return (
    <div className="mantenimiento-preventivo-catalogo">
      <div className="mantenimiento-preventivo-busqueda-centro">
        <label className="form-field" htmlFor={id}>
          Buscar por código o nombre
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
      {resultado === undefined && <p role="status">Cargando centros…</p>}
      {resultado?.error && <p className="field-error" role="alert">{resultado.error}</p>}
      {resultado?.datos?.items.length === 0 && <p role="status">No se encontraron centros de trabajo.</p>}
      {resultado?.datos && resultado.datos.items.length > 0 && (
        <>
          <ul aria-label={`Opciones de ${etiqueta.toLowerCase()}`} className="mantenimiento-preventivo-opciones">
            {resultado.datos.items.map((centro) => (
              <li key={centro.id}>
                <button className="secondary-button" onClick={() => elegir(centro)} type="button">{nombreCentro(centro)}</button>
              </li>
            ))}
          </ul>
          {resultado.datos.total > resultado.datos.items.length && (
            <p className="mantenimiento-preventivo-nota">Se muestran los primeros {resultado.datos.items.length} de {resultado.datos.total}. Escribe parte del código o del nombre para acotar.</p>
          )}
        </>
      )}
    </div>
  );
}
