import type { FiltrosAlerta, TipoAlerta } from './tipos';

interface Props {
  filtros: FiltrosAlerta;
  cambiar: (cambios: Partial<FiltrosAlerta>) => void;
}

export function AlertasFiltros({ filtros, cambiar }: Props) {
  return (
    <div className="solicitudes-filters alertas-filters">
      <div>
        <label htmlFor="alertas-tipo">Tipo</label>
        <select
          id="alertas-tipo"
          onChange={(event) => cambiar({ tipo: event.currentTarget.value as TipoAlerta | '' })}
          value={filtros.tipo}
        >
          <option value="">Todos los tipos</option>
          <option value="ReclamoVencido">Reclamo vencido</option>
          <option value="OrdenAbastecimientoAtrasada">Orden de abastecimiento atrasada</option>
          <option value="ContratoExcedido">Contrato excedido</option>
          <option value="AlmacenCasiLleno">Almacén casi lleno</option>
          <option value="ContratoPorVencer">Contrato por vencer</option>
          <option value="CotizacionVencida">Cotización vencida</option>
          <option value="SolicitudPendienteAntigua">Solicitud pendiente antigua</option>
        </select>
      </div>
      <div>
        <label htmlFor="alertas-severidad">Severidad</label>
        <select
          id="alertas-severidad"
          onChange={(event) => cambiar({ severidad: event.currentTarget.value as FiltrosAlerta['severidad'] })}
          value={filtros.severidad}
        >
          <option value="">Todas</option>
          <option value="alta">Alta</option>
          <option value="media">Media</option>
          <option value="baja">Baja</option>
        </select>
      </div>
      <label className="cotizaciones-expired alertas-discarded-filter">
        <input
          checked={filtros.incluirDescartadas}
          onChange={(event) => cambiar({ incluirDescartadas: event.currentTarget.checked })}
          type="checkbox"
        />
        Mostrar descartadas
      </label>
    </div>
  );
}
