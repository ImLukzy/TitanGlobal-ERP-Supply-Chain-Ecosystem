import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { formateadorMoneda } from '../../lib/formato';
import type { PaginaEmpleados } from '../empleados/tipos';
import {
  aCentavos,
  centavosAImporte,
  evaluarTope,
  generarCronograma,
  interpretarMonto,
  MAXIMO_CUOTAS,
  textoMes,
  type CuotaEvaluada,
} from './cronograma';
import { aTipo, esFechaIso, hoyUtc, mensajeError, textoTipo, TIPOS } from './presentacion';
import type { CapacidadPrestamo, RegistrarPrestamoRequest } from './tipos';
import { MAXIMO_EMPLEADOS_SELECTOR, useCapacidadPrestamo } from './usePrestamosData';
import {
  construirRequest,
  cuotasEfectivas,
  interpretarMes,
  LIMITE_MOTIVO,
  validarPrestamo,
  type CamposPrestamo,
  type ErroresPrestamo,
} from './validacionPrestamo';

interface Props {
  empleados: PaginaEmpleados | null;
  errorEmpleados: string;
  revision: number;
  cancelar: () => void;
  guardar: (request: RegistrarPrestamoRequest) => Promise<void>;
}

function camposIniciales(): CamposPrestamo {
  return {
    empleadoId: '',
    tipo: 'Prestamo',
    monto: '',
    numeroCuotas: '1',
    fechaOtorgamiento: hoyUtc(),
    primerMes: '',
    motivo: '',
  };
}

function importe(centavos: number): string {
  return formateadorMoneda.format(centavosAImporte(centavos));
}

// Cronograma previsto con la misma regla del backend; null mientras los datos no permitan calcularlo.
function calcularVistaPrevia(campos: CamposPrestamo): ReturnType<typeof generarCronograma> | null {
  const monto = interpretarMonto(campos.monto);
  const cuotas = cuotasEfectivas(campos);
  const mes = interpretarMes(campos.primerMes);
  if (monto.estado !== 'valido' || cuotas === null || cuotas < 1 || cuotas > MAXIMO_CUOTAS || mes === null) {
    return null;
  }
  return generarCronograma(monto.centavos, cuotas, mes.anio, mes.mes);
}

function AvisoTope({ superadas, cuotaMaximaCentavos }: { superadas: CuotaEvaluada[]; cuotaMaximaCentavos: number }) {
  const primera = superadas[0];
  if (primera === undefined) {
    return <p className="adelantos-prestamos-ok">Las cuotas previstas respetan el máximo mensual.</p>;
  }
  return (
    <p className="adelantos-prestamos-alerta" role="alert">
      {`Aviso: la cuota de ${textoMes(primera.anio, primera.mes)} sumaría ${importe(primera.totalCentavos)} y supera el máximo de ${importe(cuotaMaximaCentavos)} (30 % del neto estimado). El servidor decide al registrar.`}
    </p>
  );
}

function PanelCapacidad({ capacidad, cuotas }: { capacidad: CapacidadPrestamo; cuotas: CuotaEvaluada[] | null }) {
  const cuotaMaximaCentavos = aCentavos(capacidad.cuotaMaxima);
  const superadas = cuotas === null ? [] : cuotas.filter((cuota) => cuota.superaMaximo);
  return (
    <div className="adelantos-prestamos-capacidad">
      <dl className="adelantos-prestamos-capacidad-grid">
        <div>
          <dt>Sueldo vigente</dt>
          <dd>{formateadorMoneda.format(capacidad.sueldoVigente)}</dd>
        </div>
        <div>
          <dt>Neto estimado</dt>
          <dd>{formateadorMoneda.format(capacidad.netoEstimado)}</dd>
        </div>
        <div>
          <dt>Cuota máxima mensual</dt>
          <dd>{formateadorMoneda.format(capacidad.cuotaMaxima)}</dd>
        </div>
      </dl>
      {capacidad.sueldoVigente === 0 && (
        <p className="adelantos-prestamos-alerta">El empleado no tiene un contrato vigente en la fecha de otorgamiento: no hay capacidad de descuento.</p>
      )}
      {capacidad.sueldoVigente > 0 && cuotas !== null && <AvisoTope cuotaMaximaCentavos={cuotaMaximaCentavos} superadas={superadas} />}
    </div>
  );
}

export function PrestamoFormulario({ empleados, errorEmpleados, revision, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposPrestamo>(camposIniciales);
  const [errores, setErrores] = useState<ErroresPrestamo>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const esAdelanto = aTipo(campos.tipo) === 'Adelanto';
  const empleadoId = campos.empleadoId === '' ? null : Number(campos.empleadoId);
  const fecha = esFechaIso(campos.fechaOtorgamiento) ? campos.fechaOtorgamiento : '';
  const capacidad = useCapacidadPrestamo(empleadoId, fecha, revision);
  const vistaPrevia = calcularVistaPrevia(campos);
  const cuotasEvaluadas = vistaPrevia !== null && capacidad.capacidad !== null
    ? evaluarTope(vistaPrevia, capacidad.capacidad.cuotasComprometidasPorMes, aCentavos(capacidad.capacidad.cuotaMaxima))
    : null;
  const hayMas = empleados !== null && empleados.total > MAXIMO_EMPLEADOS_SELECTOR;

  function cambiarCampo(clave: keyof CamposPrestamo, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarPrestamo(campos);
    setErrores(nuevosErrores);
    const request = construirRequest(campos);
    if (Object.keys(nuevosErrores).length > 0 || request === null) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      // Un rechazo del servidor (409 por contrato, cese o tope, por ejemplo) se muestra junto al formulario y conserva lo escrito.
      setMensaje(mensajeError(error, 'No se pudo registrar el préstamo.'));
    } finally {
      setGuardando(false);
    }
  }

  const descripcionMotivo = errores.motivo
    ? 'prestamo-motivo-contador prestamo-motivo-error'
    : 'prestamo-motivo-contador';

  return (
    <section aria-labelledby="prestamo-form-title" className="clientes-form-panel adelantos-prestamos-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">PRÉSTAMOS</div>
          <h2 id="prestamo-form-title">Nuevo préstamo</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="adelantos-prestamos-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Empleado" htmlFor="prestamo-empleado" error={errores.empleadoId} idError="prestamo-empleado-error">
          <select
            aria-describedby={errores.empleadoId ? 'prestamo-empleado-error' : undefined}
            aria-invalid={Boolean(errores.empleadoId)}
            id="prestamo-empleado"
            onChange={(event) => cambiarCampo('empleadoId', event.currentTarget.value)}
            value={campos.empleadoId}
          >
            <option value="">{empleados === null && errorEmpleados === '' ? 'Cargando empleados…' : 'Selecciona un empleado'}</option>
            {(empleados?.items ?? []).map((empleado) => (
              <option key={empleado.id} value={String(empleado.id)}>
                {`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}
              </option>
            ))}
          </select>
        </CampoConError>
        {errorEmpleados !== '' && (
          <p className="clientes-error adelantos-prestamos-form-wide" role="alert">{errorEmpleados}</p>
        )}
        {hayMas && (
          <p className="adelantos-prestamos-aviso adelantos-prestamos-form-wide">Se muestran los primeros 100 empleados activos.</p>
        )}
        <CampoConError etiqueta="Tipo" htmlFor="prestamo-tipo" error={errores.tipo} idError="prestamo-tipo-error">
          <select
            aria-describedby={errores.tipo ? 'prestamo-tipo-error' : undefined}
            aria-invalid={Boolean(errores.tipo)}
            id="prestamo-tipo"
            onChange={(event) => cambiarCampo('tipo', event.currentTarget.value)}
            value={campos.tipo}
          >
            {TIPOS.map((item) => <option key={item} value={item}>{textoTipo(item)}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Monto" htmlFor="prestamo-monto" error={errores.monto} idError="prestamo-monto-error">
          <input
            aria-describedby={errores.monto ? 'prestamo-monto-error' : undefined}
            aria-invalid={Boolean(errores.monto)}
            autoComplete="off"
            id="prestamo-monto"
            inputMode="decimal"
            onChange={(event) => cambiarCampo('monto', event.currentTarget.value)}
            type="text"
            value={campos.monto}
          />
        </CampoConError>
        <CampoConError etiqueta="Número de cuotas" htmlFor="prestamo-cuotas" error={errores.numeroCuotas} idError="prestamo-cuotas-error">
          <input
            aria-describedby={errores.numeroCuotas ? 'prestamo-cuotas-error' : undefined}
            aria-invalid={Boolean(errores.numeroCuotas)}
            disabled={esAdelanto}
            id="prestamo-cuotas"
            inputMode="numeric"
            onChange={(event) => cambiarCampo('numeroCuotas', event.currentTarget.value)}
            type="text"
            value={esAdelanto ? '1' : campos.numeroCuotas}
          />
        </CampoConError>
        <CampoConError etiqueta="Fecha de otorgamiento" htmlFor="prestamo-fecha" error={errores.fechaOtorgamiento} idError="prestamo-fecha-error">
          <input
            aria-describedby={errores.fechaOtorgamiento ? 'prestamo-fecha-error' : undefined}
            aria-invalid={Boolean(errores.fechaOtorgamiento)}
            id="prestamo-fecha"
            onChange={(event) => cambiarCampo('fechaOtorgamiento', event.currentTarget.value)}
            type="date"
            value={campos.fechaOtorgamiento}
          />
        </CampoConError>
        <CampoConError etiqueta="Primer mes de descuento" htmlFor="prestamo-primer-mes" error={errores.primerMes} idError="prestamo-primer-mes-error">
          <input
            aria-describedby={errores.primerMes ? 'prestamo-primer-mes-error' : undefined}
            aria-invalid={Boolean(errores.primerMes)}
            id="prestamo-primer-mes"
            onChange={(event) => cambiarCampo('primerMes', event.currentTarget.value)}
            type="month"
            value={campos.primerMes}
          />
        </CampoConError>
        {empleadoId !== null && (
          <section aria-label="Capacidad de descuento" className="adelantos-prestamos-form-wide adelantos-prestamos-capacidad-panel">
            <h3>Capacidad de descuento</h3>
            {capacidad.cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando capacidad de descuento…</p>}
            {capacidad.error !== '' && <p className="clientes-error" role="alert">{capacidad.error}</p>}
            {capacidad.capacidad !== null && <PanelCapacidad capacidad={capacidad.capacidad} cuotas={cuotasEvaluadas} />}
          </section>
        )}
        <section aria-label="Cronograma previsto" className="adelantos-prestamos-form-wide adelantos-prestamos-capacidad-panel">
          <h3>Cronograma previsto</h3>
          {vistaPrevia === null && (
            <p aria-live="polite" className="adelantos-prestamos-aviso" role="status">Indica monto, cuotas y primer mes para ver las cuotas.</p>
          )}
          {vistaPrevia !== null && (
            <div className="adelantos-prestamos-cuotas-scroll" role="region" aria-label="Tabla del cronograma previsto" tabIndex={0}>
              <table className="adelantos-prestamos-cuotas">
                <thead>
                  <tr>
                    <th scope="col">N.º</th>
                    <th scope="col">Mes</th>
                    <th scope="col">Cuota</th>
                    {cuotasEvaluadas !== null && <th scope="col">Ya comprometido</th>}
                    {cuotasEvaluadas !== null && <th scope="col">Total del mes</th>}
                    {cuotasEvaluadas !== null && <th scope="col">Control</th>}
                  </tr>
                </thead>
                <tbody>
                  {vistaPrevia.map((cuota, indice) => {
                    const evaluada = cuotasEvaluadas?.[indice];
                    return (
                      <tr key={cuota.numeroCuota}>
                        <td className="adelantos-prestamos-numero">{cuota.numeroCuota}</td>
                        <td className="adelantos-prestamos-numero">{textoMes(cuota.anio, cuota.mes)}</td>
                        <td className="adelantos-prestamos-numero">{importe(cuota.montoCentavos)}</td>
                        {evaluada !== undefined && (
                          <>
                            <td className="adelantos-prestamos-numero">{importe(evaluada.comprometidoCentavos)}</td>
                            <td className="adelantos-prestamos-numero">{importe(evaluada.totalCentavos)}</td>
                            <td>{evaluada.superaMaximo ? 'Supera el máximo' : 'Dentro del máximo'}</td>
                          </>
                        )}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>
        <div className="adelantos-prestamos-form-wide">
          <CampoConError etiqueta="Motivo" htmlFor="prestamo-motivo" error={errores.motivo} idError="prestamo-motivo-error">
            <textarea
              aria-describedby={descripcionMotivo}
              aria-invalid={Boolean(errores.motivo)}
              id="prestamo-motivo"
              onChange={(event) => cambiarCampo('motivo', event.currentTarget.value)}
              rows={3}
              value={campos.motivo}
            />
          </CampoConError>
          <p className="adelantos-prestamos-contador" id="prestamo-motivo-contador">{`${campos.motivo.length}/${LIMITE_MOTIVO}`}</p>
        </div>
        <div className="adelantos-prestamos-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Registrar préstamo'}
          </button>
        </div>
      </form>
    </section>
  );
}
