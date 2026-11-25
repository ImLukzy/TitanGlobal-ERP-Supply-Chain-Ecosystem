import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { PaginaEmpleados } from '../empleados/tipos';
import { mensajeError, useDialogoModal } from './presentacion';
import type { CalcularJornadasRequest, ResultadoCalculo } from './tipos';
import { construirRequestCalculo, validarCalculo, type CamposCalculo, type ErroresCalculo } from './validacionMarcacion';

interface Props {
  empleados: PaginaEmpleados | null;
  desde: string;
  hasta: string;
  cerrar: () => void;
  calcular: (request: CalcularJornadasRequest) => Promise<ResultadoCalculo>;
}

// Diálogo modal propio: calcula las jornadas de un rango y, al terminar, muestra cuántas se calcularon, conservaron y eliminaron.
export function DialogoCalculo({ empleados, desde, hasta, cerrar, calcular }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const primerCampoRef = useRef<HTMLInputElement>(null);
  const [campos, setCampos] = useState<CamposCalculo>({ desde, hasta, empleadoId: '' });
  const [errores, setErrores] = useState<ErroresCalculo>({});
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [resultado, setResultado] = useState<ResultadoCalculo | null>(null);

  useEffect(() => {
    primerCampoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  function cambiarCampo(clave: keyof CamposCalculo, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    setMensaje('');
    const nuevosErrores = validarCalculo(campos);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setEnviando(true);
    try {
      setResultado(await calcular(construirRequestCalculo(campos)));
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudieron calcular las jornadas.'));
    } finally {
      setEnviando(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="calculo-dialog-title"
        aria-modal="true"
        className="clientes-dialog asistencia-dialog"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">CÁLCULO</div>
        <h2 id="calculo-dialog-title">Calcular jornadas</h2>
        {resultado === null && (
          <>
            <p>Regenera las jornadas del rango. Las que ya tienen las horas extra aprobadas o rechazadas se conservan.</p>
            <form className="asistencia-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
              <CampoConError etiqueta="Desde" htmlFor="calculo-desde" error={errores.desde} idError="calculo-desde-error">
                <input
                  aria-describedby={errores.desde ? 'calculo-desde-error' : undefined}
                  aria-invalid={Boolean(errores.desde)}
                  id="calculo-desde"
                  onChange={(event) => cambiarCampo('desde', event.currentTarget.value)}
                  ref={primerCampoRef}
                  type="date"
                  value={campos.desde}
                />
              </CampoConError>
              <CampoConError etiqueta="Hasta" htmlFor="calculo-hasta" error={errores.hasta} idError="calculo-hasta-error">
                <input
                  aria-describedby={errores.hasta ? 'calculo-hasta-error' : undefined}
                  aria-invalid={Boolean(errores.hasta)}
                  id="calculo-hasta"
                  onChange={(event) => cambiarCampo('hasta', event.currentTarget.value)}
                  type="date"
                  value={campos.hasta}
                />
              </CampoConError>
              <CampoConError etiqueta="Empleado (opcional)" htmlFor="calculo-empleado" idError="calculo-empleado-error">
                <select
                  id="calculo-empleado"
                  onChange={(event) => cambiarCampo('empleadoId', event.currentTarget.value)}
                  value={campos.empleadoId}
                >
                  <option value="">Todos los empleados</option>
                  {(empleados?.items ?? []).map((empleado) => (
                    <option key={empleado.id} value={String(empleado.id)}>
                      {`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}
                    </option>
                  ))}
                </select>
              </CampoConError>
              {mensaje !== '' && (
                <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
                  <span aria-hidden="true">!</span>
                  {mensaje}
                </p>
              )}
              <div className="clientes-dialog-actions">
                <button className="secondary-button" disabled={enviando} onClick={cerrar} type="button">
                  Cancelar
                </button>
                <button className="primary-button" disabled={enviando} type="submit">
                  {enviando ? 'Calculando…' : 'Calcular'}
                </button>
              </div>
            </form>
          </>
        )}
        {resultado !== null && (
          <>
            <dl aria-label="Resultado del cálculo" className="asistencia-resultado">
              <div>
                <dt>Calculadas</dt>
                <dd>{resultado.calculadas}</dd>
              </div>
              <div>
                <dt>Conservadas</dt>
                <dd>{resultado.conservadas}</dd>
              </div>
              <div>
                <dt>Eliminadas</dt>
                <dd>{resultado.eliminadas}</dd>
              </div>
            </dl>
            <div className="clientes-dialog-actions">
              <button className="primary-button" onClick={cerrar} type="button">Cerrar</button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
