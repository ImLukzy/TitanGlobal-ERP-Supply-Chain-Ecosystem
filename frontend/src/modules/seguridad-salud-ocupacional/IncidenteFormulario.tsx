import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { EmpleadoListado } from '../empleados/tipos';
import { hoyIso, mensajeError, SEVERIDADES, textoValor, TIPOS_INCIDENTE } from './presentacion';
import type { RegistrarIncidenteRequest } from './tipos';
import { construirRequestIncidente, validarIncidente, type CamposIncidente, type Errores } from './validacionSst';

interface Props {
  empleados: EmpleadoListado[];
  cancelar: () => void;
  guardar: (request: RegistrarIncidenteRequest) => Promise<void>;
}

const INICIALES: CamposIncidente = { empleadoId: '', tipo: '', severidad: '', fecha: '', diasBaja: '', lugar: '', descripcion: '' };

// Formulario de incidente (mensajes 8, 11 y 20 a 25 de la spec backend).
export function IncidenteFormulario({ empleados, cancelar, guardar }: Props) {
  const [campos, setCampos] = useState<CamposIncidente>(INICIALES);
  const [errores, setErrores] = useState<Errores<CamposIncidente>>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const hoy = hoyIso();

  function cambiarCampo(clave: keyof CamposIncidente, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevos = validarIncidente(campos, hoy);
    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(construirRequestIncidente(campos));
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo registrar el incidente.'));
    } finally {
      setGuardando(false);
    }
  }

  function describir(clave: keyof CamposIncidente): string | undefined {
    return errores[clave] ? `incidente-${clave}-error` : undefined;
  }

  return (
    <section aria-labelledby="incidente-form-title" className="clientes-form-panel sst-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">INCIDENTES</div>
          <h2 id="incidente-form-title">Nuevo incidente</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="sst-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Empleado" htmlFor="incidente-empleado" error={errores.empleadoId} idError="incidente-empleadoId-error">
          <select aria-describedby={describir('empleadoId')} aria-invalid={Boolean(errores.empleadoId)} id="incidente-empleado" onChange={(event) => cambiarCampo('empleadoId', event.currentTarget.value)} value={campos.empleadoId}>
            <option value="">Selecciona un empleado</option>
            {empleados.map((empleado) => <option key={empleado.id} value={String(empleado.id)}>{`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Fecha" htmlFor="incidente-fecha" error={errores.fecha} idError="incidente-fecha-error">
          <input aria-describedby={describir('fecha')} aria-invalid={Boolean(errores.fecha)} id="incidente-fecha" max={hoy} onChange={(event) => cambiarCampo('fecha', event.currentTarget.value)} type="date" value={campos.fecha} />
        </CampoConError>
        <CampoConError etiqueta="Tipo" htmlFor="incidente-tipo" error={errores.tipo} idError="incidente-tipo-error">
          <select aria-describedby={describir('tipo')} aria-invalid={Boolean(errores.tipo)} id="incidente-tipo" onChange={(event) => cambiarCampo('tipo', event.currentTarget.value)} value={campos.tipo}>
            <option value="">Selecciona un tipo</option>
            {TIPOS_INCIDENTE.map((tipo) => <option key={tipo} value={tipo}>{textoValor(tipo)}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Severidad" htmlFor="incidente-severidad" error={errores.severidad} idError="incidente-severidad-error">
          <select aria-describedby={describir('severidad')} aria-invalid={Boolean(errores.severidad)} id="incidente-severidad" onChange={(event) => cambiarCampo('severidad', event.currentTarget.value)} value={campos.severidad}>
            <option value="">Selecciona la severidad</option>
            {SEVERIDADES.map((severidad) => <option key={severidad} value={severidad}>{severidad}</option>)}
          </select>
        </CampoConError>
        {campos.tipo === 'AccidenteConBaja' && (
          <CampoConError etiqueta="Días de baja" htmlFor="incidente-dias" error={errores.diasBaja} idError="incidente-diasBaja-error">
            <input aria-describedby={describir('diasBaja')} aria-invalid={Boolean(errores.diasBaja)} autoComplete="off" id="incidente-dias" inputMode="numeric" onChange={(event) => cambiarCampo('diasBaja', event.currentTarget.value)} type="text" value={campos.diasBaja} />
          </CampoConError>
        )}
        <CampoConError etiqueta="Lugar (opcional)" htmlFor="incidente-lugar" error={errores.lugar} idError="incidente-lugar-error">
          <input aria-describedby={describir('lugar')} aria-invalid={Boolean(errores.lugar)} autoComplete="off" id="incidente-lugar" onChange={(event) => cambiarCampo('lugar', event.currentTarget.value)} type="text" value={campos.lugar} />
        </CampoConError>
        <div className="sst-form-wide">
          <CampoConError etiqueta="Descripción" htmlFor="incidente-descripcion" error={errores.descripcion} idError="incidente-descripcion-error">
            <textarea aria-describedby={describir('descripcion')} aria-invalid={Boolean(errores.descripcion)} id="incidente-descripcion" onChange={(event) => cambiarCampo('descripcion', event.currentTarget.value)} rows={3} value={campos.descripcion} />
          </CampoConError>
        </div>
        <div className="sst-form-actions">
          {mensaje !== '' && <p aria-live="polite" className="clientes-error" role="alert">{mensaje}</p>}
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Registrar incidente'}</button>
        </div>
      </form>
    </section>
  );
}
