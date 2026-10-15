import type { ResumenPromociones } from './tipos';

interface Props { datos: ResumenPromociones | null; error: string; }

export function PromocionesResumen({ datos, error }: Props) {
  if (error) return <p className="clientes-error" role="alert">{error}</p>;
  if (datos === null) return <p role="status">Cargando resumen de promociones…</p>;
  const tarjetas = [
    ['Total', datos.total], ['Activas', datos.activas], ['Vigentes', datos.vigentes],
    ['De producto', datos.deProducto], ['Por monto mínimo', datos.deMontoMinimo],
    ['De segmento', datos.deSegmento], ['Descuento promedio vigente', `${datos.porcentajePromedioVigentes.toFixed(2)} %`],
  ] as const;
  return <section aria-label="Resumen de promociones" className="promociones-summary">
    {tarjetas.map(([etiqueta, valor]) => <article key={etiqueta}><span>{etiqueta}</span><strong>{valor}</strong></article>)}
  </section>;
}
