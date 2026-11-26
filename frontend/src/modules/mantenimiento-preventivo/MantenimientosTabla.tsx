import { TablaPaginada } from '../../components/TablaPaginada';
import { formatearDia, formatearHoras } from './presentacion';
import { etiquetasEstado, type EstadoMantenimiento, type PaginaMantenimientos } from './tipos';

const columnas = [
  { id: 'numero', etiqueta: 'Número' },
  { id: 'centro', etiqueta: 'Centro' },
  { id: 'descripcion', etiqueta: 'Descripción' },
  { id: 'fecha', etiqueta: 'Fecha' },
  { id: 'duracion', etiqueta: 'Duración' },
  { id: 'estado', etiqueta: 'Estado' },
];

export function EstadoMantenimientoEtiqueta({ estado, vencido }: { estado: EstadoMantenimiento; vencido: boolean }) {
  return (
    <span className="mantenimiento-preventivo-estados">
      <span className={`mantenimiento-preventivo-estado is-${estado.toLowerCase()}`}>{etiquetasEstado[estado]}</span>
      {vencido && <span className="mantenimiento-preventivo-estado is-vencido">Vencido</span>}
    </span>
  );
}

interface TablaProps {
  datos: PaginaMantenimientos;
  seleccion: number | null;
  seleccionar: (id: number) => void;
  cambiarPagina: (pagina: number) => void;
}

export function MantenimientosTabla({ datos, seleccion, seleccionar, cambiarPagina }: TablaProps) {
  return (
    <TablaPaginada
      cambiarPagina={cambiarPagina}
      caption="Paradas de mantenimiento preventivo"
      className="mantenimiento-preventivo-tabla"
      classNameContenedor="mantenimiento-preventivo-tabla-scroll"
      columnas={columnas}
      elementos={datos.items}
      etiquetaRegion="Mantenimientos preventivos"
      obtenerClave={(fila) => fila.id}
      pagina={datos.pagina}
      renderizarFila={(fila) => (
        <tr className={fila.id === seleccion ? 'is-selected' : undefined}>
          <th scope="row">
            <button
              aria-label={`Ver mantenimiento ${fila.numero}`}
              aria-pressed={fila.id === seleccion}
              className="mantenimiento-preventivo-enlace"
              onClick={() => seleccionar(fila.id)}
              type="button"
            >
              {fila.numero}
            </button>
          </th>
          <td>
            <span className="mantenimiento-preventivo-cifra">{fila.centroTrabajoCodigo}</span>{' '}
            <span className="mantenimiento-preventivo-centro-nombre">{fila.centroTrabajoNombre}</span>
          </td>
          <td>{fila.descripcion}</td>
          <td className="mantenimiento-preventivo-cifra">{formatearDia(fila.fechaProgramada)}</td>
          <td className="mantenimiento-preventivo-cifra">{formatearHoras(fila.duracionHorasPlanificada)}</td>
          <td><EstadoMantenimientoEtiqueta estado={fila.estado} vencido={fila.vencido} /></td>
        </tr>
      )}
      totalPaginas={Math.max(1, Math.ceil(datos.total / datos.tamanoPagina))}
    />
  );
}
