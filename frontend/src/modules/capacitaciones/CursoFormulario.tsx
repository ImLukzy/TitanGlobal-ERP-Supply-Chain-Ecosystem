import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError, TIPOS_CURSO } from './presentacion';
import type { CursoListado, GuardarCursoRequest } from './tipos';
import { construirRequestCurso, LIMITE_CODIGO, LIMITE_NOMBRE, LIMITE_PROVEEDOR, validarCurso, type CamposCurso, type ErroresCurso } from './validacionCapacitacion';

interface Props {
  curso: CursoListado | null;
  cancelar: () => void;
  guardar: (request: GuardarCursoRequest) => Promise<void>;
}

function camposIniciales(curso: CursoListado | null): CamposCurso {
  if (curso === null) {
    return { codigo: '', nombre: '', horas: '', tipo: 'Interno', proveedor: '' };
  }
  return { codigo: curso.codigo, nombre: curso.nombre, horas: String(curso.horas), tipo: curso.tipo, proveedor: curso.proveedor ?? '' };
}

// Formulario de curso (mensajes 1 a 6 de la spec backend).
export function CursoFormulario({ curso, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposCurso>(() => camposIniciales(curso));
  const [errores, setErrores] = useState<ErroresCurso>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  function cambiarCampo(clave: keyof CamposCurso, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevosErrores = validarCurso(campos);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(construirRequestCurso(campos));
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el curso.'));
    } finally {
      setGuardando(false);
    }
  }

  function describir(clave: keyof CamposCurso): string | undefined {
    return errores[clave] ? `curso-${clave}-error` : undefined;
  }

  return (
    <section aria-labelledby="curso-form-title" className="clientes-form-panel capacitaciones-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">CURSOS</div>
          <h2 id="curso-form-title">{curso === null ? 'Nuevo curso' : `Editar curso ${curso.codigo}`}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="capacitaciones-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Código" htmlFor="curso-codigo" error={errores.codigo} idError="curso-codigo-error">
          <input aria-describedby={describir('codigo')} aria-invalid={Boolean(errores.codigo)} autoComplete="off" id="curso-codigo" maxLength={LIMITE_CODIGO + 10} onChange={(event) => cambiarCampo('codigo', event.currentTarget.value)} type="text" value={campos.codigo} />
        </CampoConError>
        <CampoConError etiqueta="Nombre" htmlFor="curso-nombre" error={errores.nombre} idError="curso-nombre-error">
          <input aria-describedby={describir('nombre')} aria-invalid={Boolean(errores.nombre)} autoComplete="off" id="curso-nombre" maxLength={LIMITE_NOMBRE + 20} onChange={(event) => cambiarCampo('nombre', event.currentTarget.value)} type="text" value={campos.nombre} />
        </CampoConError>
        <CampoConError etiqueta="Horas" htmlFor="curso-horas" error={errores.horas} idError="curso-horas-error">
          <input aria-describedby={describir('horas')} aria-invalid={Boolean(errores.horas)} autoComplete="off" id="curso-horas" inputMode="decimal" onChange={(event) => cambiarCampo('horas', event.currentTarget.value)} type="text" value={campos.horas} />
        </CampoConError>
        <CampoConError etiqueta="Tipo" htmlFor="curso-tipo" error={errores.tipo} idError="curso-tipo-error">
          <select aria-describedby={describir('tipo')} aria-invalid={Boolean(errores.tipo)} id="curso-tipo" onChange={(event) => cambiarCampo('tipo', event.currentTarget.value)} value={campos.tipo}>
            {TIPOS_CURSO.map((tipo) => <option key={tipo} value={tipo}>{tipo}</option>)}
          </select>
        </CampoConError>
        <div className="capacitaciones-form-wide">
          <CampoConError etiqueta="Proveedor (opcional)" htmlFor="curso-proveedor" error={errores.proveedor} idError="curso-proveedor-error">
            <input aria-describedby={describir('proveedor')} aria-invalid={Boolean(errores.proveedor)} autoComplete="off" id="curso-proveedor" maxLength={LIMITE_PROVEEDOR + 20} onChange={(event) => cambiarCampo('proveedor', event.currentTarget.value)} type="text" value={campos.proveedor} />
          </CampoConError>
        </div>
        <div className="capacitaciones-form-actions">
          {mensaje !== '' && <p aria-live="polite" className="clientes-error" role="alert">{mensaje}</p>}
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar curso'}</button>
        </div>
      </form>
    </section>
  );
}
