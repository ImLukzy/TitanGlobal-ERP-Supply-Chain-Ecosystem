import { useEffect, useState } from 'react';
import { listarTransportistas } from './api';
import { mensajeError } from './presentacion';
import type { Pagina, Transportista } from './tipos';

export function SelectorTransportista({
  valor,
  cambiar,
  soloActivos = false,
  error,
}: {
  valor: number;
  cambiar: (id: number) => void;
  soloActivos?: boolean;
  error?: string;
}) {
  const [busqueda, setBusqueda] = useState('');
  const [consulta, setConsulta] = useState('');
  const [pagina, setPagina] = useState(1);
  const [datos, setDatos] = useState<Pagina<Transportista> | null>(null);
  const [fallo, setFallo] = useState('');
  const [intento, setIntento] = useState(0);
  useEffect(() => {
    if (busqueda === consulta) return;
    const timer = setTimeout(() => {
      setConsulta(busqueda);
      setPagina(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [busqueda, consulta]);
  useEffect(() => {
    let vigente = true;
    listarTransportistas({
      busqueda: consulta,
      activo: soloActivos ? true : undefined,
      pagina,
      tamanoPagina: 10,
    })
      .then((respuesta) => {
        if (vigente) {
          setDatos(respuesta);
          setFallo('');
        }
      })
      .catch((e) => {
        if (vigente) setFallo(mensajeError(e));
      });
    return () => {
      vigente = false;
    };
  }, [consulta, pagina, soloActivos, intento]);
  return (
    <fieldset className="transportistas-terceros-selector">
      <legend>{soloActivos ? 'Transportista activo' : 'Filtrar por transportista'}</legend>
      <label htmlFor="selector-transportista-busqueda">Buscar transportista</label>
      <input
        id="selector-transportista-busqueda"
        value={busqueda}
        onChange={(e) => setBusqueda(e.target.value)}
      />
      {valor > 0 && (
        <p>
          Transportista seleccionado: #{valor}{' '}
          <button type="button" className="secondary-button" onClick={() => cambiar(0)}>
            Quitar selección
          </button>
        </p>
      )}
      {fallo ? (
        <p role="alert">
          {fallo}{' '}
          <button type="button" onClick={() => setIntento(intento + 1)}>
            Reintentar
          </button>
        </p>
      ) : datos === null ? (
        <p role="status">Cargando transportistas…</p>
      ) : (
        <>
          <label htmlFor="selector-transportista">Transportista</label>
          <select
            id="selector-transportista"
            value={datos.items.some((t) => t.id === valor) ? valor : ''}
            aria-invalid={!!error}
            aria-describedby={error ? 'selector-transportista-error' : undefined}
            onChange={(e) => cambiar(Number(e.target.value))}
          >
            <option value="">Selecciona un transportista</option>
            {datos.items.map((t) => (
              <option key={t.id} value={t.id}>
                {t.razonSocial} · {t.documentoFiscal} · {t.pais}
              </option>
            ))}
          </select>
          {datos.items.length === 0 && <p role="status">Sin transportistas coincidentes</p>}
          <div className="transportistas-terceros-actions">
            <button
              type="button"
              className="secondary-button"
              disabled={pagina === 1}
              onClick={() => setPagina(pagina - 1)}
            >
              Transportistas anteriores
            </button>
            <span>
              Página {pagina} de {Math.max(1, Math.ceil(datos.total / 10))}
            </span>
            <button
              type="button"
              className="secondary-button"
              disabled={pagina * 10 >= datos.total}
              onClick={() => setPagina(pagina + 1)}
            >
              Más transportistas
            </button>
          </div>
        </>
      )}
      {error && (
        <span id="selector-transportista-error" className="field-error">
          {error}
        </span>
      )}
    </fieldset>
  );
}
