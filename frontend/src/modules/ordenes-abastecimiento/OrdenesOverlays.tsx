import { OrdenAccionesDialog } from './OrdenAccionesDialog';
import { OrdenFormulario } from './OrdenFormulario';
import type { GuardarOrdenAbastecimientoRequest, OrdenAbastecimientoDetalle, ProductoOrdenOpcion, ProveedorOrdenOpcion } from './tipos';

interface OrdenesOverlaysProps {
  formulario: 'crear' | 'editar' | null;
  orden: OrdenAbastecimientoDetalle | null;
  proveedores: ProveedorOrdenOpcion[];
  productos: ProductoOrdenOpcion[];
  cancelarFormulario: () => void;
  guardar: (request: GuardarOrdenAbastecimientoRequest) => Promise<void>;
  accion: 'recibir' | 'anular' | null;
  cerrarAccion: () => void;
  confirmarAccion: (motivo?: string) => Promise<void>;
  errorAccion: string;
  ocupada: boolean;
}

export function OrdenesOverlays({
  formulario,
  orden,
  proveedores,
  productos,
  cancelarFormulario,
  guardar,
  accion,
  cerrarAccion,
  confirmarAccion,
  errorAccion,
  ocupada,
}: OrdenesOverlaysProps) {
  return (
    <>
      {formulario && (
        <OrdenFormulario
          cancelar={cancelarFormulario}
          guardar={guardar}
          key={formulario === 'editar' ? `editar-${orden?.id ?? 0}` : 'crear-orden'}
          orden={formulario === 'editar' ? orden : null}
          productos={productos}
          proveedores={proveedores}
        />
      )}
      {accion && orden && (
        <OrdenAccionesDialog
          cerrar={cerrarAccion}
          confirmar={confirmarAccion}
          error={errorAccion}
          numero={orden.numero}
          ocupado={ocupada}
          tipo={accion}
        />
      )}
    </>
  );
}
