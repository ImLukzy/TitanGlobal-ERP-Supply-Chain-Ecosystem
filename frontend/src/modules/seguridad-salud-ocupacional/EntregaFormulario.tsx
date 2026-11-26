import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import type { EmpleadoListado } from '../empleados/tipos';
import { hoyIso, mensajeError, sumarDias, textoFecha } from './presentacion';
import type { EquipoProteccion, RegistrarEntregaRequest } from './tipos';
import { construirRequestEntrega, validarEntrega, type CamposEntrega, type Errores } from './validacionSst';

interface Props {
  empleados: EmpleadoListado[];
  equipos: EquipoProteccion[];
  cancelar: () => void;
  guardar: (request: RegistrarEntregaRequest) => Promise<void>;
}

// Formulario de entrega (mensajes 8, 11 y 12). Solo se ofrecen empleados activos y equipos activos;
// el vencimiento (entrega + vida útil) se calcula a la vista y lo guarda el backend.
export function EntregaFormulario({ empleados, equipos, cancelar, guardar }: Props) {
  const hoy = hoyIso();
  const [campos, setCampos] = useState<CamposEntrega>({ equipoId: '', empleadoId: '', fechaEntrega: hoy, cantidad: '1' });
  const [errores, setErrores] = useState<Errores<CamposEntrega>>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const equipo = equipos.find((item) => String(item.id) === campos.equipoId);
  const vencimiento = equipo === undefined ? null : sumarDias(campos.fechaEntrega, equipo.vidaUtilDias);

  function cambiarCampo(clave: keyof CamposEntrega, valor: string): void {
    setCampos((actual) => ({ ...actual, [clave]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const nuevos = validarEntrega(campos, hoy);
    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) {
      return;
    }
    setGuardando(true);
    try {
      await guardar(construirRequestEntrega(campos));
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo registrar la entrega.'));
    } finally {
      setGuardando(false);
    }
  }

  function describir(clave: keyof CamposEntrega): string | undefined {
    return errores[clave] ? `entrega-${clave}-error` : undefined;
  }

  return (
    <section aria-labelledby="entrega-form-title" className="clientes-form-panel sst-form-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">EPP</div>
          <h2 id="entrega-form-title">Nueva entrega</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="sst-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Empleado" htmlFor="entrega-empleado" error={errores.empleadoId} idError="entrega-empleadoId-error">
          <select aria-describedby={describir('empleadoId')} aria-invalid={Boolean(errores.empleadoId)} id="entrega-empleado" onChange={(event) => cambiarCampo('empleadoId', event.currentTarget.value)} value={campos.empleadoId}>
            <option value="">Selecciona un empleado</option>
            {empleados.filter((empleado) => empleado.estado === 'Activo').map((empleado) => <option key={empleado.id} value={String(empleado.id)}>{`${empleado.legajo} · ${empleado.apellidos}, ${empleado.nombres}`}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Equipo" htmlFor="entrega-equipo" error={errores.equipoId} idError="entrega-equipoId-error">
          <select aria-describedby={describir('equipoId')} aria-invalid={Boolean(errores.equipoId)} id="entrega-equipo" onChange={(event) => cambiarCampo('equipoId', event.currentTarget.value)} value={campos.equipoId}>
            <option value="">Selecciona un equipo</option>
            {equipos.filter((item) => item.activo).map((item) => <option key={item.id} value={String(item.id)}>{`${item.codigo} · ${item.nombre}`}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Fecha de entrega" htmlFor="entrega-fecha" error={errores.fechaEntrega} idError="entrega-fechaEntrega-error">
          <input aria-describedby={describir('fechaEntrega')} aria-invalid={Boolean(errores.fechaEntrega)} id="entrega-fecha" max={hoy} onChange={(event) => cambiarCampo('fechaEntrega', event.currentTarget.value)} type="date" value={campos.fechaEntrega} />
        </CampoConError>
        <CampoConError etiqueta="Cantidad" htmlFor="entrega-cantidad" error={errores.cantidad} idError="entrega-cantidad-error">
          <input aria-describedby={describir('cantidad')} aria-invalid={Boolean(errores.cantidad)} autoComplete="off" id="entrega-cantidad" inputMode="numeric" onChange={(event) => cambiarCampo('cantidad', event.currentTarget.value)} type="text" value={campos.cantidad} />
        </CampoConError>
        <p aria-live="polite" className="sst-form-wide sst-vencimiento" data-testid="entrega-vencimiento">
          {vencimiento === null ? 'El vencimiento se calcula al elegir el equipo y la fecha.' : `Vence el ${textoFecha(vencimiento)}`}
        </p>
        <div className="sst-form-actions">
          {mensaje !== '' && <p aria-live="polite" className="clientes-error" role="alert">{mensaje}</p>}
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Registrar entrega'}</button>
        </div>
      </form>
    </section>
  );
}
