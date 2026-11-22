import { DialogoVale } from './DialogoVale';
export function DialogoConfirmarVale({
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
    <DialogoVale
      titulo="Confirmar vale de combustible"
      id="confirmar-vale-title"
      ocupado={ocupado}
      error={error}
      cerrar={cerrar}
    >
      <p>Se requiere un intervalo de odómetro completo. El vale quedará de solo lectura.</p>
      <div className="control-combustible-actions">
        <button className="secondary-button" type="button" disabled={ocupado} onClick={cerrar}>
          Volver
        </button>
        <button className="primary-button" type="button" disabled={ocupado} onClick={confirmar}>
          Confirmar intervalo
        </button>
      </div>
    </DialogoVale>
  );
}
