import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError } from './presentacion';
import type { Competencia, GuardarCompetenciaRequest } from './tipos';
import {
  construirRequestCompetencia,
  formatearDecimal,
  validarCompetencia,
  type CamposCompetencia,
  type ErroresCompetencia,
} from './validacionEvaluacion';

interface Props {
  competencia: Competencia | null;
  cancelar: () => void;
  guardar: (request: GuardarCompetenciaRequest) => Promise<void>;
}

function camposIniciales(competencia: Competencia | null): CamposCompetencia {
  if (competencia === null) {
    return { nombre: '', descripcion: '', peso: '' };
  }
  return { nombre: competencia.nombre, descripcion: competencia.descripcion ?? '', peso: formatearDecimal(competencia.peso) };
}

// Formulario de competencia (mensajes 2, 11 y 34): nombre, descripción y peso en porcentaje.
export function CompetenciaFormulario({ competencia, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposCompetencia>(() => camposIniciales(competencia));
  const [errores, setErrores] = useState<ErroresCompetencia>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  function cambiarCampo(clave: keyof CamposCompetencia, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarCompetencia(campos);
    setErrores(nuevosErrores);
    const request = construirRequestCompetencia(campos);
    if (Object.keys(nuevosErrores).length > 0 || request === null) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar la competencia.'));
    } finally {
      setGuardando(false);
    }
  }

  return (
    <form aria-label={competencia === null ? 'Nueva competencia' : 'Editar competencia'} className="evaluacion-desempeno-form evaluacion-desempeno-competencia-form" noValidate onSubmit={(event) => void enviar(event)}>
      <CampoConError etiqueta="Nombre de la competencia" htmlFor="competencia-nombre" error={errores.nombre} idError="competencia-nombre-error">
        <input
          aria-describedby={errores.nombre ? 'competencia-nombre-error' : undefined}
          aria-invalid={Boolean(errores.nombre)}
          autoComplete="off"
          id="competencia-nombre"
          onChange={(event) => cambiarCampo('nombre', event.currentTarget.value)}
          type="text"
          value={campos.nombre}
        />
      </CampoConError>
      <CampoConError etiqueta="Peso (%)" htmlFor="competencia-peso" error={errores.peso} idError="competencia-peso-error">
        <input
          aria-describedby={errores.peso ? 'competencia-peso-error' : undefined}
          aria-invalid={Boolean(errores.peso)}
          autoComplete="off"
          id="competencia-peso"
          inputMode="decimal"
          onChange={(event) => cambiarCampo('peso', event.currentTarget.value)}
          type="text"
          value={campos.peso}
        />
      </CampoConError>
      <div className="evaluacion-desempeno-form-wide">
        <CampoConError etiqueta="Descripción" htmlFor="competencia-descripcion" error={errores.descripcion} idError="competencia-descripcion-error">
          <input
            aria-describedby={errores.descripcion ? 'competencia-descripcion-error' : undefined}
            aria-invalid={Boolean(errores.descripcion)}
            autoComplete="off"
            id="competencia-descripcion"
            onChange={(event) => cambiarCampo('descripcion', event.currentTarget.value)}
            type="text"
            value={campos.descripcion}
          />
        </CampoConError>
      </div>
      <div className="evaluacion-desempeno-form-actions">
        {mensaje !== '' && <p aria-live="polite" className="clientes-error" role="alert">{mensaje}</p>}
        <button className="secondary-button" disabled={guardando} onClick={cancelar} type="button">Cancelar</button>
        <button className="primary-button" disabled={guardando} type="submit">
          {guardando ? 'Guardando…' : 'Guardar competencia'}
        </button>
      </div>
    </form>
  );
}
