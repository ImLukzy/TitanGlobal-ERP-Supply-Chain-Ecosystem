import { useCallback, useState, type FormEvent } from 'react';
import { CampoModulo } from '../../components/CampoModulo';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { monedaFinanzas, textoFormulario, useEscrituraFinanzas, useOperacionFinanzas } from '../../lib/accionesFinanzas';
import { useConsultaPantalla } from '../../lib/consultaPantalla';
import { diaDePeru } from '../../lib/fechaNegocioPeru';
import { cuentasDeMoneda, guardarCuenta, listarCuentas, listarMovimientos, obtenerCuenta, obtenerTransferencia,
  registrarMovimiento, registrarTransferencia, revertirMovimiento } from './api';

const columnas = [
  { id: 'cuenta', etiqueta: 'Cuenta' }, { id: 'banco', etiqueta: 'Banco' }, { id: 'tipo', etiqueta: 'Tipo' },
  { id: 'moneda', etiqueta: 'Moneda' }, { id: 'saldo', etiqueta: 'Saldo' },
];
const columnasMovimientos = [
  { id: 'fecha', etiqueta: 'Fecha' }, { id: 'tipo', etiqueta: 'Tipo' }, { id: 'importe', etiqueta: 'Importe' },
  { id: 'referencia', etiqueta: 'Referencia' }, { id: 'actividad', etiqueta: 'Estado y acciones' },
];

type Modo = 'alta' | 'edicion' | 'Ingreso' | 'Egreso' | 'transferir' | 'revertir';

export function TesoreriaBancosPage() {
  const [moneda, filtrarMoneda] = useState('');
  const [tipo, filtrarTipo] = useState('');
  const [pagina, navegar] = useState(1);
  const [id, seleccionar] = useState<number | null>(null);
  const [historial, filtrarHistorial] = useState({ tipo: '', desde: '', hasta: '', pagina: 1 });
  const [modo, cambiarModo] = useState<Modo | null>(null);
  const [movimiento, elegirMovimiento] = useState<number | null>(null);
  const [transferenciaId, mostrarTransferencia] = useState<number | null>(null);
  const escritura = useEscrituraFinanzas();
  const operacion = useOperacionFinanzas();
  const cargar = useCallback(() => listarCuentas(moneda, tipo, pagina), [moneda, tipo, pagina]);
  const cargarDetalle = useCallback(() => obtenerCuenta(id ?? 0), [id]);
  const cargarHistorial = useCallback(() => listarMovimientos(id ?? 0, historial.tipo, historial.desde, historial.hasta, historial.pagina), [id, historial]);
  const cargarPresencia = useCallback(() => listarMovimientos(id ?? 0, '', '', '', 1, 1), [id]);
  const cargarTransferencia = useCallback(() => obtenerTransferencia(transferenciaId ?? 0), [transferenciaId]);
  const lista = useConsultaPantalla(cargar, operacion.revision);
  const detalle = useConsultaPantalla(id === null ? null : cargarDetalle, operacion.revision);
  const movimientos = useConsultaPantalla(id === null ? null : cargarHistorial, operacion.revision);
  const presencia = useConsultaPantalla(modo === 'edicion' && id !== null ? cargarPresencia : null, operacion.revision);
  const transferencia = useConsultaPantalla(transferenciaId === null ? null : cargarTransferencia, operacion.revision);
  const cuenta = detalle.datos;
  const monedaCuenta = cuenta?.moneda ?? '';
  const cargarDestinos = useCallback(() => cuentasDeMoneda(monedaCuenta), [monedaCuenta]);
  const destinos = useConsultaPantalla(modo === 'transferir' && monedaCuenta !== '' ? cargarDestinos : null);

  function abrir(nuevo: Modo | null) {
    cambiarModo(nuevo);
    operacion.limpiar();
    mostrarTransferencia(null);
  }

  function guardar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const datos = new FormData(evento.currentTarget);
    const texto = (campo: string) => textoFormulario(datos, campo);
    if (modo === 'alta' || modo === 'edicion') {
      const banco = texto('banco'), numero = texto('numero'), nombre = texto('nombre');
      if (!banco || !numero || !nombre) {
        operacion.indicarError('Los datos de tesorería no son válidos.');
        return;
      }
      void operacion.ejecutar(async () => {
        const guardada = await guardarCuenta({ banco, numero, nombre, tipo: texto('tipo') === 'Ahorros' ? 'Ahorros' : 'Corriente',
          moneda: Number(presencia.datos?.totalRegistros ?? 0) > 0 && cuenta ? cuenta.moneda : texto('moneda') === 'USD' ? 'USD' : 'PEN' },
        modo === 'edicion' ? id ?? undefined : undefined);
        seleccionar(guardada.id);
      }, 'Cuenta guardada.', () => cambiarModo(null));
    } else if (cuenta && modo === 'revertir' && movimiento !== null) {
      const motivo = texto('motivo');
      if (motivo.length < 3 || motivo.length > 300) {
        operacion.indicarError('El motivo debe tener entre 3 y 300 caracteres.');
        return;
      }
      void operacion.ejecutar(() => revertirMovimiento(cuenta.id, movimiento, motivo), 'Movimiento revertido.', () => cambiarModo(null));
    } else if (cuenta && (modo === 'Ingreso' || modo === 'Egreso' || modo === 'transferir')) {
      const importe = Number(datos.get('importe'));
      const fechaOperacion = texto('fecha');
      const referencia = texto('referencia') || null;
      if (importe <= 0 || !fechaOperacion || fechaOperacion > diaDePeru()) {
        operacion.indicarError('Los datos de tesorería no son válidos.');
        return;
      }
      if (modo === 'transferir') {
        const cuentaDestinoId = Number(datos.get('destino'));
        if (!cuentaDestinoId || cuentaDestinoId === cuenta.id) {
          operacion.indicarError('Las cuentas de origen y destino deben ser distintas.');
          return;
        }
        void operacion.ejecutar(async () => {
          const registrada = await registrarTransferencia({ cuentaOrigenId: cuenta.id, cuentaDestinoId, importe, fechaOperacion, referencia });
          mostrarTransferencia(registrada.id);
        }, 'Transferencia registrada.', () => cambiarModo(null));
      } else {
        void operacion.ejecutar(() => registrarMovimiento(cuenta.id, { tipo: modo, importe, fechaOperacion, referencia }),
          'Movimiento registrado.', () => cambiarModo(null));
      }
    }
  }

  return <PaginaModulo modulo={55}>
    <div className="finanzas-columnas">
      <section aria-labelledby="lista-cuentas" className="finanzas-superficie">
        <div className="finanzas-acciones">
          <h2 id="lista-cuentas">Cuentas bancarias</h2>
          {escritura && <button type="button" className="primary-button" disabled={operacion.ocupado}
            onClick={() => abrir('alta')}>Nueva cuenta</button>}
        </div>
        <div className="finanzas-campos">
          <CampoModulo etiqueta="Moneda de cuenta">{campo => <select id={campo} value={moneda}
            onChange={e => { filtrarMoneda(e.target.value); navegar(1); }}>
            <option value="">Todas</option><option>PEN</option><option>USD</option>
          </select>}</CampoModulo>
          <CampoModulo etiqueta="Tipo de cuenta">{campo => <select id={campo} value={tipo}
            onChange={e => { filtrarTipo(e.target.value); navegar(1); }}>
            <option value="">Todos</option><option>Corriente</option><option>Ahorros</option>
          </select>}</CampoModulo>
        </div>
        {lista.cargando && <p role="status">Cargando cuentas…</p>}
        {lista.error && <p role="alert">{lista.error}</p>}
        {lista.datos?.items.length === 0 && <p>No hay cuentas para estos filtros.</p>}
        {lista.datos && (pagina > 1 || lista.datos.items.length > 0) && <TablaPaginada
          caption="Saldos por cuenta y moneda" etiquetaRegion="Cuentas bancarias" columnas={columnas}
          elementos={lista.datos.items} obtenerClave={item => item.id} pagina={pagina}
          totalPaginas={Math.max(1, lista.datos.totalPaginas)} cambiarPagina={navegar} renderizarFila={fila => <tr>
            <td><button type="button" className="secondary-button" disabled={operacion.ocupado} onClick={() => {
              seleccionar(fila.id); filtrarHistorial({ tipo: '', desde: '', hasta: '', pagina: 1 }); abrir(null);
            }}>{fila.nombre}</button><div>{fila.numero}</div></td>
            <td>{fila.banco}</td><td>{fila.tipo}</td><td>{fila.moneda}</td><td>{monedaFinanzas(fila.saldo, fila.moneda)}</td>
          </tr>} />}
      </section>

      <PanelDetalle ariaLabelledBy="detalle-cuenta" className="finanzas-superficie">
        <h2 id="detalle-cuenta">{modo === 'alta' ? 'Nueva cuenta bancaria' : cuenta?.nombre ?? 'Detalle de cuenta'}</h2>
        {detalle.cargando && modo !== 'alta' && <p role="status">Cargando cuenta…</p>}
        {detalle.error && modo !== 'alta' && <p role="alert">{detalle.error}</p>}
        {id === null && !modo && <p>Selecciona una cuenta para consultar sus movimientos.</p>}
        {operacion.aviso && <p role="status">{operacion.aviso}</p>}
        {operacion.error && <p role="alert" className="field-error">{operacion.error}</p>}
        {cuenta && modo !== 'alta' && <>
          <p>{cuenta.banco} · {cuenta.numero} · {cuenta.tipo}</p>
          <p><strong>Saldo {monedaFinanzas(cuenta.saldo, cuenta.moneda)}</strong></p>
          {escritura && modo === null && <div className="finanzas-acciones">
            <button type="button" className="secondary-button" onClick={() => abrir('edicion')}>Editar cuenta</button>
            <button type="button" className="secondary-button" onClick={() => abrir('Ingreso')}>Registrar ingreso</button>
            <button type="button" className="secondary-button" onClick={() => abrir('Egreso')}>Registrar egreso</button>
            <button type="button" className="secondary-button" onClick={() => abrir('transferir')}>Transferir</button>
          </div>}
        </>}
        {escritura && modo && <form key={modo + String(id)} onSubmit={guardar} aria-label="Operación bancaria">
          <h3>{modo === 'alta' || modo === 'edicion' ? 'Datos de la cuenta' :
            modo === 'transferir' ? 'Transferencia' : modo === 'revertir' ? 'Reversión' : modo}</h3>
          {presencia.error && <p role="alert">{presencia.error}</p>}
          {destinos.error && <p role="alert">{destinos.error}</p>}
          {(presencia.cargando || destinos.cargando) && <p role="status">Cargando opciones…</p>}
          <fieldset className="finanzas-campos" disabled={operacion.ocupado}>
            {(modo === 'alta' || modo === 'edicion') && (modo !== 'edicion' || presencia.datos) && <>
              <CampoModulo etiqueta="Banco">{campo => <input id={campo} name="banco" required maxLength={120}
                defaultValue={modo === 'edicion' ? cuenta?.banco : ''} />}</CampoModulo>
              <CampoModulo etiqueta="Número de cuenta">{campo => <input id={campo} name="numero" required maxLength={40}
                defaultValue={modo === 'edicion' ? cuenta?.numero : ''} />}</CampoModulo>
              <CampoModulo etiqueta="Nombre">{campo => <input id={campo} name="nombre" required maxLength={120}
                defaultValue={modo === 'edicion' ? cuenta?.nombre : ''} />}</CampoModulo>
              <CampoModulo etiqueta="Tipo">{campo => <select id={campo} name="tipo" defaultValue={modo === 'edicion' ? cuenta?.tipo : 'Corriente'}>
                <option>Corriente</option><option>Ahorros</option>
              </select>}</CampoModulo>
              <CampoModulo etiqueta="Moneda">{campo => <select id={campo} name="moneda"
                disabled={modo === 'edicion' && (presencia.datos?.totalRegistros ?? 0) > 0}
                defaultValue={modo === 'edicion' ? cuenta?.moneda : 'PEN'}><option>PEN</option><option>USD</option></select>}</CampoModulo>
            </>}
            {modo === 'revertir' && <CampoModulo etiqueta="Motivo de reversión">{campo =>
              <textarea id={campo} name="motivo" required minLength={3} maxLength={300} />}</CampoModulo>}
            {(modo === 'Ingreso' || modo === 'Egreso' || modo === 'transferir') && <>
              {modo === 'transferir' && <CampoModulo etiqueta="Cuenta destino">{campo => <select id={campo} name="destino" required>
                <option value="">Seleccionar destino</option>
                {destinos.datos?.filter(item => item.id !== id).map(item =>
                  <option key={item.id} value={item.id}>{item.nombre} · {item.numero}</option>)}
              </select>}</CampoModulo>}
              <CampoModulo etiqueta="Importe">{campo => <input id={campo} name="importe" required type="number" step="0.01" min="0.01" />}</CampoModulo>
              <CampoModulo etiqueta="Fecha de operación">{campo => <input id={campo} name="fecha" required type="date"
                defaultValue={diaDePeru()} max={diaDePeru()} />}</CampoModulo>
              <CampoModulo etiqueta="Referencia">{campo => <input id={campo} name="referencia" maxLength={100} />}</CampoModulo>
            </>}
          </fieldset>
          <div className="finanzas-acciones">
            <button type="button" className="secondary-button" disabled={operacion.ocupado} onClick={() => abrir(null)}>Cancelar</button>
            <button type="submit" className="primary-button" disabled={operacion.ocupado || (modo === 'edicion' && !presencia.datos)
              || (modo === 'transferir' && !destinos.datos)}>{modo === 'revertir' ? 'Confirmar reversión' : 'Guardar operación'}</button>
          </div>
        </form>}
        {transferencia.cargando && <p role="status">Cargando transferencia…</p>}
        {transferencia.error && <p role="alert">{transferencia.error}</p>}
        {transferencia.datos && <p>Transferencia {transferencia.datos.id} · {monedaFinanzas(transferencia.datos.importe, transferencia.datos.moneda)}
          {' · '}{transferencia.datos.fechaOperacion} · Cuenta {transferencia.datos.cuentaOrigenId} a {transferencia.datos.cuentaDestinoId}</p>}
      </PanelDetalle>
    </div>
    {cuenta && modo !== 'alta' && <section className="finanzas-superficie" aria-labelledby="movimientos-banco">
      <h2 id="movimientos-banco">Movimientos de {cuenta.nombre}</h2>
      <div className="finanzas-campos">
        <CampoModulo etiqueta="Tipo de movimiento">{campo => <select id={campo} value={historial.tipo} onChange={e =>
          filtrarHistorial({ ...historial, tipo: e.target.value, pagina: 1 })}>
          <option value="">Todos</option><option>Ingreso</option><option>Egreso</option>
        </select>}</CampoModulo>
        <CampoModulo etiqueta="Movimientos desde">{campo => <input id={campo} type="date" value={historial.desde}
          onChange={e => filtrarHistorial({ ...historial, desde: e.target.value, pagina: 1 })} />}</CampoModulo>
        <CampoModulo etiqueta="Movimientos hasta">{campo => <input id={campo} type="date" value={historial.hasta}
          onChange={e => filtrarHistorial({ ...historial, hasta: e.target.value, pagina: 1 })} />}</CampoModulo>
      </div>
      {movimientos.cargando && <p role="status">Cargando movimientos…</p>}
      {movimientos.error && <p role="alert">{movimientos.error}</p>}
      {movimientos.datos?.items.length === 0 && <p>No hay movimientos para estos filtros.</p>}
      {movimientos.datos && (historial.pagina > 1 || movimientos.datos.items.length > 0) && <TablaPaginada
        caption="Historial bancario" etiquetaRegion="Movimientos" columnas={columnasMovimientos} elementos={movimientos.datos.items}
        obtenerClave={item => item.id} pagina={historial.pagina} totalPaginas={Math.max(1, movimientos.datos.totalPaginas)}
        cambiarPagina={p => filtrarHistorial({ ...historial, pagina: p })} renderizarFila={item => <tr>
          <td>{item.fechaOperacion}</td><td>{item.tipo}</td><td>{monedaFinanzas(item.importe, item.moneda)}</td>
          <td>{item.referencia ?? '—'}</td><td>
            {item.fechaReversion ? 'Revertido · ' + item.motivoReversion : 'Registrado'}
            {item.transferenciaBancariaId !== null && <button type="button" className="secondary-button" disabled={operacion.ocupado}
              onClick={() => mostrarTransferencia(item.transferenciaBancariaId)}>Ver transferencia {item.transferenciaBancariaId}</button>}
            {escritura && !item.fechaReversion && item.transferenciaBancariaId === null &&
              <button type="button" className="secondary-button" disabled={operacion.ocupado} onClick={() => {
                elegirMovimiento(item.id); abrir('revertir');
              }}>Revertir movimiento {item.id}</button>}
          </td>
        </tr>} />}
    </section>}
  </PaginaModulo>;
}
