import { DialogoBaja } from '../../components/DialogoBaja';
import { DecisionSolicitudDialog } from './DecisionSolicitudDialog';
import { SolicitudFormulario } from './SolicitudFormulario';
import type {
  GuardarSolicitudRequest,
  ProductoOpcion,
  ProveedorOpcion,
  SolicitudDetalle,
} from './tipos';

interface SolicitudesOverlaysProps {
  formulario: 'crear' | 'editar' | null;
  solicitud: SolicitudDetalle | null;
  proveedores: ProveedorOpcion[];
  productos: ProductoOpcion[];
  cancelarFormulario: () => void;
  guardar: (request: GuardarSolicitudRequest) => Promise<void>;
  decision: 'aprobar' | 'rechazar' | null;
  cerrarDecision: () => void;
  confirmarAprobacion: () => Promise<void>;
  confirmarRechazo: (motivo: string) => Promise<void>;
  dialogoBaja: boolean;
  cerrarBaja: () => void;
  confirmarBaja: () => void;
  eliminando: boolean;
  errorBaja: string;
}

export function SolicitudesOverlays({
  formulario,
  solicitud,
  proveedores,
  productos,
  cancelarFormulario,
  guardar,
  decision,
  cerrarDecision,
  confirmarAprobacion,
  confirmarRechazo,
  dialogoBaja,
  cerrarBaja,
  confirmarBaja,
  eliminando,
  errorBaja,
}: SolicitudesOverlaysProps) {
  return (
    <>
      {formulario && (
        <SolicitudFormulario
          key={formulario === 'editar' ? `edit-${solicitud?.id ?? 0}` : 'new-request'}
          solicitud={formulario === 'editar' ? solicitud : null}
          proveedores={proveedores}
          productos={productos}
          cancelar={cancelarFormulario}
          guardar={guardar}
        />
      )}
      {decision && solicitud && (
        <DecisionSolicitudDialog
          numero={solicitud.numero}
          tipo={decision}
          cerrar={cerrarDecision}
          confirmarAprobacion={confirmarAprobacion}
          confirmarRechazo={confirmarRechazo}
        />
      )}
      {dialogoBaja && solicitud && (
        <DialogoBaja
          entidad="solicitud"
          nombre={solicitud.numero}
          cerrar={cerrarBaja}
          confirmar={confirmarBaja}
          eliminando={eliminando}
          error={errorBaja}
        />
      )}
    </>
  );
}
