import { aEstado, aModalidadFiltro, ESTADOS, MODALIDADES, textoEstado, textoModalidad } from './presentacion';
import type { EstadoContrato, ModalidadContrato } from './tipos';

interface Props {
  busqueda: string;
  estado: '' | EstadoContrato;
  modalidad: '' | ModalidadContrato;
  cambiarBusqueda: (valor: string) => void;
  cambiarEstado: (estado: '' | EstadoContrato) => void;
  cambiarModalidad: (modalidad: '' | ModalidadContrato) => void;
}

export function ContratosLaboralesFiltros({
  busqueda,
  estado,
  modalidad,
  cambiarBusqueda,
  cambiarEstado,
  cambiarModalidad,
}: Props) {
  return (
    <div className="solicitudes-filters contratos-laborales-filters">
      <div>
        <label htmlFor="contratos-laborales-busqueda">Buscar por número, cargo o empleado</label>
        <input
          autoComplete="off"
          id="contratos-laborales-busqueda"
          onChange={(event) => cambiarBusqueda(event.currentTarget.value)}
          placeholder="Escribe para buscar…"
          type="search"
          value={busqueda}
        />
      </div>
      <div>
        <label htmlFor="contratos-laborales-estado">Estado</label>
        <select
          id="contratos-laborales-estado"
          onChange={(event) => cambiarEstado(aEstado(event.currentTarget.value))}
          value={estado}
        >
          <option value="">Todos</option>
          {ESTADOS.map((item) => <option key={item} value={item}>{textoEstado(item)}</option>)}
        </select>
      </div>
      <div>
        <label htmlFor="contratos-laborales-modalidad">Modalidad</label>
        <select
          id="contratos-laborales-modalidad"
          onChange={(event) => cambiarModalidad(aModalidadFiltro(event.currentTarget.value))}
          value={modalidad}
        >
          <option value="">Todas</option>
          {MODALIDADES.map((item) => <option key={item} value={item}>{textoModalidad(item)}</option>)}
        </select>
      </div>
    </div>
  );
}
