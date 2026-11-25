import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import type { AcuerdoDetalle, CrearAcuerdoRequest, ProveedorOpcion } from './tipos';
import { validarAcuerdo, type CampoAcuerdo, type ErroresAcuerdo } from './validacion';
import { aFechaLocal } from './vigencia';

interface AcuerdoFormularioProps {
  acuerdo: AcuerdoDetalle | null;
  proveedores: ProveedorOpcion[];
  cancelar: () => void;
  guardar: (datos: CrearAcuerdoRequest) => Promise<void>;
}

const campos: CampoAcuerdo[] = ['numero', 'proveedor', 'descripcion', 'montoMaximo', 'fechas'];

function describir(errores: ErroresAcuerdo, campo: CampoAcuerdo): string | undefined {
  return errores[campo] ? `acuerdo-${campo}-error` : undefined;
}

export function AcuerdoFormulario({ acuerdo, proveedores, cancelar, guardar }: AcuerdoFormularioProps) {
  const edicion = acuerdo !== null;
  const [numero, setNumero] = useState(acuerdo?.numero ?? '');
  const [proveedorId, setProveedorId] = useState<number | ''>(acuerdo?.proveedorId ?? '');
  const [descripcion, setDescripcion] = useState(acuerdo?.descripcion ?? '');
  const [fechaInicio, setFechaInicio] = useState(acuerdo === null ? '' : aFechaLocal(acuerdo.fechaInicio));
  const [fechaFin, setFechaFin] = useState(acuerdo === null ? '' : aFechaLocal(acuerdo.fechaFin));
  const [montoMaximo, setMontoMaximo] = useState(acuerdo === null ? '' : String(acuerdo.montoMaximo));
  const [errores, setErrores] = useState<ErroresAcuerdo>({});
  const [errorApi, setErrorApi] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    const nuevos = validarAcuerdo({ numero, proveedorId, descripcion, fechaInicio, fechaFin, montoMaximo }, edicion);
    setErrores(nuevos);
    if (campos.some((campo) => nuevos[campo] !== undefined)) {
      return;
    }
    const datos: CrearAcuerdoRequest = {
      numero: numero.trim(),
      proveedorId: Number(proveedorId),
      fechaInicio: new Date(`${fechaInicio}T00:00:00`).toISOString(),
      fechaFin: new Date(`${fechaFin}T23:59:59`).toISOString(),
      montoMaximo: Number(montoMaximo),
    };
    if (descripcion.trim() !== '') {
      datos.descripcion = descripcion.trim();
    }
    setErrorApi('');
    setGuardando(true);
    try {
      await guardar(datos);
    } catch (error: unknown) {
      setErrorApi(error instanceof ApiError ? error.message : 'No se pudo guardar el acuerdo.');
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="acuerdo-form-title" className="acuerdos-panel acuerdo-form">
      <div className="acuerdos-panel-heading">
        <div>
          <span className="page-eyebrow">ACUERDO DE ABASTECIMIENTO</span>
          <h2 id="acuerdo-form-title">{edicion ? `Editar acuerdo ${acuerdo.numero}` : 'Nuevo acuerdo'}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form noValidate onSubmit={(evento) => void enviar(evento)}>
        <div className="acuerdo-form-fields">
          {edicion ? (
            <dl className="acuerdo-form-readonly">
              <div><dt>Número</dt><dd>{acuerdo.numero}</dd></div>
              <div><dt>Proveedor</dt><dd>{acuerdo.proveedorNombre}</dd></div>
            </dl>
          ) : (
            <>
              <CampoConError etiqueta="Número" htmlFor="acuerdo-numero" error={errores.numero} idError="acuerdo-numero-error">
                <input
                  id="acuerdo-numero"
                  aria-describedby={describir(errores, 'numero')}
                  aria-invalid={Boolean(errores.numero)}
                  maxLength={20}
                  onChange={(evento) => setNumero(evento.currentTarget.value.toUpperCase())}
                  value={numero}
                />
              </CampoConError>
              <CampoConError etiqueta="Proveedor" htmlFor="acuerdo-proveedor" error={errores.proveedor} idError="acuerdo-proveedor-error">
                <select
                  id="acuerdo-proveedor"
                  aria-describedby={describir(errores, 'proveedor')}
                  aria-invalid={Boolean(errores.proveedor)}
                  onChange={(evento) => setProveedorId(evento.currentTarget.value === '' ? '' : Number(evento.currentTarget.value))}
                  value={proveedorId}
                >
                  <option value="">Selecciona proveedor</option>
                  {proveedores.map((proveedor) => <option key={proveedor.id} value={proveedor.id}>{proveedor.razonSocial}</option>)}
                </select>
              </CampoConError>
            </>
          )}
          <CampoConError etiqueta="Fecha de inicio" htmlFor="acuerdo-fecha-inicio" idError="acuerdo-fechaInicio-error">
            <input id="acuerdo-fecha-inicio" onChange={(evento) => setFechaInicio(evento.currentTarget.value)} type="date" value={fechaInicio} />
          </CampoConError>
          <CampoConError etiqueta="Fecha de fin" htmlFor="acuerdo-fecha-fin" error={errores.fechas} idError="acuerdo-fechas-error">
            <input
              id="acuerdo-fecha-fin"
              aria-describedby={describir(errores, 'fechas')}
              aria-invalid={Boolean(errores.fechas)}
              onChange={(evento) => setFechaFin(evento.currentTarget.value)}
              type="date"
              value={fechaFin}
            />
          </CampoConError>
          <CampoConError etiqueta="Monto máximo" htmlFor="acuerdo-monto-maximo" error={errores.montoMaximo} idError="acuerdo-montoMaximo-error">
            <input
              id="acuerdo-monto-maximo"
              aria-describedby={describir(errores, 'montoMaximo')}
              aria-invalid={Boolean(errores.montoMaximo)}
              inputMode="decimal"
              onChange={(evento) => setMontoMaximo(evento.currentTarget.value)}
              value={montoMaximo}
            />
          </CampoConError>
          <CampoConError etiqueta="Descripción" htmlFor="acuerdo-descripcion" error={errores.descripcion} idError="acuerdo-descripcion-error">
            <textarea
              id="acuerdo-descripcion"
              aria-describedby={describir(errores, 'descripcion')}
              aria-invalid={Boolean(errores.descripcion)}
              maxLength={520}
              onChange={(evento) => setDescripcion(evento.currentTarget.value)}
              value={descripcion}
            />
            <span className="acuerdo-contador">{descripcion.length}/500</span>
          </CampoConError>
        </div>
        {errorApi && <p className="acuerdos-error" role="alert">{errorApi}</p>}
        <div className="acuerdo-form-actions">
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar acuerdo'}</button>
        </div>
      </form>
    </section>
  );
}
