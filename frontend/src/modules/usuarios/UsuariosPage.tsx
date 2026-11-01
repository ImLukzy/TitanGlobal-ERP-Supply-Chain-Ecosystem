import { useEffect, useMemo, useState } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { UsuarioDetallePanel } from './UsuarioDetallePanel';
import { UsuariosFiltros } from './UsuariosFiltros';
import { UsuariosResumen } from './UsuariosResumen';
import { UsuariosTabla } from './UsuariosTabla';
import { useUsuariosData } from './useUsuariosData';
import type { FiltrosUsuarios } from './tipos';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosUsuarios = { rolId: '', activo: '', pagina: 1, tamanoPagina: TAMANO_PAGINA };

export function UsuariosPage() {
  const [busqueda, setBusqueda] = useState('');
  const [busquedaAplicada, setBusquedaAplicada] = useState('');
  const [filtros, setFiltros] = useState<FiltrosUsuarios>(INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const filtrosConsulta = useMemo<FiltrosUsuarios>(() => {
    return { ...filtros, busqueda: busquedaAplicada };
  }, [filtros, busquedaAplicada]);
  const data = useUsuariosData(filtrosConsulta, seleccionadoId);

  useEffect(() => {
    const temporizador = window.setTimeout(() => setBusquedaAplicada(busqueda), 300);
    return () => window.clearTimeout(temporizador);
  }, [busqueda]);

  function cambiarBusqueda(valor: string): void {
    setBusqueda(valor);
    setFiltros((actual) => (actual.pagina === 1 ? actual : { ...actual, pagina: 1 }));
  }

  function cambiarFiltro(cambios: Partial<FiltrosUsuarios>): void {
    setFiltros((actual) => ({ ...actual, ...cambios, pagina: 1 }));
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actual) => ({ ...actual, pagina }));
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  const cargandoDetalle = seleccionadoId !== null && data.usuario === null && data.errorDetalle === '';

  return (
    <PaginaModulo className="usuarios-page" modulo="71-usuarios">
      <p className="module-intro">Administra los usuarios, sus roles y los permisos efectivos que de ellos resultan.</p>
      <UsuariosResumen datos={data.resumen} error={data.errorResumen} />
      {data.errorRoles !== '' && <p className="clientes-error" role="alert">{data.errorRoles}</p>}
      <div className="usuarios-work-area">
        <section aria-labelledby="usuarios-list-title" className="clientes-list-panel usuarios-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">DIRECTORIO</div>
              <h2 id="usuarios-list-title">Todos los usuarios</h2>
            </div>
          </div>
          <UsuariosFiltros
            busqueda={busqueda}
            cambiarBusqueda={cambiarBusqueda}
            cambiarFiltro={cambiarFiltro}
            filtros={filtros}
            roles={data.roles}
          />
          <UsuariosTabla
            cambiarPagina={cambiarPagina}
            cargando={data.lista === null}
            error={data.errorLista}
            items={data.lista?.items ?? []}
            pagina={filtros.pagina ?? 1}
            seleccionadoId={seleccionadoId}
            seleccionar={setSeleccionadoId}
            totalPaginas={paginas}
          />
        </section>
        <UsuarioDetallePanel
          cargando={cargandoDetalle}
          error={data.errorDetalle}
          errorPermisos={data.errorPermisos}
          permisos={data.permisos}
          usuario={data.usuario}
        />
      </div>
    </PaginaModulo>
  );
}
