import { useCallback, useState, type FormEvent } from 'react';
import { CampoModulo } from '../../components/CampoModulo';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { mensajeApi, useConsultaPantalla } from '../../lib/consultaPantalla';
import { diaDePeru } from '../../lib/fechaNegocioPeru';
import { anularFactura, emitirFactura, guardarFactura, listarFacturas, obtenerFactura, resumenFacturas } from './api';
import { FormularioFactura } from './FormularioFactura';
import type { FacturaDetalle, FiltrosFactura, GuardarFactura } from './tipos';

const inicial: FiltrosFactura = { busqueda: '', clienteId: '', tipoComprobante: '', estado: '', moneda: '', fechaDesde: '', fechaHasta: '' };
const columnas = [
  { id: 'numero', etiqueta: 'Comprobante' }, { id: 'cliente', etiqueta: 'Cliente' }, { id: 'emision', etiqueta: 'Emisión' },
  { id: 'estado', etiqueta: 'Estado' }, { id: 'total', etiqueta: 'Total' },
];

function importe(cifra: number, moneda: string) {
  return new Intl.NumberFormat('es-PE', { style: 'currency', currency: moneda }).format(cifra);
}

export function FacturacionElectronicaPage() {
  const [filtros, cambiarFiltros] = useState(inicial);
  const [pagina, cambiarPagina] = useState(1);
  const [id, seleccionar] = useState<number | null>(null);
  const [modo, cambiarModo] = useState<'alta' | 'editar' | 'emitir' | 'anular' | null>(null);
  const [revision, revisar] = useState(0);
  const [ocupado, bloquear] = useState(false);
  const [mensaje, avisar] = useState('');
  const [error, avisarError] = useState('');
  const cargar = useCallback(() => listarFacturas(filtros, pagina), [filtros, pagina]);
  const cargarDetalle = useCallback(() => obtenerFactura(id ?? 0), [id]);
  const { moneda, fechaDesde, fechaHasta } = filtros;
  const cargarResumen = useCallback(() => resumenFacturas(moneda, fechaDesde, fechaHasta), [moneda, fechaDesde, fechaHasta]);
  const lista = useConsultaPantalla(cargar, revision);
  const detalle = useConsultaPantalla(id === null ? null : cargarDetalle, revision);
  const resumen = useConsultaPantalla(cargarResumen, revision);
  const factura = detalle.datos;

  function filtrar(clave: keyof FiltrosFactura, valor: string) {
    cambiarFiltros(actual => ({ ...actual, [clave]: valor }));
    cambiarPagina(1);
  }

  function abrir(valor: typeof modo) {
    cambiarModo(valor);
    avisar('');
    avisarError('');
  }

  async function mutar(operacion: () => Promise<FacturaDetalle>, exito: string) {
    bloquear(true);
    avisarError('');
    try {
      const resultado = await operacion();
      seleccionar(resultado.id);
      cambiarModo(null);
      revisar(valor => valor + 1);
      avisar(exito);
    } catch (causa) {
      avisarError(mensajeApi(causa));
    } finally {
      bloquear(false);
    }
  }

  function guardar(datos: GuardarFactura) {
    void mutar(() => guardarFactura(datos, modo === 'editar' ? id ?? undefined : undefined), 'Borrador guardado.');
  }

  function anular(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const motivo = String(new FormData(evento.currentTarget).get('motivo') ?? '').trim();
    if (motivo === '') {
      avisarError('El motivo de anulación es obligatorio.');
      return;
    }
    if (id !== null) void mutar(() => anularFactura(id, motivo), 'Comprobante anulado.');
  }

  return <PaginaModulo modulo={51}>
    <div className="finanzas-columnas">
      <section className="finanzas-superficie" aria-labelledby="lista-facturas">
        <div className="finanzas-acciones">
          <h2 id="lista-facturas">Facturas y boletas</h2>
          <button className="primary-button" type="button" disabled={ocupado} onClick={() => abrir('alta')}>Nuevo comprobante</button>
        </div>
        <div className="finanzas-campos" aria-label="Filtros de facturación" role="group">
          <CampoModulo etiqueta="Buscar cliente, serie o número">{campo => <input id={campo} type="search"
            value={filtros.busqueda} onChange={e => filtrar('busqueda', e.target.value)} />}</CampoModulo>
          <CampoModulo etiqueta="Id del cliente">{campo => <input id={campo} type="number" min={1}
            value={filtros.clienteId} onChange={e => filtrar('clienteId', e.target.value)} />}</CampoModulo>
          <CampoModulo etiqueta="Tipo">{campo => <select id={campo} value={filtros.tipoComprobante} onChange={e => filtrar('tipoComprobante', e.target.value)}>
            <option value="">Todos</option><option>Factura</option><option>Boleta</option>
          </select>}</CampoModulo>
          <CampoModulo etiqueta="Estado">{campo => <select id={campo} value={filtros.estado} onChange={e => filtrar('estado', e.target.value)}>
            <option value="">Todos</option><option>Borrador</option><option>Emitida</option><option>Anulada</option>
          </select>}</CampoModulo>
          <CampoModulo etiqueta="Moneda (tres letras)">{campo => <input id={campo} maxLength={3} defaultValue=""
            onBlur={e => filtrar('moneda', e.target.value.trim().toUpperCase())} />}</CampoModulo>
          <CampoModulo etiqueta="Desde">{campo => <input id={campo} type="date" value={fechaDesde}
            onChange={e => filtrar('fechaDesde', e.target.value)} />}</CampoModulo>
          <CampoModulo etiqueta="Hasta">{campo => <input id={campo} type="date" value={fechaHasta}
            onChange={e => filtrar('fechaHasta', e.target.value)} />}</CampoModulo>
        </div>
        {resumen.datos && <section aria-label="Resumen de facturación" className="finanzas-confirmacion">
          <p><strong>{importe(resumen.datos.totalFacturado, resumen.datos.moneda)}</strong> facturado en {resumen.datos.moneda}</p>
          <p>{resumen.datos.totalBorradores} borradores · {resumen.datos.totalEmitidas} emitidos · {resumen.datos.totalAnuladas} anulados</p>
        </section>}
        {resumen.error && <p role="alert">{resumen.error}</p>}
        {lista.cargando && <p role="status">Cargando comprobantes…</p>}
        {lista.error && <p role="alert" className="field-error">{lista.error}</p>}
        {lista.datos?.items.length === 0 && <p>No hay comprobantes para estos filtros.</p>}
        {lista.datos && (lista.datos.items.length > 0 || pagina > 1) && <TablaPaginada
          caption="Comprobantes registrados" etiquetaRegion="Facturas y boletas" columnas={columnas}
          elementos={lista.datos.items} obtenerClave={fila => fila.id} pagina={pagina}
          totalPaginas={Math.max(1, lista.datos.totalPaginas)} cambiarPagina={cambiarPagina} renderizarFila={fila => <tr>
            <td><button type="button" className="secondary-button" disabled={ocupado} onClick={() => {
              seleccionar(fila.id); abrir(null);
            }}>{fila.numeroComprobante}</button><div>{fila.tipoComprobante}</div></td>
            <td>{fila.clienteNombre}</td><td>{diaDePeru(new Date(fila.fechaEmision))}</td>
            <td>{fila.estado}</td><td>{importe(fila.total, fila.moneda)}</td>
          </tr>}
        />}
      </section>
      <PanelDetalle ariaLabelledBy="detalle-factura" className="finanzas-superficie">
        <h2 id="detalle-factura">{modo === 'alta' ? 'Nuevo comprobante' : factura?.numeroComprobante ?? 'Detalle del comprobante'}</h2>
        {id === null && modo === null && <p>Selecciona una factura o boleta.</p>}
        {detalle.cargando && modo !== 'alta' && <p role="status">Cargando detalle…</p>}
        {detalle.error && modo !== 'alta' && <p role="alert">{detalle.error}</p>}
        {mensaje && <p role="status">{mensaje}</p>}
        {error && <p role="alert" className="field-error">{error}</p>}
        {(modo === 'alta' || (modo === 'editar' && factura)) && <FormularioFactura key={modo + id}
          inicial={modo === 'editar' ? factura ?? undefined : undefined} ocupado={ocupado}
          guardar={guardar} cancelar={() => abrir(null)} />}
        {factura && modo !== 'alta' && modo !== 'editar' && <>
          <p>{factura.tipoComprobante} · {factura.estado} · {factura.clienteNombre} ({factura.clienteRuc})</p>
          <p>Emitido el {diaDePeru(new Date(factura.fechaEmision))}
            {factura.fechaVencimiento && <span> · Vence el {diaDePeru(new Date(factura.fechaVencimiento))}</span>}
          </p>
          {factura.numeroOrdenCompra && <p>Orden de compra {factura.numeroOrdenCompra}</p>}
          {factura.observaciones && <p>{factura.observaciones}</p>}
          {factura.motivoAnulacion && <p>Motivo de anulación: {factura.motivoAnulacion}</p>}
          <h3>Líneas</h3>
          <ul className="finanzas-historial">{factura.detalles.map(linea => <li key={linea.id}>
            <strong>{linea.descripcion}</strong>
            <div>{linea.cantidad} × {importe(linea.precioUnitario, factura.moneda)} · Descuento {linea.descuentoPorcentaje}% · Impuesto {linea.porcentajeImpuesto}%</div>
            <div>Total {importe(linea.total, factura.moneda)}</div>
          </li>)}</ul>
          <p>Subtotal {importe(factura.subtotal, factura.moneda)} · Descuentos {importe(factura.totalDescuento, factura.moneda)} · Impuesto {importe(factura.totalImpuesto, factura.moneda)}</p>
          <p><strong>Total {importe(factura.total, factura.moneda)}</strong></p>
          {modo === null && factura.estado !== 'Anulada' && <div className="finanzas-acciones">
            {factura.estado === 'Borrador' && <>
              <button type="button" className="secondary-button" onClick={() => abrir('editar')}>Editar borrador</button>
              <button type="button" className="primary-button" onClick={() => abrir('emitir')}>Emitir comprobante</button>
            </>}
            <button type="button" className="secondary-button" onClick={() => abrir('anular')}>Anular comprobante</button>
          </div>}
          {modo === 'emitir' && <div className="finanzas-confirmacion" role="group" aria-label="Confirmar emisión">
            <p>¿Emitir {factura.numeroComprobante}?</p>
            <button type="button" className="secondary-button" disabled={ocupado} onClick={() => abrir(null)}>Cancelar</button>
            <button type="button" className="primary-button" disabled={ocupado}
              onClick={() => void mutar(() => emitirFactura(factura.id), 'Comprobante emitido.')}>Confirmar emisión</button>
          </div>}
          {modo === 'anular' && <form onSubmit={anular} aria-label="Confirmar anulación">
            <CampoModulo etiqueta="Motivo de anulación">{campo => <textarea id={campo} name="motivo"
              required maxLength={500} disabled={ocupado} />}</CampoModulo>
            <div className="finanzas-acciones">
              <button type="button" className="secondary-button" disabled={ocupado} onClick={() => abrir(null)}>Cancelar</button>
              <button type="submit" className="danger-button" disabled={ocupado}>Confirmar anulación</button>
            </div>
          </form>}
        </>}
      </PanelDetalle>
    </div>
  </PaginaModulo>;
}
