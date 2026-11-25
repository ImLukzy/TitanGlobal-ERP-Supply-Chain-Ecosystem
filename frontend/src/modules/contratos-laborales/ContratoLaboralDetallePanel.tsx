import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearFecha, formateadorMoneda } from '../../lib/formato';
import { ContratoLaboralEstadoPastilla } from './ContratoLaboralEstadoPastilla';
import { TEXTO_INDETERMINADO, TEXTO_SIN_FECHA_FIN, TEXTO_SOLO_RRHH, textoModalidad } from './presentacion';
import type { ContratoLaboralDetalle } from './tipos';

interface Props {
  contrato: ContratoLaboralDetalle | null;
  cargando: boolean;
  error: string;
  puedeEscribir: boolean;
  renovar: () => void;
  agregarAdenda: () => void;
  anular: () => void;
  botonRenovarRef: RefObject<HTMLButtonElement | null>;
  botonAdendaRef: RefObject<HTMLButtonElement | null>;
  botonAnularRef: RefObject<HTMLButtonElement | null>;
  tituloRef: RefObject<HTMLHeadingElement | null>;
}

export function ContratoLaboralDetallePanel({
  contrato,
  cargando,
  error,
  puedeEscribir,
  renovar,
  agregarAdenda,
  anular,
  botonRenovarRef,
  botonAdendaRef,
  botonAnularRef,
  tituloRef,
}: Props) {
  const vigente = contrato?.estado === 'Vigente';
  const vencido = contrato?.estado === 'Vencido';
  const indeterminado = contrato?.modalidad === 'Indeterminado';
  const puedeRenovar = puedeEscribir && (vigente || vencido) && !indeterminado;
  const puedeAdendar = puedeEscribir && vigente;
  const puedeAnular = puedeEscribir && vigente;
  const cambiaCargoOSueldo = contrato !== null
    && (contrato.cargoVigente !== contrato.cargo || contrato.sueldoVigente !== contrato.sueldoBasico);
  const cerrado = contrato !== null && (contrato.estado === 'Cerrado' || contrato.estado === 'Anulado');
  const avisos: string[] = [];
  if (contrato !== null) {
    if (!puedeEscribir) {
      avisos.push(TEXTO_SOLO_RRHH);
    } else if (indeterminado && vigente) {
      avisos.push(TEXTO_INDETERMINADO);
    } else if (!vigente && !vencido) {
      avisos.push('Contrato no vigente: no admite cambios.');
    } else if (vencido) {
      avisos.push('Contrato vencido: solo se puede renovar.');
    }
  }
  const descripcionRenovar = !puedeEscribir ? 'contrato-aviso-detalle' : indeterminado ? 'contrato-aviso-detalle' : undefined;
  const descripcionOtros = avisos.length > 0 && !puedeAdendar ? 'contrato-aviso-detalle' : undefined;

  return (
    <PanelDetalle ariaLabelledBy="contrato-laboral-detail-title" className="clientes-detail-panel contratos-laborales-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando contrato…</p>}
      {error === '' && !cargando && contrato === null && (
        <p aria-live="polite" className="clientes-status" role="status">Selecciona un contrato para ver su ficha.</p>
      )}
      {contrato !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{contrato.numero}</div>
              <h2 id="contrato-laboral-detail-title" ref={tituloRef} tabIndex={-1}>{contrato.empleadoNombre}</h2>
            </div>
            <ContratoLaboralEstadoPastilla estado={contrato.estado} />
          </div>
          <section aria-labelledby="contrato-laboral-general-title" className="contratos-laborales-detail-section">
            <h3 id="contrato-laboral-general-title">Datos del contrato</h3>
            <dl className="contratos-laborales-detail-grid">
              <div>
                <dt>Empleado</dt>
                <dd>{`${contrato.empleadoLegajo} · ${contrato.empleadoNombre}`}</dd>
              </div>
              <div>
                <dt>Modalidad</dt>
                <dd>{textoModalidad(contrato.modalidad)}</dd>
              </div>
              <div>
                <dt>Inicio</dt>
                <dd>{formatearFecha(contrato.fechaInicio)}</dd>
              </div>
              <div>
                <dt>Fin</dt>
                <dd>{contrato.fechaFin === null ? TEXTO_SIN_FECHA_FIN : formatearFecha(contrato.fechaFin)}</dd>
              </div>
              <div>
                <dt>Cargo original</dt>
                <dd>{contrato.cargo}</dd>
              </div>
              <div>
                <dt>Sueldo básico original</dt>
                <dd>{formateadorMoneda.format(contrato.sueldoBasico)}</dd>
              </div>
              {cambiaCargoOSueldo && (
                <>
                  <div>
                    <dt>Cargo vigente hoy</dt>
                    <dd>{contrato.cargoVigente}</dd>
                  </div>
                  <div>
                    <dt>Sueldo básico vigente hoy</dt>
                    <dd>{formateadorMoneda.format(contrato.sueldoVigente)}</dd>
                  </div>
                </>
              )}
              {contrato.contratoAnteriorNumero !== null && (
                <div className="contratos-laborales-detail-wide">
                  <dt>Contrato anterior</dt>
                  <dd>{contrato.contratoAnteriorNumero}</dd>
                </div>
              )}
            </dl>
          </section>
          {cerrado && (
            <section aria-labelledby="contrato-laboral-cierre-title" className="contratos-laborales-detail-section">
              <h3 id="contrato-laboral-cierre-title">Cierre</h3>
              <dl className="contratos-laborales-detail-grid">
                <div>
                  <dt>Fecha de cierre</dt>
                  <dd>{contrato.fechaCierre === null ? 'Sin dato' : formatearFecha(contrato.fechaCierre)}</dd>
                </div>
                <div>
                  <dt>Motivo de cierre</dt>
                  <dd>{contrato.motivoCierre === null || contrato.motivoCierre.trim() === '' ? 'Sin dato' : contrato.motivoCierre}</dd>
                </div>
              </dl>
            </section>
          )}
          <section aria-labelledby="contrato-laboral-adendas-title" className="contratos-laborales-detail-section">
            <h3 id="contrato-laboral-adendas-title">Adendas</h3>
            {contrato.adendas.length === 0 && <p className="contratos-laborales-aviso">Sin adendas</p>}
            {contrato.adendas.length > 0 && (
              <div className="contratos-laborales-adendas-scroll">
                <table className="contratos-laborales-adendas">
                  <caption>Adendas del contrato</caption>
                  <thead>
                    <tr>
                      <th scope="col">Vigencia</th>
                      <th scope="col">Cargo</th>
                      <th scope="col">Sueldo básico</th>
                    </tr>
                  </thead>
                  <tbody>
                    {contrato.adendas.map((adenda) => (
                      <tr key={adenda.id}>
                        <td>{formatearFecha(adenda.fechaVigencia)}</td>
                        <td>{adenda.cargo}</td>
                        <td className="contratos-laborales-numero">{formateadorMoneda.format(adenda.sueldoBasico)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
          <div className="clientes-detail-actions contratos-laborales-actions">
            <button
              aria-describedby={descripcionRenovar}
              className="secondary-button"
              disabled={!puedeRenovar}
              onClick={renovar}
              ref={botonRenovarRef}
              type="button"
            >
              Renovar
            </button>
            <button
              aria-describedby={descripcionOtros}
              className="secondary-button"
              disabled={!puedeAdendar}
              onClick={agregarAdenda}
              ref={botonAdendaRef}
              type="button"
            >
              Agregar adenda
            </button>
            <button
              aria-describedby={descripcionOtros}
              className="danger-button"
              disabled={!puedeAnular}
              onClick={anular}
              ref={botonAnularRef}
              type="button"
            >
              Anular
            </button>
          </div>
          {avisos.length > 0 && <p className="contratos-laborales-aviso" id="contrato-aviso-detalle">{avisos.join(' ')}</p>}
        </>
      )}
    </PanelDetalle>
  );
}
