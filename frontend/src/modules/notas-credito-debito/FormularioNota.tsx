import { useCallback, useState, type FormEvent } from 'react';
import { CampoModulo } from '../../components/CampoModulo';
import { textoFormulario } from '../../lib/accionesFinanzas';
import { useConsultaPantalla } from '../../lib/consultaPantalla';
import { facturaParaNota, facturasElegibles } from './api';
import type { CrearNota, NotaDetalle } from './tipos';

interface Fila { clave: number; id: string; cantidad: string; precio: string }

const vacia: Fila = { clave: 0, id: '', cantidad: '', precio: '' };

export function FormularioNota({ inicial, ocupado, guardar, cancelar }: {
  inicial?: NotaDetalle; ocupado: boolean; guardar: (nota: CrearNota) => void; cancelar: () => void;
}) {
  const [factura, seleccionar] = useState(inicial?.facturaId.toString() ?? '');
  const [filas, cambiarFilas] = useState<Fila[]>(() => inicial?.detalles.map((linea, clave) => ({
    clave, id: String(linea.detalleFacturaId), cantidad: String(linea.cantidad), precio: String(linea.precioUnitario),
  })) ?? [vacia]);
  const [error, indicarError] = useState('');
  const elegibles = useConsultaPantalla(inicial ? null : facturasElegibles);
  const cargar = useCallback(() => facturaParaNota(Number(factura)), [factura]);
  const origen = useConsultaPantalla(factura ? cargar : null);

  function actualizar(clave: number, cambio: Partial<Fila>) {
    cambiarFilas(actual => actual.map(fila => fila.clave === clave ? { ...fila, ...cambio } : fila));
  }

  function enviar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const datos = new FormData(evento.currentTarget);
    const motivo = textoFormulario(datos, 'motivo');
    if (motivo.length < 3 || motivo.length > 300) {
      indicarError('El motivo debe tener entre 3 y 300 caracteres.');
      return;
    }
    if (!factura || !origen.datos || filas.some(fila => !fila.id) || new Set(filas.map(fila => fila.id)).size !== filas.length) {
      indicarError('Indique al menos una línea válida y sin duplicados.');
      return;
    }
    if (filas.some(fila => Number(fila.cantidad) <= 0 || Number(fila.precio) <= 0)) {
      indicarError('La cantidad o el importe de la nota no es válido.');
      return;
    }
    guardar({
      facturaId: Number(factura), tipo: inicial?.tipo ?? (datos.get('tipo') === 'Debito' ? 'Debito' : 'Credito'),
      motivo, detalles: filas.map(fila => ({ detalleFacturaId: Number(fila.id), cantidad: Number(fila.cantidad), precioUnitario: Number(fila.precio) })),
    });
  }

  return <form aria-label="Datos de la nota" onSubmit={enviar}>
    {error && <p role="alert" className="field-error">{error}</p>}
    {(elegibles.cargando || origen.cargando) && <p role="status">Cargando comprobantes o líneas…</p>}
    {elegibles.error && <p role="alert">{elegibles.error}</p>}
    {origen.error && <p role="alert">{origen.error}</p>}
    <fieldset className="finanzas-campos" disabled={ocupado}>
      {inicial ?
        <p>{inicial.tipo === 'Credito' ? 'Crédito' : 'Débito'} · {inicial.facturaSerie}-{inicial.facturaNumero}</p> :
        <>
          <CampoModulo etiqueta="Comprobante emitido">{id => <select id={id} required value={factura} onChange={evento => {
            seleccionar(evento.target.value);
            cambiarFilas([vacia]);
          }}>
            <option value="">Seleccionar comprobante</option>
            {elegibles.datos?.map(item => <option key={item.id} value={item.id}>{item.numeroComprobante} · {item.clienteNombre} · {item.moneda}</option>)}
          </select>}</CampoModulo>
          <CampoModulo etiqueta="Tipo de nota">{id => <select id={id} name="tipo">
            <option value="Credito">Crédito</option><option value="Debito">Débito</option>
          </select>}</CampoModulo>
        </>}
      <CampoModulo etiqueta="Motivo">{id => <textarea id={id} name="motivo" required minLength={3}
        maxLength={300} defaultValue={inicial?.motivo} />}</CampoModulo>
    </fieldset>
    {origen.datos && <p>Moneda del comprobante {origen.datos.moneda}</p>}
    {origen.datos && filas.map((fila, indice) => <fieldset key={fila.clave} className="finanzas-campos" disabled={ocupado}>
      <legend>Línea {indice + 1}</legend>
      <CampoModulo etiqueta="Línea de origen">{id => <select id={id} required value={fila.id} onChange={evento => {
        const seleccion = origen.datos?.detalles.find(linea => String(linea.id) === evento.target.value);
        actualizar(fila.clave, { id: evento.target.value, precio: String(seleccion?.precioUnitario ?? '') });
      }}>
        <option value="">Seleccionar línea</option>
        {origen.datos?.detalles.map(linea => <option key={linea.id} value={linea.id}
          disabled={filas.some(otra => otra.clave !== fila.clave && otra.id === String(linea.id))}>
          {linea.descripcion} · {linea.cantidad} · Impuesto {linea.porcentajeImpuesto} %
        </option>)}
      </select>}</CampoModulo>
      <CampoModulo etiqueta="Cantidad">{id => <input id={id} type="number" required min="0.0001" step="0.0001"
        value={fila.cantidad} onChange={e => actualizar(fila.clave, { cantidad: e.target.value })} />}</CampoModulo>
      <CampoModulo etiqueta="Precio unitario">{id => <input id={id} type="number" required min="0.01" step="0.01"
        value={fila.precio} onChange={e => actualizar(fila.clave, { precio: e.target.value })} />}</CampoModulo>
      <button type="button" className="secondary-button" disabled={filas.length <= 1}
        onClick={() => cambiarFilas(actual => actual.filter(item => item.clave !== fila.clave))}>Quitar línea {indice + 1}</button>
    </fieldset>)}
    <div className="finanzas-acciones">
      <button type="button" className="secondary-button" disabled={ocupado || !origen.datos || filas.length >= origen.datos.detalles.length}
        onClick={() => cambiarFilas(actual => [...actual, { ...vacia, clave: Math.max(...actual.map(item => item.clave)) + 1 }])}>Añadir línea</button>
      <button type="button" className="secondary-button" disabled={ocupado} onClick={cancelar}>Cancelar</button>
      <button type="submit" className="primary-button" disabled={ocupado || !origen.datos}>Guardar borrador</button>
    </div>
  </form>;
}
