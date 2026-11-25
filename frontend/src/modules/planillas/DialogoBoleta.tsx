import { useEffect, useRef, useState } from 'react';
import { formatearFecha, formateadorMoneda } from '../../lib/formato';
import { obtenerBoleta } from './api';
import { formatearMinutosExtra, mensajeError, textoPeriodo, useDialogoModal } from './presentacion';
import type { BoletaPago } from './tipos';

interface Props {
  detalleId: number;
  cerrar: () => void;
}

// Clase que activa las reglas @media print del módulo: solo existe mientras la boleta está abierta.
const CLASE_IMPRESION = 'planillas-imprimiendo';

interface ResultadoBoleta {
  detalleId: number;
  boleta: BoletaPago | null;
  error: string;
}

function Fila({ etiqueta, valor }: { etiqueta: string; valor: string }) {
  return (
    <tr>
      <th scope="row">{etiqueta}</th>
      <td className="planillas-numero">{valor}</td>
    </tr>
  );
}

function Cuerpo({ boleta }: { boleta: BoletaPago }) {
  const moneda = (valor: number): string => formateadorMoneda.format(valor);
  return (
    <article aria-label={`Boleta de pago ${boleta.numeroBoleta}`} className="planillas-boleta">
      <header className="planillas-boleta-cabecera">
        <strong>{boleta.empresa}</strong>
        <h3>Boleta de pago</h3>
        <p>{`N.º ${boleta.numeroBoleta} · Planilla ${boleta.numeroPlanilla} · ${boleta.tipo} ${textoPeriodo(boleta.mes, boleta.anio)}`}</p>
      </header>
      <dl className="planillas-boleta-datos">
        <div><dt>Empleado</dt><dd>{`${boleta.empleadoLegajo} · ${boleta.empleadoNombre}`}</dd></div>
        <div><dt>Cargo</dt><dd>{boleta.cargo}</dd></div>
        <div><dt>Sistema de pensiones</dt><dd>{boleta.codigoPension ?? 'Sin afiliación'}</dd></div>
        <div><dt>Días laborados</dt><dd>{`${boleta.diasLaborados} de ${boleta.diasContrato}`}</dd></div>
      </dl>
      <div className="planillas-boleta-bloques">
        <table className="planillas-boleta-tabla">
          <caption>Ingresos</caption>
          <tbody>
            <Fila etiqueta="Sueldo básico" valor={moneda(boleta.ingresoBasico)} />
            <Fila etiqueta={`Horas extra (${formatearMinutosExtra(boleta.minutosExtra)})`} valor={moneda(boleta.ingresoHorasExtra)} />
            <Fila etiqueta="Bonos" valor={moneda(boleta.ingresoBonos)} />
            <Fila etiqueta="Total ingresos" valor={moneda(boleta.totalIngresos)} />
          </tbody>
        </table>
        <table className="planillas-boleta-tabla">
          <caption>Descuentos</caption>
          <tbody>
            <Fila etiqueta="Pensión" valor={moneda(boleta.descuentoPension)} />
            <Fila etiqueta="Quinta categoría" valor={moneda(boleta.descuentoQuinta)} />
            <Fila etiqueta="Préstamos" valor={moneda(boleta.descuentoPrestamos)} />
            <Fila etiqueta="Total descuentos" valor={moneda(boleta.totalDescuentos)} />
          </tbody>
        </table>
      </div>
      <dl className="planillas-boleta-neto">
        <div><dt>Aportes del empleador (EsSalud)</dt><dd>{moneda(boleta.aporteEssalud)}</dd></div>
        <div><dt>Neto a pagar</dt><dd>{moneda(boleta.neto)}</dd></div>
        <div><dt>Fecha de pago</dt><dd>{boleta.fechaPago === null ? 'Pendiente' : formatearFecha(boleta.fechaPago)}</dd></div>
      </dl>
    </article>
  );
}

// Diálogo modal propio con la boleta maquetada para imprimir. Mientras está abierto, el body lleva la clase que oculta el resto en la impresión.
export function DialogoBoleta({ detalleId, cerrar }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const cerrarRef = useRef<HTMLButtonElement>(null);
  const [resultado, setResultado] = useState<ResultadoBoleta | null>(null);

  useEffect(() => {
    document.body.classList.add(CLASE_IMPRESION);
    return () => document.body.classList.remove(CLASE_IMPRESION);
  }, []);

  useEffect(() => {
    cerrarRef.current?.focus();
  }, []);

  useEffect(() => {
    let vigente = true;
    void obtenerBoleta(detalleId).then((boleta) => {
      if (vigente) {
        setResultado({ detalleId, boleta, error: '' });
      }
    }).catch((falla: unknown) => {
      if (vigente) {
        setResultado({ detalleId, boleta: null, error: mensajeError(falla, 'No se pudo cargar la boleta.') });
      }
    });
    return () => {
      vigente = false;
    };
  }, [detalleId]);

  useDialogoModal(dialogoRef, cerrar, false);

  const actual = resultado !== null && resultado.detalleId === detalleId ? resultado : null;
  const boleta = actual === null ? null : actual.boleta;

  return (
    <div className="clientes-dialog-backdrop planillas-boleta-fondo">
      <div
        aria-labelledby="planilla-boleta-dialog-title"
        aria-modal="true"
        className="clientes-dialog planillas-dialog planillas-dialog-boleta"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow planillas-no-imprimir">PLANILLAS</div>
        <h2 className="planillas-no-imprimir" id="planilla-boleta-dialog-title">Boleta de pago</h2>
        {actual === null && <p aria-live="polite" className="clientes-status" role="status">Cargando boleta…</p>}
        {actual !== null && actual.error !== '' && <p className="clientes-error" role="alert">{actual.error}</p>}
        {boleta !== null && <Cuerpo boleta={boleta} />}
        <div className="clientes-dialog-actions planillas-no-imprimir">
          <button className="secondary-button" onClick={cerrar} ref={cerrarRef} type="button">Cerrar</button>
          <button className="primary-button" disabled={boleta === null} onClick={() => window.print()} type="button">Imprimir</button>
        </div>
      </div>
    </div>
  );
}
