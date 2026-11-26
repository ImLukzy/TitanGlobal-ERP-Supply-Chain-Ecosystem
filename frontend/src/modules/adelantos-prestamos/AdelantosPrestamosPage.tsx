import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { PaginaModulo } from '../../components/PaginaModulo';
import { useSesionOpcional } from '../../layout/SesionContext';
import { anularPrestamo, registrarPrestamo } from './api';
import { DialogoAnulacionPrestamo } from './DialogoAnulacionPrestamo';
import { PrestamoDetallePanel } from './PrestamoDetallePanel';
import { PrestamoFormulario } from './PrestamoFormulario';
import { PrestamosFiltros } from './PrestamosFiltros';
import { PrestamosResumen } from './PrestamosResumen';
import { PrestamosTabla } from './PrestamosTabla';
import { TEXTO_SOLO_RRHH } from './presentacion';
import type { EstadoPrestamo, FiltrosPrestamos, RegistrarPrestamoRequest, TipoPrestamo } from './tipos';
import { usePrestamosData } from './usePrestamosData';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosPrestamos = {
  busqueda: '',
  tipo: '',
  estado: '',
  empleadoId: null,
  pagina: 1,
  tamanoPagina: TAMANO_PAGINA,
};
const ROLES_ESCRITURA = ['RRHH', 'ADMIN'];

export function AdelantosPrestamosPage() {
  const sesion = useSesionOpcional();
  const roles = sesion?.sesion?.roles ?? [];
  const puedeEscribir = roles.some((rol) => ROLES_ESCRITURA.includes(rol));
  const [busqueda, setBusqueda] = useState('');
  const [filtros, setFiltros] = useState<FiltrosPrestamos>(INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [formularioAbierto, setFormularioAbierto] = useState(false);
  const [dialogoAbierto, setDialogoAbierto] = useState(false);
  const botonAnularRef = useRef<HTMLButtonElement>(null);
  const tituloDetalleRef = useRef<HTMLHeadingElement>(null);
  const data = usePrestamosData(filtros, seleccionadoId, formularioAbierto);

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

  function cambiarTipo(tipo: '' | TipoPrestamo): void {
    setFiltros((actual) => ({ ...actual, tipo, pagina: 1 }));
  }

  function cambiarEstado(estado: '' | EstadoPrestamo): void {
    setFiltros((actual) => ({ ...actual, estado, pagina: 1 }));
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actual) => ({ ...actual, pagina }));
  }

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setFormularioAbierto(false);
  }

  async function guardarPrestamo(request: RegistrarPrestamoRequest): Promise<void> {
    const resultado = await registrarPrestamo(request);
    setSeleccionadoId(resultado.id);
    setFormularioAbierto(false);
    data.recargar();
  }

  // Un 409 indica que el préstamo cambió por otra vía: se pone al día la pantalla y el mensaje se muestra dentro del diálogo.
  async function anular(motivo: string): Promise<void> {
    if (data.prestamo === null) {
      return;
    }
    try {
      await anularPrestamo(data.prestamo.id, { motivo });
      setDialogoAbierto(false);
        // Tras anular, el botón queda deshabilitado: el foco pasa al título de la ficha.
      tituloDetalleRef.current?.focus();
      data.recargar();
    } catch (error: unknown) {
      if (error instanceof ApiError && error.statusCode === 409) {
        data.recargar();
      }
      throw error;
    }
  }

  // Al cancelar o con Escape el foco vuelve al botón que abrió el diálogo, o a la ficha si ya no está disponible.
  function cerrarDialogo(): void {
    const boton = botonAnularRef.current;
    setDialogoAbierto(false);
    if (boton !== null && !boton.disabled) {
      boton.focus();
    } else {
      tituloDetalleRef.current?.focus();
    }
  }

  const paginas = Math.max(1, Math.ceil((data.lista?.total ?? 0) / TAMANO_PAGINA));
  // Si tras recargar la página actual queda fuera de rango, se vuelve a la última válida.
  if (data.lista !== null && data.lista.pagina === filtros.pagina && filtros.pagina > paginas) {
    setFiltros({ ...filtros, pagina: paginas });
  }
  const cargandoDetalle = seleccionadoId !== null && data.prestamo === null && data.errorDetalle === '';

  return (
    <PaginaModulo className="adelantos-prestamos-page" modulo="67-adelantos-prestamos">
      <p className="module-intro">Registra adelantos y préstamos al personal con su cronograma de cuotas mensuales y el tope de descuento sobre el sueldo neto estimado.</p>
      <PrestamosResumen datos={data.resumen} error={data.errorResumen} />
      <div className="adelantos-prestamos-work-area">
        <section aria-labelledby="prestamos-list-title" className="clientes-list-panel adelantos-prestamos-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">PRÉSTAMOS</div>
              <h2 id="prestamos-list-title">Todos los préstamos</h2>
            </div>
            <button
              aria-describedby={puedeEscribir ? undefined : 'prestamo-nuevo-solo-rrhh'}
              className="primary-button"
              disabled={!puedeEscribir}
              onClick={() => setFormularioAbierto(true)}
              type="button"
            >
              Nuevo préstamo
            </button>
          </div>
          {!puedeEscribir && <p className="adelantos-prestamos-aviso" id="prestamo-nuevo-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
          <PrestamosFiltros
            busqueda={busqueda}
            cambiarBusqueda={setBusqueda}
            cambiarEstado={cambiarEstado}
            cambiarTipo={cambiarTipo}
            estado={filtros.estado}
            tipo={filtros.tipo}
          />
          <PrestamosTabla
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
        <PrestamoDetallePanel
          anular={() => setDialogoAbierto(true)}
          botonAnularRef={botonAnularRef}
          cargando={cargandoDetalle}
          error={data.errorDetalle}
          prestamo={data.prestamo}
          puedeEscribir={puedeEscribir}
          tituloRef={tituloDetalleRef}
        />
      </div>
      {formularioAbierto && (
        <PrestamoFormulario
          cancelar={() => setFormularioAbierto(false)}
          empleados={data.empleados}
          errorEmpleados={data.errorEmpleados}
          guardar={guardarPrestamo}
          revision={data.revision}
        />
      )}
      {dialogoAbierto && data.prestamo !== null && (
        <DialogoAnulacionPrestamo anular={anular} cerrar={cerrarDialogo} numero={data.prestamo.numero} />
      )}
    </PaginaModulo>
  );
}
