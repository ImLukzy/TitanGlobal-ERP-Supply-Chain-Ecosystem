import { CampoConError } from '../../components/CampoConError';

interface Props {
  clienteId: number | '';
  fechaValidez: string;
  observaciones: string;
  errores: {
    cliente?: string;
    fechaValidez?: string;
    observaciones?: string;
  };
  clientes: { id: number; nombre: string }[];
  cambiarCliente: (clienteId: number | '') => void;
  cambiarFechaValidez: (fecha: string) => void;
  cambiarObservaciones: (observaciones: string) => void;
}

export function CotizacionDatosFormulario({
  clienteId,
  fechaValidez,
  observaciones,
  errores,
  clientes,
  cambiarCliente,
  cambiarFechaValidez,
  cambiarObservaciones,
}: Props) {
  return (
    <div className="solicitudes-form-fields cotizacion-form-fields">
      <CampoConError
        etiqueta="Cliente"
        htmlFor="cotizacion-cliente"
        error={errores.cliente}
        idError="cotizacion-cliente-error"
      >
        <select
          id="cotizacion-cliente"
          aria-describedby={
            errores.cliente ? 'cotizacion-cliente-error' : undefined
          }
          aria-invalid={Boolean(errores.cliente)}
          value={clienteId}
          onChange={(event) =>
            cambiarCliente(
              event.currentTarget.value
                ? Number(event.currentTarget.value)
                : '',
            )
          }
        >
          <option value="">Seleccionar cliente</option>
          {clientes.map((cliente) => (
            <option key={cliente.id} value={cliente.id}>
              {cliente.nombre}
            </option>
          ))}
        </select>
      </CampoConError>
      <CampoConError
        etiqueta="Válida hasta"
        htmlFor="cotizacion-validez"
        error={errores.fechaValidez}
        idError="cotizacion-validez-error"
      >
        <input
          id="cotizacion-validez"
          aria-describedby={
            errores.fechaValidez ? 'cotizacion-validez-error' : undefined
          }
          aria-invalid={Boolean(errores.fechaValidez)}
          type="date"
          value={fechaValidez}
          onChange={(event) => cambiarFechaValidez(event.currentTarget.value)}
        />
      </CampoConError>
      <CampoConError
        etiqueta="Observaciones"
        htmlFor="cotizacion-observaciones"
        error={errores.observaciones}
        idError="cotizacion-observaciones-error"
      >
        <textarea
          id="cotizacion-observaciones"
          aria-describedby={
            errores.observaciones ? 'cotizacion-observaciones-error' : undefined
          }
          aria-invalid={Boolean(errores.observaciones)}
          maxLength={520}
          value={observaciones}
          onChange={(event) => cambiarObservaciones(event.currentTarget.value)}
        />
      </CampoConError>
    </div>
  );
}
