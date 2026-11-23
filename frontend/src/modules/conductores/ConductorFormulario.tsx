import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { actualizarConductor, crearConductor } from './api';
import { diaUtc, licenciaVencida, mensajeError } from './presentacion';
import type { ConductorDetalle, GuardarConductorRequest } from './tipos';
import { fechaParaGuardar, validarConductor, type ErroresConductor, type ValoresConductor } from './validacion';

interface Props {
  conductor: ConductorDetalle | null;
  cancelar: () => void;
  guardado: (id: number) => void;
}

export function ConductorFormulario({ conductor, cancelar, guardado }: Props) {
  const fechaOriginal = conductor?.vencimientoLicencia ?? null;
  const [valores, setValores] = useState<ValoresConductor>({
    nombre: conductor?.nombre ?? '', documentoLicencia: conductor?.documentoLicencia ?? '',
    vencimientoLicencia: fechaOriginal === null ? '' : diaUtc(fechaOriginal),
    habilitado: conductor?.habilitado ?? true, activo: conductor?.activo ?? true,
    observaciones: conductor?.observaciones ?? '',
  });
  const [errores, setErrores] = useState<ErroresConductor>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const formularioRef = useRef<HTMLFormElement>(null);
  const enviandoRef = useRef(false);
  useEffect(() => { formularioRef.current?.querySelector<HTMLInputElement>('input')?.focus(); }, []);

  function cambiar(campo: keyof ValoresConductor, valor: string | boolean): void {
    setValores((actual) => ({ ...actual, [campo]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (enviandoRef.current) { return; }
    setMensaje('');
    const nuevosErrores = validarConductor(valores, fechaOriginal);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      window.requestAnimationFrame(() => formularioRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus());
      return;
    }
    const request: GuardarConductorRequest = {
      nombre: valores.nombre.trim(), documentoLicencia: valores.documentoLicencia.trim(),
      vencimientoLicencia: fechaParaGuardar(valores.vencimientoLicencia, fechaOriginal),
      habilitado: valores.habilitado, activo: valores.activo,
    };
    if (valores.observaciones.trim() !== '') { request.observaciones = valores.observaciones.trim(); }
    enviandoRef.current = true;
    setGuardando(true);
    try {
      const respuesta = conductor === null ? await crearConductor(request) : await actualizarConductor(conductor.id, request);
      guardado(respuesta.id);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el conductor.'));
    } finally {
      enviandoRef.current = false;
      setGuardando(false);
    }
  }

  return (
    <section className="clientes-form-panel conductores-form-panel" aria-labelledby="conductor-form-title">
      <div className="clientes-panel-heading"><div><div className="page-eyebrow">CONDUCTOR</div>
        <h2 id="conductor-form-title">{conductor === null ? 'Nuevo conductor' : 'Editar conductor'}</h2></div>
        <button className="secondary-button" disabled={guardando} onClick={cancelar} type="button">Cancelar</button>
      </div>
      {fechaOriginal !== null && licenciaVencida(fechaOriginal) && <p className="conductores-warning">
        Licencia vencida: puedes conservar su fecha al editar o desactivar. Para renovarla, indica hoy o una fecha futura.
      </p>}
      <form ref={formularioRef} className="conductores-form" noValidate onSubmit={(event) => void enviar(event)}>
        {([
          ['nombre', 'Nombre', 100], ['documentoLicencia', 'Documento de licencia', 20],
        ] as const).map(([campo, etiqueta, maximo]) => {
          const id = `conductor-${campo}`;
          return <CampoConError key={campo} etiqueta={etiqueta} htmlFor={id} error={errores[campo]} idError={`${id}-error`}>
            <input id={id} disabled={guardando} maxLength={maximo} autoComplete="off" value={valores[campo]}
              aria-invalid={Boolean(errores[campo])} aria-describedby={errores[campo] ? `${id}-error` : undefined}
              onChange={(event) => cambiar(campo, event.currentTarget.value)} />
          </CampoConError>;
        })}
        <CampoConError etiqueta="Vencimiento de licencia" htmlFor="conductor-vencimiento" error={errores.vencimientoLicencia} idError="conductor-vencimiento-error">
          <input id="conductor-vencimiento" type="date" disabled={guardando} value={valores.vencimientoLicencia}
            aria-invalid={Boolean(errores.vencimientoLicencia)} aria-describedby={errores.vencimientoLicencia ? 'conductor-vencimiento-error' : undefined}
            onChange={(event) => cambiar('vencimientoLicencia', event.currentTarget.value)} />
        </CampoConError>
        <CampoConError etiqueta="Observaciones (opcional)" htmlFor="conductor-observaciones" error={errores.observaciones} idError="conductor-observaciones-error">
          <textarea id="conductor-observaciones" maxLength={500} rows={3} disabled={guardando} value={valores.observaciones}
            aria-invalid={Boolean(errores.observaciones)} aria-describedby={errores.observaciones ? 'conductor-observaciones-error' : undefined}
            onChange={(event) => cambiar('observaciones', event.currentTarget.value)} />
        </CampoConError>
        <label className="conductores-active-field"><input type="checkbox" checked={valores.habilitado} disabled={guardando}
          aria-describedby="conductor-permiso-ayuda" onChange={(event) => cambiar('habilitado', event.currentTarget.checked)} />Habilitado</label>
        <label className="conductores-active-field"><input type="checkbox" checked={valores.activo} disabled={guardando}
          onChange={(event) => cambiar('activo', event.currentTarget.checked)} />Activo</label>
        <p id="conductor-permiso-ayuda" className="conductores-help">La habilitación es administrativa; comprueba también la vigencia de la licencia.</p>
        <div className="conductores-form-actions">
          <p className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar conductor'}</button>
        </div>
      </form>
    </section>
  );
}
