import { useCallback, useState } from 'react';
import { DialogoBaja } from '../../components/DialogoBaja';
import { crearBono, eliminarBono } from './api';
import { BonoFormulario } from './BonoFormulario';
import { BonosFiltros } from './BonosFiltros';
import { BonosTabla } from './BonosTabla';
import { esConflicto, interpretarAnioFiltro, mensajeError, TEXTO_SOLO_RRHH } from './presentacion';
import type { BonoPlanilla, FiltrosBonos, GuardarBonoRequest } from './tipos';
import { useBonosData } from './useBonosData';

const TAMANO_PAGINA = 10;
const FILTROS_INICIALES: FiltrosBonos = { anio: null, mes: null, empleadoId: null, pagina: 1, tamanoPagina: TAMANO_PAGINA };

export function BonosVista({ puedeEscribir }: { puedeEscribir: boolean }) {
  const [anioTexto, setAnioTexto] = useState('');
  const [filtros, setFiltros] = useState<FiltrosBonos>(FILTROS_INICIALES);
  const [formularioAbierto, setFormularioAbierto] = useState(false);
  const [aEliminar, setAEliminar] = useState<BonoPlanilla | null>(null);
  const [errorEliminar, setErrorEliminar] = useState('');
  const [eliminando, setEliminando] = useState(false);
  const data = useBonosData(filtros);

  function cambiarFiltros(cambio: Partial<FiltrosBonos>): void {
    setFiltros((actual) => ({ ...actual, ...cambio, pagina: 1 }));
  }

  function cambiarAnio(texto: string): void {
    setAnioTexto(texto);
    const anio = interpretarAnioFiltro(texto);
    if (anio !== undefined) {
      cambiarFiltros({ anio });
    }
  }

  async function guardarBono(request: GuardarBonoRequest): Promise<void> {
    await crearBono(request);
    setFormularioAbierto(false);
    data.recargar();
  }

  async function confirmarEliminacion(): Promise<void> {
    if (aEliminar === null) {
      return;
    }
    setEliminando(true);
    setErrorEliminar('');
    try {
      await eliminarBono(aEliminar.id);
      setAEliminar(null);
      data.recargar();
    } catch (error: unknown) {
      setErrorEliminar(mensajeError(error, 'No se pudo eliminar el bono.'));
      if (esConflicto(error)) {
        data.recargar();
      }
    } finally {
      setEliminando(false);
    }
  }

  const cerrarEliminacion = useCallback(() => {
    setAEliminar(null);
    setErrorEliminar('');
  }, []);

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }

  return (
    <>
      <section aria-labelledby="bonos-list-title" className="clientes-list-panel planillas-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">BONOS</div>
            <h2 id="bonos-list-title">Bonos registrados</h2>
          </div>
          <button
            aria-describedby={puedeEscribir ? undefined : 'bonos-solo-rrhh'}
            className="primary-button"
            disabled={!puedeEscribir}
            onClick={() => setFormularioAbierto(true)}
            type="button"
          >
            Nuevo bono
          </button>
        </div>
        {!puedeEscribir && <p className="planillas-aviso" id="bonos-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        <BonosFiltros
          anioTexto={anioTexto}
          cambiarAnio={cambiarAnio}
          cambiarEmpleado={(empleadoId) => cambiarFiltros({ empleadoId })}
          cambiarMes={(mes) => cambiarFiltros({ mes })}
          empleadoId={filtros.empleadoId}
          empleados={data.empleados}
          mes={filtros.mes}
        />
        <BonosTabla
          cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))}
          cargando={data.lista === null}
          eliminar={(bono) => {
            setAEliminar(bono);
            setErrorEliminar('');
          }}
          error={data.errorLista}
          items={data.lista?.items ?? []}
          pagina={filtros.pagina}
          puedeEscribir={puedeEscribir}
          totalPaginas={paginas}
        />
      </section>
      {formularioAbierto && puedeEscribir && (
        <BonoFormulario
          cancelar={() => setFormularioAbierto(false)}
          empleados={data.empleados}
          errorEmpleados={data.errorEmpleados}
          guardar={guardarBono}
        />
      )}
      {aEliminar !== null && (
        <DialogoBaja
          cerrar={cerrarEliminacion}
          confirmar={() => void confirmarEliminacion()}
          eliminando={eliminando}
          entidad="bono"
          error={errorEliminar}
          nombre={`${aEliminar.concepto} de ${aEliminar.empleadoNombre}`}
        />
      )}
    </>
  );
}
