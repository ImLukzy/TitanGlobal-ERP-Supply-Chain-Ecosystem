import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import type { CrearOfertaRequest, LicitacionDetalle, ProveedorOpcion } from './tipos';
import { validarOferta, type CampoOferta, type ErroresOferta } from './validacion';

interface OfertaFormularioProps {
  licitacion: LicitacionDetalle;
  proveedores: ProveedorOpcion[];
  cancelar: () => void;
  guardar: (datos: CrearOfertaRequest) => Promise<void>;
}

const campos: CampoOferta[] = ['proveedor', 'monto', 'plazo', 'observaciones'];

function describir(errores: ErroresOferta, campo: CampoOferta): string | undefined {
  return errores[campo] ? `oferta-${campo}-error` : undefined;
}

export function OfertaFormulario({ licitacion, proveedores, cancelar, guardar }: OfertaFormularioProps) {
  const [proveedorId, setProveedorId] = useState<number | ''>('');
  const [monto, setMonto] = useState('');
  const [plazo, setPlazo] = useState('');
  const [observaciones, setObservaciones] = useState('');
  const [errores, setErrores] = useState<ErroresOferta>({});
  const [errorApi, setErrorApi] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const nuevos = validarOferta({ proveedorId, monto, plazo, observaciones }, licitacion.presupuestoReferencial);
    setErrores(nuevos);
    if (campos.some((campo) => nuevos[campo] !== undefined)) {
      return;
    }
    const datos: CrearOfertaRequest = {
      proveedorId: Number(proveedorId),
      monto: Number(monto),
      plazoEntregaDias: Number(plazo),
    };
    if (observaciones.trim() !== '') {
      datos.observaciones = observaciones.trim();
    }
    setErrorApi('');
    setGuardando(true);
    try {
      await guardar(datos);
    } catch (error: unknown) {
      setErrorApi(error instanceof ApiError ? error.message : 'No se pudo registrar la oferta.');
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="oferta-form-title" className="licitaciones-panel licitacion-form">
      <div className="licitaciones-panel-heading">
        <div>
          <span className="page-eyebrow">OFERTA</span>
          <h2 id="oferta-form-title">Presentar oferta a {licitacion.numero}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <div className="licitacion-form-fields">
          <CampoConError etiqueta="Proveedor" htmlFor="oferta-proveedor" error={errores.proveedor} idError="oferta-proveedor-error">
            <select
              id="oferta-proveedor"
              aria-describedby={describir(errores, 'proveedor')}
              aria-invalid={Boolean(errores.proveedor)}
              onChange={(event) => setProveedorId(event.currentTarget.value === '' ? '' : Number(event.currentTarget.value))}
              value={proveedorId}
            >
              <option value="">Selecciona proveedor</option>
              {proveedores.map((proveedor) => <option key={proveedor.id} value={proveedor.id}>{proveedor.razonSocial}</option>)}
            </select>
          </CampoConError>
          <CampoConError etiqueta="Monto" htmlFor="oferta-monto" error={errores.monto} idError="oferta-monto-error">
            <input
              id="oferta-monto"
              aria-describedby={describir(errores, 'monto')}
              aria-invalid={Boolean(errores.monto)}
              inputMode="decimal"
              onChange={(event) => setMonto(event.currentTarget.value)}
              value={monto}
            />
          </CampoConError>
          <CampoConError etiqueta="Plazo de entrega (días)" htmlFor="oferta-plazo" error={errores.plazo} idError="oferta-plazo-error">
            <input
              id="oferta-plazo"
              aria-describedby={describir(errores, 'plazo')}
              aria-invalid={Boolean(errores.plazo)}
              inputMode="numeric"
              onChange={(event) => setPlazo(event.currentTarget.value)}
              value={plazo}
            />
          </CampoConError>
          <CampoConError etiqueta="Observaciones" htmlFor="oferta-observaciones" error={errores.observaciones} idError="oferta-observaciones-error">
            <textarea
              id="oferta-observaciones"
              aria-describedby={describir(errores, 'observaciones')}
              aria-invalid={Boolean(errores.observaciones)}
              maxLength={520}
              onChange={(event) => setObservaciones(event.currentTarget.value)}
              value={observaciones}
            />
            <span className="licitacion-contador">{observaciones.length}/500</span>
          </CampoConError>
        </div>
        {errorApi && <p className="licitaciones-error" role="alert">{errorApi}</p>}
        <div className="licitacion-form-actions">
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar oferta'}</button>
        </div>
      </form>
    </section>
  );
}
