import { useState } from 'react';
import { CertificacionCelda } from './InscritosTabla';
import { mensajeError } from './presentacion';
import type { Inscripcion, RegistrarResultadosRequest } from './tipos';
import { interpretarNota } from './validacionCapacitacion';

type Decision = '' | 'Aprobado' | 'Desaprobado';

interface Fila {
  decision: Decision;
  nota: string;
}

interface Props {
  inscripciones: Inscripcion[];
  guardar: (request: RegistrarResultadosRequest) => Promise<void>;
  emitir: (inscripcion: Inscripcion) => void;
  guardado: boolean;
}

function aDecision(valor: string): Decision {
  if (valor === 'Aprobado' || valor === 'Desaprobado') {
    return valor;
  }
  return '';
}

const MENSAJE_NOTA = 'La nota debe estar entre 0 y 20, con un decimal.';

function filaInicial(inscripcion: Inscripcion): Fila {
  const decision: Decision = inscripcion.estado === 'Aprobado' || inscripcion.estado === 'Desaprobado' ? inscripcion.estado : '';
  return { decision, nota: inscripcion.nota === null ? '' : String(inscripcion.nota) };
}

// Resultados de una programación realizada: una fila por inscrito y un único «Guardar resultados».
// Las inscripciones con certificación ya no se pueden corregir (mensaje 26).
export function ResultadosEditor({ inscripciones, guardar, emitir, guardado }: Props) {
  const evaluables = inscripciones.filter((inscripcion) => inscripcion.estado !== 'Retirado');
  const [filas, setFilas] = useState<Record<number, Fila>>(() => Object.fromEntries(evaluables.map((inscripcion) => [inscripcion.id, filaInicial(inscripcion)])));
  const [errores, setErrores] = useState<Record<number, string>>({});
  const [mensaje, setMensaje] = useState('');
  const [editado, setEditado] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const editables = evaluables.filter((inscripcion) => inscripcion.certificacion === null);
  const hayDecisiones = editables.some((inscripcion) => (filas[inscripcion.id]?.decision ?? '') !== '');

  function cambiar(id: number, cambios: Partial<Fila>): void {
    setEditado(true);
    setFilas((actual) => ({ ...actual, [id]: { ...(actual[id] ?? { decision: '', nota: '' }), ...cambios } }));
  }

  async function enviar(): Promise<void> {
    setMensaje('');
    const nuevosErrores: Record<number, string> = {};
    const resultados: RegistrarResultadosRequest['resultados'] = [];
    for (const inscripcion of editables) {
      const fila = filas[inscripcion.id];
      if (fila === undefined || fila.decision === '') {
        continue;
      }
      const texto = fila.nota.trim();
      const nota = texto === '' ? null : interpretarNota(texto);
      if (texto !== '' && nota === null) {
        nuevosErrores[inscripcion.id] = MENSAJE_NOTA;
      }
      resultados.push({ inscripcionId: inscripcion.id, aprobado: fila.decision === 'Aprobado', nota });
    }
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setEnviando(true);
    try {
      await guardar({ resultados });
      setEditado(false);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudieron guardar los resultados.'));
    } finally {
      setEnviando(false);
    }
  }

  if (evaluables.length === 0) {
    return <p className="capacitaciones-aviso">La programación no tiene inscritos para evaluar.</p>;
  }
  return (
    <div>
      <div className="capacitaciones-scroll">
        <table className="capacitaciones-mini-tabla">
          <caption>Resultados de los inscritos</caption>
          <thead>
            <tr>
              <th scope="col">Empleado</th>
              <th scope="col">Resultado</th>
              <th scope="col">Nota</th>
              <th scope="col">Certificación</th>
            </tr>
          </thead>
          <tbody>
            {evaluables.map((inscripcion) => {
              const fila = filas[inscripcion.id] ?? filaInicial(inscripcion);
              const bloqueada = inscripcion.certificacion !== null;
              return (
                <tr key={inscripcion.id}>
                  <td>{`${inscripcion.empleadoLegajo} · ${inscripcion.empleadoNombre}`}</td>
                  <td>
                    <select aria-label={`Resultado de ${inscripcion.empleadoNombre}`} className="capacitaciones-celda-control" disabled={bloqueada || enviando} onChange={(event) => cambiar(inscripcion.id, { decision: aDecision(event.currentTarget.value) })} value={fila.decision}>
                      <option value="">Sin resultado</option>
                      <option value="Aprobado">Aprobado</option>
                      <option value="Desaprobado">Desaprobado</option>
                    </select>
                  </td>
                  <td>
                    <input aria-describedby={errores[inscripcion.id] ? `nota-error-${inscripcion.id}` : undefined} aria-invalid={Boolean(errores[inscripcion.id])} aria-label={`Nota de ${inscripcion.empleadoNombre}`} autoComplete="off" className="capacitaciones-celda-control capacitaciones-nota" disabled={bloqueada || enviando} inputMode="decimal" onChange={(event) => cambiar(inscripcion.id, { nota: event.currentTarget.value })} type="text" value={fila.nota} />
                    {errores[inscripcion.id] && <span className="field-error" id={`nota-error-${inscripcion.id}`}>{errores[inscripcion.id]}</span>}
                  </td>
                  <td>
                    <CertificacionCelda certificacion={inscripcion.certificacion} />
                    {inscripcion.estado === 'Aprobado' && !bloqueada && (
                      <button aria-label={`Emitir certificado a ${inscripcion.empleadoNombre}`} className="secondary-button capacitaciones-fila-boton" onClick={() => emitir(inscripcion)} type="button">Emitir certificado</button>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {mensaje !== '' && <p aria-live="polite" className="clientes-error" role="alert">{mensaje}</p>}
      {guardado && !editado && <p aria-live="polite" className="capacitaciones-ok" role="status">Resultados guardados.</p>}
      <div className="clientes-detail-actions capacitaciones-actions">
        <button className="primary-button" disabled={!hayDecisiones || enviando} onClick={() => void enviar()} type="button">{enviando ? 'Guardando…' : 'Guardar resultados'}</button>
      </div>
    </div>
  );
}
