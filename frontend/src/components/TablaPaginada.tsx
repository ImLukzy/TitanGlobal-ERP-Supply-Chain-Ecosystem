import type { ReactNode } from 'react';

interface ColumnaTabla {
  id: string;
  etiqueta: string;
}

interface TablaPaginadaProps<T> {
  caption: string;
  etiquetaRegion: string;
  columnas: ColumnaTabla[];
  elementos: T[];
  obtenerClave: (elemento: T) => string | number;
  renderizarFila: (elemento: T) => ReactNode;
  pagina: number;
  totalPaginas: number;
  cambiarPagina: (pagina: number) => void;
  className?: string;
  classNameContenedor?: string;
  classNamePaginacion?: string;
}

export function TablaPaginada<T>({
  caption,
  etiquetaRegion,
  columnas,
  elementos,
  obtenerClave,
  renderizarFila,
  pagina,
  totalPaginas,
  cambiarPagina,
  className = '',
  classNameContenedor = '',
  classNamePaginacion = '',
}: TablaPaginadaProps<T>) {
  return (
    <>
      <div
        aria-label={etiquetaRegion}
        className={`tabla-paginada-scroll ${classNameContenedor}`.trim()}
        role="region"
        tabIndex={0}
      >
        <table className={`tabla-paginada ${className}`.trim()}>
          <caption>{caption}</caption>
          <thead>
            <tr>
              {columnas.map((columna) => (
                <th key={columna.id} scope="col">
                  {columna.etiqueta}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {elementos.map((elemento) => (
              <FilaPaginada key={obtenerClave(elemento)} renderizar={() => renderizarFila(elemento)} />
            ))}
          </tbody>
        </table>
      </div>
      <nav aria-label={`Paginación: ${etiquetaRegion}`} className={`tabla-paginada-nav ${classNamePaginacion}`.trim()}>
        <button
          className="secondary-button"
          disabled={pagina <= 1}
          onClick={() => cambiarPagina(pagina - 1)}
          type="button"
        >
          Anterior
        </button>
        <span>Página {pagina} de {totalPaginas}</span>
        <button
          className="secondary-button"
          disabled={pagina >= totalPaginas}
          onClick={() => cambiarPagina(pagina + 1)}
          type="button"
        >
          Siguiente
        </button>
      </nav>
    </>
  );
}

function FilaPaginada({ renderizar }: { renderizar: () => ReactNode }) {
  return <>{renderizar()}</>;
}
