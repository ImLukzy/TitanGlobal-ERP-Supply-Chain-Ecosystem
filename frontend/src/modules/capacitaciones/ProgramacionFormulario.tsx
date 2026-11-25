import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError } from './presentacion';
import type { CursoListado, GuardarProgramacionRequest } from './tipos';
import { construirRequestProgramacion, LIMITE_LUGAR, validarProgramacion, type CamposProgramacion, type ErroresProgramacion } from './validacionCapacitacion';

interface Props {
  cursos: CursoListado[];
  cargando: boolean;
  cancelar: () => void;
  guardar: (request: GuardarProgramacionRequest) => Promise<void>;
}

// Formulario de programación (mensajes 10 a 13 de la spec backend); solo ofrece cursos activos.
export function ProgramacionFormulario({ cursos, cargando, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposProgramacion>({ cursoId: '', fechaInicio: '', fechaFin: '', cupo: '', lugar: '' });
  const [errores, setErrores] = useState<ErroresProgramacion>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const activos = cursos.filter((curso) => curso.activo);

  function cambiarCampo(clave: keyof CamposProgramacion, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarProgramacion(campos);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(construirRequestProgramacion(campos));
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar la programación.'));
    } finally {
      setGuardando(false);
    }
  }

  function describir(clave: keyof CamposProgramacion): string | undefined {
    return errores[clave] ? `programacion-${clave}-error` : undefined;
  }

  return (
    <section aria-labelledby="programacion-form-title" className="clientes-form-panel capacitaciones-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">PROGRAMACIONES</div>
          <h2 id="programacion-form-title">Nueva programación</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="capacitaciones-form" noValidate onSubmit={(event) => void enviar(event)}>
        <div className="capacitaciones-form-wide">
          <CampoConError etiqueta="Curso" htmlFor="programacion-cursoId" error={errores.cursoId} idError="programacion-cursoId-error">
            <select aria-describedby={describir('cursoId')} aria-invalid={Boolean(errores.cursoId)} id="programacion-cursoId" onChange={(event) => cambiarCampo('cursoId', event.currentTarget.value)} value={campos.cursoId}>
              <option value="">{cargando ? 'Cargando…' : 'Selecciona un curso'}</option>
              {activos.map((curso) => <option key={curso.id} value={String(curso.id)}>{`${curso.codigo} · ${curso.nombre}`}</option>)}
            </select>
          </CampoConError>
        </div>
        <CampoConError etiqueta="Fecha de inicio" htmlFor="programacion-fechaInicio" error={errores.fechaInicio} idError="programacion-fechaInicio-error">
          <input aria-describedby={describir('fechaInicio')} aria-invalid={Boolean(errores.fechaInicio)} id="programacion-fechaInicio" onChange={(event) => cambiarCampo('fechaInicio', event.currentTarget.value)} type="date" value={campos.fechaInicio} />
        </CampoConError>
        <CampoConError etiqueta="Fecha de fin" htmlFor="programacion-fechaFin" error={errores.fechaFin} idError="programacion-fechaFin-error">
          <input aria-describedby={describir('fechaFin')} aria-invalid={Boolean(errores.fechaFin)} id="programacion-fechaFin" onChange={(event) => cambiarCampo('fechaFin', event.currentTarget.value)} type="date" value={campos.fechaFin} />
        </CampoConError>
        <CampoConError etiqueta="Cupo" htmlFor="programacion-cupo" error={errores.cupo} idError="programacion-cupo-error">
          <input aria-describedby={describir('cupo')} aria-invalid={Boolean(errores.cupo)} autoComplete="off" id="programacion-cupo" inputMode="numeric" onChange={(event) => cambiarCampo('cupo', event.currentTarget.value)} type="text" value={campos.cupo} />
        </CampoConError>
        <CampoConError etiqueta="Lugar (opcional)" htmlFor="programacion-lugar" error={errores.lugar} idError="programacion-lugar-error">
          <input aria-describedby={describir('lugar')} aria-invalid={Boolean(errores.lugar)} autoComplete="off" id="programacion-lugar" maxLength={LIMITE_LUGAR + 20} onChange={(event) => cambiarCampo('lugar', event.currentTarget.value)} type="text" value={campos.lugar} />
        </CampoConError>
        <div className="capacitaciones-form-actions">
          {mensaje !== '' && <p aria-live="polite" className="clientes-error" role="alert">{mensaje}</p>}
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar programación'}</button>
        </div>
      </form>
    </section>
  );
}
