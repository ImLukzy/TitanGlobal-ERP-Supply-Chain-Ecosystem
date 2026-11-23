import { useEffect, useRef, useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { actualizarVehiculoFlota, crearVehiculoFlota } from './api';
import { mensajeError, nombreTipo } from './presentacion';
import type { GuardarVehiculoFlotaRequest, TipoVehiculo, VehiculoFlotaDetalle } from './tipos';
import { validarVehiculoFlota, type ErroresVehiculoFlota, type ValoresVehiculoFlota } from './validacion';

interface Props {
  vehiculo: VehiculoFlotaDetalle | null;
  cancelar: () => void;
  guardado: (id: number) => void;
}

export function VehiculoFlotaFormulario({ vehiculo, cancelar, guardado }: Props) {
  const [valores, setValores] = useState<ValoresVehiculoFlota>({
    placa: vehiculo?.placa ?? '', marca: vehiculo?.marca ?? '', modelo: vehiculo?.modelo ?? '',
    anio: String(vehiculo?.anio ?? new Date().getUTCFullYear()), tipo: vehiculo?.tipo ?? 'Camion',
    capacidadKg: String(vehiculo?.capacidadKg ?? ''), activo: vehiculo?.activo ?? true,
    observaciones: vehiculo?.observaciones ?? '',
  });
  const [errores, setErrores] = useState<ErroresVehiculoFlota>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);
  const formularioRef = useRef<HTMLFormElement>(null);
  const enviandoRef = useRef(false);

  useEffect(() => { formularioRef.current?.querySelector<HTMLInputElement>('input')?.focus(); }, []);

  function cambiar(campo: keyof ValoresVehiculoFlota, valor: string | boolean): void {
    setValores((actual) => ({ ...actual, [campo]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (enviandoRef.current) { return; }
    setMensaje('');
    const nuevosErrores = validarVehiculoFlota(valores);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      window.requestAnimationFrame(() => formularioRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus());
      return;
    }
    const request: GuardarVehiculoFlotaRequest = {
      placa: valores.placa.trim(), marca: valores.marca.trim(), modelo: valores.modelo.trim(),
      anio: Number(valores.anio), tipo: valores.tipo as TipoVehiculo,
      capacidadKg: Number(valores.capacidadKg), activo: valores.activo,
    };
    if (valores.observaciones.trim() !== '') { request.observaciones = valores.observaciones.trim(); }
    enviandoRef.current = true;
    setGuardando(true);
    try {
      const respuesta = vehiculo === null
        ? await crearVehiculoFlota(request)
        : await actualizarVehiculoFlota(vehiculo.id, request);
      guardado(respuesta.id);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el vehículo.'));
    } finally {
      enviandoRef.current = false;
      setGuardando(false);
    }
  }

  const campos = [
    { campo: 'placa', etiqueta: 'Placa', maximo: 20 },
    { campo: 'marca', etiqueta: 'Marca', maximo: 100 },
    { campo: 'modelo', etiqueta: 'Modelo', maximo: 100 },
    { campo: 'anio', etiqueta: 'Año', maximo: undefined },
    { campo: 'capacidadKg', etiqueta: 'Capacidad (kg)', maximo: undefined },
  ] as const;

  return (
    <section className="clientes-form-panel vehiculos-flota-form-panel" aria-labelledby="vehiculo-form-title">
      <div className="clientes-panel-heading">
        <div><div className="page-eyebrow">VEHÍCULO DE FLOTA</div>
          <h2 id="vehiculo-form-title">{vehiculo === null ? 'Nuevo vehículo' : 'Editar vehículo'}</h2></div>
        <button className="secondary-button" disabled={guardando} onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form ref={formularioRef} className="vehiculos-flota-form" noValidate onSubmit={(event) => void enviar(event)}>
        {campos.map(({ campo, etiqueta, maximo }) => {
          const id = `vehiculo-${campo}`;
          return (
            <CampoConError key={campo} etiqueta={etiqueta} htmlFor={id} error={errores[campo]} idError={`${id}-error`}>
              <input id={id} aria-describedby={errores[campo] ? `${id}-error` : undefined}
                aria-invalid={Boolean(errores[campo])} autoComplete="off" disabled={guardando}
                inputMode={campo === 'capacidadKg' ? 'decimal' : campo === 'anio' ? 'numeric' : undefined}
                maxLength={maximo} value={valores[campo]} onChange={(event) => cambiar(campo, event.currentTarget.value)} />
            </CampoConError>
          );
        })}
        <CampoConError etiqueta="Tipo" htmlFor="vehiculo-tipo" error={errores.tipo} idError="vehiculo-tipo-error">
          <select id="vehiculo-tipo" disabled={guardando} value={valores.tipo}
            aria-invalid={Boolean(errores.tipo)} aria-describedby={errores.tipo ? 'vehiculo-tipo-error' : undefined}
            onChange={(event) => cambiar('tipo', event.currentTarget.value)}>
            {['Camion', 'Furgon', 'Moto', 'Otro'].map((tipo) => <option key={tipo} value={tipo}>{nombreTipo(tipo)}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Observaciones (opcional)" htmlFor="vehiculo-observaciones" error={errores.observaciones} idError="vehiculo-observaciones-error">
          <textarea id="vehiculo-observaciones" maxLength={500} rows={3} disabled={guardando}
            aria-invalid={Boolean(errores.observaciones)} aria-describedby={errores.observaciones ? 'vehiculo-observaciones-error' : undefined}
            value={valores.observaciones} onChange={(event) => cambiar('observaciones', event.currentTarget.value)} />
        </CampoConError>
        <label className="vehiculos-flota-active-field">
          <input type="checkbox" checked={valores.activo} disabled={guardando}
            onChange={(event) => cambiar('activo', event.currentTarget.checked)} />Activo
        </label>
        <div className="vehiculos-flota-form-actions">
          <p className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar vehículo'}</button>
        </div>
      </form>
    </section>
  );
}
