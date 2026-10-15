import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { generarOrdenDesdeSolicitud } from './api';
import type { GenerarDesdeSolicitudRequest, OrdenAbastecimientoResponse } from './tipos';
import type { ProveedorOpcion, SolicitudDetalle } from '../solicitudes-compra/tipos';

interface GenerarOrdenDialogProps {
  solicitud: SolicitudDetalle;
  proveedores: ProveedorOpcion[];
  cerrar: () => void;
  generada: (orden: OrdenAbastecimientoResponse) => void;
}

export function GenerarOrdenDialog({ solicitud, proveedores, cerrar, generada }: GenerarOrdenDialogProps) {
  const hoy = new Date().toISOString().slice(0, 10);
  const [proveedorId, setProveedorId] = useState<number | ''>(solicitud.proveedorId ?? '');
  const [fechaEntregaEstimada, setFechaEntregaEstimada] = useState(hoy);
  const [observaciones, setObservaciones] = useState('');
  const [error, setError] = useState('');
  const [ocupado, setOcupado] = useState(false);
  const dialogoRef = useRef<HTMLElement>(null);

  useEffect(() => {
    const dialogo = dialogoRef.current;
    const primerCampo = dialogo?.querySelector<HTMLElement>('select, input, textarea, button');
    primerCampo?.focus();
    function manejarTeclado(event: KeyboardEvent): void {
      if (event.key === 'Escape') {
        event.preventDefault();
        cerrar();
      }
      if (event.key === 'Tab' && dialogo !== null) {
        const elementos = Array.from(dialogo.querySelectorAll<HTMLElement>('select, input, textarea, button'));
        const primero = elementos[0];
        const ultimo = elementos[elementos.length - 1];
        if (event.shiftKey && document.activeElement === primero && ultimo !== undefined) {
          event.preventDefault();
          ultimo.focus();
        } else if (!event.shiftKey && document.activeElement === ultimo && primero !== undefined) {
          event.preventDefault();
          primero.focus();
        }
      }
    }
    document.addEventListener('keydown', manejarTeclado);
    return () => document.removeEventListener('keydown', manejarTeclado);
  }, [cerrar]);

  async function enviar(): Promise<void> {
    if (proveedorId === '') {
      setError('Indica el proveedor de la orden.');
      return;
    }
    if (fechaEntregaEstimada === '') {
      setError('La fecha de entrega estimada es obligatoria.');
      return;
    }
    if (fechaEntregaEstimada < hoy) {
      setError('La fecha de entrega estimada no puede ser anterior a hoy.');
      return;
    }
    if (observaciones.trim().length > 500) {
      setError('Las observaciones no pueden superar 500 caracteres.');
      return;
    }
    setError('');
    setOcupado(true);
    const request: GenerarDesdeSolicitudRequest = { proveedorId: Number(proveedorId), fechaEntregaEstimada };
    if (observaciones.trim() !== '') {
      request.observaciones = observaciones.trim();
    }
    try {
      const orden = await generarOrdenDesdeSolicitud(solicitud.id, request);
      generada(orden);
    } catch (cause: unknown) {
      setError(cause instanceof ApiError ? cause.message : 'No se pudo generar la orden.');
    } finally {
      setOcupado(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <section aria-labelledby="generar-orden-title" aria-modal="true" className="clientes-dialog orden-action-dialog" ref={dialogoRef} role="dialog" tabIndex={-1}>
        <span className="page-eyebrow">SOLICITUD APROBADA</span>
        <h2 id="generar-orden-title">Generar orden desde {solicitud.numero}</h2>
        <label className="orden-dialog-field" htmlFor="generar-orden-proveedor">
          Proveedor
          <select id="generar-orden-proveedor" onChange={(event) => setProveedorId(event.currentTarget.value === '' ? '' : Number(event.currentTarget.value))} value={proveedorId}>
            <option value="">Selecciona proveedor</option>
            {proveedores.map((proveedor) => <option key={proveedor.id} value={proveedor.id}>{proveedor.razonSocial}</option>)}
          </select>
        </label>
        <label className="orden-dialog-field" htmlFor="generar-orden-fecha">
          Fecha de entrega estimada
          <input id="generar-orden-fecha" min={hoy} onChange={(event) => setFechaEntregaEstimada(event.currentTarget.value)} type="date" value={fechaEntregaEstimada} />
        </label>
        <label className="orden-dialog-field" htmlFor="generar-orden-observaciones">
          Observaciones
          <textarea id="generar-orden-observaciones" maxLength={500} onChange={(event) => setObservaciones(event.currentTarget.value)} value={observaciones} />
        </label>
        {error && <p className="orden-dialog-error" role="alert">{error}</p>}
        <div className="clientes-dialog-actions">
          <button className="secondary-button" disabled={ocupado} onClick={cerrar} type="button">Cancelar</button>
          <button className="primary-button" disabled={ocupado} onClick={() => void enviar()} type="button">{ocupado ? 'Generando…' : 'Generar orden'}</button>
        </div>
      </section>
    </div>
  );
}
