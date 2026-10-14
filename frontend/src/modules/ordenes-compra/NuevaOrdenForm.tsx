import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { crearOrdenCompra } from './api';
import type { CrearOrdenCompraRequest } from './tipos';

interface ErroresFormulario {
  numeroOrden?: string;
  clienteId?: string;
  total?: string;
}

export function NuevaOrdenForm() {
  const [numeroOrden, setNumeroOrden] = useState('');
  const [clienteId, setClienteId] = useState('');
  const [total, setTotal] = useState('');
  const [descripcion, setDescripcion] = useState('');
  const [errores, setErrores] = useState<ErroresFormulario>({});
  const [mensaje, setMensaje] = useState('');
  const [esError, setEsError] = useState(false);
  const [enviando, setEnviando] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');

    const nuevoError: ErroresFormulario = {};
    const clienteNumero = Number(clienteId);
    const totalNumero = Number(total);
    if (numeroOrden.trim() === '') {
      nuevoError.numeroOrden = 'El número de orden es obligatorio.';
    }
    if (clienteId.trim() === '' || !Number.isInteger(clienteNumero) || clienteNumero <= 0) {
      nuevoError.clienteId = 'El cliente debe ser un entero mayor que 0.';
    }
    if (total.trim() === '' || !Number.isFinite(totalNumero) || totalNumero < 0) {
      nuevoError.total = 'El total debe ser un número mayor o igual a 0.';
    }
    setErrores(nuevoError);

    if (Object.keys(nuevoError).length > 0) {
      setEsError(true);
      return;
    }

    const request: CrearOrdenCompraRequest = {
      numeroOrden: numeroOrden.trim(),
      clienteId: clienteNumero,
      total: totalNumero,
    };
    if (descripcion.trim() !== '') {
      request.descripcion = descripcion.trim();
    }

    setEnviando(true);
    setEsError(false);
    try {
      const response = await crearOrdenCompra(request);
      setMensaje(`Orden creada con Id ${response.id}`);
      setNumeroOrden('');
      setClienteId('');
      setTotal('');
      setDescripcion('');
      setErrores({});
    } catch (error: unknown) {
      setEsError(true);
      setMensaje(error instanceof ApiError ? error.message : 'No se pudo crear la orden.');
    } finally {
      setEnviando(false);
    }
  }

  return (
    <section aria-labelledby="nueva-orden-title" className="business-section">
      <div className="business-section-heading">
        <div>
          <div className="page-eyebrow">OPERACIÓN</div>
          <h2 id="nueva-orden-title">Nueva orden</h2>
        </div>
        <span className="section-note">Registro de compra</span>
      </div>
      <form className="order-form" noValidate onSubmit={(event) => void handleSubmit(event)}>
        <div className="form-grid">
          <div className="form-field">
            <label htmlFor="numeroOrden">Número de orden <span aria-hidden="true">*</span></label>
            <input
              aria-describedby={errores.numeroOrden ? 'numeroOrden-error' : undefined}
              aria-invalid={Boolean(errores.numeroOrden)}
              autoComplete="off"
              id="numeroOrden"
              name="numeroOrden"
              onChange={(event) => setNumeroOrden(event.currentTarget.value)}
              placeholder="OC-2026-001"
              value={numeroOrden}
            />
            {errores.numeroOrden && <span className="field-error" id="numeroOrden-error">{errores.numeroOrden}</span>}
          </div>
          <div className="form-field">
            <label htmlFor="clienteId">Cliente ID <span aria-hidden="true">*</span></label>
            <input
              aria-describedby={errores.clienteId ? 'clienteId-error' : undefined}
              aria-invalid={Boolean(errores.clienteId)}
              id="clienteId"
              inputMode="numeric"
              min="1"
              name="clienteId"
              onChange={(event) => setClienteId(event.currentTarget.value)}
              placeholder="1"
              type="number"
              value={clienteId}
            />
            {errores.clienteId && <span className="field-error" id="clienteId-error">{errores.clienteId}</span>}
          </div>
          <div className="form-field">
            <label htmlFor="total">Total <span aria-hidden="true">*</span></label>
            <div className="input-prefix"><span aria-hidden="true">S/</span><input
              aria-describedby={errores.total ? 'total-error' : undefined}
              aria-invalid={Boolean(errores.total)}
              id="total"
              min="0"
              name="total"
              onChange={(event) => setTotal(event.currentTarget.value)}
              placeholder="0.00"
              step="0.01"
              type="number"
              value={total}
            /></div>
            {errores.total && <span className="field-error" id="total-error">{errores.total}</span>}
          </div>
          <div className="form-field form-field-description">
            <label htmlFor="descripcion">Descripción <span className="optional-label">Opcional</span></label>
            <input
              id="descripcion"
              name="descripcion"
              onChange={(event) => setDescripcion(event.currentTarget.value)}
              placeholder="Detalle adicional de la compra"
              value={descripcion}
            />
          </div>
        </div>
        <div className="form-actions">
          <p aria-live="polite" className={`form-feedback${esError ? ' is-error' : ' is-success'}`} role="status">{mensaje}</p>
          <button className="primary-button" disabled={enviando} type="submit">
            {enviando ? 'Creando…' : 'Crear orden'}
          </button>
        </div>
      </form>
    </section>
  );
}
