import { useCallback, useState, type FormEvent } from 'react';
import { CampoModulo } from '../../components/CampoModulo';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { monedaFinanzas, textoFormulario, useEscrituraFinanzas, useOperacionFinanzas } from '../../lib/accionesFinanzas';
import { useConsultaPantalla } from '../../lib/consultaPantalla';
import { diaDePeru } from '../../lib/fechaNegocioPeru';
import { anularCuenta, detalleCuenta, listarCuentas, listarPagos, proveedoresCuenta, registrarCuenta,
  registrarPago, resumenCuentas, revertirPago } from './api';
import { FormularioCuentaPorPagar } from './FormularioCuentaPorPagar';
import type { CuentaFila, GuardarCuentaPorPagar } from './tipos';

const columnas = [
  { id: 'documento', etiqueta: 'Documento' }, { id: 'proveedor', etiqueta: 'Proveedor' }, { id: 'vence', etiqueta: 'Vence' },
  { id: 'estado', etiqueta: 'Estado' }, { id: 'saldo', etiqueta: 'Saldo' },
];
const columnasPagos = [
  { id: 'fecha', etiqueta: 'Fecha' }, { id: 'importe', etiqueta: 'Importe' }, { id: 'resultado', etiqueta: 'Estado y acciones' },
];
type Modo = 'alta' | 'pago' | 'revertir' | 'anular';

function estado(cuenta: CuentaFila) {
  if (cuenta.anulada) return 'Anulada';
  if (cuenta.saldo === 0) return 'Pagada';
  return cuenta.saldo < cuenta.total ? 'Parcial' : 'Pendiente';
}

export function CuentasPorPagarPage() {
  const [proveedor, filtrarProveedor] = useState('');
  const [moneda, filtrarMoneda] = useState('');
  const [vencidas, filtrarVencidas] = useState(false);
  const [pagina, navegar] = useState(1);
  const [id, seleccionar] = useState<number | null>(null);
  const [paginaPagos, navegarPagos] = useState(1);
  const [modo, cambiarModo] = useState<Modo | null>(null);
  const [pagoId, elegirPago] = useState<number | null>(null);
  const permiso = useEscrituraFinanzas();
  const operacion = useOperacionFinanzas();
  const cargar = useCallback(() => listarCuentas(proveedor, moneda, vencidas, pagina), [proveedor, moneda, vencidas, pagina]);
  const cargarResumen = useCallback(() => resumenCuentas(proveedor, moneda), [proveedor, moneda]);
  const cargarDetalle = useCallback(() => detalleCuenta(id ?? 0), [id]);
  const cargarPagos = useCallback(() => listarPagos(id ?? 0, paginaPagos), [id, paginaPagos]);
  const lista = useConsultaPantalla(cargar, operacion.revision);
  const resumen = useConsultaPantalla(cargarResumen, operacion.revision);
  const detalle = useConsultaPantalla(id === null ? null : cargarDetalle, operacion.revision);
  const pagos = useConsultaPantalla(id === null ? null : cargarPagos, operacion.revision);
  const proveedores = useConsultaPantalla(proveedoresCuenta);
  const cuenta = detalle.datos;

  function nombreProveedor(proveedorId: number) {
    return proveedores.datos?.find(item => item.id === proveedorId)?.razonSocial ?? 'Proveedor ' + proveedorId;
  }

  function abrir(nuevo: Modo | null) {
    operacion.limpiar();
    cambiarModo(nuevo);
  }

  function alta(datos: GuardarCuentaPorPagar) {
    void operacion.ejecutar(async () => {
      const guardada = await registrarCuenta(datos);
      seleccionar(guardada.id);
      navegarPagos(1);
    }, 'Factura registrada.', () => cambiarModo(null));
  }

  function guardar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    if (!cuenta) return;
    const datos = new FormData(evento.currentTarget);
    if (modo === 'pago') {
      const importe = Number(datos.get('importe'));
      const fechaPago = textoFormulario(datos, 'fechaPago');
      if (importe <= 0 || importe > cuenta.saldo || !fechaPago || fechaPago > diaDePeru()) {
        operacion.indicarError('Indica un importe hasta el saldo pendiente y una fecha de pago válida.');
        return;
      }
      void operacion.ejecutar(() => registrarPago(cuenta.id, { importe, fechaPago }), 'Pago registrado.', () => {
        cambiarModo(null); navegarPagos(1);
      });
    } else if (modo === 'revertir' || modo === 'anular') {
      const motivo = textoFormulario(datos, 'motivo');
      if (motivo.length < 3 || motivo.length > 300) {
        operacion.indicarError('El motivo debe tener entre 3 y 300 caracteres.');
        return;
      }
      if (modo === 'revertir' && pagoId !== null) {
        void operacion.ejecutar(() => revertirPago(cuenta.id, pagoId, motivo), 'Pago revertido.', () => cambiarModo(null));
      } else if (modo === 'anular') {
        void operacion.ejecutar(() => anularCuenta(cuenta.id, motivo), 'Cuenta anulada.', () => cambiarModo(null));
      }
    }
  }

  return <PaginaModulo modulo={54}>
    <div className="finanzas-columnas">
      <section aria-labelledby="cartera-proveedores" className="finanzas-superficie">
        <div className="finanzas-acciones">
          <h2 id="cartera-proveedores">Facturas por pagar</h2>
          {permiso && <button type="button" className="primary-button" disabled={operacion.ocupado}
            onClick={() => abrir('alta')}>Registrar factura</button>}
        </div>
        <div className="finanzas-campos">
          <CampoModulo etiqueta="Filtrar por proveedor">{campo => <select id={campo} value={proveedor}
            onChange={e => { filtrarProveedor(e.target.value); navegar(1); }}>
            <option value="">Todos</option>
            {proveedores.datos?.map(item => <option key={item.id} value={item.id}>{item.razonSocial}</option>)}
          </select>}</CampoModulo>
          <CampoModulo etiqueta="Moneda de la cartera">{campo => <select id={campo} value={moneda}
            onChange={e => { filtrarMoneda(e.target.value); navegar(1); }}>
            <option value="">Todas</option><option>PEN</option><option>USD</option>
          </select>}</CampoModulo>
          <label><input type="checkbox" checked={vencidas} onChange={e => {
            filtrarVencidas(e.target.checked); navegar(1);
          }} /> Solo vencidas</label>
        </div>
        {lista.cargando && <p role="status">Cargando cuentas por pagar…</p>}
        {lista.error && <p role="alert">{lista.error}</p>}
        {lista.datos?.items.length === 0 && <p>No hay cuentas para estos filtros.</p>}
        {lista.datos && (pagina > 1 || lista.datos.items.length > 0) && <TablaPaginada
          caption="Cuentas y saldos pendientes" etiquetaRegion="Cuentas por pagar" columnas={columnas}
          elementos={lista.datos.items} obtenerClave={fila => fila.id} pagina={pagina} cambiarPagina={navegar}
          totalPaginas={Math.max(1, lista.datos.totalPaginas)} renderizarFila={fila => <tr>
            <td><button type="button" className="secondary-button" disabled={operacion.ocupado} onClick={() => {
              seleccionar(fila.id); navegarPagos(1); abrir(null);
            }}>{fila.serie}-{fila.numeroDocumento}</button></td>
            <td>{nombreProveedor(fila.proveedorId)}</td><td>{fila.fechaVencimiento}</td>
            <td>{estado(fila)}{fila.vencida ? ' · Vencida' : ''}</td><td>{monedaFinanzas(fila.saldo, fila.moneda)}</td>
          </tr>} />}
      </section>

      <PanelDetalle ariaLabelledBy="cxp-detalle" className="finanzas-superficie">
        <h2 id="cxp-detalle">{modo === 'alta' ? 'Factura de proveedor' : cuenta ? cuenta.serie + '-' + cuenta.numeroDocumento : 'Detalle de la cuenta'}</h2>
        {id === null && modo === null && <p>Selecciona una cuenta para consultar su saldo y pagos.</p>}
        {detalle.cargando && modo !== 'alta' && <p role="status">Cargando detalle…</p>}
        {detalle.error && modo !== 'alta' && <p role="alert">{detalle.error}</p>}
        {operacion.aviso && <p role="status">{operacion.aviso}</p>}
        {operacion.error && <p role="alert" className="field-error">{operacion.error}</p>}
        {permiso && modo === 'alta' && <>
          {proveedores.cargando && <p role="status">Cargando proveedores…</p>}
          {proveedores.error && <p role="alert">{proveedores.error}</p>}
          {proveedores.datos && <FormularioCuentaPorPagar proveedores={proveedores.datos}
            ocupado={operacion.ocupado} guardar={alta} cancelar={() => abrir(null)} />}
        </>}
        {cuenta && modo !== 'alta' && <>
          <p>{nombreProveedor(cuenta.proveedorId)} · {estado(cuenta)}{cuenta.vencida ? ' · Vencida' : ''}</p>
          <p>Documento {cuenta.fechaDocumento} · Vencimiento {cuenta.fechaVencimiento}</p>
          {cuenta.ordenAbastecimientoId && <p>Orden de abastecimiento {cuenta.ordenAbastecimientoId}</p>}
          <p>Base {monedaFinanzas(cuenta.baseImponible, cuenta.moneda)} · Impuesto {monedaFinanzas(cuenta.importeImpuesto, cuenta.moneda)}</p>
          <p>Total {monedaFinanzas(cuenta.total, cuenta.moneda)} · Pagado {monedaFinanzas(cuenta.importePagado, cuenta.moneda)}</p>
          <p><strong>Saldo {monedaFinanzas(cuenta.saldo, cuenta.moneda)}</strong></p>
          {cuenta.motivoAnulacion && <p>Motivo de anulación: {cuenta.motivoAnulacion}</p>}
          {permiso && modo === null && !cuenta.anulada && <div className="finanzas-acciones">
            {cuenta.saldo > 0 && <button type="button" className="primary-button" onClick={() => abrir('pago')}>Registrar pago</button>}
            {cuenta.importePagado === 0 && <button type="button" className="secondary-button" onClick={() => abrir('anular')}>Anular cuenta</button>}
          </div>}
          {permiso && modo !== null && <form onSubmit={guardar} key={modo} aria-label={modo === 'pago' ? 'Registrar pago' : 'Confirmar operación'}>
            <fieldset className="finanzas-campos" disabled={operacion.ocupado}>
              {modo === 'pago' ? <>
                <CampoModulo etiqueta="Importe del pago">{campo => <input id={campo} type="number" name="importe" step="0.01"
                  min="0.01" max={cuenta.saldo} required />}</CampoModulo>
                <CampoModulo etiqueta="Fecha de pago">{campo => <input id={campo} type="date" name="fechaPago" required
                  max={diaDePeru()} defaultValue={diaDePeru()} />}</CampoModulo>
              </> : <CampoModulo etiqueta={modo === 'anular' ? 'Motivo de anulación' : 'Motivo de reversión'}>{campo =>
                <textarea id={campo} name="motivo" required minLength={3} maxLength={300} />}</CampoModulo>}
            </fieldset>
            <div className="finanzas-acciones">
              <button type="button" className="secondary-button" disabled={operacion.ocupado} onClick={() => abrir(null)}>Cancelar</button>
              <button type="submit" className="primary-button" disabled={operacion.ocupado}>
                {modo === 'pago' ? 'Guardar pago' : modo === 'revertir' ? 'Confirmar reversión' : 'Confirmar anulación'}
              </button>
            </div>
          </form>}
          <h3>Historial de pagos</h3>
          {pagos.cargando && <p role="status">Cargando pagos…</p>}
          {pagos.error && <p role="alert">{pagos.error}</p>}
          {pagos.datos?.items.length === 0 && <p>Sin pagos registrados.</p>}
          {pagos.datos && (paginaPagos > 1 || pagos.datos.items.length > 0) && <TablaPaginada caption="Pagos de la cuenta"
            etiquetaRegion="Historial de pagos" columnas={columnasPagos} elementos={pagos.datos.items} obtenerClave={p => p.id}
            pagina={paginaPagos} totalPaginas={Math.max(1, pagos.datos.totalPaginas)} cambiarPagina={navegarPagos} renderizarFila={pago => <tr>
              <td>{pago.fechaPago}</td><td>{monedaFinanzas(pago.importe, pago.moneda)}</td>
              <td>{pago.fechaReversion ? 'Revertido · ' + pago.motivoReversion : 'Aplicado'}
                {permiso && !pago.fechaReversion && !cuenta.anulada && <button type="button" className="secondary-button"
                  disabled={operacion.ocupado} onClick={() => { elegirPago(pago.id); abrir('revertir'); }}>Revertir pago {pago.id}</button>}
              </td>
            </tr>} />}
        </>}
      </PanelDetalle>
    </div>
    <section className="finanzas-superficie" aria-labelledby="cxp-vencimientos">
      <h2 id="cxp-vencimientos">Reporte de vencimientos</h2>
      {resumen.cargando && <p role="status">Cargando vencimientos…</p>}
      {resumen.error && <p role="alert">{resumen.error}</p>}
      {resumen.datos && <p>Fecha de corte {resumen.datos.fechaCorte}</p>}
      {resumen.datos?.grupos.length === 0 && <p>Sin vencimientos para el proveedor y la moneda seleccionados.</p>}
      {resumen.datos && resumen.datos.grupos.length > 0 && <div className="tabla-paginada-scroll" tabIndex={0}
        role="region" aria-label="Vencimientos por proveedor y moneda">
        <table className="tabla-paginada">
          <caption>Importes por tramo y moneda</caption>
          <thead><tr>{['Proveedor', 'Moneda', 'Al día', 'Vencido', '0–30 días', '31–60 días', '61–90 días', 'Más de 90 días'].map(celda =>
            <th key={celda} scope="col">{celda}</th>)}</tr></thead>
          <tbody>{resumen.datos.grupos.map(grupo => <tr key={grupo.proveedorId + '-' + grupo.moneda}>
            <td>{nombreProveedor(grupo.proveedorId)}</td><td>{grupo.moneda}</td>
            <td>{monedaFinanzas(grupo.saldoAlDia, grupo.moneda)}</td><td>{monedaFinanzas(grupo.saldoVencido, grupo.moneda)}</td>
            <td>{monedaFinanzas(grupo.vencido0A30, grupo.moneda)}</td><td>{monedaFinanzas(grupo.vencido31A60, grupo.moneda)}</td>
            <td>{monedaFinanzas(grupo.vencido61A90, grupo.moneda)}</td><td>{monedaFinanzas(grupo.vencidoMasDe90, grupo.moneda)}</td>
          </tr>)}</tbody>
        </table>
      </div>}
    </section>
  </PaginaModulo>;
}
