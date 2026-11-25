import { PanelDetalle } from '../../components/PanelDetalle';
import { formatearHoras, textoFecha } from './presentacion';
import type { CapacitacionEmpleado } from './tipos';

interface Props {
  ficha: CapacitacionEmpleado | null;
  cargando: boolean;
  error: string;
  anio: number;
}

// Ficha del empleado: cursos del año (sin retirados) y todas sus certificaciones; «Vencida» siempre con texto.
export function FichaEmpleado({ ficha, cargando, error, anio }: Props) {
  return (
    <PanelDetalle ariaLabelledBy="ficha-title" className="clientes-detail-panel capacitaciones-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando ficha…</p>}
      {error === '' && !cargando && ficha === null && <p aria-live="polite" className="clientes-status" role="status">Selecciona un empleado para ver su ficha.</p>}
      {ficha !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{ficha.empleadoLegajo}</div>
              <h2 id="ficha-title">{ficha.empleadoNombre}</h2>
            </div>
          </div>
          <p className="capacitaciones-puntaje">{`Horas del año ${anio}: ${formatearHoras(ficha.horas)}`}</p>
          <section className="capacitaciones-section">
            <h3>Cursos</h3>
            {ficha.cursos.length === 0 ? <p className="capacitaciones-aviso">El empleado no tiene cursos en {anio}.</p> : (
              <div className="capacitaciones-scroll">
                <table className="capacitaciones-mini-tabla">
                  <caption>Cursos del empleado</caption>
                  <thead>
                    <tr><th scope="col">Curso</th><th scope="col">Fechas</th><th scope="col">Horas</th><th scope="col">Estado</th><th scope="col">Nota</th></tr>
                  </thead>
                  <tbody>
                    {ficha.cursos.map((curso) => (
                      <tr key={curso.inscripcionId}>
                        <td>{`${curso.cursoCodigo} · ${curso.cursoNombre}`}</td>
                        <td className="capacitaciones-numero">{`${textoFecha(curso.fechaInicio)} - ${textoFecha(curso.fechaFin)}`}</td>
                        <td className="capacitaciones-numero">{formatearHoras(curso.horas)}</td>
                        <td>{curso.estado}</td>
                        <td className="capacitaciones-numero">{curso.nota === null ? '—' : formatearHoras(curso.nota)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
          <section className="capacitaciones-section">
            <h3>Certificaciones</h3>
            {ficha.certificaciones.length === 0 ? <p className="capacitaciones-aviso">El empleado no tiene certificaciones.</p> : (
              <div className="capacitaciones-scroll">
                <table className="capacitaciones-mini-tabla">
                  <caption>Certificaciones del empleado</caption>
                  <thead>
                    <tr><th scope="col">Código</th><th scope="col">Curso</th><th scope="col">Emisión</th><th scope="col">Vencimiento</th><th scope="col">Estado</th></tr>
                  </thead>
                  <tbody>
                    {ficha.certificaciones.map((certificacion) => (
                      <tr key={certificacion.id}>
                        <td>{certificacion.codigo}</td>
                        <td>{certificacion.cursoNombre}</td>
                        <td className="capacitaciones-numero">{textoFecha(certificacion.fechaEmision)}</td>
                        <td className="capacitaciones-numero">{certificacion.fechaVencimiento === null ? 'Sin vencimiento' : textoFecha(certificacion.fechaVencimiento)}</td>
                        <td><span className={`capacitaciones-estado ${certificacion.vencida ? 'is-vencida' : 'is-vigente'}`}>{certificacion.vencida ? 'Vencida' : 'Vigente'}</span></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </PanelDetalle>
  );
}
