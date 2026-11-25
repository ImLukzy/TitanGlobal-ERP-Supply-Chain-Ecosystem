import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../api/client';
import { PaginaModulo } from '../../components/PaginaModulo';
import { useSesionOpcional } from '../../layout/SesionContext';
import { agregarAdendaContrato, anularContratoLaboral, crearContratoLaboral, renovarContratoLaboral } from './api';
import { ContratoLaboralDetallePanel } from './ContratoLaboralDetallePanel';
import { ContratoLaboralFormulario } from './ContratoLaboralFormulario';
import { ContratosLaboralesFiltros } from './ContratosLaboralesFiltros';
import { ContratosLaboralesResumen } from './ContratosLaboralesResumen';
import { ContratosLaboralesTabla } from './ContratosLaboralesTabla';
import { DialogoAdenda } from './DialogoAdenda';
import { DialogoAnulacion } from './DialogoAnulacion';
import { DialogoRenovacion } from './DialogoRenovacion';
import { TEXTO_SOLO_RRHH } from './presentacion';
import { useContratosLaboralesData } from './useContratosLaboralesData';
import type {
  AdendaContratoRequest,
  AnularContratoLaboralRequest,
  EstadoContrato,
  FiltrosContratosLaborales,
  GuardarContratoLaboralRequest,
  ModalidadContrato,
  RenovarContratoLaboralRequest,
} from './tipos';

const TAMANO_PAGINA = 10;
const INICIALES: FiltrosContratosLaborales = {
  busqueda: '',
  estado: '',
  modalidad: '',
  empleadoId: null,
  pagina: 1,
  tamanoPagina: TAMANO_PAGINA,
};
const ROLES_ESCRITURA = ['RRHH', 'ADMIN'];

type ModoFormulario = 'cerrado' | 'nuevo';
type Dialogo = 'ninguno' | 'renovar' | 'adenda' | 'anular';

export function ContratosLaboralesPage() {
  const sesion = useSesionOpcional();
  const roles = sesion?.sesion?.roles ?? [];
  const puedeEscribir = roles.some((rol) => ROLES_ESCRITURA.includes(rol));
  const [busqueda, setBusqueda] = useState('');
  const [filtros, setFiltros] = useState<FiltrosContratosLaborales>(INICIALES);
  const [seleccionadoId, setSeleccionadoId] = useState<number | null>(null);
  const [modoFormulario, setModoFormulario] = useState<ModoFormulario>('cerrado');
  const [dialogo, setDialogo] = useState<Dialogo>('ninguno');
  const botonRenovarRef = useRef<HTMLButtonElement>(null);
  const botonAdendaRef = useRef<HTMLButtonElement>(null);
  const botonAnularRef = useRef<HTMLButtonElement>(null);
  const tituloDetalleRef = useRef<HTMLHeadingElement>(null);
  const data = useContratosLaboralesData(filtros, seleccionadoId, modoFormulario === 'nuevo');

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

  function cambiarEstado(estado: '' | EstadoContrato): void {
    setFiltros((actual) => ({ ...actual, estado, pagina: 1 }));
  }

  function cambiarModalidad(modalidad: '' | ModalidadContrato): void {
    setFiltros((actual) => ({ ...actual, modalidad, pagina: 1 }));
  }

  function cambiarPagina(pagina: number): void {
    setFiltros((actual) => ({ ...actual, pagina }));
  }

  function seleccionar(id: number): void {
    setSeleccionadoId(id);
    setModoFormulario('cerrado');
  }

  async function guardarContrato(request: GuardarContratoLaboralRequest): Promise<void> {
    const resultado = await crearContratoLaboral(request);
    setSeleccionadoId(resultado.id);
    setModoFormulario('cerrado');
    data.recargar();
  }

  // Un 409 indica que el contrato cambió por otra vía: se pone al día la pantalla y el diálogo muestra el mensaje.
  function alFallar(error: unknown): never {
    if (error instanceof ApiError && error.statusCode === 409) {
      data.recargar();
    }
    throw error;
  }

  async function renovarContrato(request: RenovarContratoLaboralRequest): Promise<void> {
    if (data.contrato === null) {
      return;
    }
    try {
      const resultado = await renovarContratoLaboral(data.contrato.id, request);
      setSeleccionadoId(resultado.id);
      setDialogo('ninguno');
      tituloDetalleRef.current?.focus();
      data.recargar();
    } catch (error: unknown) {
      alFallar(error);
    }
  }

  async function agregarAdenda(request: AdendaContratoRequest): Promise<void> {
    if (data.contrato === null) {
      return;
    }
    try {
      await agregarAdendaContrato(data.contrato.id, request);
      setDialogo('ninguno');
      tituloDetalleRef.current?.focus();
      data.recargar();
    } catch (error: unknown) {
      alFallar(error);
    }
  }

  async function anularContrato(request: AnularContratoLaboralRequest): Promise<void> {
    if (data.contrato === null) {
      return;
    }
    try {
      await anularContratoLaboral(data.contrato.id, request);
      setDialogo('ninguno');
      // Tras anular, los botones quedan deshabilitados: el foco pasa al título de la ficha.
      tituloDetalleRef.current?.focus();
      data.recargar();
    } catch (error: unknown) {
      alFallar(error);
    }
  }

  // Al cancelar o con Escape el foco vuelve al botón que abrió el diálogo, o a la ficha si ya no está disponible.
  function cerrarDialogo(): void {
    const boton = dialogo === 'renovar' ? botonRenovarRef.current : dialogo === 'adenda' ? botonAdendaRef.current : botonAnularRef.current;
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
  const cargandoDetalle = seleccionadoId !== null && data.contrato === null && data.errorDetalle === '';

  return (
    <PaginaModulo className="contratos-laborales-page" modulo="62-contratos-laborales">
      <p className="module-intro">Administra los contratos laborales: alta, renovación, adendas y anulación, con su historial por empleado.</p>
      <ContratosLaboralesResumen datos={data.resumen} error={data.errorResumen} />
      <div className="contratos-laborales-work-area">
        <section aria-labelledby="contratos-laborales-list-title" className="clientes-list-panel contratos-laborales-list-panel">
          <div className="clientes-panel-heading">
            <div>
              <div className="page-eyebrow">CONTRATOS</div>
              <h2 id="contratos-laborales-list-title">Todos los contratos</h2>
            </div>
            <button
              aria-describedby={puedeEscribir ? undefined : 'contratos-laborales-solo-rrhh'}
              className="primary-button"
              disabled={!puedeEscribir}
              onClick={() => setModoFormulario('nuevo')}
              type="button"
            >
              Nuevo contrato
            </button>
          </div>
          {!puedeEscribir && <p className="contratos-laborales-aviso" id="contratos-laborales-solo-rrhh">{TEXTO_SOLO_RRHH}</p>}
          <ContratosLaboralesFiltros
            busqueda={busqueda}
            cambiarBusqueda={setBusqueda}
            cambiarEstado={cambiarEstado}
            cambiarModalidad={cambiarModalidad}
            estado={filtros.estado}
            modalidad={filtros.modalidad}
          />
          <ContratosLaboralesTabla
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
        <ContratoLaboralDetallePanel
          agregarAdenda={() => setDialogo('adenda')}
          anular={() => setDialogo('anular')}
          botonAdendaRef={botonAdendaRef}
          botonAnularRef={botonAnularRef}
          botonRenovarRef={botonRenovarRef}
          cargando={cargandoDetalle}
          contrato={data.contrato}
          error={data.errorDetalle}
          puedeEscribir={puedeEscribir}
          renovar={() => setDialogo('renovar')}
          tituloRef={tituloDetalleRef}
        />
      </div>
      {modoFormulario === 'nuevo' && puedeEscribir && (
        <ContratoLaboralFormulario
          cancelar={() => setModoFormulario('cerrado')}
          empleados={data.empleados}
          errorEmpleados={data.errorEmpleados}
          guardar={guardarContrato}
        />
      )}
      {dialogo === 'renovar' && data.contrato !== null && (
        <DialogoRenovacion cerrar={cerrarDialogo} contrato={data.contrato} renovar={renovarContrato} />
      )}
      {dialogo === 'adenda' && data.contrato !== null && (
        <DialogoAdenda agregar={agregarAdenda} cerrar={cerrarDialogo} numero={data.contrato.numero} />
      )}
      {dialogo === 'anular' && data.contrato !== null && (
        <DialogoAnulacion anular={anularContrato} cerrar={cerrarDialogo} numero={data.contrato.numero} />
      )}
    </PaginaModulo>
  );
}
