import { DialogoEntrega } from './DialogoEntrega';
export function DialogoSalidaEntrega({
  ocupado,
  error,
  cerrar,
  confirmar,
}: {
  ocupado: boolean;
  error: string;
  cerrar: () => void;
  confirmar: () => void;
}) {
  return (
    <DialogoEntrega
      titulo="Registrar salida a ruta"
      id="salida-entrega-title"
      ocupado={ocupado}
      error={error}
      cerrar={cerrar}
    >
      <p>El despacho pasará a En ruta. La recepción se registrará por separado.</p>
      <div className="monitoreo-entregas-actions">
        <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
          Volver
        </button>
        <button className="primary-button" type="button" disabled={ocupado} onClick={confirmar}>
          Confirmar salida
        </button>
      </div>
    </DialogoEntrega>
  );
}
