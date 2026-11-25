import { Link } from 'react-router';
import type { ResumenSst as Resumen } from './tipos';

interface Props {
  datos: Resumen | null;
  error: string;
}

interface Tarjeta {
  etiqueta: string;
  valor: number;
  destino: string | null;
  alerta: boolean;
}

function consulta(vista: string, filtros: Record<string, string>): string {
  return `?${new URLSearchParams({ vista, ...filtros }).toString()}`;
}

// Cada número con una vista asociada enlaza a ella con el filtro aplicado; los avisos se destacan con texto, no solo con color.
function tarjetas(datos: Resumen): Tarjeta[] {
  const rango = { desde: `${datos.anio}-01-01`, hasta: `${datos.anio}-12-31` };
  return [
    { etiqueta: 'Incidentes abiertos', valor: datos.incidentesAbiertos, destino: consulta('incidentes', { estado: 'Abierto' }), alerta: false },
    { etiqueta: 'Incidentes del año', valor: datos.incidentesAnio, destino: consulta('incidentes', rango), alerta: false },
    { etiqueta: 'Accidentes con baja', valor: datos.accidentesConBaja, destino: consulta('incidentes', { ...rango, tipo: 'AccidenteConBaja' }), alerta: false },
    { etiqueta: 'Días de baja', valor: datos.diasBajaAnio, destino: null, alerta: false },
    { etiqueta: 'EPP vencidos', valor: datos.entregasEppVencidas, destino: consulta('epp', { vencidas: 'true' }), alerta: datos.entregasEppVencidas > 0 },
    { etiqueta: 'Exámenes vencidos', valor: datos.examenesVencidos, destino: consulta('examenes', { vencidos: 'true' }), alerta: datos.examenesVencidos > 0 },
    { etiqueta: 'Empleados no aptos', valor: datos.empleadosNoAptos, destino: consulta('examenes', { resultado: 'NoApto' }), alerta: datos.empleadosNoAptos > 0 },
  ];
}

export function ResumenSst({ datos, error }: Props) {
  if (error !== '') {
    return <p aria-live="polite" className="clientes-error" role="alert">{error}</p>;
  }
  if (datos === null) {
    return <p aria-live="polite" className="clientes-status" role="status">Cargando resumen de seguridad y salud…</p>;
  }
  return (
    <section aria-label={`Resumen de seguridad y salud ${datos.anio}`} className="sst-summary">
      {tarjetas(datos).map((tarjeta) => {
        const contenido = (
          <>
            <span>{tarjeta.etiqueta}</span>
            <strong>{tarjeta.valor}</strong>
            {tarjeta.alerta && <small className="sst-aviso-resumen">Requiere atención</small>}
          </>
        );
        const clase = `cliente-stat sst-stat${tarjeta.alerta ? ' is-alerta' : ''}`;
        return tarjeta.destino === null
          ? <article className={clase} key={tarjeta.etiqueta}>{contenido}</article>
          : <Link className={`${clase} sst-stat-enlace`} key={tarjeta.etiqueta} to={tarjeta.destino}>{contenido}</Link>;
      })}
    </section>
  );
}
