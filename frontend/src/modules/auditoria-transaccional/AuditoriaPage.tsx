import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router';
import { PaginaModulo } from '../../components/PaginaModulo';
import { AuditoriaDetallePanel } from './AuditoriaDetallePanel';
import { AuditoriaFiltros } from './AuditoriaFiltros';
import { AuditoriaResumen } from './AuditoriaResumen';
import { AuditoriaTabla } from './AuditoriaTabla';
import { useAuditoriaData } from './useAuditoriaData';
import type { FiltrosAuditoria } from './tipos';

function leerFiltrosIniciales(parametros: URLSearchParams): FiltrosAuditoria {
  return {
    entidad: parametros.get('entidad') ?? '',
    entidadId: parametros.get('entidadId') ?? '',
    accion: '',
    usuario: '',
    fechaDesde: '',
    fechaHasta: '',
    pagina: 1,
    tamanoPagina: 20,
  };
}

export function AuditoriaPage() {
  const [parametrosUrl, establecerParametrosUrl] = useSearchParams();
  const [filtros, setFiltros] = useState<FiltrosAuditoria>(() => leerFiltrosIniciales(parametrosUrl));
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const filtrosConsulta = useMemo(() => ({ ...filtros }), [filtros]);
  const datos = useAuditoriaData(filtrosConsulta, seleccionadoId);

  useEffect(() => {
    void Promise.resolve().then(() => {
      if (filtros.entidadId === '' || datos.pagina === null || datos.pagina.items.length === 0 || seleccionadoId !== null) {
        return;
      }
      setSeleccionadoId(datos.pagina.items[0].id);
    });
  }, [datos.pagina, filtros.entidadId, seleccionadoId]);

  function cambiarFiltro(cambios: Partial<FiltrosAuditoria>): void {
    setFiltros((actuales) => ({ ...actuales, ...cambios, pagina: 1 }));
    setSeleccionadoId(null);
  }

  function aplicarPagina(pagina: number): void {
    setFiltros((actuales) => ({ ...actuales, pagina }));
  }

  function verHistorial(entidad: string, entidadId: string): void {
    setFiltros((actuales) => ({ ...actuales, entidad, entidadId, pagina: 1 }));
    setSeleccionadoId(null);
    establecerParametrosUrl({ entidad, entidadId });
  }

  const registros = datos.pagina?.items ?? [];
  return (
    <PaginaModulo className="auditoria-page" modulo="77-auditoria-transaccional">
      <p className="module-intro">Consulta los cambios realizados en los registros del sistema.</p>
      <AuditoriaResumen datos={datos.resumen} error={datos.errorResumen} />
      <div className="auditoria-work-area">
        <section aria-labelledby="auditoria-list-title" className="clientes-list-panel auditoria-list-panel">
          <div className="clientes-panel-heading"><div><span className="page-eyebrow">GOBIERNO Y AUDITORÍA</span><h2 id="auditoria-list-title">Auditoría transaccional</h2></div></div>
          <AuditoriaFiltros cambiar={cambiarFiltro} entidades={datos.entidades} errorEntidades={datos.errorEntidades} filtros={filtros} />
          {datos.rangoInvalido && <p className="clientes-error" role="alert">La fecha desde no puede ser posterior a la fecha hasta.</p>}
          <AuditoriaTabla cambiarPagina={aplicarPagina} cargando={!datos.rangoInvalido && datos.pagina === null && datos.errorPagina === ''} error={datos.errorPagina} items={registros} pagina={filtros.pagina} seleccionar={setSeleccionadoId} seleccionadoId={seleccionadoId} total={datos.pagina?.total ?? 0} />
        </section>
        <AuditoriaDetallePanel cargando={seleccionadoId !== null && datos.detalle === null && datos.errorDetalle === ''} error={datos.errorDetalle} registro={datos.detalle} verHistorial={verHistorial} />
      </div>
    </PaginaModulo>
  );
}
