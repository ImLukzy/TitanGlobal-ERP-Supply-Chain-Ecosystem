import { useCallback, useRef, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import { eliminarMarcacion, importarMarcaciones, registrarMarcacion } from './api';
import { DialogoImportacion } from './DialogoImportacion';
import { MarcacionFormulario } from './MarcacionFormulario';
import { MarcacionesFiltros } from './MarcacionesFiltros';
import { MarcacionesTabla } from './MarcacionesTabla';
import { aFechaHoraLocal, esConflicto, mensajeError, semanaActual, TEXTO_SOLO_RRHH } from './presentacion';
import type {
  FiltrosMarcaciones,
  ImportarMarcacionesRequest,
  Marcacion,
  RegistrarMarcacionRequest,
  ResultadoImportacion,
} from './tipos';
import { useMarcacionesData } from './useMarcacionesData';

const TAMANO_PAGINA = 10;

type Dialogo = 'ninguno' | 'importar' | 'eliminar';

function filtrosIniciales(): FiltrosMarcaciones {
  const { desde, hasta } = semanaActual();
  return { empleadoId: null, desde, hasta, tipo: '', origen: '', pagina: 1, tamanoPagina: TAMANO_PAGINA };
}

export function MarcacionesVista({ puedeEscribir }: { puedeEscribir: boolean }) {
  const [filtros, setFiltros] = useState<FiltrosMarcaciones>(filtrosIniciales);
  const [formularioAbierto, setFormularioAbierto] = useState(false);
  const [dialogo, setDialogo] = useState<Dialogo>('ninguno');
  const [aEliminar, setAEliminar] = useState<Marcacion | null>(null);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const botonImportarRef = useRef<HTMLButtonElement>(null);
  const data = useMarcacionesData(filtros);

  function cambiarFiltros(cambio: Partial<FiltrosMarcaciones>): void {
    setFiltros((actual) => ({ ...actual, ...cambio, pagina: 1 }));
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actual) => ({ ...actual, pagina }));
  }

  async function guardarMarcacion(request: RegistrarMarcacionRequest): Promise<void> {
    try {
      await registrarMarcacion(request);
      setFormularioAbierto(false);
      data.recargar();
    } catch (error: unknown) {
      if (esConflicto(error)) {
        data.recargar();
      }
      throw error;
    }
  }

  async function importar(request: ImportarMarcacionesRequest): Promise<ResultadoImportacion> {
    const resultado = await importarMarcaciones(request);
    data.recargar();
    return resultado;
  }

  async function confirmarEliminacion(): Promise<void> {
    if (aEliminar === null) {
      return;
    }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await eliminarMarcacion(aEliminar.id);
      setDialogo('ninguno');
      setAEliminar(null);
      data.recargar();
    } catch (error: unknown) {
      setErrorEliminar(mensajeError(error, 'No se pudo eliminar la marcación.'));
      if (esConflicto(error)) {
        data.recargar();
      }
    } finally {
      setEliminando(false);
    }
  }

  const cerrarEliminacion = useCallback(() => {
    setDialogo('ninguno');
    setErrorEliminar('');
    setAEliminar(null);
  }, []);

  const cerrarImportacion = useCallback(() => {
    setDialogo('ninguno');
    window.requestAnimationFrame(() => botonImportarRef.current?.focus());
  }, []);

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const descripcion = aEliminar === null ? '' : `${aEliminar.empleadoNombre}, ${aFechaHoraLocal(aEliminar.instante)}`;

  return (
    <>
      <section aria-labelledby="marcaciones-list-title" className="clientes-list-panel asistencia-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">MARCACIONES</div>
            <h2 id="marcaciones-list-title">Marcaciones</h2>
          </div>
          <div className="asistencia-heading-actions">
            <button
              aria-describedby={puedeEscribir ? undefined : 'marcaciones-solo-rrhh'}
              className="secondary-button"
              disabled={!puedeEscribir}
              onClick={() => setDialogo('importar')}
              ref={botonImportarRef}
              type="button"
            >
              Importar archivo del reloj
            </button>
            <button
              aria-describedby={puedeEscribir ? undefined : 'marcaciones-solo-rrhh'}
              className="primary-button"
              disabled={!puedeEscribir}
              onClick={() => setFormularioAbierto(true)}
              type="button"
            >
              Registrar marcación
            </button>
          </div>
        </div>
        {!puedeEscribir && <p className="asistencia-aviso" id="marcaciones-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        <MarcacionesFiltros
          cambiarDesde={(desde) => cambiarFiltros({ desde })}
          cambiarEmpleado={(empleadoId) => cambiarFiltros({ empleadoId })}
          cambiarHasta={(hasta) => cambiarFiltros({ hasta })}
          cambiarOrigen={(origen) => cambiarFiltros({ origen })}
          cambiarTipo={(tipo) => cambiarFiltros({ tipo })}
          desde={filtros.desde}
          empleadoId={filtros.empleadoId}
          empleados={data.empleados}
          hasta={filtros.hasta}
          origen={filtros.origen}
          tipo={filtros.tipo}
        />
        <MarcacionesTabla
          cambiarPagina={cambiarPagina}
          cargando={data.lista === null}
          eliminar={(marcacion) => {
            setAEliminar(marcacion);
            setErrorEliminar('');
            setDialogo('eliminar');
          }}
          error={data.errorLista}
          items={data.lista?.items ?? []}
          pagina={filtros.pagina}
          puedeEscribir={puedeEscribir}
          totalPaginas={paginas}
        />
      </section>
      {formularioAbierto && puedeEscribir && (
        <MarcacionFormulario
          cancelar={() => setFormularioAbierto(false)}
          empleados={data.empleados}
          errorEmpleados={data.errorEmpleados}
          guardar={guardarMarcacion}
        />
      )}
      {dialogo === 'importar' && puedeEscribir && <DialogoImportacion cerrar={cerrarImportacion} importar={importar} />}
      {dialogo === 'eliminar' && aEliminar !== null && (
        <DialogoBaja
          cerrar={cerrarEliminacion}
          confirmar={() => void confirmarEliminacion()}
          eliminando={eliminando}
          entidad="marcación"
          error={errorEliminar}
          nombre={descripcion}
        />
      )}
    </>
  );
}
