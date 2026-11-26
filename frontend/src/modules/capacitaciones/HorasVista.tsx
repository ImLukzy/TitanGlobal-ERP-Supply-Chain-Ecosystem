import { useEffect, useMemo, useState } from 'react';
import { FichaEmpleado } from './FichaEmpleado';
import { HorasTabla } from './HorasTabla';
import type { FiltrosHoras } from './tipos';
import { useFichaEmpleado, useHorasData } from './useHorasData';

const TAMANO_PAGINA = 10;

export function HorasVista({ anio }: { anio: number }) {
  const [busqueda, setBusqueda] = useState('');
  const [terminoAplicado, setTerminoAplicado] = useState('');
  const [pagina, setPagina] = useState(1);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const filtros = useMemo<FiltrosHoras>(() => ({ anio, busqueda: terminoAplicado, pagina, tamanoPagina: TAMANO_PAGINA }), [anio, terminoAplicado, pagina]);
  const data = useHorasData(filtros);
  const ficha = useFichaEmpleado(seleccionadoId, anio);

  // La búsqueda se aplica 300 ms después del último cambio y vuelve a la página 1 solo si el texto cambia.
  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      const termino = busqueda.trim();
      if (termino !== terminoAplicado) {
        setTerminoAplicado(termino);
        setPagina(1);
      }
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda, terminoAplicado]);

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  const cargando = data.lista === null;
  const cargandoFicha = seleccionadoId !== null && ficha.ficha === null && ficha.error === '';

  return (
    <>
      <section aria-labelledby="horas-list-title" className="clientes-list-panel capacitaciones-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">HORAS</div>
            <h2 id="horas-list-title">Horas por empleado</h2>
          </div>
        </div>
        <div className="solicitudes-filters capacitaciones-filters">
          <div>
            <label htmlFor="horas-busqueda">Buscar por legajo o nombre</label>
            <input autoComplete="off" id="horas-busqueda" onChange={(event) => setBusqueda(event.currentTarget.value)} placeholder="Escribe para buscar…" type="search" value={busqueda} />
          </div>
        </div>
        <HorasTabla
          cambiarPagina={setPagina}
          cargando={cargando && data.errorLista === ''}
          error={data.errorLista}
          items={data.lista?.items ?? []}
          pagina={pagina}
          seleccionadoId={seleccionadoId}
          seleccionar={setSeleccionadoId}
          totalPaginas={paginas}
        />
      </section>
      <FichaEmpleado anio={anio} cargando={cargandoFicha} error={ficha.error} ficha={ficha.ficha} />
    </>
  );
}
