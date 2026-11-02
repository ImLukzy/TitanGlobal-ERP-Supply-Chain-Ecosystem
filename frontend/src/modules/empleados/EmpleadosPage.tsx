import { useEffect, useState } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { EmpleadoDetallePanel } from './EmpleadoDetallePanel';
import { EmpleadosFiltros } from './EmpleadosFiltros';
import { EmpleadosResumen } from './EmpleadosResumen';
import { EmpleadosTabla } from './EmpleadosTabla';
import { useEmpleadosData } from './useEmpleadosData';
import type { EstadoEmpleado, FiltrosEmpleados } from './tipos';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosEmpleados = { busqueda: '', estado: '', pagina: 1, tamanoPagina: TAMANO_PAGINA };

export function EmpleadosPage() {
  const [busqueda, setBusqueda] = useState('');
  const [filtros, setFiltros] = useState<FiltrosEmpleados>(INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const data = useEmpleadosData(filtros, seleccionadoId);

  // La búsqueda se aplica 300 ms después del último cambio y vuelve a la página 1 solo si el texto cambia.
  useEffect(() => {
    const temporizador = window.setTimeout(() => {
      setFiltros((actual) => {
        const termino = busqueda.trim();
        if (actual.busqueda === termino) {
          return actual;
        }
        return { ...actual, busqueda: termino, pagina: 1 };
      });
    }, 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
  }

  function cambiarEstado(estado: '' | EstadoEmpleado): void {
    setFiltros((actual) => ({ ...actual, estado, pagina: 1 }));
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actual) => ({ ...actual, pagina }));
  }

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  const cargandoDetalle = seleccionadoId !== null && data.empleado === null && data.errorDetalle === '';

  return (
    <PaginaModulo className="empleados-page" modulo="61-empleados">
      <p className="module-intro">Administra el legajo de los empleados: datos personales, laborales y de contacto, y su cese.</p>
      <EmpleadosResumen datos={data.resumen} error={data.errorResumen} />
      <div className="empleados-work-area">
        <section aria-labelledby="empleados-list-title" className="clientes-list-panel empleados-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">LEGAJOS</div>
              <h2 id="empleados-list-title">Todos los empleados</h2>
            </div>
          </div>
          <EmpleadosFiltros
            busqueda={busqueda}
            cambiarBusqueda={cambiarBusqueda}
            cambiarEstado={cambiarEstado}
            estado={filtros.estado}
          />
          <EmpleadosTabla
            cambiarPagina={cambiarPagina}
            cargando={data.lista === null}
            error={data.errorLista}
            items={data.lista?.items ?? []}
            pagina={filtros.pagina}
            seleccionadoId={seleccionadoId}
            seleccionar={seleccionar}
            totalPaginas={paginas}
          />
        </section>
        <EmpleadoDetallePanel
          cargando={cargandoDetalle}
          empleado={data.empleado}
          error={data.errorDetalle}
        />
      </div>
    </PaginaModulo>
  );
}
