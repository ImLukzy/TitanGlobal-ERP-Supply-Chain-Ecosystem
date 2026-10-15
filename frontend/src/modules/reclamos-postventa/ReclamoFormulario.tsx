import { useEffect, useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import type { ClienteOpcion, GuardarReclamoRequest, OrdenOpcion, PrioridadReclamo, ReclamoDetalle, TipoReclamo } from './tipos';

export function ReclamoFormulario({ reclamo, clientes, cargarOrdenes, cancelar, guardar }: {
  reclamo: ReclamoDetalle | null;
  clientes: ClienteOpcion[];
  cargarOrdenes: (clienteId: number) => Promise<OrdenOpcion[]>;
  cancelar: () => void;
  guardar: (request: GuardarReclamoRequest) => Promise<void>;
}) {
  const [clienteId, setClienteId] = useState<number | ''>(reclamo?.clienteId ?? '');
  const [ordenId, setOrdenId] = useState<number | ''>(reclamo?.ordenCompraId ?? '');
  const [ordenes, setOrdenes] = useState<OrdenOpcion[]>([]);
  const [productoId, setProductoId] = useState<number | ''>('');
  const [tipo, setTipo] = useState<TipoReclamo>(reclamo?.tipo ?? 'Defecto');
  const [prioridad, setPrioridad] = useState<PrioridadReclamo>(reclamo?.prioridad ?? 'Media');
  const [descripcion, setDescripcion] = useState(reclamo?.descripcion ?? '');
  const [errores, setErrores] = useState<Record<string, string>>({});
  const [errorApi, setErrorApi] = useState('');
  const [ocupado, setOcupado] = useState(false);

  useEffect(() => {
    if (reclamo === null) return;
    void cargarOrdenes(reclamo.clienteId).then((opciones) => {
      setOrdenes(opciones);
      const producto = opciones.flatMap((orden) => orden.productos).find((item) => item.nombre === reclamo.productoNombre);
      setProductoId(producto?.id ?? '');
    });
  }, [cargarOrdenes, reclamo]);

  async function cambiarCliente(valor: string): Promise<void> {
    const id = valor === '' ? '' : Number(valor);
    setClienteId(id);
    setOrdenId('');
    setProductoId('');
    setOrdenes([]);
    if (id !== '') setOrdenes(await cargarOrdenes(id));
  }

  function validar(): Record<string, string> {
    const siguientes: Record<string, string> = {};
    if (clienteId === '') siguientes.cliente = 'Selecciona un cliente.';
    if (ordenId === '') siguientes.orden = 'Selecciona una orden de compra.';
    if (descripcion.trim() === '') siguientes.descripcion = 'La descripción es obligatoria.';
    if (descripcion.trim().length > 1000) siguientes.descripcion = 'La descripción no puede superar 1000 caracteres.';
    return siguientes;
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const siguientes = validar();
    setErrores(siguientes);
    if (Object.keys(siguientes).length > 0) return;
    const request: GuardarReclamoRequest = { clienteId: Number(clienteId), ordenCompraId: Number(ordenId), tipo, prioridad, descripcion: descripcion.trim() };
    if (productoId !== '') request.productoId = Number(productoId);
    setErrorApi('');
    setOcupado(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      setErrorApi(error instanceof ApiError ? error.message : 'No se pudo guardar el reclamo.');
    } finally {
      setOcupado(false);
    }
  }

  const productos = ordenes.find((orden) => orden.id === ordenId)?.productos ?? [];
  const plazo = prioridad === 'Alta' ? 2 : prioridad === 'Media' ? 5 : 10;
  return (
    <section aria-labelledby="reclamo-form-title" className="reclamos-panel reclamo-form">
      <div className="reclamos-panel-heading"><div><span className="page-eyebrow">RECLAMO POSTVENTA</span><h2 id="reclamo-form-title">{reclamo ? `Editar ${reclamo.numero}` : 'Nuevo reclamo'}</h2></div><button className="secondary-button" onClick={cancelar} type="button">Cancelar</button></div>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <div className="reclamo-form-fields">
          <CampoConError etiqueta="Cliente" htmlFor="reclamo-cliente" error={errores.cliente} idError="reclamo-cliente-error"><select aria-describedby={errores.cliente ? 'reclamo-cliente-error' : undefined} aria-invalid={Boolean(errores.cliente)} id="reclamo-cliente" onChange={(event) => void cambiarCliente(event.currentTarget.value)} value={clienteId}><option value="">Selecciona cliente</option>{clientes.map((cliente) => <option key={cliente.id} value={cliente.id}>{cliente.nombre}</option>)}</select></CampoConError>
          <CampoConError etiqueta="Orden de compra" htmlFor="reclamo-orden" error={errores.orden} idError="reclamo-orden-error"><select aria-describedby={errores.orden ? 'reclamo-orden-error' : undefined} aria-invalid={Boolean(errores.orden)} id="reclamo-orden" onChange={(event) => { setOrdenId(event.currentTarget.value === '' ? '' : Number(event.currentTarget.value)); setProductoId(''); }} value={ordenId}><option value="">Selecciona orden</option>{ordenes.map((orden) => <option key={orden.id} value={orden.id}>{orden.numero}</option>)}</select></CampoConError>
          <CampoConError etiqueta="Producto (opcional)" htmlFor="reclamo-producto" idError="reclamo-producto-error"><select id="reclamo-producto" onChange={(event) => setProductoId(event.currentTarget.value === '' ? '' : Number(event.currentTarget.value))} value={productoId}><option value="">Sin producto específico</option>{productos.map((producto) => <option key={producto.id} value={producto.id}>{producto.nombre}</option>)}</select></CampoConError>
          <CampoConError etiqueta="Tipo" htmlFor="reclamo-tipo" idError="reclamo-tipo-error"><select id="reclamo-tipo" onChange={(event) => setTipo(event.currentTarget.value as TipoReclamo)} value={tipo}><option>Defecto</option><option>Retraso</option><option>Faltante</option><option value="Facturacion">Facturación</option><option>Otro</option></select></CampoConError>
          <CampoConError etiqueta="Prioridad" htmlFor="reclamo-prioridad" idError="reclamo-prioridad-error"><select id="reclamo-prioridad" onChange={(event) => setPrioridad(event.currentTarget.value as PrioridadReclamo)} value={prioridad}><option>Alta</option><option>Media</option><option>Baja</option></select><small>Plazo: {plazo} días</small></CampoConError>
          <CampoConError etiqueta="Descripción" htmlFor="reclamo-descripcion" error={errores.descripcion} idError="reclamo-descripcion-error"><textarea aria-describedby={errores.descripcion ? 'reclamo-descripcion-error' : undefined} aria-invalid={Boolean(errores.descripcion)} id="reclamo-descripcion" maxLength={1000} onChange={(event) => setDescripcion(event.currentTarget.value)} value={descripcion} /></CampoConError>
        </div>
        {errorApi && <p className="clientes-error" role="alert">{errorApi}</p>}
        <div className="reclamo-form-actions"><button className="primary-button" disabled={ocupado} type="submit">{ocupado ? 'Guardando…' : 'Guardar reclamo'}</button></div>
      </form>
    </section>
  );
}
