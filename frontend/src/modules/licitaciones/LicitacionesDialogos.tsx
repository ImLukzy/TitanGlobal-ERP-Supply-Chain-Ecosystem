import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { formateadorMoneda } from '../../lib/formato';
import { DialogoLicitacion } from './DialogoLicitacion';
import type { LicitacionDetalle, OfertaLicitacion } from './tipos';
import { validarMejora } from './validacion';

function mensajeDe(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

function useAccion(respaldo: string, accion: () => Promise<void>) {
  const [error, setError] = useState('');
  const [ocupado, setOcupado] = useState(false);
  async function ejecutar(): Promise<void> {
    setError('');
    setOcupado(true);
    try {
      await accion();
    } catch (fallo: unknown) {
      setError(mensajeDe(fallo, respaldo));
    } finally {
      setOcupado(false);
    }
  }
  return { error, ocupado, ejecutar, setError };
}

function MensajeError({ error }: { error: string }) {
  if (error === '') {
    return null;
  }
  return <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">{error}</p>;
}

interface DialogoConfirmarProps {
  tipo: 'cerrar' | 'cancelar';
  licitacion: LicitacionDetalle;
  cerrar: () => void;
  confirmar: () => Promise<void>;
}

export function DialogoConfirmarLicitacion({ tipo, licitacion, cerrar, confirmar }: DialogoConfirmarProps) {
  const cerrando = tipo === 'cerrar';
  const { error, ocupado, ejecutar } = useAccion(cerrando ? 'No se pudo cerrar la licitación.' : 'No se pudo cancelar la licitación.', confirmar);
  return (
    <DialogoLicitacion cerrar={cerrar} titulo={cerrando ? 'Cerrar licitación' : 'Cancelar licitación'} tituloId="licitacion-confirmar-title">
      <p>
        {cerrando
          ? <>¿Cerrar la licitación <strong>{licitacion.numero}</strong>? Ya no se recibirán más ofertas.</>
          : <>¿Cancelar la licitación <strong>{licitacion.numero}</strong>? Esta acción no se puede deshacer.</>}
      </p>
      <MensajeError error={error} />
      <div className="clientes-dialog-actions">
        <button className="secondary-button" disabled={ocupado} onClick={cerrar} type="button">Volver</button>
        <button className={cerrando ? 'primary-button' : 'danger-button'} disabled={ocupado} onClick={() => void ejecutar()} type="button">
          {ocupado ? 'Procesando…' : cerrando ? 'Cerrar licitación' : 'Cancelar licitación'}
        </button>
      </div>
    </DialogoLicitacion>
  );
}

interface DialogoAdjudicarProps {
  licitacion: LicitacionDetalle;
  cerrar: () => void;
  confirmar: (ofertaId: number) => Promise<void>;
}

export function DialogoAdjudicarLicitacion({ licitacion, cerrar, confirmar }: DialogoAdjudicarProps) {
  const ofertas = licitacion.ofertas;
  const menor = ofertas.reduce<OfertaLicitacion | null>((actual, oferta) => (actual === null || oferta.monto < actual.monto ? oferta : actual), null);
  const [ofertaId, setOfertaId] = useState<number | null>(menor?.id ?? null);
  const { error, ocupado, ejecutar } = useAccion('No se pudo adjudicar la licitación.', async () => {
    if (ofertaId !== null) {
      await confirmar(ofertaId);
    }
  });
  return (
    <DialogoLicitacion cerrar={cerrar} titulo="Adjudicar licitación" tituloId="licitacion-adjudicar-title">
      {ofertas.length === 0 ? (
        <p>La licitación {licitacion.numero} no tiene ofertas para adjudicar.</p>
      ) : (
        <>
          <p>Se propone la oferta de menor monto, pero puedes elegir otra.</p>
          <fieldset className="licitacion-adjudicar-opciones">
            <legend>Oferta a adjudicar</legend>
            {ofertas.map((oferta) => (
              <label key={oferta.id}>
                <input
                  checked={ofertaId === oferta.id}
                  name="oferta-adjudicada"
                  onChange={() => setOfertaId(oferta.id)}
                  type="radio"
                  value={oferta.id}
                />
                {oferta.proveedorNombre} · {formateadorMoneda.format(oferta.monto)}
              </label>
            ))}
          </fieldset>
        </>
      )}
      <MensajeError error={error} />
      <div className="clientes-dialog-actions">
        <button className="secondary-button" disabled={ocupado} onClick={cerrar} type="button">Volver</button>
        <button className="primary-button" disabled={ocupado || ofertaId === null} onClick={() => void ejecutar()} type="button">
          {ocupado ? 'Adjudicando…' : 'Adjudicar licitación'}
        </button>
      </div>
    </DialogoLicitacion>
  );
}

interface DialogoMejorarProps {
  oferta: OfertaLicitacion;
  cerrar: () => void;
  confirmar: (monto: number) => Promise<void>;
}

export function DialogoMejorarOferta({ oferta, cerrar, confirmar }: DialogoMejorarProps) {
  const [monto, setMonto] = useState('');
  const [errorMonto, setErrorMonto] = useState('');
  const { error, ocupado, ejecutar } = useAccion('No se pudo mejorar la oferta.', () => confirmar(Number(monto)));

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const invalido = validarMejora(monto, oferta.monto);
    setErrorMonto(invalido ?? '');
    if (invalido === null) {
      await ejecutar();
    }
  }

  return (
    <DialogoLicitacion cerrar={cerrar} titulo="Mejorar oferta" tituloId="licitacion-mejorar-title">
      <p>Oferta actual de <strong>{oferta.proveedorNombre}</strong>: {formateadorMoneda.format(oferta.monto)}</p>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <div className="form-field">
          <label htmlFor="licitacion-mejora-monto">Nuevo monto</label>
          <input
            aria-describedby={errorMonto === '' ? undefined : 'licitacion-mejora-monto-error'}
            aria-invalid={errorMonto !== ''}
            id="licitacion-mejora-monto"
            inputMode="decimal"
            onChange={(event) => setMonto(event.currentTarget.value)}
            value={monto}
          />
          {errorMonto !== '' && <span className="field-error" id="licitacion-mejora-monto-error">{errorMonto}</span>}
        </div>
        <MensajeError error={error} />
        <div className="clientes-dialog-actions">
          <button className="secondary-button" disabled={ocupado} onClick={cerrar} type="button">Volver</button>
          <button className="primary-button" disabled={ocupado} type="submit">{ocupado ? 'Guardando…' : 'Guardar mejora'}</button>
        </div>
      </form>
    </DialogoLicitacion>
  );
}
