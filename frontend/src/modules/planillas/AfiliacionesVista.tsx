import { useState } from 'react';
import { guardarAfiliacion } from './api';
import { AfiliacionFormulario } from './AfiliacionFormulario';
import { AfiliacionesTabla, type FilaAfiliacion } from './AfiliacionesTabla';
import { TEXTO_SOLO_RRHH } from './presentacion';
import type { FiltrosAfiliaciones, GuardarAfiliacionRequest } from './tipos';
import { useAfiliacionesData } from './useAfiliacionesData';

const TAMANO_PAGINA = 10;
// Una sola petición con todas las afiliaciones (hasta 100, como el selector de empleados): se cruzan con los empleados para marcar los que no tienen.
const FILTROS_AFILIACIONES: FiltrosAfiliaciones = { empleadoId: null, pagina: 1, tamanoPagina: 100 };

interface Edicion {
  clave: number;
  empleadoId: number | null;
  fila: FilaAfiliacion | null;
}

export function AfiliacionesVista({ puedeEscribir }: { puedeEscribir: boolean }) {
  const [empleadoId, setEmpleadoId] = useState<number | null>(null);
  const [pagina, setPagina] = useState(1);
  const [edicion, setEdicion] = useState<Edicion | null>(null);
  const data = useAfiliacionesData(FILTROS_AFILIACIONES);

  const filas: FilaAfiliacion[] = (data.empleados?.items ?? [])
    .filter((empleado) => empleadoId === null || empleado.id === empleadoId)
    .map((empleado) => ({
      empleado,
      afiliacion: data.lista?.items.find((afiliacion) => afiliacion.empleadoId === empleado.id) ?? null,
    }));
  const paginas = Math.max(1, Math.ceil(filas.length / TAMANO_PAGINA));
  const paginaActual = Math.min(pagina, paginas);
  const visibles = filas.slice((paginaActual - 1) * TAMANO_PAGINA, paginaActual * TAMANO_PAGINA);
  const cargando = data.lista === null || data.empleados === null;
  const sinAfiliacion = filas.filter((fila) => fila.afiliacion === null).length;

  async function guardar(idEmpleado: number, request: GuardarAfiliacionRequest): Promise<void> {
    await guardarAfiliacion(idEmpleado, request);
    setEdicion(null);
    data.recargar();
  }

  return (
    <>
      <section aria-labelledby="afiliaciones-list-title" className="clientes-list-panel planillas-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">AFILIACIONES</div>
            <h2 id="afiliaciones-list-title">Sistema de pensiones por empleado</h2>
          </div>
          <button
            aria-describedby={puedeEscribir ? undefined : 'afiliaciones-solo-rrhh'}
            className="primary-button"
            disabled={!puedeEscribir}
            onClick={() => setEdicion({ clave: -1, empleadoId: null, fila: null })}
            type="button"
          >
            Nueva afiliación
          </button>
        </div>
        {!puedeEscribir && <p className="planillas-aviso" id="afiliaciones-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        <div className="solicitudes-filters planillas-filters">
          <div>
            <label htmlFor="afiliaciones-empleado">Empleado</label>
            <select
              id="afiliaciones-empleado"
              onChange={(event) => {
                setEmpleadoId(event.currentTarget.value === '' ? null : Number(event.currentTarget.value));
                setPagina(1);
              }}
              value={empleadoId === null ? '' : String(empleadoId)}
            >
              <option value="">Todos</option>
              {(data.empleados?.items ?? []).map((empleado) => (
                <option key={empleado.id} value={String(empleado.id)}>
                  {`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}
                </option>
              ))}
            </select>
          </div>
        </div>
        {!cargando && sinAfiliacion > 0 && (
          <p className="planillas-aviso">{`${sinAfiliacion} ${sinAfiliacion === 1 ? 'empleado no tiene' : 'empleados no tienen'} sistema de pensiones y se excluyen del cálculo de planilla.`}</p>
        )}
        <AfiliacionesTabla
          cambiarPagina={setPagina}
          cargando={cargando && data.errorLista === '' && data.errorEmpleados === ''}
          editar={(fila) => setEdicion({ clave: fila.empleado.id, empleadoId: fila.empleado.id, fila })}
          error={data.errorLista !== '' ? data.errorLista : data.errorEmpleados}
          items={visibles}
          pagina={paginaActual}
          puedeEscribir={puedeEscribir}
          totalPaginas={paginas}
        />
      </section>
      {edicion !== null && puedeEscribir && (
        <AfiliacionFormulario
          afiliacion={edicion.fila?.afiliacion ?? null}
          cancelar={() => setEdicion(null)}
          empleadoId={edicion.empleadoId}
          empleados={data.empleados}
          errorEmpleados={data.errorEmpleados}
          errorParametros={data.errorParametros}
          guardar={guardar}
          key={edicion.clave}
          parametros={data.parametros}
        />
      )}
    </>
  );
}
