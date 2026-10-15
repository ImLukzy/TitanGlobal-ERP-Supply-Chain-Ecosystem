import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import type { OpcionCatalogo, OpcionSegmento, PromocionDetalle, TipoPromocion, GuardarPromocionRequest } from './tipos';

interface Props { promocion: PromocionDetalle | null; productos: (OpcionCatalogo & { precio: number })[]; segmentos: OpcionSegmento[]; cancelar: () => void; guardar: (request: GuardarPromocionRequest) => Promise<void>; }
type Campo = 'codigo' | 'nombre' | 'productoId' | 'montoMinimo' | 'porcentajeDescuento' | 'fechaInicio' | 'fechaFin';
type Errores = Partial<Record<Campo, string>>;

function fechaActual(): string { return new Date().toISOString().slice(0, 10); }

export function PromocionFormulario({ promocion, productos, segmentos, cancelar, guardar }: Props) {
  const [codigo, setCodigo] = useState(promocion?.codigo ?? '');
  const [nombre, setNombre] = useState(promocion?.nombre ?? '');
  const [tipo, setTipo] = useState<TipoPromocion>(promocion?.tipo ?? 'Producto');
  const [productoId, setProductoId] = useState(promocion?.productoId ?? 0);
  const [montoMinimo, setMontoMinimo] = useState(promocion?.montoMinimo ?? 0);
  const [porcentaje, setPorcentaje] = useState(promocion?.porcentajeDescuento ?? 0);
  const [segmentoId, setSegmentoId] = useState(promocion?.segmentoId ?? 0);
  const [fechaInicio, setFechaInicio] = useState(promocion?.fechaInicio.slice(0, 10) ?? fechaActual());
  const [fechaFin, setFechaFin] = useState(promocion?.fechaFin.slice(0, 10) ?? `${new Date().getUTCFullYear()}-12-31`);
  const [activa, setActiva] = useState(promocion?.activa ?? true);
  const [errores, setErrores] = useState<Errores>({});
  const [errorApi, setErrorApi] = useState('');
  const [ocupado, setOcupado] = useState(false);

  function cambiarTipo(siguiente: TipoPromocion): void {
    setTipo(siguiente);
    if (siguiente === 'Producto') setMontoMinimo(0);
    else setProductoId(0);
  }

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    const siguientes: Errores = {};
    const codigoNormalizado = codigo.trim().toUpperCase();
    const nombreNormalizado = nombre.trim();
    if (!codigoNormalizado) siguientes.codigo = 'El código es obligatorio.';
    else if (!/^[A-Z0-9-]{2,20}$/.test(codigoNormalizado)) siguientes.codigo = 'El código solo admite letras, números y guiones (2 a 20 caracteres).';
    if (!nombreNormalizado) siguientes.nombre = 'El nombre de la promoción es obligatorio.';
    else if (nombreNormalizado.length > 100) siguientes.nombre = 'El nombre de la promoción no puede superar 100 caracteres.';
    if (tipo === 'Producto' && !productoId) siguientes.productoId = 'La promoción de producto requiere un producto.';
    if (tipo === 'MontoMinimo' && montoMinimo <= 0) siguientes.montoMinimo = 'El monto mínimo debe ser mayor que 0.';
    if (porcentaje <= 0 || porcentaje > 100) siguientes.porcentajeDescuento = 'El descuento debe ser mayor que 0 y como máximo 100.';
    if (fechaFin < fechaInicio) siguientes.fechaFin = 'La fecha de fin no puede ser anterior a la de inicio.';
    setErrores(siguientes);
    if (Object.keys(siguientes).length > 0) return;
    setOcupado(true);
    setErrorApi('');
    try {
      await guardar({ codigo: codigoNormalizado, nombre: nombreNormalizado, tipo, productoId: tipo === 'Producto' ? productoId : undefined, montoMinimo: tipo === 'MontoMinimo' ? montoMinimo : undefined, porcentajeDescuento: porcentaje, segmentoClienteId: segmentoId || undefined, fechaInicio, fechaFin, activa });
    } catch (error: unknown) {
      setErrorApi(error instanceof ApiError ? error.message : 'No se pudo guardar la promoción.');
    } finally {
      setOcupado(false);
    }
  }

  return <div className="clientes-dialog-backdrop"><div aria-labelledby="promocion-form-title" aria-modal="true" className="clientes-dialog promocion-form-dialog" role="dialog">
    <h2 id="promocion-form-title">{promocion ? 'Editar promoción' : 'Nueva promoción'}</h2>
    <form noValidate onSubmit={(evento) => void enviar(evento)}>
      <CampoConError error={errores.codigo} htmlFor="promocion-codigo" etiqueta="Código" idError="promocion-codigo-error"><input aria-describedby={errores.codigo ? 'promocion-codigo-error' : undefined} aria-invalid={Boolean(errores.codigo)} id="promocion-codigo" maxLength={20} onChange={(evento) => setCodigo(evento.currentTarget.value.toUpperCase())} value={codigo} /></CampoConError>
      <CampoConError error={errores.nombre} htmlFor="promocion-nombre" etiqueta="Nombre" idError="promocion-nombre-error"><input aria-describedby={errores.nombre ? 'promocion-nombre-error' : undefined} aria-invalid={Boolean(errores.nombre)} id="promocion-nombre" maxLength={100} onChange={(evento) => setNombre(evento.currentTarget.value)} value={nombre} /></CampoConError>
      <fieldset className="promocion-tipos"><legend>Tipo de promoción</legend><label><input checked={tipo === 'Producto'} name="tipo-promocion" onChange={() => cambiarTipo('Producto')} type="radio" />Por producto</label><label><input checked={tipo === 'MontoMinimo'} name="tipo-promocion" onChange={() => cambiarTipo('MontoMinimo')} type="radio" />Por monto mínimo</label></fieldset>
      {tipo === 'Producto' ? <CampoConError error={errores.productoId} htmlFor="promocion-producto" etiqueta="Producto" idError="promocion-producto-error"><select aria-describedby={errores.productoId ? 'promocion-producto-error' : undefined} aria-invalid={Boolean(errores.productoId)} id="promocion-producto" onChange={(evento) => setProductoId(Number(evento.currentTarget.value))} value={productoId || ''}><option value="">Selecciona un producto</option>{productos.map((producto) => <option key={producto.id} value={producto.id}>{producto.nombre}</option>)}</select></CampoConError> : <CampoConError error={errores.montoMinimo} htmlFor="promocion-monto" etiqueta="Monto mínimo" idError="promocion-monto-error"><input aria-describedby={errores.montoMinimo ? 'promocion-monto-error' : undefined} aria-invalid={Boolean(errores.montoMinimo)} id="promocion-monto" min="0" onChange={(evento) => setMontoMinimo(Number(evento.currentTarget.value))} step="0.01" type="number" value={montoMinimo || ''} /></CampoConError>}
      <div className="promocion-form-row"><CampoConError error={errores.porcentajeDescuento} htmlFor="promocion-porcentaje" etiqueta="Porcentaje de descuento" idError="promocion-porcentaje-error"><input aria-describedby={errores.porcentajeDescuento ? 'promocion-porcentaje-error' : undefined} aria-invalid={Boolean(errores.porcentajeDescuento)} id="promocion-porcentaje" min="0" max="100" onChange={(evento) => setPorcentaje(Number(evento.currentTarget.value))} step="0.01" type="number" value={porcentaje || ''} /></CampoConError><CampoConError error="" htmlFor="promocion-segmento" etiqueta="Segmento (opcional)" idError="promocion-segmento-error"><select id="promocion-segmento" onChange={(evento) => setSegmentoId(Number(evento.currentTarget.value))} value={segmentoId || ''}><option value="">Todos los clientes</option>{segmentos.map((segmento) => <option key={segmento.id} value={segmento.id}>{segmento.codigo} · {segmento.nombre}</option>)}</select></CampoConError></div>
      <div className="promocion-form-row"><CampoConError error={errores.fechaInicio} htmlFor="promocion-inicio" etiqueta="Fecha de inicio" idError="promocion-inicio-error"><input id="promocion-inicio" onChange={(evento) => setFechaInicio(evento.currentTarget.value)} type="date" value={fechaInicio} /></CampoConError><CampoConError error={errores.fechaFin} htmlFor="promocion-fin" etiqueta="Fecha de fin" idError="promocion-fin-error"><input aria-describedby={errores.fechaFin ? 'promocion-fin-error' : undefined} aria-invalid={Boolean(errores.fechaFin)} id="promocion-fin" onChange={(evento) => setFechaFin(evento.currentTarget.value)} type="date" value={fechaFin} /></CampoConError></div>
      <label className="promociones-checkbox"><input checked={activa} onChange={(evento) => setActiva(evento.currentTarget.checked)} type="checkbox" />Activa</label>
      {errorApi && <p className="solicitudes-error" role="alert">{errorApi}</p>}
      <div className="clientes-dialog-actions"><button className="secondary-button" onClick={cancelar} type="button">Cancelar</button><button className="primary-button" disabled={ocupado} type="submit">{ocupado ? 'Guardando…' : 'Guardar promoción'}</button></div>
    </form>
  </div></div>;
}
