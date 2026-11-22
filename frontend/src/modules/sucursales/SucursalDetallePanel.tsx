import { PanelDetalle } from '../../components/PanelDetalle';
import { fechaUtc } from './presentacion';
import type { SucursalDetalle } from './tipos';
export function SucursalDetallePanel({
  datos: d,
  ocupado,
  editar,
  eliminar,
  cambiarEstado,
}: {
  datos: SucursalDetalle;
  ocupado: boolean;
  editar: () => void;
  eliminar: () => void;
  cambiarEstado: () => void;
}) {
  return (
    <PanelDetalle ariaLabelledBy="sucursal-detalle-title" className="sucursales-card">
      <p>Sucursal</p>
      <h2 id="sucursal-detalle-title">{d.codigo}</h2>
      <p>
        Estado:{' '}
        <span className={`sucursales-badge sucursales-${d.activa ? 'activa' : 'inactiva'}`}>
          {d.activa ? 'Activa' : 'Inactiva'}
        </span>
      </p>
      <dl className="sucursales-details">
        {(
          [
            ['Nombre', d.nombre],
            ['Tipo', d.tipo],
            ['País', d.pais],
            ['Ciudad', d.ciudad],
            ['Dirección', d.direccion],
            ['Contacto', d.contacto || '—'],
            ['Email', d.email || '—'],
            ['Teléfono', d.telefono || '—'],
            ['Observaciones', d.observaciones || '—'],
          ] as const
        ).map(([l, v]) => (
          <div key={l}>
            <dt>{l}</dt>
            <dd>{v}</dd>
          </div>
        ))}
        <div>
          <dt>Apertura (UTC)</dt>
          <dd className="sucursales-number">
            {d.fechaApertura ? fechaUtc(d.fechaApertura) : 'No registrada'}
          </dd>
        </div>
        <div>
          <dt>Registro (UTC)</dt>
          <dd className="sucursales-number">{fechaUtc(d.fechaRegistro)}</dd>
        </div>
        {d.fechaCambioEstado && (
          <div>
            <dt>Último cambio de estado (UTC)</dt>
            <dd className="sucursales-number">{fechaUtc(d.fechaCambioEstado)}</dd>
          </div>
        )}
      </dl>
      <div className="sucursales-actions">
        <button className="secondary-button" disabled={ocupado} onClick={editar}>
          Editar sucursal
        </button>
        <button className="primary-button" disabled={ocupado} onClick={cambiarEstado}>
          {d.activa ? 'Desactivar sucursal' : 'Activar sucursal'}
        </button>
        {!d.activa && (
          <button className="danger-button" disabled={ocupado} onClick={eliminar}>
            Eliminar sucursal
          </button>
        )}
      </div>
    </PanelDetalle>
  );
}
