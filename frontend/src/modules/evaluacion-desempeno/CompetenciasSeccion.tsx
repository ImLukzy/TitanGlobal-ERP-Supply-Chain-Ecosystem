import { CompetenciaFormulario } from './CompetenciaFormulario';
import type { CicloDetalle, Competencia, GuardarCompetenciaRequest } from './tipos';
import { formatearDecimal } from './validacionEvaluacion';

// Formulario abierto: ninguno, uno nuevo o el de una competencia existente.
export type FormularioCompetencia = null | 'nueva' | number;

interface Props {
  ciclo: CicloDetalle;
  puedeEscribir: boolean;
  formulario: FormularioCompetencia;
  cambiarFormulario: (formulario: FormularioCompetencia) => void;
  guardar: (request: GuardarCompetenciaRequest) => Promise<void>;
  pedirEliminar: (competencia: Competencia) => void;
}

export function CompetenciasSeccion({ ciclo, puedeEscribir, formulario, cambiarFormulario, guardar, pedirEliminar }: Props) {
  const editable = ciclo.estado === 'Configuracion';
  const suma = formatearDecimal(ciclo.sumaPesos);
  const completa = Math.round(ciclo.sumaPesos * 100) === 10000;
  const competenciaEnEdicion = typeof formulario === 'number' ? ciclo.competencias.find((item) => item.id === formulario) ?? null : null;
  return (
    <section aria-labelledby="ciclo-competencias-title" className="evaluacion-desempeno-section">
      <div className="clientes-panel-heading">
        <h3 id="ciclo-competencias-title">Competencias</h3>
        {editable && (
          <button className="secondary-button" disabled={!puedeEscribir} onClick={() => cambiarFormulario('nueva')} type="button">
            Agregar competencia
          </button>
        )}
      </div>
      <p aria-live="polite" className={completa ? 'evaluacion-desempeno-suma is-completa' : 'evaluacion-desempeno-suma'} role="status">
        {`Suma de pesos: ${suma} de 100`}
      </p>
      {ciclo.competencias.length === 0 && <p className="clientes-status">Este ciclo todavía no tiene competencias.</p>}
      {ciclo.competencias.length > 0 && (
        <div className="evaluacion-desempeno-scroll">
          <table className="evaluacion-desempeno-mini-tabla">
            <caption>Competencias del ciclo</caption>
            <thead>
              <tr>
                <th scope="col">Competencia</th>
                <th scope="col">Peso</th>
                {editable && <th scope="col">Acciones</th>}
              </tr>
            </thead>
            <tbody>
              {ciclo.competencias.map((competencia) => (
                <tr key={competencia.id}>
                  <td>
                    <strong>{competencia.nombre}</strong>
                    {competencia.descripcion !== null && competencia.descripcion !== '' && <span className="evaluacion-desempeno-descripcion">{competencia.descripcion}</span>}
                  </td>
                  <td className="evaluacion-desempeno-numero">{`${formatearDecimal(competencia.peso)} %`}</td>
                  {editable && (
                    <td>
                      <div className="evaluacion-desempeno-fila-acciones">
                        <button aria-label={`Editar competencia ${competencia.nombre}`} className="secondary-button evaluacion-desempeno-fila-boton" disabled={!puedeEscribir} onClick={() => cambiarFormulario(competencia.id)} type="button">Editar</button>
                        <button aria-label={`Quitar competencia ${competencia.nombre}`} className="secondary-button evaluacion-desempeno-fila-boton" disabled={!puedeEscribir} onClick={() => pedirEliminar(competencia)} type="button">Quitar</button>
                      </div>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {editable && puedeEscribir && formulario === 'nueva' && (
        <CompetenciaFormulario cancelar={() => cambiarFormulario(null)} competencia={null} guardar={guardar} key="nueva" />
      )}
      {editable && puedeEscribir && competenciaEnEdicion !== null && (
        <CompetenciaFormulario cancelar={() => cambiarFormulario(null)} competencia={competenciaEnEdicion} guardar={guardar} key={competenciaEnEdicion.id} />
      )}
    </section>
  );
}
