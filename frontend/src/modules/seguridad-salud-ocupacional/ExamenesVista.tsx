import { useState } from 'react';
import { registrarExamen } from './api';
import { ExamenesTabla } from './ExamenesTabla';
import { ExamenFormulario } from './ExamenFormulario';
import { aNumeroOnulo, aResultado, aTipoExamen, RESULTADOS_EXAMEN, TAMANO_PAGINA, TEXTO_SOLO_RRHH, textoValor, TIPOS_EXAMEN } from './presentacion';
import type { FiltrosExamenes, RegistrarExamenRequest } from './tipos';
import { useExamenesData } from './useExamenesData';
import { useEmpleados } from './useIncidentesData';

interface Props {
  puedeEscribir: boolean;
  alCambiar: () => void;
  parametros: URLSearchParams;
}

function filtrosIniciales(parametros: URLSearchParams): FiltrosExamenes {
  return {
    empleadoId: null,
    tipo: aTipoExamen(parametros.get('tipo') ?? ''),
    resultado: aResultado(parametros.get('resultado') ?? ''),
    vencidos: parametros.get('vencidos') === 'true',
    pagina: 1,
    tamanoPagina: TAMANO_PAGINA,
  };
}

export function ExamenesVista({ puedeEscribir, alCambiar, parametros }: Props) {
  const [filtros, setFiltros] = useState<FiltrosExamenes>(() => filtrosIniciales(parametros));
  const [formulario, setFormulario] = useState(false);
  const data = useExamenesData(filtros);
  const empleados = useEmpleados();

  async function registrar(request: RegistrarExamenRequest): Promise<void> {
    await registrarExamen(request);
    setFormulario(false);
    data.recargar();
    alCambiar();
  }

  function cambiar(cambios: Partial<FiltrosExamenes>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: 1 }));
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }

  return (
    <>
      <section aria-labelledby="examenes-list-title" className="clientes-list-panel sst-list-panel">
        <div className="clientes-panel-heading">
          <div>
            <div className="page-eyebrow">EXÁMENES MÉDICOS</div>
            <h2 id="examenes-list-title">Exámenes médicos</h2>
          </div>
          <button aria-describedby={puedeEscribir ? undefined : 'examenes-solo-rrhh'} className="primary-button" disabled={!puedeEscribir} onClick={() => setFormulario(true)} type="button">Nuevo examen</button>
        </div>
        {!puedeEscribir && <p className="sst-aviso" id="examenes-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
        <div className="solicitudes-filters sst-filters sst-filters-cuatro">
          <div>
            <label htmlFor="examenes-empleado">Empleado</label>
            <select id="examenes-empleado" onChange={(event) => cambiar({ empleadoId: aNumeroOnulo(event.currentTarget.value) })} value={filtros.empleadoId === null ? '' : String(filtros.empleadoId)}>
              <option value="">Todos</option>
              {(empleados.empleados ?? []).map((empleado) => <option key={empleado.id} value={String(empleado.id)}>{`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}</option>)}
            </select>
          </div>
          <div>
            <label htmlFor="examenes-tipo">Tipo</label>
            <select id="examenes-tipo" onChange={(event) => cambiar({ tipo: aTipoExamen(event.currentTarget.value) })} value={filtros.tipo}>
              <option value="">Todos</option>
              {TIPOS_EXAMEN.map((tipo) => <option key={tipo} value={tipo}>{textoValor(tipo)}</option>)}
            </select>
          </div>
          <div>
            <label htmlFor="examenes-resultado">Resultado</label>
            <select id="examenes-resultado" onChange={(event) => cambiar({ resultado: aResultado(event.currentTarget.value) })} value={filtros.resultado}>
              <option value="">Todos</option>
              {RESULTADOS_EXAMEN.map((resultado) => <option key={resultado} value={resultado}>{textoValor(resultado)}</option>)}
            </select>
          </div>
          <label className="sst-check" htmlFor="examenes-vencidos">
            <input checked={filtros.vencidos} id="examenes-vencidos" onChange={(event) => cambiar({ vencidos: event.currentTarget.checked })} type="checkbox" />
            Solo vencidos
          </label>
        </div>
        {empleados.errorEmpleados !== '' && <p className="clientes-error" role="alert">{empleados.errorEmpleados}</p>}
        <ExamenesTabla
          cambiarPagina={(pagina) => setFiltros((actual) => ({ ...actual, pagina }))}
          cargando={data.lista === null}
          error={data.errorLista}
          items={data.lista?.items ?? []}
          pagina={filtros.pagina}
          totalPaginas={paginas}
        />
      </section>
      {formulario && puedeEscribir && <ExamenFormulario cancelar={() => setFormulario(false)} empleados={empleados.empleados ?? []} guardar={registrar} />}
    </>
  );
}
