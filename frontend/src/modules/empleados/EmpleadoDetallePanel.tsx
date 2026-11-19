import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha } from '../../lib/formato';
import { EmpleadoEstadoPastilla } from './EmpleadoEstadoPastilla';
import type { EmpleadoDetalle } from './tipos';

interface Props {
  empleado: EmpleadoDetalle | null;
  cargando: boolean;
  error: string;
  editar: () => void;
  cesar: () => void;
  botonCeseRef: RefObject<HTMLButtonElement | null>;
  tituloRef: RefObject<HTMLHeadingElement | null>;
}

const TEXTO_SIN_DATO = 'Sin dato';

function textoOpcional(valor: string | null): string {
  if (valor === null || valor.trim() === '') {
    return TEXTO_SIN_DATO;
  }
  return valor;
}

export function EmpleadoDetallePanel({ empleado, cargando, error, editar, cesar, botonCeseRef, tituloRef }: Props) {
  const cesado = empleado?.estado === 'Cesado';
  return (
    <PanelDetalle ariaLabelledBy="empleado-detail-title" className="clientes-detail-panel empleados-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando empleado…</p>}
      {error === '' && !cargando && empleado === null && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona un empleado para ver su ficha.</p>
      )}
      {empleado !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{empleado.legajo}</div>
              <h2 id="empleado-detail-title" ref={tituloRef} tabIndex={-1}>{`${empleado.nombres} ${empleado.apellidos}`}</h2>
            </div>
            <span aria-hidden="true" className="cliente-initials">
              {empleado.nombres.slice(0, 1).toLocaleUpperCase('es-PE')}
            </span>
          </div>
          <section aria-labelledby="empleado-personales-title" className="empleados-detail-section">
            <h3 id="empleado-personales-title">Datos personales</h3>
            <dl className="empleados-detail-grid">
              <div>
                <dt>Documento</dt>
                <dd>{`${empleado.tipoDocumento} ${empleado.numeroDocumento}`}</dd>
              </div>
              <div>
                <dt>Fecha de nacimiento</dt>
                <dd>{formatearFecha(empleado.fechaNacimiento)}</dd>
              </div>
            </dl>
          </section>
          <section aria-labelledby="empleado-laborales-title" className="empleados-detail-section">
            <h3 id="empleado-laborales-title">Datos laborales</h3>
            <dl className="empleados-detail-grid">
              <div>
                <dt>Cargo</dt>
                <dd>{empleado.cargo}</dd>
              </div>
              <div>
                <dt>Área</dt>
                <dd>{empleado.area}</dd>
              </div>
              <div>
                <dt>Fecha de ingreso</dt>
                <dd>{formatearFecha(empleado.fechaIngreso)}</dd>
              </div>
              <div>
                <dt>Estado</dt>
                <dd><EmpleadoEstadoPastilla estado={empleado.estado} /></dd>
              </div>
            </dl>
          </section>
          <section aria-labelledby="empleado-contacto-title" className="empleados-detail-section">
            <h3 id="empleado-contacto-title">Contacto</h3>
            <dl className="empleados-detail-grid">
              <div className="empleados-detail-wide">
                <dt>Email</dt>
                <dd>{textoOpcional(empleado.email)}</dd>
              </div>
              <div>
                <dt>Teléfono</dt>
                <dd>{textoOpcional(empleado.telefono)}</dd>
              </div>
            </dl>
          </section>
          {cesado && (
            <section aria-labelledby="empleado-cese-title" className="empleados-detail-section">
              <h3 id="empleado-cese-title">Cese</h3>
              <dl className="empleados-detail-grid">
                <div>
                  <dt>Fecha de cese</dt>
                  <dd>{empleado.fechaCese === null ? TEXTO_SIN_DATO : formatearFecha(empleado.fechaCese)}</dd>
                </div>
                <div>
                  <dt>Motivo de cese</dt>
                  <dd>{textoOpcional(empleado.motivoCese)}</dd>
                </div>
              </dl>
            </section>
          )}
          <div className="clientes-detail-actions">
            <button
              aria-describedby={cesado ? 'empleado-cesado-aviso' : undefined}
              className="secondary-button"
              disabled={cesado}
              onClick={editar}
              type="button"
            >
              Editar
            </button>
            <button
              aria-describedby={cesado ? 'empleado-cesado-aviso' : undefined}
              className="danger-button"
              disabled={cesado}
              onClick={cesar}
              ref={botonCeseRef}
              type="button"
            >
              Registrar cese
            </button>
          </div>
          {cesado && <p className="empleado-aviso" id="empleado-cesado-aviso">Empleado cesado: no admite cambios.</p>}
        </>
      )}
    </PanelDetalle>
  );
}
