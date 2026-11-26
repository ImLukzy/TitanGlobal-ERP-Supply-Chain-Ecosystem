import { useCallback, useState, type FormEvent } from 'react';
import { CampoModulo } from '../../components/CampoModulo';
import { textoFormulario } from '../../lib/accionesFinanzas';
import { useConsultaPantalla } from '../../lib/consultaPantalla';
import { diaDePeru } from '../../lib/fechaNegocioPeru';
import type { ProveedorListado } from '../proveedores/tipos';
import { ordenesProveedor } from './api';
import type { GuardarCuentaPorPagar } from './tipos';

export function FormularioCuentaPorPagar({ proveedores, ocupado, guardar, cancelar }: {
  proveedores: ProveedorListado[]; ocupado: boolean; guardar: (datos: GuardarCuentaPorPagar) => void; cancelar: () => void;
}) {
  const [proveedor, elegirProveedor] = useState('');
  const [error, indicarError] = useState('');
  const cargar = useCallback(() => ordenesProveedor(Number(proveedor)), [proveedor]);
  const ordenes = useConsultaPantalla(proveedor === '' ? null : cargar);

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const campos = new FormData(evento.currentTarget);
    const serie = textoFormulario(campos, 'serie');
    const numeroDocumento = textoFormulario(campos, 'numero');
    const fechaDocumento = textoFormulario(campos, 'fechaDocumento');
    const fechaVencimiento = textoFormulario(campos, 'fechaVencimiento');
    const base = textoFormulario(campos, 'base');
    const impuesto = textoFormulario(campos, 'impuesto');
    if (!serie || !numeroDocumento) {
      indicarError('La serie y el número de la factura del proveedor son obligatorios y deben respetar su longitud.');
      return;
    }
    if (!fechaVencimiento) {
      indicarError('La fecha de vencimiento es obligatoria.');
      return;
    }
    if (!proveedor || !fechaDocumento || fechaDocumento > diaDePeru() || !base || !impuesto ||
      Number(base) < 0 || Number(impuesto) < 0 || Number(base) + Number(impuesto) <= 0) {
      indicarError('Indica un proveedor activo, una fecha documentaria válida e importes que sumen un total positivo.');
      return;
    }
    guardar({
      proveedorId: Number(proveedor), ordenAbastecimientoId: Number(campos.get('orden')) || null, serie, numeroDocumento,
      fechaDocumento, fechaVencimiento, moneda: campos.get('moneda') === 'USD' ? 'USD' : 'PEN',
      baseImponible: Number(base), importeImpuesto: Number(impuesto),
    });
  }

  return <form onSubmit={enviar} aria-label="Factura de proveedor">
    {error && <p role="alert" className="field-error">{error}</p>}
    {ordenes.error && <p role="alert">{ordenes.error}</p>}
    <fieldset disabled={ocupado} className="finanzas-campos">
      <CampoModulo etiqueta="Proveedor activo">{id => <select id={id} required value={proveedor}
        onChange={e => elegirProveedor(e.target.value)}>
        <option value="">Seleccionar proveedor</option>
        {proveedores.filter(item => item.activo).map(item => <option key={item.id} value={item.id}>{item.razonSocial}</option>)}
      </select>}</CampoModulo>
      <CampoModulo etiqueta="Orden de abastecimiento (opcional)">{id => <select id={id} name="orden" key={proveedor}>
        <option value="">Sin orden</option>
        {ordenes.datos?.filter(item => String(item.proveedorId) === proveedor).map(item =>
          <option key={item.id} value={item.id}>{item.numero}</option>)}
      </select>}</CampoModulo>
      <CampoModulo etiqueta="Serie">{id => <input id={id} name="serie" required maxLength={10} />}</CampoModulo>
      <CampoModulo etiqueta="Número">{id => <input id={id} name="numero" required maxLength={40} />}</CampoModulo>
      <CampoModulo etiqueta="Fecha documentaria">{id => <input id={id} name="fechaDocumento" type="date" required
        defaultValue={diaDePeru()} max={diaDePeru()} />}</CampoModulo>
      <CampoModulo etiqueta="Fecha de vencimiento">{id => <input id={id} name="fechaVencimiento" type="date" required />}</CampoModulo>
      <CampoModulo etiqueta="Moneda">{id => <select id={id} name="moneda"><option>PEN</option><option>USD</option></select>}</CampoModulo>
      <CampoModulo etiqueta="Base imponible">{id => <input id={id} name="base" type="number" min="0" step="0.01" required />}</CampoModulo>
      <CampoModulo etiqueta="Impuesto">{id => <input id={id} name="impuesto" type="number" min="0" step="0.01" required defaultValue={0} />}</CampoModulo>
    </fieldset>
    <div className="finanzas-acciones">
      <button type="button" className="secondary-button" disabled={ocupado} onClick={cancelar}>Cancelar</button>
      <button type="submit" className="primary-button" disabled={ocupado}>Guardar factura</button>
    </div>
  </form>;
}
