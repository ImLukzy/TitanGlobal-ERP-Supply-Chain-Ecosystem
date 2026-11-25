import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { aTipoTurno, DIAS_SEMANA, formatearHora, mensajeError } from './presentacion';
import type { GuardarTurnoRequest, TurnoDetalle } from './tipos';
import { construirRequestTurno, validarTurno, type CamposTurno, type ErroresTurno } from './validacionTurno';

interface Props {
  turno: TurnoDetalle | null;
  cancelar: () => void;
  guardar: (request: GuardarTurnoRequest) => Promise<void>;
}

const CAMPOS_INICIALES: CamposTurno = {
  codigo: '',
  nombre: '',
  tipo: 'Fijo',
  horaEntrada: '',
  horaSalida: '',
  cruzaMedianoche: false,
  toleranciaMinutos: '0',
  diasLaborables: [],
  diasTrabajo: '',
  diasDescanso: '',
  fechaInicioCiclo: '',
};

function camposDesdeTurno(turno: TurnoDetalle): CamposTurno {
  return {
    codigo: turno.codigo,
    nombre: turno.nombre,
    tipo: turno.tipo,
    horaEntrada: formatearHora(turno.horaEntrada),
    horaSalida: formatearHora(turno.horaSalida),
    cruzaMedianoche: turno.cruzaMedianoche,
    toleranciaMinutos: String(turno.toleranciaMinutos),
    diasLaborables: turno.diasLaborables,
    diasTrabajo: turno.diasTrabajo === null ? '' : String(turno.diasTrabajo),
    diasDescanso: turno.diasDescanso === null ? '' : String(turno.diasDescanso),
    fechaInicioCiclo: turno.fechaInicioCiclo ?? '',
  };
}

export function TurnoFormulario({ turno, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposTurno>(turno === null ? CAMPOS_INICIALES : camposDesdeTurno(turno));
  const [errores, setErrores] = useState<ErroresTurno>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const fijo = campos.tipo === 'Fijo';

  function cambiarTexto(clave: 'codigo' | 'nombre' | 'toleranciaMinutos' | 'diasTrabajo' | 'diasDescanso' | 'fechaInicioCiclo', valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  // Al cambiar de tipo se descartan los datos del otro tipo para no enviarlos por error.
  function cambiarTipo(valor: string): void {
    const tipo = aTipoTurno(valor);
    setCampos((actual) => ({
      ...actual,
      tipo,
      diasLaborables: tipo === 'Fijo' ? actual.diasLaborables : [],
      diasTrabajo: tipo === 'Rotativo' ? actual.diasTrabajo : '',
      diasDescanso: tipo === 'Rotativo' ? actual.diasDescanso : '',
      fechaInicioCiclo: tipo === 'Rotativo' ? actual.fechaInicioCiclo : '',
    }));
  }

  // «Cruza medianoche» se marca sola cuando la salida es anterior a la entrada; la persona puede revisarla.
  function cambiarHora(clave: 'horaEntrada' | 'horaSalida', valor: string): void {
    setCampos((actual) => {
      const siguiente = { ...actual, [clave]: valor };
      if (siguiente.horaEntrada !== '' && siguiente.horaSalida !== '') {
        siguiente.cruzaMedianoche = siguiente.horaSalida < siguiente.horaEntrada;
      }
      return siguiente;
    });
  }

  function cambiarCruza(marcado: boolean): void {
    setCampos((actual) => ({ ...actual, cruzaMedianoche: marcado }));
  }

  function alternarDia(dia: number, marcado: boolean): void {
    setCampos((actual) => ({
      ...actual,
      diasLaborables: marcado
        ? [...actual.diasLaborables.filter((item) => item !== dia), dia]
        : actual.diasLaborables.filter((item) => item !== dia),
    }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarTurno(campos);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(construirRequestTurno(campos, aTipoTurno(campos.tipo)));
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el turno.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="turno-form-title" className="clientes-form-panel turnos-horarios-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">TURNOS</div>
          <h2 id="turno-form-title">{turno === null ? 'Nuevo turno' : `Editar turno ${turno.codigo}`}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="turnos-horarios-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Código" htmlFor="turno-codigo" error={errores.codigo} idError="turno-codigo-error">
          <input
            aria-describedby={errores.codigo ? 'turno-codigo-error' : undefined}
            aria-invalid={Boolean(errores.codigo)}
            autoComplete="off"
            id="turno-codigo"
            onChange={(event) => cambiarTexto('codigo', event.currentTarget.value)}
            value={campos.codigo}
          />
        </CampoConError>
        <CampoConError etiqueta="Nombre" htmlFor="turno-nombre" error={errores.nombre} idError="turno-nombre-error">
          <input
            aria-describedby={errores.nombre ? 'turno-nombre-error' : undefined}
            aria-invalid={Boolean(errores.nombre)}
            autoComplete="off"
            id="turno-nombre"
            onChange={(event) => cambiarTexto('nombre', event.currentTarget.value)}
            value={campos.nombre}
          />
        </CampoConError>
        <fieldset className="turnos-horarios-fieldset turnos-horarios-form-wide">
          <legend>Tipo</legend>
          <div className="turnos-horarios-opciones">
            <label htmlFor="turno-tipo-fijo">
              <input checked={fijo} id="turno-tipo-fijo" name="turno-tipo" onChange={() => cambiarTipo('Fijo')} type="radio" />
              Fijo
            </label>
            <label htmlFor="turno-tipo-rotativo">
              <input checked={!fijo} id="turno-tipo-rotativo" name="turno-tipo" onChange={() => cambiarTipo('Rotativo')} type="radio" />
              Rotativo
            </label>
          </div>
          {errores.tipo && <span className="field-error" id="turno-tipo-error">{errores.tipo}</span>}
        </fieldset>
        <CampoConError etiqueta="Hora de entrada" htmlFor="turno-hora-entrada" error={errores.horaEntrada} idError="turno-hora-entrada-error">
          <input
            aria-describedby={errores.horaEntrada ? 'turno-hora-entrada-error' : undefined}
            aria-invalid={Boolean(errores.horaEntrada)}
            id="turno-hora-entrada"
            onChange={(event) => cambiarHora('horaEntrada', event.currentTarget.value)}
            type="time"
            value={campos.horaEntrada}
          />
        </CampoConError>
        <CampoConError etiqueta="Hora de salida" htmlFor="turno-hora-salida" error={errores.horaSalida} idError="turno-hora-salida-error">
          <input
            aria-describedby={errores.horaSalida ? 'turno-hora-salida-error' : undefined}
            aria-invalid={Boolean(errores.horaSalida)}
            id="turno-hora-salida"
            onChange={(event) => cambiarHora('horaSalida', event.currentTarget.value)}
            type="time"
            value={campos.horaSalida}
          />
        </CampoConError>
        <div className="turnos-horarios-form-wide">
          <label className="turnos-horarios-casilla" htmlFor="turno-cruza-medianoche">
            <input
              aria-describedby={errores.cruzaMedianoche ? 'turno-cruza-medianoche-error' : undefined}
              aria-invalid={Boolean(errores.cruzaMedianoche)}
              checked={campos.cruzaMedianoche}
              id="turno-cruza-medianoche"
              onChange={(event) => cambiarCruza(event.currentTarget.checked)}
              type="checkbox"
            />
            Cruza medianoche
          </label>
          {errores.cruzaMedianoche && <span className="field-error" id="turno-cruza-medianoche-error">{errores.cruzaMedianoche}</span>}
        </div>
        <CampoConError etiqueta="Tolerancia (minutos)" htmlFor="turno-tolerancia" error={errores.toleranciaMinutos} idError="turno-tolerancia-error">
          <input
            aria-describedby={errores.toleranciaMinutos ? 'turno-tolerancia-error' : undefined}
            aria-invalid={Boolean(errores.toleranciaMinutos)}
            autoComplete="off"
            id="turno-tolerancia"
            inputMode="numeric"
            onChange={(event) => cambiarTexto('toleranciaMinutos', event.currentTarget.value)}
            value={campos.toleranciaMinutos}
          />
        </CampoConError>
        {fijo && (
          <fieldset className="turnos-horarios-fieldset turnos-horarios-form-wide">
            <legend>Días laborables</legend>
            <div className="turnos-horarios-opciones">
              {DIAS_SEMANA.map((dia) => (
                <label htmlFor={`turno-dia-${dia.numero}`} key={dia.numero}>
                  <input
                    aria-describedby={errores.diasLaborables ? 'turno-dias-error' : undefined}
                    checked={campos.diasLaborables.includes(dia.numero)}
                    id={`turno-dia-${dia.numero}`}
                    onChange={(event) => alternarDia(dia.numero, event.currentTarget.checked)}
                    type="checkbox"
                  />
                  {dia.nombre}
                </label>
              ))}
            </div>
            {errores.diasLaborables && <span className="field-error" id="turno-dias-error">{errores.diasLaborables}</span>}
          </fieldset>
        )}
        {!fijo && (
          <>
            <CampoConError etiqueta="Días de trabajo" htmlFor="turno-dias-trabajo" error={errores.diasTrabajo} idError="turno-dias-trabajo-error">
              <input
                aria-describedby={errores.diasTrabajo ? 'turno-dias-trabajo-error' : undefined}
                aria-invalid={Boolean(errores.diasTrabajo)}
                autoComplete="off"
                id="turno-dias-trabajo"
                inputMode="numeric"
                onChange={(event) => cambiarTexto('diasTrabajo', event.currentTarget.value)}
                value={campos.diasTrabajo}
              />
            </CampoConError>
            <CampoConError etiqueta="Días de descanso" htmlFor="turno-dias-descanso" error={errores.diasDescanso} idError="turno-dias-descanso-error">
              <input
                aria-describedby={errores.diasDescanso ? 'turno-dias-descanso-error' : undefined}
                aria-invalid={Boolean(errores.diasDescanso)}
                autoComplete="off"
                id="turno-dias-descanso"
                inputMode="numeric"
                onChange={(event) => cambiarTexto('diasDescanso', event.currentTarget.value)}
                value={campos.diasDescanso}
              />
            </CampoConError>
            <CampoConError etiqueta="Inicio del ciclo" htmlFor="turno-inicio-ciclo" error={errores.fechaInicioCiclo} idError="turno-inicio-ciclo-error">
              <input
                aria-describedby={errores.fechaInicioCiclo ? 'turno-inicio-ciclo-error' : undefined}
                aria-invalid={Boolean(errores.fechaInicioCiclo)}
                id="turno-inicio-ciclo"
                onChange={(event) => cambiarTexto('fechaInicioCiclo', event.currentTarget.value)}
                type="date"
                value={campos.fechaInicioCiclo}
              />
            </CampoConError>
          </>
        )}
        <div className="turnos-horarios-form-actions">
          <p aria-live="polite" className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar turno'}
          </button>
        </div>
      </form>
    </section>
  );
}
