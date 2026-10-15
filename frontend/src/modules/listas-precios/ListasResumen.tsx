import type { ResumenListas } from "./tipos";

export function ListasResumen({ resumen }: { resumen: ResumenListas | null }) {
  const tarjetas = [
    ["Total de listas", resumen?.total ?? "—"],
    ["Activas", resumen?.activas ?? "—"],
    ["Vigentes", resumen?.vigentes ?? "—"],
    ["De segmento", resumen?.deSegmento ?? "—"],
    ["Generales", resumen?.generales ?? "—"],
    [
      "Productos con precio especial",
      resumen?.productosConPrecioEspecial ?? "—",
    ],
  ];

  return (
    <section
      aria-label="Resumen de listas de precios"
      className="contratos-summary listas-summary"
    >
      {tarjetas.map(([titulo, valor]) => (
        <article key={titulo}>
          <span>{titulo}</span>
          <strong>{valor}</strong>
        </article>
      ))}
    </section>
  );
}
