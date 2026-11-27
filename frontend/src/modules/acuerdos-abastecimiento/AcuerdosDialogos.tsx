import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { DialogoAcuerdo } from './DialogoAcuerdo';
import type { AcuerdoDetalle, LineaAcuerdo } from './tipos';
import { validarConsumo } from './validacion';

function useAccion(respaldo: string, accion: () => Promise<void>) {
  const [error, setError] = useState('');
  const [ocupado, setOcupado] = useState(false);
  async function ejecutar(): Promise<void> {
    setError('');
    setOcupado(true);
    try {
      await accion();
    } catch (fallo: unknown) {
      setError(fallo instanceof ApiError ? fallo.message : respaldo);
    } finally {
      setOcupado(false);
    }
  }
  return { error, ocupado, ejecutar };
}

function MensajeError({ error }: { error: string }) {
  if (error === '') {
    return null;
  }
  return <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">{error}</p>;
}

export type TipoConfirmacion = 'suspender' | 'reactivar' | 'cancelar' | 'quitar';

const textos: Record<TipoConfirmacion, { titulo: string; boton: string; respaldo: string; peligro: boolean }> = {
  suspender: { titulo: 'Suspender acuerdo', boton: 'Suspender acuerdo', respaldo: 'No se pudo suspender el acuerdo.', peligro: false },
  reactivar: { titulo: 'Reactivar acuerdo', boton: 'Reactivar acuerdo', respaldo: 'No se pudo reactivar el acuerdo.', peligro: false },
  cancelar: { titulo: 'Cancelar acuerdo', boton: 'Cancelar acuerdo', respaldo: 'No se pudo cancelar el acuerdo.', peligro: true },
  quitar: { titulo: 'Quitar línea', boton: 'Quitar línea', respaldo: 'No se pudo quitar la línea.', peligro: true },
};

interface DialogoConfirmarProps {
  tipo: TipoConfirmacion;
  acuerdo: AcuerdoDetalle;
  linea: LineaAcuerdo | null;
  cerrar: () => void;
  confirmar: () => Promise<void>;
}

export function DialogoConfirmarAcuerdo({ tipo, acuerdo, linea, cerrar, confirmar }: DialogoConfirmarProps) {
  const texto = textos[tipo];
  const { error, ocupado, ejecutar } = useAccion(texto.respaldo, confirmar);
  return (
    <DialogoAcuerdo cerrar={cerrar} titulo={texto.titulo} tituloId="acuerdo-confirmar-title">
      <p>
        {tipo === 'suspender' && <>¿Suspender el acuerdo <strong>{acuerdo.numero}</strong>? No se podrá registrar consumo hasta reactivarlo.</>}
        {tipo === 'reactivar' && <>¿Reactivar el acuerdo <strong>{acuerdo.numero}</strong>?</>}
        {tipo === 'cancelar' && <>¿Cancelar el acuerdo <strong>{acuerdo.numero}</strong>? Esta acción no se puede deshacer.</>}
        {tipo === 'quitar' && <>¿Quitar la línea de <strong>{linea?.productoNombre ?? ''}</strong> del acuerdo <strong>{acuerdo.numero}</strong>?</>}
      </p>
      <MensajeError error={error} />
      <div className="clientes-dialog-actions">
        <button className="secondary-button" disabled={ocupado} onClick={cerrar} type="button">Volver</button>
        <button className={texto.peligro ? 'danger-button' : 'primary-button'} disabled={ocupado} onClick={() => void ejecutar()} type="button">
          {ocupado ? 'Procesando…' : texto.boton}
        </button>
      </div>
    </DialogoAcuerdo>
  );
}

interface DialogoConsumoProps {
  linea: LineaAcuerdo;
  cerrar: () => void;
  confirmar: (cantidad: number) => Promise<void>;
}

export function DialogoConsumo({ linea, cerrar, confirmar }: DialogoConsumoProps) {
  const [cantidad, setCantidad] = useState('');
  const [errorCantidad, setErrorCantidad] = useState('');
  const { error, ocupado, ejecutar } = useAccion('No se pudo registrar el consumo.', () => confirmar(Number(cantidad)));

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    const invalido = validarConsumo(cantidad, linea.saldo);
    setErrorCantidad(invalido ?? '');
    if (invalido === null) {
      await ejecutar();
    }
  }

  return (
    <DialogoAcuerdo cerrar={cerrar} titulo="Registrar consumo" tituloId="acuerdo-consumo-title">
      <p>Producto: <strong>{linea.productoNombre}</strong></p>
      <p>Saldo disponible: <strong>{linea.saldo}</strong></p>
      <form noValidate onSubmit={(evento) => void enviar(evento)}>
        <div className="form-field">
          <label htmlFor="acuerdo-consumo-cantidad">Cantidad a consumir</label>
          <input
            aria-describedby={errorCantidad === '' ? undefined : 'acuerdo-consumo-cantidad-error'}
            aria-invalid={errorCantidad !== ''}
            id="acuerdo-consumo-cantidad"
            inputMode="numeric"
            onChange={(evento) => setCantidad(evento.currentTarget.value)}
            value={cantidad}
          />
          {errorCantidad !== '' && <span className="field-error" id="acuerdo-consumo-cantidad-error">{errorCantidad}</span>}
        </div>
        <MensajeError error={error} />
        <div className="clientes-dialog-actions">
          <button className="secondary-button" disabled={ocupado} onClick={cerrar} type="button">Volver</button>
          <button className="primary-button" disabled={ocupado} type="submit">{ocupado ? 'Guardando…' : 'Guardar consumo'}</button>
        </div>
      </form>
    </DialogoAcuerdo>
  );
}
