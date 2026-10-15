import { useEffect, useState } from 'react';
import { ApiError } from '../../api/client';
import { TablaPaginada } from '../../components/TablaPaginada';
import { formateadorMoneda, formatearCantidad } from '../../lib/formato';
import { liquidarMes, obtenerComisiones, pagarLiquidacion } from './api';
import { ConfirmacionComision } from './ConfirmacionComision';
import type { ComisionesMes, Liquidacion } from './tipos';

const MESES = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'];

interface Props {
  activo: boolean;
}

function mensaje(error: unknown, respaldo: string): string {
  return error instanceof ApiError ? error.message : respaldo;
}

function EtiquetaEstado({ estado }: { estado: string }) {
  return <span className={`comision-status is-${estado.toLowerCase()}`}>{estado}</span>;
}

export function ComisionesTab({ activo }: Props) {
  const [anio, setAnio] = useState(2026);
  const [mes, setMes] = useState(1);
  const [datos, setDatos] = useState<ComisionesMes | null>(null);
  const [errorCarga, setErrorCarga] = useState('');
  const [cargando, setCargando] = useState(true);
  const [dialogoLiquidacion, setDialogoLiquidacion] = useState(false);
  const [dialogoPago, setDialogoPago] = useState<Liquidacion | null>(null);
  const [errorDialogo, setErrorDialogo] = useState('');
  const [ocupado, setOcupado] = useState(false);
  const [actualizacion, setActualizacion] = useState(0);

  useEffect(() => {
    if (!activo) {
      return;
    }
    let vigente = true;
    setCargando(true);
    setErrorCarga('');
    void obtenerComisiones(anio, mes).then((respuesta) => {
      if (vigente) {
        setDatos(respuesta);
      }
    }).catch((error: unknown) => {
      if (vigente) {
        setErrorCarga(mensaje(error, 'No se pudieron cargar las comisiones del mes.'));
      }
    }).finally(() => {
      if (vigente) {
        setCargando(false);
      }
    });
    return () => {
      vigente = false;
    };
  }, [activo, anio, mes, actualizacion]);

  function cambiarPeriodo(siguienteAnio: number, siguienteMes: number): void {
    setAnio(siguienteAnio);
    setMes(siguienteMes);
  }

  async function confirmarLiquidacion(): Promise<void> {
    setOcupado(true);
    setErrorDialogo('');
    try {
      await liquidarMes(anio, mes);
      setDialogoLiquidacion(false);
      setActualizacion((actual) => actual + 1);
    } catch (error: unknown) {
      setErrorDialogo(mensaje(error, 'No se pudo liquidar el mes.'));
    } finally {
      setOcupado(false);
    }
  }

  async function confirmarPago(): Promise<void> {
    if (dialogoPago === null) {
      return;
    }
    setOcupado(true);
    setErrorDialogo('');
    try {
      await pagarLiquidacion(dialogoPago.liquidacionId ?? dialogoPago.id);
      setDialogoPago(null);
      setActualizacion((actual) => actual + 1);
    } catch (error: unknown) {
      setErrorDialogo(mensaje(error, 'No se pudo marcar pagada la liquidación.'));
    } finally {
      setOcupado(false);
    }
  }

  function cerrarDialogo(): void {
    if (!ocupado) {
      setDialogoLiquidacion(false);
      setDialogoPago(null);
      setErrorDialogo('');
    }
  }

  return (
    <section aria-label="Comisiones del mes" className="panel-card comisiones-list-panel">
      <div className="clientes-panel-heading">
        <div><div className="page-eyebrow">LIQUIDACIÓN DE COMISIONES</div><h2>Comisiones del mes</h2></div>
      </div>
      <div className="solicitudes-filters comisiones-filtros periodo-filtros">
        <div><label htmlFor="comisiones-anio">Año</label><select id="comisiones-anio" onChange={(event) => cambiarPeriodo(Number(event.currentTarget.value), mes)} value={anio}>{Array.from({ length: 11 }, (_, indice) => 2020 + indice).map((valor) => <option key={valor} value={valor}>{valor}</option>)}</select></div>
        <div><label htmlFor="comisiones-mes">Mes</label><select id="comisiones-mes" onChange={(event) => cambiarPeriodo(anio, Number(event.currentTarget.value))} value={mes}>{MESES.map((nombre, indice) => <option key={nombre} value={indice + 1}>{nombre}</option>)}</select></div>
      </div>
      {datos !== null && <section aria-label={`Totales de comisiones de ${MESES[datos.mes - 1]} ${datos.anio}`} className="comisiones-totales">
        <article><span>Ventas del periodo</span><strong className="cantidad-sin-partir">{formateadorMoneda.format(datos.totalVentas)}</strong></article>
        <article><span>Comisiones del periodo</span><strong className="cantidad-sin-partir">{formateadorMoneda.format(datos.totalComision)}</strong></article>
      </section>}
      {datos?.liquidado && <p className="comision-locked-note" role="status">Mes liquidado: los importes ya no cambian.</p>}
      {errorCarga !== '' && <p className="clientes-error" role="alert">{errorCarga}</p>}
      {cargando && <p role="status">Cargando comisiones…</p>}
      {!cargando && !errorCarga && (datos?.filas.length ?? 0) === 0 && <p role="status">No hay ventas asignadas en este mes.</p>}
      {!cargando && !errorCarga && datos !== null && datos.filas.length > 0 && <>
        <TablaPaginada
          caption={`Comisiones de ${MESES[datos.mes - 1]} ${datos.anio}`}
          className="solicitudes-table comisiones-table liquidacion-table"
          classNameContenedor="solicitudes-table-scroll"
          cambiarPagina={() => undefined}
          columnas={[{ id: 'vendedor', etiqueta: 'Vendedor' }, { id: 'ordenes', etiqueta: 'Órdenes' }, { id: 'ventas', etiqueta: 'Ventas' }, { id: 'porcentaje', etiqueta: 'Comisión %' }, { id: 'comision', etiqueta: 'Importe' }, { id: 'estado', etiqueta: 'Estado' }, { id: 'accion', etiqueta: 'Acción' }]}
          elementos={datos.filas}
          etiquetaRegion="Tabla de comisiones del mes"
          obtenerClave={(fila) => fila.vendedorId}
          pagina={1}
          renderizarFila={(fila) => <tr key={fila.vendedorId}>
            <th scope="row">{fila.codigo} · {fila.nombre}</th>
            <td>{formatearCantidad(fila.ordenes, 'orden', 'órdenes')}</td>
            <td className="cantidad-sin-partir">{formateadorMoneda.format(fila.ventas)}</td>
            <td className="cantidad-sin-partir">{fila.porcentaje.toFixed(2)} %</td>
            <td className="cantidad-sin-partir">{formateadorMoneda.format(fila.comision)}</td>
            <td><EtiquetaEstado estado={fila.estado} /></td>
            <td>{fila.estado === 'Calculada' && fila.liquidacionId !== null ? <button className="secondary-button" onClick={() => { setErrorDialogo(''); setDialogoPago({ ...fila, id: fila.liquidacionId ?? 0, anio, mes, fechaCalculo: '', fechaPago: null }); }} type="button">Marcar pagada</button> : <span>—</span>}</td>
          </tr>}
          totalPaginas={1}
        />
        {!datos.liquidado && <div className="comisiones-actions"><button className="primary-button" onClick={() => { setErrorDialogo(''); setDialogoLiquidacion(true); }} type="button">Liquidar mes</button></div>}
      </>}
      {dialogoLiquidacion && <ConfirmacionComision cerrar={cerrarDialogo} confirmar={() => void confirmarLiquidacion()} confirmarEtiqueta="Confirmar liquidación" error={errorDialogo} ocupado={ocupado} texto={`Se guardarán los importes de las comisiones de ${MESES[mes - 1]} ${anio}.`} titulo="¿Liquidar este mes?" />}
      {dialogoPago !== null && <ConfirmacionComision cerrar={cerrarDialogo} confirmar={() => void confirmarPago()} confirmarEtiqueta="Marcar pagada" error={errorDialogo} ocupado={ocupado} texto={`Se marcará como pagada la comisión de ${dialogoPago.codigo} · ${dialogoPago.nombre}.`} titulo="¿Confirmar el pago?" />}
    </section>
  );
}
