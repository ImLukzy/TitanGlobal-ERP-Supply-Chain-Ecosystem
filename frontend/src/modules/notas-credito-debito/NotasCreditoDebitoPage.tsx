import { useCallback, useState, type FormEvent } from 'react';
import { CampoModulo } from '../../components/CampoModulo';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { monedaFinanzas, textoFormulario, useEscrituraFinanzas, useOperacionFinanzas } from '../../lib/accionesFinanzas';
import { useConsultaPantalla } from '../../lib/consultaPantalla';
import { anularNota, crearNota, editarNota, emitirNota, listarNotas, obtenerNota } from './api';
import { FormularioNota } from './FormularioNota';
import type { CrearNota } from './tipos';

const columnas = [
  { id: 'numero', etiqueta: 'Número' }, { id: 'factura', etiqueta: 'Comprobante (Id)' },
  { id: 'tipo', etiqueta: 'Tipo' }, { id: 'estado', etiqueta: 'Estado' },
  { id: 'emision', etiqueta: 'Emisión' }, { id: 'total', etiqueta: 'Total' },
];

export function NotasCreditoDebitoPage() {
  const [factura, filtrarFactura] = useState('');
  const [tipo, filtrarTipo] = useState('');
  const [estado, filtrarEstado] = useState('');
  const [pagina, cambiarPagina] = useState(1);
  const [seleccion, seleccionar] = useState<number | null>(null);
  const [modo, modoFormulario] = useState<'alta' | 'editar' | 'emitir' | 'anular' | null>(null);
  const permiso = useEscrituraFinanzas();
  const accion = useOperacionFinanzas();
  const cargar = useCallback(() => listarNotas(factura, tipo, estado, pagina), [factura, tipo, estado, pagina]);
  const cargarDetalle = useCallback(() => obtenerNota(seleccion ?? 0), [seleccion]);
  const lista = useConsultaPantalla(cargar, accion.revision);
  const detalle = useConsultaPantalla(seleccion === null ? null : cargarDetalle, accion.revision);
  const nota = detalle.datos;

  function abrir(nuevo: typeof modo) {
    accion.limpiar();
    modoFormulario(nuevo);
  }

  function guardar(datos: CrearNota) {
    const { motivo, detalles } = datos;
    void accion.ejecutar(async () => {
      const guardada = modo === 'editar' && nota ? await editarNota(nota.id, { motivo, detalles }) : await crearNota(datos);
      seleccionar(guardada.id);
    }, 'Borrador guardado.', () => modoFormulario(null));
  }

  function anular(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const motivo = textoFormulario(new FormData(evento.currentTarget), 'motivo');
    if (motivo.length < 3 || motivo.length > 300) {
      accion.indicarError('El motivo debe tener entre 3 y 300 caracteres.');
    } else if (nota) {
      void accion.ejecutar(() => anularNota(nota.id, motivo), 'Nota anulada.', () => modoFormulario(null));
    }
  }

  return <PaginaModulo modulo={52}>
    <div className="finanzas-columnas">
      <section aria-labelledby="lista-notas" className="finanzas-superficie">
        <div className="finanzas-acciones">
          <h2 id="lista-notas">Notas registradas</h2>
          {permiso && <button type="button" className="primary-button" disabled={accion.ocupado}
            onClick={() => abrir('alta')}>Nueva nota</button>}
        </div>
        <div className="finanzas-campos">
          <CampoModulo etiqueta="Id del comprobante">{id => <input id={id} type="number" min={1} value={factura}
            onChange={e => { filtrarFactura(e.target.value); cambiarPagina(1); }} />}</CampoModulo>
          <CampoModulo etiqueta="Tipo">{id => <select id={id} value={tipo}
            onChange={e => { filtrarTipo(e.target.value); cambiarPagina(1); }}>
            <option value="">Todos</option><option value="Credito">Crédito</option><option value="Debito">Débito</option>
          </select>}</CampoModulo>
          <CampoModulo etiqueta="Estado">{id => <select id={id} value={estado}
            onChange={e => { filtrarEstado(e.target.value); cambiarPagina(1); }}>
            <option value="">Todos</option><option>Borrador</option><option>Emitida</option><option>Anulada</option>
          </select>}</CampoModulo>
        </div>
        {lista.cargando && <p role="status">Cargando notas…</p>}
        {lista.error && <p role="alert">{lista.error}</p>}
        {lista.datos?.items.length === 0 && <p>No hay notas para estos filtros.</p>}
        {lista.datos && (pagina > 1 || lista.datos.items.length > 0) && <TablaPaginada caption="Notas de crédito y débito"
          etiquetaRegion="Notas" columnas={columnas} elementos={lista.datos.items} obtenerClave={item => item.id}
          pagina={pagina} totalPaginas={Math.max(1, lista.datos.totalPaginas)} cambiarPagina={cambiarPagina}
          renderizarFila={fila => <tr>
            <td><button type="button" className="secondary-button" disabled={accion.ocupado}
              onClick={() => { seleccionar(fila.id); abrir(null); }}>{fila.numero}</button></td>
            <td>{fila.facturaId}</td><td>{fila.tipo === 'Credito' ? 'Crédito' : 'Débito'}</td>
            <td>{fila.estado}</td><td>{fila.fechaEmision ?? '—'}</td><td>{monedaFinanzas(fila.total, fila.moneda)}</td>
          </tr>} />}
      </section>
      <PanelDetalle ariaLabelledBy="nota-detalle" className="finanzas-superficie">
        <h2 id="nota-detalle">{modo === 'alta' ? 'Nueva nota' : nota?.numero ?? 'Detalle de la nota'}</h2>
        {seleccion === null && modo !== 'alta' && <p>Selecciona una nota para consultar sus datos.</p>}
        {detalle.cargando && modo !== 'alta' && <p role="status">Cargando detalle…</p>}
        {detalle.error && modo !== 'alta' && <p role="alert">{detalle.error}</p>}
        {accion.aviso && <p role="status">{accion.aviso}</p>}
        {accion.error && <p role="alert" className="field-error">{accion.error}</p>}
        {permiso && (modo === 'alta' || (modo === 'editar' && nota)) &&
          <FormularioNota key={modo + String(seleccion)} inicial={modo === 'editar' ? nota ?? undefined : undefined}
            ocupado={accion.ocupado} guardar={guardar} cancelar={() => abrir(null)} />}
        {nota && modo !== 'alta' && modo !== 'editar' && <>
          <p>{nota.estado} · {nota.clienteNombre} · {nota.moneda}</p>
          <p>{nota.facturaTipoComprobante} {nota.facturaSerie}-{nota.facturaNumero} · {nota.motivo}</p>
          {nota.fechaEmision && <p>Emisión {nota.fechaEmision}</p>}
          {nota.motivoAnulacion && <p>Motivo de anulación: {nota.motivoAnulacion}</p>}
          <h3>Líneas de origen</h3>
          <ul className="finanzas-historial">{nota.detalles.map(linea => <li key={linea.id}>
            Línea {linea.detalleFacturaId} · {linea.cantidad} × {monedaFinanzas(linea.precioUnitario, nota.moneda)}
            <div>Impuesto histórico {linea.porcentajeImpuesto}% · Total {monedaFinanzas(linea.total, nota.moneda)}</div>
          </li>)}</ul>
          <p>Base {monedaFinanzas(nota.baseImponible, nota.moneda)} · Impuesto {monedaFinanzas(nota.importeImpuesto, nota.moneda)}</p>
          <p><strong>Total {monedaFinanzas(nota.total, nota.moneda)}</strong></p>
          {permiso && modo === null && <div className="finanzas-acciones">
            {nota.estado === 'Borrador' && <>
              <button type="button" className="secondary-button" onClick={() => abrir('editar')}>Editar borrador</button>
              <button type="button" className="primary-button" onClick={() => abrir('emitir')}>Emitir nota</button>
            </>}
            {nota.estado === 'Emitida' && <button type="button" className="secondary-button" onClick={() => abrir('anular')}>Anular nota</button>}
          </div>}
          {permiso && modo === 'emitir' && <div role="group" aria-label="Confirmar emisión" className="finanzas-confirmacion">
            <p>¿Emitir {nota.numero}?</p>
            <button type="button" className="secondary-button" disabled={accion.ocupado} onClick={() => abrir(null)}>Cancelar</button>
            <button type="button" className="primary-button" disabled={accion.ocupado}
              onClick={() => void accion.ejecutar(() => emitirNota(nota.id), 'Nota emitida.', () => modoFormulario(null))}>Confirmar emisión</button>
          </div>}
          {permiso && modo === 'anular' && <form onSubmit={anular} aria-label="Confirmar anulación">
            <CampoModulo etiqueta="Motivo de anulación">{id => <textarea id={id} name="motivo" required
              minLength={3} maxLength={300} disabled={accion.ocupado} />}</CampoModulo>
            <div className="finanzas-acciones">
              <button type="button" className="secondary-button" disabled={accion.ocupado} onClick={() => abrir(null)}>Cancelar</button>
              <button type="submit" className="danger-button" disabled={accion.ocupado}>Confirmar anulación</button>
            </div>
          </form>}
        </>}
      </PanelDetalle>
    </div>
  </PaginaModulo>;
}
