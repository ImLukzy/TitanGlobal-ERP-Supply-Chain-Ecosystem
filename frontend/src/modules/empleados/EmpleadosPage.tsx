import { useEffect, useRef, useState } from 'react';
import { PaginaModulo } from '../../components/PaginaModulo';
import { ApiError } from '../../api/client';
import { actualizarEmpleado, crearEmpleado, registrarCeseEmpleado } from './api';
import { DialogoCese } from './DialogoCese';
import { EmpleadoDetallePanel } from './EmpleadoDetallePanel';
import { EmpleadoFormulario } from './EmpleadoFormulario';
import { EmpleadosFiltros } from './EmpleadosFiltros';
import { EmpleadosResumen } from './EmpleadosResumen';
import { EmpleadosTabla } from './EmpleadosTabla';
import { useEmpleadosData } from './useEmpleadosData';
import type { CeseEmpleadoRequest, EstadoEmpleado, FiltrosEmpleados, GuardarEmpleadoRequest } from './tipos';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosEmpleados = { busqueda: '', estado: '', pagina: 1, tamanoPagina: TAMANO_PAGINA };

type ModoFormulario = 'cerrado' | 'nuevo' | 'editar';

export function EmpleadosPage() {
  const [busqueda, setBusqueda] = useState('');
  const [filtros, setFiltros] = useState<FiltrosEmpleados>(INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [modoFormulario, setModoFormulario] = useState<ModoFormulario>('cerrado');
  const [dialogoCeseAbierto, setDialogoCeseAbierto] = useState(false);
  const botonCeseRef = useRef<HTMLButtonElement>(null);
  const tituloDetalleRef = useRef<HTMLHeadingElement>(null);
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
    setModoFormulario('cerrado');
  }

  async function guardarEmpleado(request: GuardarEmpleadoRequest): Promise<void> {
    const resultado = modoFormulario === 'editar' && data.empleado !== null
      ? await actualizarEmpleado(data.empleado.id, request)
      : await crearEmpleado(request);
    setSeleccionadoId(resultado.id);
    setModoFormulario('cerrado');
    data.recargar();
  }

  async function registrarCese(request: CeseEmpleadoRequest): Promise<void> {
    if (data.empleado === null) {
      return;
    }
    try {
      const resultado = await registrarCeseEmpleado(data.empleado.id, request);
      setSeleccionadoId(resultado.id);
      setDialogoCeseAbierto(false);
      // El botón de cese queda deshabilitado: el foco pasa al título de la ficha.
      tituloDetalleRef.current?.focus();
      data.recargar();
    } catch (error: unknown) {
      // Si ya estaba cesado, el panel y la lista se ponen al día; el diálogo muestra el mensaje.
      if (error instanceof ApiError && error.statusCode === 409) {
        data.recargar();
      }
      throw error;
    }
  }

  // Al cancelar o con Escape el foco vuelve al botón que abrió el diálogo, o a la ficha si ya no admite cese.
  function cerrarCese(): void {
    setDialogoCeseAbierto(false);
    const boton = botonCeseRef.current;
    if (boton !== null && !boton.disabled) {
      boton.focus();
    } else {
      tituloDetalleRef.current?.focus();
    }
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras cesar o recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const cargandoDetalle = seleccionadoId !== null && data.empleado === null && data.errorDetalle === '';
  const nombreEmpleado = data.empleado === null ? '' : `${data.empleado.nombres} ${data.empleado.apellidos}`;

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
            <button
              className="primary-button"
              onClick={() => setModoFormulario('nuevo')}
              type="button"
            >
              Nuevo empleado
            </button>
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
          botonCeseRef={botonCeseRef}
          tituloRef={tituloDetalleRef}
          cargando={cargandoDetalle}
          cesar={() => setDialogoCeseAbierto(true)}
          editar={() => setModoFormulario('editar')}
          empleado={data.empleado}
          error={data.errorDetalle}
        />
      </div>
      {modoFormulario !== 'cerrado' && (
        <EmpleadoFormulario
          cancelar={() => setModoFormulario('cerrado')}
          empleado={modoFormulario === 'editar' ? data.empleado : null}
          guardar={guardarEmpleado}
          key={modoFormulario === 'editar' && data.empleado !== null ? `editar-${data.empleado.id}` : 'nuevo'}
        />
      )}
      {dialogoCeseAbierto && data.empleado !== null && (
        <DialogoCese
          cerrar={cerrarCese}
          fechaIngreso={data.empleado.fechaIngreso}
          nombre={nombreEmpleado}
          registrar={registrarCese}
        />
      )}
    </PaginaModulo>
  );
}
