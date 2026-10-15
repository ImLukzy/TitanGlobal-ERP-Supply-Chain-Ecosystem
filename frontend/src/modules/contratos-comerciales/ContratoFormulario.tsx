import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import type { ClienteOpcion, CondicionPago, ContratoDetalle, GuardarContratoRequest } from './tipos';

interface Props { clientes: ClienteOpcion[]; contrato: ContratoDetalle | null; cancelar: () => void; guardar: (request: GuardarContratoRequest) => Promise<void>; }
type Errores = Partial<Record<'clienteId' | 'titulo' | 'fechaFin' | 'montoComprometido' | 'condicionPago' | 'observaciones', string>>;

export function ContratoFormulario({ clientes, contrato, cancelar, guardar }: Props) {
  const [clienteId, setClienteId] = useState(contrato?.clienteId ?? 0);
  const [titulo, setTitulo] = useState(contrato?.titulo ?? '');
  const [fechaInicio, setFechaInicio] = useState(contrato?.fechaInicio.slice(0, 10) ?? '2026-01-01');
  const [fechaFin, setFechaFin] = useState(contrato?.fechaFin.slice(0, 10) ?? '2026-12-31');
  const [monto, setMonto] = useState(contrato?.montoComprometido ?? 0);
  const [condicion, setCondicion] = useState<CondicionPago>(contrato?.condicionPago ?? 'Contado');
  const [observaciones, setObservaciones] = useState(contrato?.observaciones ?? '');
  const [errores, setErrores] = useState<Errores>({});
  const [errorApi, setErrorApi] = useState('');
  const [ocupado, setOcupado] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const siguientes: Errores = {};
    if (!clienteId) siguientes.clienteId = 'Selecciona un cliente.';
    if (!titulo.trim()) siguientes.titulo = 'El título es obligatorio.';
    else if (titulo.trim().length > 200) siguientes.titulo = 'El título no puede superar 200 caracteres.';
    if (fechaInicio && fechaFin && fechaFin <= fechaInicio) siguientes.fechaFin = 'La fecha de fin debe ser posterior a la de inicio.';
    if (monto <= 0) siguientes.montoComprometido = 'El monto comprometido debe ser mayor que 0.';
    if (!['Contado', 'Credito30', 'Credito60', 'Credito90'].includes(condicion)) siguientes.condicionPago = 'La condición de pago no es válida.';
    if (observaciones.trim().length > 500) siguientes.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
    setErrores(siguientes);
    if (Object.keys(siguientes).length) return;
    setOcupado(true);
    setErrorApi('');
    try {
      await guardar({ clienteId, titulo: titulo.trim(), fechaInicio, fechaFin, montoComprometido: monto, condicionPago: condicion, observaciones: observaciones.trim() || undefined });
    } catch (cause: unknown) {
      setErrorApi(cause instanceof ApiError ? cause.message : 'No se pudo guardar el contrato.');
    } finally {
      setOcupado(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <div aria-labelledby="contrato-form-title" aria-modal="true" className="clientes-dialog contrato-form-dialog" role="dialog">
        <h2 id="contrato-form-title">{contrato ? 'Editar contrato' : 'Nuevo contrato comercial'}</h2>
        <form noValidate onSubmit={(event) => void enviar(event)}>
          <CampoConError error={errores.clienteId} htmlFor="contrato-cliente" etiqueta="Cliente" idError="contrato-cliente-error">
            <select aria-describedby={errores.clienteId ? 'contrato-cliente-error' : undefined} aria-invalid={Boolean(errores.clienteId)} id="contrato-cliente" onChange={(event) => setClienteId(Number(event.currentTarget.value))} value={clienteId || ''}><option value="">Selecciona un cliente</option>{clientes.map((cliente) => <option key={cliente.id} value={cliente.id}>{cliente.nombre}</option>)}</select>
          </CampoConError>
          <CampoConError error={errores.titulo} htmlFor="contrato-titulo" etiqueta="Título" idError="contrato-titulo-error">
            <input aria-describedby={errores.titulo ? 'contrato-titulo-error' : undefined} aria-invalid={Boolean(errores.titulo)} id="contrato-titulo" maxLength={220} onChange={(event) => setTitulo(event.currentTarget.value)} value={titulo} />
          </CampoConError>
          <div className="contrato-form-row">
            <CampoConError error="" htmlFor="contrato-inicio" etiqueta="Fecha de inicio" idError="contrato-inicio-error"><input id="contrato-inicio" onChange={(event) => setFechaInicio(event.currentTarget.value)} type="date" value={fechaInicio} /></CampoConError>
            <CampoConError error={errores.fechaFin} htmlFor="contrato-fin" etiqueta="Fecha de fin" idError="contrato-fin-error"><input aria-describedby={errores.fechaFin ? 'contrato-fin-error' : undefined} aria-invalid={Boolean(errores.fechaFin)} id="contrato-fin" onChange={(event) => setFechaFin(event.currentTarget.value)} type="date" value={fechaFin} /></CampoConError>
          </div>
          <div className="contrato-form-row">
            <CampoConError error={errores.montoComprometido} htmlFor="contrato-monto" etiqueta="Monto comprometido" idError="contrato-monto-error"><input aria-describedby={errores.montoComprometido ? 'contrato-monto-error' : undefined} aria-invalid={Boolean(errores.montoComprometido)} id="contrato-monto" min="0" onChange={(event) => setMonto(Number(event.currentTarget.value))} step="0.01" type="number" value={monto || ''} /></CampoConError>
            <CampoConError error={errores.condicionPago} htmlFor="contrato-condicion" etiqueta="Condición de pago" idError="contrato-condicion-error"><select aria-describedby={errores.condicionPago ? 'contrato-condicion-error' : undefined} aria-invalid={Boolean(errores.condicionPago)} id="contrato-condicion" onChange={(event) => setCondicion(event.currentTarget.value as CondicionPago)} value={condicion}><option>Contado</option><option>Credito30</option><option>Credito60</option><option>Credito90</option></select></CampoConError>
          </div>
          <CampoConError error={errores.observaciones} htmlFor="contrato-observaciones" etiqueta="Observaciones" idError="contrato-observaciones-error"><textarea aria-describedby={errores.observaciones ? 'contrato-observaciones-error' : undefined} aria-invalid={Boolean(errores.observaciones)} id="contrato-observaciones" maxLength={520} onChange={(event) => setObservaciones(event.currentTarget.value)} value={observaciones} /></CampoConError>
          {errorApi && <p className="solicitudes-error" role="alert">{errorApi}</p>}
          <div className="clientes-dialog-actions"><button className="secondary-button" onClick={cancelar} type="button">Cancelar</button><button className="primary-button" disabled={ocupado} type="submit">{ocupado ? 'Guardando…' : 'Guardar contrato'}</button></div>
        </form>
      </div>
    </div>
  );
}
