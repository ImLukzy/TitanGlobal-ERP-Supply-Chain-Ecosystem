import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { formateadorMoneda } from '../../lib/formato';
import type { AcuerdoDetalle, CrearLineaRequest, ProductoOpcion } from './tipos';
import { validarLinea, type CampoLinea, type ErroresLinea } from './validacion';

interface LineaFormularioProps {
  acuerdo: AcuerdoDetalle;
  productos: ProductoOpcion[];
  cancelar: () => void;
  guardar: (datos: CrearLineaRequest) => Promise<void>;
}

const campos: CampoLinea[] = ['producto', 'precio', 'cantidad'];

function describir(errores: ErroresLinea, campo: CampoLinea): string | undefined {
  return errores[campo] ? `linea-${campo}-error` : undefined;
}

export function LineaFormulario({ acuerdo, productos, cancelar, guardar }: LineaFormularioProps) {
  const [productoId, setProductoId] = useState<number | ''>('');
  const [precio, setPrecio] = useState('');
  const [cantidad, setCantidad] = useState('');
  const [errores, setErrores] = useState<ErroresLinea>({});
  const [errorApi, setErrorApi] = useState('');
  const [guardando, setGuardando] = useState(false);
  const usados = new Set(acuerdo.lineas.map((linea) => linea.productoId));

  const precioNumero = Number(precio);
  const cantidadNumero = Number(cantidad);
  const hayVistaPrevia = precio.trim() !== '' && cantidad.trim() !== '' && precioNumero > 0 && cantidadNumero > 0;
  const compromiso = hayVistaPrevia ? acuerdo.montoComprometido + precioNumero * cantidadNumero : null;

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    const nuevos = validarLinea({ productoId, precio, cantidad });
    setErrores(nuevos);
    if (campos.some((campo) => nuevos[campo] !== undefined)) {
      return;
    }
    setErrorApi('');
    setGuardando(true);
    try {
      await guardar({ productoId: Number(productoId), precioAcordado: Number(precio), cantidadComprometida: Number(cantidad) });
    } catch (error: unknown) {
      setErrorApi(error instanceof ApiError ? error.message : 'No se pudo agregar la línea.');
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="linea-form-title" className="acuerdos-panel acuerdo-form">
      <div className="acuerdos-panel-heading">
        <div>
          <span className="page-eyebrow">LÍNEA DEL ACUERDO</span>
          <h2 id="linea-form-title">Agregar línea a {acuerdo.numero}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form noValidate onSubmit={(evento) => void enviar(evento)}>
        <div className="acuerdo-form-fields">
          <CampoConError etiqueta="Producto" htmlFor="linea-producto" error={errores.producto} idError="linea-producto-error">
            <select
              id="linea-producto"
              aria-describedby={describir(errores, 'producto')}
              aria-invalid={Boolean(errores.producto)}
              onChange={(evento) => setProductoId(evento.currentTarget.value === '' ? '' : Number(evento.currentTarget.value))}
              value={productoId}
            >
              <option value="">Selecciona producto</option>
              {productos.map((producto) => (
                <option disabled={usados.has(producto.id)} key={producto.id} value={producto.id}>
                  {producto.nombre}{usados.has(producto.id) ? ' (ya en el acuerdo)' : ''}
                </option>
              ))}
            </select>
          </CampoConError>
          <CampoConError etiqueta="Precio acordado" htmlFor="linea-precio" error={errores.precio} idError="linea-precio-error">
            <input
              id="linea-precio"
              aria-describedby={describir(errores, 'precio')}
              aria-invalid={Boolean(errores.precio)}
              inputMode="decimal"
              onChange={(evento) => setPrecio(evento.currentTarget.value)}
              value={precio}
            />
          </CampoConError>
          <CampoConError etiqueta="Cantidad comprometida" htmlFor="linea-cantidad" error={errores.cantidad} idError="linea-cantidad-error">
            <input
              id="linea-cantidad"
              aria-describedby={describir(errores, 'cantidad')}
              aria-invalid={Boolean(errores.cantidad)}
              inputMode="numeric"
              onChange={(evento) => setCantidad(evento.currentTarget.value)}
              value={cantidad}
            />
          </CampoConError>
        </div>
        <p aria-live="polite" className="acuerdo-preview">
          {compromiso === null
            ? 'Compromiso tras agregar: completa precio y cantidad.'
            : `Compromiso tras agregar: ${formateadorMoneda.format(compromiso)} de ${formateadorMoneda.format(acuerdo.montoMaximo)}`}
        </p>
        {errorApi && <p className="acuerdos-error" role="alert">{errorApi}</p>}
        <div className="acuerdo-form-actions">
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar línea'}</button>
        </div>
      </form>
    </section>
  );
}
