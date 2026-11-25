import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError, PERIODOS } from './presentacion';
import type { CicloDetalle, GuardarCicloRequest } from './tipos';
import { construirRequestCiclo, LIMITE_CODIGO, LIMITE_NOMBRE, validarCiclo, type CamposCiclo, type ErroresCiclo } from './validacionEvaluacion';

interface Props {
  ciclo: CicloDetalle | null;
  cancelar: () => void;
  guardar: (request: GuardarCicloRequest) => Promise<void>;
}

type ClaveFecha = 'fechaInicioPeriodo' | 'fechaFinPeriodo' | 'fechaApertura' | 'fechaCierre';

const FECHAS: { clave: ClaveFecha; etiqueta: string }[] = [
  { clave: 'fechaInicioPeriodo', etiqueta: 'Inicio del periodo' },
  { clave: 'fechaFinPeriodo', etiqueta: 'Fin del periodo' },
  { clave: 'fechaApertura', etiqueta: 'Apertura de evaluaciones' },
  { clave: 'fechaCierre', etiqueta: 'Cierre de evaluaciones' },
];

function camposIniciales(ciclo: CicloDetalle | null): CamposCiclo {
  if (ciclo === null) {
    return { codigo: '', nombre: '', periodo: 'Semestral', fechaInicioPeriodo: '', fechaFinPeriodo: '', fechaApertura: '', fechaCierre: '' };
  }
  return {
    codigo: ciclo.codigo,
    nombre: ciclo.nombre,
    periodo: ciclo.periodo,
    fechaInicioPeriodo: ciclo.fechaInicioPeriodo,
    fechaFinPeriodo: ciclo.fechaFinPeriodo,
    fechaApertura: ciclo.fechaApertura,
    fechaCierre: ciclo.fechaCierre,
  };
}

// Formulario de ciclo (mensajes 1 a 7 de la spec backend); solo se edita un ciclo en configuración.
export function CicloFormulario({ ciclo, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposCiclo>(() => camposIniciales(ciclo));
  const [errores, setErrores] = useState<ErroresCiclo>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  function cambiarCampo(clave: keyof CamposCiclo, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarCiclo(campos);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(construirRequestCiclo(campos));
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el ciclo.'));
    } finally {
      setGuardando(false);
    }
  }

  function describir(clave: keyof CamposCiclo): string | undefined {
    return errores[clave] ? `ciclo-${clave}-error` : undefined;
  }

  return (
    <section aria-labelledby="ciclo-form-title" className="clientes-form-panel evaluacion-desempeno-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">CICLOS</div>
          <h2 id="ciclo-form-title">{ciclo === null ? 'Nuevo ciclo' : `Editar ciclo ${ciclo.codigo}`}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="evaluacion-desempeno-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Código" htmlFor="ciclo-codigo" error={errores.codigo} idError="ciclo-codigo-error">
          <input
            aria-describedby={describir('codigo')}
            aria-invalid={Boolean(errores.codigo)}
            autoComplete="off"
            id="ciclo-codigo"
            maxLength={LIMITE_CODIGO + 10}
            onChange={(event) => cambiarCampo('codigo', event.currentTarget.value)}
            type="text"
            value={campos.codigo}
          />
        </CampoConError>
        <CampoConError etiqueta="Nombre" htmlFor="ciclo-nombre" error={errores.nombre} idError="ciclo-nombre-error">
          <input
            aria-describedby={describir('nombre')}
            aria-invalid={Boolean(errores.nombre)}
            autoComplete="off"
            id="ciclo-nombre"
            maxLength={LIMITE_NOMBRE + 20}
            onChange={(event) => cambiarCampo('nombre', event.currentTarget.value)}
            type="text"
            value={campos.nombre}
          />
        </CampoConError>
        <CampoConError etiqueta="Periodo" htmlFor="ciclo-periodo" error={errores.periodo} idError="ciclo-periodo-error">
          <select
            aria-describedby={describir('periodo')}
            aria-invalid={Boolean(errores.periodo)}
            id="ciclo-periodo"
            onChange={(event) => cambiarCampo('periodo', event.currentTarget.value)}
            value={campos.periodo}
          >
            {PERIODOS.map((periodo) => <option key={periodo} value={periodo}>{periodo}</option>)}
          </select>
        </CampoConError>
        {FECHAS.map(({ clave, etiqueta }) => (
          <CampoConError key={clave} etiqueta={etiqueta} htmlFor={`ciclo-${clave}`} error={errores[clave]} idError={`ciclo-${clave}-error`}>
            <input
              aria-describedby={describir(clave)}
              aria-invalid={Boolean(errores[clave])}
              id={`ciclo-${clave}`}
              onChange={(event) => cambiarCampo(clave, event.currentTarget.value)}
              type="date"
              value={campos[clave]}
            />
          </CampoConError>
        ))}
        <div className="evaluacion-desempeno-form-actions">
          {mensaje !== '' && <p aria-live="polite" className="clientes-error" role="alert">{mensaje}</p>}
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar ciclo'}
          </button>
        </div>
      </form>
    </section>
  );
}
