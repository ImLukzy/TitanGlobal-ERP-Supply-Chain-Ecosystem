import { PanelDetalle } from '../../components/PanelDetalle';
import { EstadoMantenimientoEtiqueta } from './MantenimientosTabla';
import { formatearDia, formatearHoras, formatearInstantePeru } from './presentacion';
import type { MantenimientoDetalle } from './tipos';

interface DetalleProps {
  seleccion: number | null;
  mantenimiento?: MantenimientoDetalle;
  error?: string;
  errorAccion: string;
  iniciando: boolean;
  reintentar: () => void;
  editar: (mantenimiento: MantenimientoDetalle) => void;
  iniciar: (mantenimiento: MantenimientoDetalle) => void;
  completar: (mantenimiento: MantenimientoDetalle) => void;
  cancelar: (mantenimiento: MantenimientoDetalle) => void;
}

export function MantenimientoDetallePanel(props: DetalleProps) {
  const { seleccion, mantenimiento, error, errorAccion, iniciando, reintentar, editar, iniciar, completar, cancelar } = props;

  return (
    <PanelDetalle ariaLabelledBy="mantenimiento-preventivo-detalle" className="mantenimiento-preventivo-tarjeta">
      <h2 id="mantenimiento-preventivo-detalle" tabIndex={-1}>
        {mantenimiento ? <>Mantenimiento <span className="mantenimiento-preventivo-cifra">{mantenimiento.numero}</span></> : 'Detalle del mantenimiento'}
      </h2>
      {seleccion === null && <p>Selecciona un mantenimiento para ver su detalle.</p>}
      {seleccion !== null && mantenimiento === undefined && error === undefined && <p role="status">Cargando detalle…</p>}
      {error !== undefined && (
        <div className="mantenimiento-preventivo-error">
          <p className="field-error" role="alert">{error}</p>
          <button className="secondary-button" onClick={reintentar} type="button">Reintentar detalle</button>
        </div>
      )}
      {mantenimiento && (
        <>
          <dl className="mantenimiento-preventivo-datos">
            <dt>Estado</dt>
            <dd><EstadoMantenimientoEtiqueta estado={mantenimiento.estado} vencido={mantenimiento.vencido} /></dd>
            <dt>Centro de trabajo</dt>
            <dd><span className="mantenimiento-preventivo-cifra">{mantenimiento.centroTrabajoCodigo}</span> {mantenimiento.centroTrabajoNombre}</dd>
            <dt>Descripción</dt>
            <dd>{mantenimiento.descripcion}</dd>
            <dt>Fecha programada</dt>
            <dd className="mantenimiento-preventivo-cifra">{formatearDia(mantenimiento.fechaProgramada)}</dd>
            <dt>Duración planificada</dt>
            <dd className="mantenimiento-preventivo-cifra">{formatearHoras(mantenimiento.duracionHorasPlanificada)}</dd>
            <dt>Frecuencia</dt>
            <dd>{mantenimiento.frecuenciaDias === null ? 'Sin frecuencia' : `Cada ${mantenimiento.frecuenciaDias} días`}</dd>
            <dt>Responsable</dt>
            <dd>{mantenimiento.responsable ?? 'Sin responsable'}</dd>
            {mantenimiento.fechaInicioReal !== null && (
              <>
                <dt>Inicio real</dt>
                <dd className="mantenimiento-preventivo-cifra">{formatearInstantePeru(mantenimiento.fechaInicioReal)}</dd>
              </>
            )}
            {mantenimiento.fechaFinReal !== null && (
              <>
                <dt>Fin real</dt>
                <dd className="mantenimiento-preventivo-cifra">{formatearInstantePeru(mantenimiento.fechaFinReal)}</dd>
              </>
            )}
            {mantenimiento.horasReales !== null && (
              <>
                <dt>Horas reales</dt>
                <dd className="mantenimiento-preventivo-cifra">{formatearHoras(mantenimiento.horasReales)}</dd>
              </>
            )}
            {mantenimiento.observaciones !== null && (
              <>
                <dt>Observaciones</dt>
                <dd>{mantenimiento.observaciones}</dd>
              </>
            )}
            {mantenimiento.motivoCancelacion !== null && (
              <>
                <dt>Motivo de cancelación</dt>
                <dd>{mantenimiento.motivoCancelacion}</dd>
              </>
            )}
            {mantenimiento.fechaSiguienteSugerida !== null && (
              <>
                <dt>Siguiente parada sugerida</dt>
                <dd className="mantenimiento-preventivo-cifra">{formatearDia(mantenimiento.fechaSiguienteSugerida)}</dd>
              </>
            )}
          </dl>
          {errorAccion !== '' && <p className="field-error" role="alert">{errorAccion}</p>}
          {mantenimiento.estado === 'Programado' && (
            <div className="mantenimiento-preventivo-acciones">
              <button className="secondary-button" disabled={iniciando} id="mantenimiento-preventivo-editar" onClick={() => editar(mantenimiento)} type="button">
                Editar
              </button>
              <button className="primary-button" disabled={iniciando} id="mantenimiento-preventivo-iniciar" onClick={() => iniciar(mantenimiento)} type="button">
                {iniciando ? 'Iniciando…' : 'Iniciar'}
              </button>
              <button className="danger-button" disabled={iniciando} id="mantenimiento-preventivo-cancelar" onClick={() => cancelar(mantenimiento)} type="button">
                Cancelar
              </button>
            </div>
          )}
          {mantenimiento.estado === 'EnEjecucion' && (
            <div className="mantenimiento-preventivo-acciones">
              <button className="primary-button" id="mantenimiento-preventivo-completar" onClick={() => completar(mantenimiento)} type="button">
                Completar
              </button>
            </div>
          )}
        </>
      )}
    </PanelDetalle>
  );
}
