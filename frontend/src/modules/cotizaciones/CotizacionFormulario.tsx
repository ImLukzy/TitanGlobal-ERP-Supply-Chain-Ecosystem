import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import {
  CotizacionLineaFormulario,
  type LineaFormulario,
} from './CotizacionLineaFormulario';
import { CotizacionDatosFormulario } from './CotizacionDatosFormulario';
import { formateadorMoneda } from '../../lib/formato';
import type {
  CotizacionDetalle,
  GuardarCotizacionRequest,
  LineaCotizacionRequest,
} from './tipos';

interface Props {
  cotizacion: CotizacionDetalle | null;
  clientes: { id: number; nombre: string }[];
  productos: { id: number; nombre: string; precio: number }[];
  cancelar: () => void;
  guardar: (request: GuardarCotizacionRequest) => Promise<void>;
}

interface Errores {
  cliente?: string;
  fechaValidez?: string;
  observaciones?: string;
  lineas?: string;
  campos: Record<string, string>;
}

function redondearImporte(valor: number): number {
  return Math.round((valor + Number.EPSILON) * 100) / 100;
}

function lineaVacia(): LineaFormulario {
  return {
    productoId: '',
    cantidad: '1',
    precioUnitario: '',
    descuentoPorcentaje: '0',
  };
}

function crearLineas(cotizacion: CotizacionDetalle | null): LineaFormulario[] {
  if (cotizacion === null || cotizacion.lineas.length === 0) {
    return [lineaVacia()];
  }
  return cotizacion.lineas.map((linea) => ({
    productoId: linea.productoId,
    cantidad: String(linea.cantidad),
    precioUnitario: String(linea.precioUnitario),
    descuentoPorcentaje: String(linea.descuentoPorcentaje),
  }));
}

export function CotizacionFormulario({
  cotizacion,
  clientes,
  productos,
  cancelar,
  guardar,
}: Props) {
  const [clienteId, setClienteId] = useState<number | ''>(
    cotizacion?.clienteId ?? '',
  );
  const [fechaValidez, setFechaValidez] = useState(
    cotizacion?.fechaValidez.slice(0, 10) ?? '',
  );
  const [observaciones, setObservaciones] = useState(
    cotizacion?.observaciones ?? '',
  );
  const [lineas, setLineas] = useState<LineaFormulario[]>(() =>
    crearLineas(cotizacion),
  );
  const [errores, setErrores] = useState<Errores>({ campos: {} });
  const [errorApi, setErrorApi] = useState('');
  const [guardando, setGuardando] = useState(false);
  const importes = lineas.map((linea) => {
    const subtotal = redondearImporte(
      Number(linea.cantidad || 0) * Number(linea.precioUnitario || 0),
    );
    const descuento = redondearImporte(
      (subtotal * Number(linea.descuentoPorcentaje || 0)) / 100,
    );
    return { subtotal, descuento, importe: subtotal - descuento };
  });
  const total = importes.reduce((suma, item) => suma + item.importe, 0);

  function cambiarLinea(
    indice: number,
    cambio: Partial<LineaFormulario>,
  ): void {
    setLineas((actuales) =>
      actuales.map((linea, actual) =>
        actual === indice ? { ...linea, ...cambio } : linea,
      ),
    );
  }

  function validar(): Errores {
    const nuevos: Errores = { campos: {} };
    if (clienteId === '') {
      nuevos.cliente = 'El cliente es obligatorio.';
    }
    const hoy = new Date().toISOString().slice(0, 10);
    if (fechaValidez === '') {
      nuevos.fechaValidez = 'La fecha de validez es obligatoria.';
    } else if (fechaValidez < hoy) {
      nuevos.fechaValidez = 'La fecha de validez no puede ser anterior a hoy.';
    }
    if (observaciones.trim().length > 500) {
      nuevos.observaciones =
        'Las observaciones no pueden superar 500 caracteres.';
    }
    if (lineas.length === 0) {
      nuevos.lineas = 'La cotización debe tener al menos una línea.';
    }
    const vistos = new Set<number>();
    lineas.forEach((linea, indice) => {
      if (linea.productoId === '') {
        nuevos.campos[`producto-${indice}`] = 'Selecciona un producto.';
      } else if (vistos.has(linea.productoId)) {
        nuevos.campos[`producto-${indice}`] =
          `El producto ${linea.productoId} está repetido en la cotización.`;
      } else {
        vistos.add(linea.productoId);
      }
      if (linea.cantidad.trim() === '' || Number(linea.cantidad) <= 0) {
        nuevos.campos[`cantidad-${indice}`] =
          'La cantidad debe ser mayor que 0.';
      }
      if (
        linea.precioUnitario.trim() === '' ||
        Number(linea.precioUnitario) <= 0
      ) {
        nuevos.campos[`precio-${indice}`] =
          'El precio unitario debe ser mayor que 0.';
      }
      if (
        Number(linea.descuentoPorcentaje) < 0 ||
        Number(linea.descuentoPorcentaje) > 100
      ) {
        nuevos.campos[`descuento-${indice}`] =
          'El descuento debe estar entre 0 y 100.';
      }
    });
    return nuevos;
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const nuevos = validar();
    setErrores(nuevos);
    if (
      nuevos.cliente ||
      nuevos.fechaValidez ||
      nuevos.observaciones ||
      nuevos.lineas ||
      Object.keys(nuevos.campos).length > 0
    ) {
      return;
    }
    const request: GuardarCotizacionRequest = {
      clienteId: Number(clienteId),
      fechaValidez,
      lineas: lineas.map(
        (linea): LineaCotizacionRequest => ({
          productoId: Number(linea.productoId),
          cantidad: Number(linea.cantidad),
          precioUnitario: Number(linea.precioUnitario),
          descuentoPorcentaje: Number(linea.descuentoPorcentaje),
        }),
      ),
    };
    if (observaciones.trim()) {
      request.observaciones = observaciones.trim();
    }
    setErrorApi('');
    setGuardando(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      setErrorApi(
        error instanceof ApiError
          ? error.message
          : 'No se pudo guardar la cotización.',
      );
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section
      aria-labelledby="cotizacion-form-title"
      className="solicitudes-panel solicitudes-form"
    >
      <div className="solicitudes-panel-heading">
        <div>
          <span className="page-eyebrow">COTIZACIÓN</span>
          <h2 id="cotizacion-form-title">
            {cotizacion ? `Editar ${cotizacion.numero}` : 'Nueva cotización'}
          </h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">
          Cancelar
        </button>
      </div>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <CotizacionDatosFormulario
          clienteId={clienteId}
          fechaValidez={fechaValidez}
          observaciones={observaciones}
          errores={errores}
          clientes={clientes}
          cambiarCliente={setClienteId}
          cambiarFechaValidez={setFechaValidez}
          cambiarObservaciones={setObservaciones}
        />
        <div className="solicitudes-lines-heading">
          <h3>Líneas de la cotización</h3>
          <button
            className="secondary-button"
            onClick={() => setLineas((actuales) => [...actuales, lineaVacia()])}
            type="button"
          >
            Añadir línea
          </button>
        </div>
        {errores.lineas && (
          <p className="solicitudes-field-error" role="alert">
            {errores.lineas}
          </p>
        )}
        <div className="solicitudes-lines">
          {lineas.map((linea, indice) => (
            <CotizacionLineaFormulario
              key={indice}
              indice={indice}
              linea={linea}
              productos={productos}
              errores={errores.campos}
              importe={importes[indice]?.importe ?? 0}
              cambiarLinea={cambiarLinea}
              quitar={(lineaIndice) =>
                setLineas((actuales) =>
                  actuales.filter((_, actual) => actual !== lineaIndice),
                )
              }
            />
          ))}
        </div>
        <p className="solicitud-total">
          Total: <strong>{formateadorMoneda.format(total)}</strong>
        </p>
        {errorApi && (
          <p className="solicitudes-error" role="alert">
            {errorApi}
          </p>
        )}
        <div className="solicitudes-form-actions">
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar cotización'}
          </button>
        </div>
      </form>
    </section>
  );
}
