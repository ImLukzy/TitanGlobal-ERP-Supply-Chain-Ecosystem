import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { PaginaModulo } from '../../components/PaginaModulo';
import { useSesionOpcional } from '../../layout/SesionContext';
import { aprobarAusencia, cancelarAusencia, rechazarAusencia, registrarAusencia } from './api';
import { AusenciaDetallePanel } from './AusenciaDetallePanel';
import { AusenciaFormulario } from './AusenciaFormulario';
import { AusenciasFiltros } from './AusenciasFiltros';
import { AusenciasResumen } from './AusenciasResumen';
import { AusenciasTabla } from './AusenciasTabla';
import { DialogoCancelacion, DialogoRechazo } from './DialogoRechazo';
import { mensajeError } from './presentacion';
import type { EstadoAusencia, FiltrosAusencias, RegistrarAusenciaRequest, TipoAusencia } from './tipos';
import { useAusenciasData } from './useAusenciasData';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosAusencias = {
  busqueda: '',
  tipo: '',
  estado: '',
  desde: '',
  hasta: '',
  empleadoId: null,
  pagina: 1,
  tamanoPagina: TAMANO_PAGINA,
};
const ROLES_DECISION = ['RRHH', 'ADMIN'];

type Dialogo = 'ninguno' | 'rechazar' | 'cancelar';

export function VacacionesPermisosPage() {
  const sesion = useSesionOpcional();
  const roles = sesion?.sesion?.roles ?? [];
  const puedeDecidir = roles.some((rol) => ROLES_DECISION.includes(rol));
  const [busqueda, setBusqueda] = useState('');
  const [filtros, setFiltros] = useState<FiltrosAusencias>(INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [formularioAbierto, setFormularioAbierto] = useState(false);
  const [dialogo, setDialogo] = useState<Dialogo>('ninguno');
  const [procesando, setProcesando] = useState(false);
  const [errorAccion, setErrorAccion] = useState('');
  const botonRechazarRef = useRef<HTMLButtonElement>(null);
  const botonCancelarRef = useRef<HTMLButtonElement>(null);
  const botonAprobarRef = useRef<HTMLButtonElement>(null);
  const tituloDetalleRef = useRef<HTMLHeadingElement>(null);
  const data = useAusenciasData(filtros, seleccionadoId, formularioAbierto);

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

  function cambiarTipo(tipo: '' | TipoAusencia): void {
    setFiltros((actual) => ({ ...actual, tipo, pagina: 1 }));
  }

  function cambiarEstado(estado: '' | EstadoAusencia): void {
    setFiltros((actual) => ({ ...actual, estado, pagina: 1 }));
  }

  function cambiarDesde(desde: string): void {
    setFiltros((actual) => ({ ...actual, desde, pagina: 1 }));
  }

  function cambiarHasta(hasta: string): void {
    setFiltros((actual) => ({ ...actual, hasta, pagina: 1 }));
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actual) => ({ ...actual, pagina }));
  }

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setFormularioAbierto(false);
    setErrorAccion('');
  }

  async function guardarAusencia(request: RegistrarAusenciaRequest): Promise<void> {
    const resultado = await registrarAusencia(request);
    setSeleccionadoId(resultado.id);
    setErrorAccion('');
    setFormularioAbierto(false);
    data.recargar();
  }

  // Un 409 indica que la ausencia cambió por otra vía: se pone al día la pantalla y el mensaje se muestra donde se pidió la acción.
  function alFallar(error: unknown): never {
    if (error instanceof ApiError && error.statusCode === 409) {
      data.recargar();
    }
    throw error;
  }

  async function aprobar(): Promise<void> {
    if (data.ausencia === null) {
      return;
    }
    setProcesando(true);
    setErrorAccion('');
    try {
      await aprobarAusencia(data.ausencia.id);
      tituloDetalleRef.current?.focus();
      data.recargar();
    } catch (error: unknown) {
      setErrorAccion(mensajeError(error, 'No se pudo aprobar la ausencia.'));
      if (error instanceof ApiError && error.statusCode === 409) {
        data.recargar();
      }
    } finally {
      setProcesando(false);
    }
  }

  async function rechazar(motivo: string): Promise<void> {
    if (data.ausencia === null) {
      return;
    }
    try {
      await rechazarAusencia(data.ausencia.id, { motivo });
      setDialogo('ninguno');
      setErrorAccion('');
      tituloDetalleRef.current?.focus();
      data.recargar();
    } catch (error: unknown) {
      alFallar(error);
    }
  }

  async function cancelar(): Promise<void> {
    if (data.ausencia === null) {
      return;
    }
    try {
      await cancelarAusencia(data.ausencia.id);
      setDialogo('ninguno');
      setErrorAccion('');
      // Tras cancelar, los botones desaparecen: el foco pasa al título de la ficha.
      tituloDetalleRef.current?.focus();
      data.recargar();
    } catch (error: unknown) {
      alFallar(error);
    }
  }

  // Al cancelar o con Escape el foco vuelve al botón que abrió el diálogo, o a la ficha si ya no está disponible.
  function cerrarDialogo(): void {
    const boton = dialogo === 'rechazar' ? botonRechazarRef.current : botonCancelarRef.current;
    setDialogo('ninguno');
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
  const cargandoDetalle = seleccionadoId !== null && data.ausencia === null && data.errorDetalle === '';

  return (
    <PaginaModulo className="vacaciones-permisos-page" modulo="65-vacaciones-permisos">
      <p className="module-intro">Administra las ausencias de los empleados: vacaciones, permisos y licencias, con su aprobación y el saldo vacacional.</p>
      <AusenciasResumen datos={data.resumen} error={data.errorResumen} />
      <div className="vacaciones-permisos-work-area">
        <section aria-labelledby="ausencias-list-title" className="clientes-list-panel vacaciones-permisos-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">AUSENCIAS</div>
              <h2 id="ausencias-list-title">Todas las ausencias</h2>
            </div>
            <button className="primary-button" onClick={() => setFormularioAbierto(true)} type="button">
              Nueva ausencia
            </button>
          </div>
          <AusenciasFiltros
            busqueda={busqueda}
            cambiarBusqueda={setBusqueda}
            cambiarDesde={cambiarDesde}
            cambiarEstado={cambiarEstado}
            cambiarHasta={cambiarHasta}
            cambiarTipo={cambiarTipo}
            desde={filtros.desde}
            estado={filtros.estado}
            hasta={filtros.hasta}
            tipo={filtros.tipo}
          />
          <AusenciasTabla
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
        <AusenciaDetallePanel
          aprobar={() => void aprobar()}
          ausencia={data.ausencia}
          botonAprobarRef={botonAprobarRef}
          botonCancelarRef={botonCancelarRef}
          botonRechazarRef={botonRechazarRef}
          cancelar={() => setDialogo('cancelar')}
          cargando={cargandoDetalle}
          error={data.errorDetalle}
          errorAccion={errorAccion}
          procesando={procesando}
          puedeDecidir={puedeDecidir}
          rechazar={() => setDialogo('rechazar')}
          revision={data.revision}
          tituloRef={tituloDetalleRef}
        />
      </div>
      {formularioAbierto && (
        <AusenciaFormulario
          cancelar={() => setFormularioAbierto(false)}
          empleados={data.empleados}
          errorEmpleados={data.errorEmpleados}
          guardar={guardarAusencia}
          revision={data.revision}
        />
      )}
      {dialogo === 'rechazar' && data.ausencia !== null && (
        <DialogoRechazo cerrar={cerrarDialogo} numero={data.ausencia.numero} rechazar={rechazar} />
      )}
      {dialogo === 'cancelar' && data.ausencia !== null && (
        <DialogoCancelacion cancelar={cancelar} cerrar={cerrarDialogo} numero={data.ausencia.numero} />
      )}
    </PaginaModulo>
  );
}
