import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { PaginaEmpleados } from '../empleados/tipos';
import { calcularDias, esFechaIso, mensajeError, textoTipo, TIPOS, TIPOS_CON_MOTIVO_OBLIGATORIO, aTipo } from './presentacion';
import { SaldoVacacional } from './SaldoVacacional';
import type { RegistrarAusenciaRequest } from './tipos';
import { MAXIMO_EMPLEADOS_SELECTOR, useSaldoVacacional } from './useAusenciasData';
import { construirRequest, LIMITE_MOTIVO, validarAusencia, type CamposAusencia, type ErroresAusencia } from './validacionAusencia';

interface Props {
  empleados: PaginaEmpleados | null;
  errorEmpleados: string;
  revision: number;
  cancelar: () => void;
  guardar: (request: RegistrarAusenciaRequest) => Promise<void>;
}

const CAMPOS_INICIALES: CamposAusencia = {
  empleadoId: '',
  tipo: 'Vacaciones',
  fechaInicio: '',
  fechaFin: '',
  motivo: '',
};

export function AusenciaFormulario({ empleados, errorEmpleados, revision, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposAusencia>(CAMPOS_INICIALES);
  const [errores, setErrores] = useState<ErroresAusencia>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const tipo = aTipo(campos.tipo);
  const esVacaciones = tipo === 'Vacaciones';
  const dias = calcularDias(campos.fechaInicio, campos.fechaFin);
  const motivoObligatorio = tipo !== null && TIPOS_CON_MOTIVO_OBLIGATORIO.includes(tipo);
  // El saldo se pide a la fecha de inicio cuando es válida; sin ella la API usa hoy.
  const saldo = useSaldoVacacional(
    esVacaciones && campos.empleadoId !== '' ? Number(campos.empleadoId) : null,
    esFechaIso(campos.fechaInicio) ? campos.fechaInicio : '',
    revision,
  );

  function cambiarCampo(clave: keyof CamposAusencia, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarAusencia(campos);
    setErrores(nuevosErrores);
    const request = construirRequest(campos);
    if (Object.keys(nuevosErrores).length > 0 || request === null) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      // Un rechazo del servidor (409 por solape o saldo, por ejemplo) se muestra junto al formulario y conserva lo escrito.
      setMensaje(mensajeError(error, 'No se pudo registrar la ausencia.'));
    } finally {
      setGuardando(false);
    }
  }

  const descripcionMotivo = errores.motivo
    ? 'ausencia-motivo-ayuda ausencia-motivo-contador ausencia-motivo-error'
    : 'ausencia-motivo-ayuda ausencia-motivo-contador';
  const hayMas = empleados !== null && empleados.total > MAXIMO_EMPLEADOS_SELECTOR;

  return (
    <section aria-labelledby="ausencia-form-title" className="clientes-form-panel vacaciones-permisos-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">AUSENCIAS</div>
          <h2 id="ausencia-form-title">Nueva ausencia</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="vacaciones-permisos-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Empleado" htmlFor="ausencia-empleado" error={errores.empleadoId} idError="ausencia-empleado-error">
          <select
            aria-describedby={errores.empleadoId ? 'ausencia-empleado-error' : undefined}
            aria-invalid={Boolean(errores.empleadoId)}
            id="ausencia-empleado"
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
          <p className="clientes-error vacaciones-permisos-form-wide" role="alert">{errorEmpleados}</p>
        )}
        {hayMas && (
          <p className="vacaciones-permisos-aviso vacaciones-permisos-form-wide">Se muestran los primeros 100 empleados activos.</p>
        )}
        <CampoConError etiqueta="Tipo" htmlFor="ausencia-tipo" error={errores.tipo} idError="ausencia-tipo-error">
          <select
            aria-describedby={errores.tipo ? 'ausencia-tipo-error' : undefined}
            aria-invalid={Boolean(errores.tipo)}
            id="ausencia-tipo"
            onChange={(event) => cambiarCampo('tipo', event.currentTarget.value)}
            value={campos.tipo}
          >
            {TIPOS.map((item) => <option key={item} value={item}>{textoTipo(item)}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Fecha de inicio" htmlFor="ausencia-fecha-inicio" error={errores.fechaInicio} idError="ausencia-fecha-inicio-error">
          <input
            aria-describedby={errores.fechaInicio ? 'ausencia-fecha-inicio-error' : undefined}
            aria-invalid={Boolean(errores.fechaInicio)}
            id="ausencia-fecha-inicio"
            onChange={(event) => cambiarCampo('fechaInicio', event.currentTarget.value)}
            type="date"
            value={campos.fechaInicio}
          />
        </CampoConError>
        <CampoConError etiqueta="Fecha de fin" htmlFor="ausencia-fecha-fin" error={errores.fechaFin} idError="ausencia-fecha-fin-error">
          <input
            aria-describedby={errores.fechaFin ? 'ausencia-fecha-fin-error' : undefined}
            aria-invalid={Boolean(errores.fechaFin)}
            id="ausencia-fecha-fin"
            onChange={(event) => cambiarCampo('fechaFin', event.currentTarget.value)}
            type="date"
            value={campos.fechaFin}
          />
        </CampoConError>
        <p aria-live="polite" className="vacaciones-permisos-dias vacaciones-permisos-form-wide" role="status">
          {dias === null ? 'Días de la solicitud: indica las fechas' : `Días de la solicitud: ${dias}`}
        </p>
        {esVacaciones && campos.empleadoId !== '' && (
          <div className="vacaciones-permisos-form-wide">
            <SaldoVacacional cargando={saldo.cargando} diasSolicitud={dias} error={saldo.error} saldo={saldo.saldo} />
          </div>
        )}
        <div className="vacaciones-permisos-form-wide">
          <CampoConError etiqueta="Motivo" htmlFor="ausencia-motivo" error={errores.motivo} idError="ausencia-motivo-error">
            <textarea
              aria-describedby={descripcionMotivo}
              aria-invalid={Boolean(errores.motivo)}
              id="ausencia-motivo"
              onChange={(event) => cambiarCampo('motivo', event.currentTarget.value)}
              rows={3}
              value={campos.motivo}
            />
          </CampoConError>
          <p className="vacaciones-permisos-contador" id="ausencia-motivo-contador">{`${campos.motivo.length}/${LIMITE_MOTIVO}`}</p>
          <p className="vacaciones-permisos-aviso" id="ausencia-motivo-ayuda">
            {motivoObligatorio ? 'Obligatorio para este tipo de ausencia.' : 'Opcional para este tipo de ausencia.'}
          </p>
        </div>
        <div className="vacaciones-permisos-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Registrar ausencia'}
          </button>
        </div>
      </form>
    </section>
  );
}
