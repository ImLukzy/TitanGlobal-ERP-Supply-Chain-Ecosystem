import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { mensajeError } from './presentacion';
import type { EquipoProteccion, GuardarEquipoRequest } from './tipos';
import { construirRequestEquipo, LIMITE_CODIGO, LIMITE_NOMBRE, validarEquipo, type CamposEquipo, type Errores } from './validacionSst';

interface Props {
  equipo: EquipoProteccion | null;
  cancelar: () => void;
  guardar: (request: GuardarEquipoRequest) => Promise<void>;
}

function camposIniciales(equipo: EquipoProteccion | null): CamposEquipo {
  if (equipo === null) {
    return { codigo: '', nombre: '', vidaUtilDias: '' };
  }
  return { codigo: equipo.codigo, nombre: equipo.nombre, vidaUtilDias: String(equipo.vidaUtilDias) };
}

// Formulario de equipo de protección (mensajes 1 a 3 de la spec backend).
export function EquipoFormulario({ equipo, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposEquipo>(() => camposIniciales(equipo));
  const [errores, setErrores] = useState<Errores<CamposEquipo>>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  function cambiarCampo(clave: keyof CamposEquipo, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevos = validarEquipo(campos);
    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(construirRequestEquipo(campos));
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el equipo.'));
    } finally {
      setGuardando(false);
    }
  }

  function describir(clave: keyof CamposEquipo): string | undefined {
    return errores[clave] ? `equipo-${clave}-error` : undefined;
  }

  return (
    <section aria-labelledby="equipo-form-title" className="clientes-form-panel sst-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">EPP</div>
          <h2 id="equipo-form-title">{equipo === null ? 'Nuevo equipo' : `Editar equipo ${equipo.codigo}`}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="sst-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Código" htmlFor="equipo-codigo" error={errores.codigo} idError="equipo-codigo-error">
          <input aria-describedby={describir('codigo')} aria-invalid={Boolean(errores.codigo)} autoComplete="off" id="equipo-codigo" maxLength={LIMITE_CODIGO + 10} onChange={(event) => cambiarCampo('codigo', event.currentTarget.value)} type="text" value={campos.codigo} />
        </CampoConError>
        <CampoConError etiqueta="Nombre" htmlFor="equipo-nombre" error={errores.nombre} idError="equipo-nombre-error">
          <input aria-describedby={describir('nombre')} aria-invalid={Boolean(errores.nombre)} autoComplete="off" id="equipo-nombre" maxLength={LIMITE_NOMBRE + 20} onChange={(event) => cambiarCampo('nombre', event.currentTarget.value)} type="text" value={campos.nombre} />
        </CampoConError>
        <CampoConError etiqueta="Vida útil (días)" htmlFor="equipo-vida" error={errores.vidaUtilDias} idError="equipo-vidaUtilDias-error">
          <input aria-describedby={describir('vidaUtilDias')} aria-invalid={Boolean(errores.vidaUtilDias)} autoComplete="off" id="equipo-vida" inputMode="numeric" onChange={(event) => cambiarCampo('vidaUtilDias', event.currentTarget.value)} type="text" value={campos.vidaUtilDias} />
        </CampoConError>
        <div className="sst-form-actions">
          {mensaje !== '' && <p aria-live="polite" className="clientes-error" role="alert">{mensaje}</p>}
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar equipo'}</button>
        </div>
      </form>
    </section>
  );
}
