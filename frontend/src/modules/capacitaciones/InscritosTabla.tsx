import { formatearHoras, textoFecha } from './presentacion';
import type { Certificacion, Inscripcion, ProgramacionDetalle } from './tipos';

interface Props {
  programacion: ProgramacionDetalle;
  puedeEscribir: boolean;
  enCurso: boolean;
  retirar: (inscripcion: Inscripcion) => void;
  emitir: (inscripcion: Inscripcion) => void;
}

// «Vencida» y «Vigente» se muestran con texto, nunca solo con color.
export function CertificacionCelda({ certificacion }: { certificacion: Certificacion | null }) {
  if (certificacion === null) {
    return <span className="capacitaciones-aviso-celda">Sin certificación</span>;
  }
  const vencimiento = certificacion.fechaVencimiento === null ? 'Sin vencimiento' : `Vence ${textoFecha(certificacion.fechaVencimiento)}`;
  return (
    <>
      <span>{certificacion.codigo}</span>
      <span className="capacitaciones-descripcion">{vencimiento}</span>
      <span className={`capacitaciones-estado ${certificacion.vencida ? 'is-vencida' : 'is-vigente'}`}>{certificacion.vencida ? 'Vencida' : 'Vigente'}</span>
    </>
  );
}

// Inscritos en solo lectura: el retiro solo aparece en programaciones programadas.
export function InscritosTabla({ programacion, puedeEscribir, enCurso, retirar, emitir }: Props) {
  if (programacion.inscripciones.length === 0) {
    return <p className="capacitaciones-aviso">La programación no tiene inscritos.</p>;
  }
  return (
    <div className="capacitaciones-scroll">
      <table className="capacitaciones-mini-tabla">
        <caption>Inscritos de la programación</caption>
        <thead>
          <tr>
            <th scope="col">Empleado</th>
            <th scope="col">Estado</th>
            <th scope="col">Nota</th>
            <th scope="col">Certificación</th>
            <th scope="col">Acciones</th>
          </tr>
        </thead>
        <tbody>
          {programacion.inscripciones.map((inscripcion) => (
            <tr key={inscripcion.id}>
              <td>{`${inscripcion.empleadoLegajo} · ${inscripcion.empleadoNombre}`}</td>
              <td>{inscripcion.estado}</td>
              <td className="capacitaciones-numero">{inscripcion.nota === null ? '—' : formatearHoras(inscripcion.nota)}</td>
              <td><CertificacionCelda certificacion={inscripcion.certificacion} /></td>
              <td>
                <div className="capacitaciones-fila-acciones">
                  {programacion.estado === 'Programada' && inscripcion.estado === 'Inscrito' && (
                    <button aria-label={`Retirar a ${inscripcion.empleadoNombre}`} className="secondary-button capacitaciones-fila-boton" disabled={!puedeEscribir || enCurso} onClick={() => retirar(inscripcion)} type="button">Retirar</button>
                  )}
                  {inscripcion.estado === 'Aprobado' && inscripcion.certificacion === null && (
                    <button aria-label={`Emitir certificado a ${inscripcion.empleadoNombre}`} className="secondary-button capacitaciones-fila-boton" disabled={!puedeEscribir} onClick={() => emitir(inscripcion)} type="button">Emitir certificado</button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
