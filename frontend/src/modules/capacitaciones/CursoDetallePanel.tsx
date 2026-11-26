import type { RefObject } from 'react';
import { PanelDetalle } from '../../components/PanelDetalle';
import { ActivoPastilla } from './CursosTabla';
import { formatearHoras, TEXTO_SOLO_RRHH } from './presentacion';
import type { CursoListado } from './tipos';

interface Props {
  curso: CursoListado | null;
  cargando: boolean;
  error: string;
  errorAccion: string;
  puedeEscribir: boolean;
  enCurso: boolean;
  editar: () => void;
  alternarActivo: () => void;
  tituloRef: RefObject<HTMLHeadingElement | null>;
}

export function CursoDetallePanel({ curso, cargando, error, errorAccion, puedeEscribir, enCurso, editar, alternarActivo, tituloRef }: Props) {
  const describedBy = puedeEscribir ? undefined : 'curso-solo-rrhh';
  return (
    <PanelDetalle ariaLabelledBy="curso-detail-title" className="clientes-detail-panel capacitaciones-detail-panel">
      {error !== '' && <p className="clientes-error" role="alert">{error}</p>}
      {error === '' && cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando curso…</p>}
      {error === '' && !cargando && curso === null && <p aria-live="polite" className="clientes-status" role="status">Selecciona un curso para ver su detalle.</p>}
      {curso !== null && (
        <>
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">{curso.codigo}</div>
              <h2 id="curso-detail-title" ref={tituloRef} tabIndex={-1}>{curso.nombre}</h2>
            </div>
            <ActivoPastilla activo={curso.activo} />
          </div>
          <dl className="capacitaciones-datos">
            <div><dt>Horas</dt><dd>{formatearHoras(curso.horas)}</dd></div>
            <div><dt>Tipo</dt><dd>{curso.tipo}</dd></div>
            <div><dt>Proveedor</dt><dd>{curso.proveedor ?? 'Sin proveedor'}</dd></div>
          </dl>
          <div className="clientes-detail-actions capacitaciones-actions">
            <button aria-describedby={describedBy} className="secondary-button" disabled={!puedeEscribir} onClick={editar} type="button">Editar curso</button>
            <button aria-describedby={describedBy} className="primary-button" disabled={!puedeEscribir || enCurso} onClick={alternarActivo} type="button">
              {curso.activo ? 'Desactivar curso' : 'Activar curso'}
            </button>
          </div>
          {!puedeEscribir && <p className="capacitaciones-aviso" id="curso-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
          {errorAccion !== '' && <p aria-live="polite" className="clientes-error" role="alert">{errorAccion}</p>}
        </>
      )}
    </PanelDetalle>
  );
}
