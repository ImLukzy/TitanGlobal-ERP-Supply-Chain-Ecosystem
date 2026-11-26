import { useCallback, useState, type FormEvent } from 'react';
import { CampoModulo } from '../../components/CampoModulo';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { useConsultaPantalla, mensajeApi } from '../../lib/consultaPantalla';
import { diaDePeru } from '../../lib/fechaNegocioPeru';
import { actualizarImpuesto, calcularImpuesto, crearImpuesto, desactivarImpuesto, listarImpuestos, obtenerImpuesto, registrarTasa } from './api';
import { tiposImpuesto, type GuardarImpuesto, type ResultadoImpuesto } from './tipos';

const columnas = [
  { id: 'codigo', etiqueta: 'Código' }, { id: 'nombre', etiqueta: 'Impuesto' },
  { id: 'tipo', etiqueta: 'Tipo' }, { id: 'pais', etiqueta: 'País' },
  { id: 'tasa', etiqueta: 'Tasa vigente' }, { id: 'estado', etiqueta: 'Estado' },
];
const porPagina = 10;
type Formulario = 'alta' | 'edicion' | 'tasa' | 'calculo';

function texto(campos: FormData, clave: string) {
  return String(campos.get(clave) ?? '').trim();
}

function pct(porcentaje: number) {
  return new Intl.NumberFormat('es-PE', { maximumFractionDigits: 6 }).format(porcentaje) + ' %';
}

export function ImpuestosTasasPage() {
  const [buscar, buscarPor] = useState('');
  const [tipo, filtrarTipo] = useState('');
  const [estado, filtrarEstado] = useState('');
  const [pagina, irAPagina] = useState(1);
  const [id, seleccionar] = useState<number | null>(null);
  const [modo, formulario] = useState<Formulario | null>(null);
  const [cambio, confirmarCambio] = useState<'activar' | 'desactivar' | null>(null);
  const [revision, revisar] = useState(0);
  const [ocupado, bloquear] = useState(false);
  const [error, avisarError] = useState('');
  const [mensaje, avisar] = useState('');
  const [calculo, mostrarCalculo] = useState<ResultadoImpuesto | null>(null);
  const lista = useConsultaPantalla(listarImpuestos, revision);
  const cargarImpuesto = useCallback(() => obtenerImpuesto(id ?? 0), [id]);
  const detalle = useConsultaPantalla(id === null ? null : cargarImpuesto, revision);
  const impuesto = detalle.datos;
  const consulta = buscar.trim().toLowerCase();
  const filas = (lista.datos ?? []).filter(item =>
    (tipo === '' || item.tipo === tipo) &&
    (estado === '' || item.activo === (estado === 'activo')) &&
    [item.nombre, item.codigo, item.pais].some(valor => valor.toLowerCase().includes(consulta)),
  );
  const totalPaginas = Math.max(1, Math.ceil(filas.length / porPagina));
  const paginaActual = Math.min(pagina, totalPaginas);

  function abrir(siguiente: Formulario | null) {
    formulario(siguiente);
    confirmarCambio(null);
    avisarError('');
    avisar('');
    mostrarCalculo(null);
  }

  async function ejecutar(operacion: () => Promise<void>, exito: string, refrescar = true) {
    bloquear(true);
    avisarError('');
    avisar('');
    try {
      await operacion();
      if (refrescar) revisar(anterior => anterior + 1);
      formulario(null);
      confirmarCambio(null);
      avisar(exito);
    } catch (causa) {
      avisarError(mensajeApi(causa));
    } finally {
      bloquear(false);
    }
  }

  function guardar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const campos = new FormData(evento.currentTarget);
    if (modo === 'alta' || modo === 'edicion') {
      const datos: GuardarImpuesto = {
        codigo: texto(campos, 'codigo'), nombre: texto(campos, 'nombre'),
        tipo: texto(campos, 'tipo'), descripcion: texto(campos, 'descripcion') || null,
        pais: texto(campos, 'pais'), activo: campos.has('activo'),
      };
      if (!datos.codigo || !datos.nombre || datos.pais.length !== 3) {
        avisarError('Indique el código, el nombre y un país de tres caracteres.');
        return;
      }
      void ejecutar(async () => {
        if (modo === 'alta') seleccionar((await crearImpuesto(datos)).id);
        else if (id !== null) await actualizarImpuesto(id, datos);
      }, 'Impuesto guardado.');
    } else if (modo === 'tasa' && id !== null) {
      const inicio = texto(campos, 'fechaInicio');
      const fin = texto(campos, 'fechaFin');
      if (fin && fin < inicio) {
        avisarError('La fecha final no puede ser anterior a la fecha inicial.');
        return;
      }
      void ejecutar(async () => {
        await registrarTasa(id, { porcentaje: Number(campos.get('porcentaje')), fechaInicio: inicio,
          fechaFin: fin || null, observacion: texto(campos, 'observacion') || null });
      }, 'Tasa registrada.');
    } else if (modo === 'calculo' && id !== null) {
      void ejecutar(async () => {
        const resultado = await calcularImpuesto({ impuestoId: id, baseImponible: Number(campos.get('base')),
          fechaOperacion: texto(campos, 'fechaOperacion') });
        mostrarCalculo(resultado);
      }, 'Cálculo completado.', false);
    }
  }

  return <PaginaModulo modulo={75}>
    <div className="finanzas-columnas">
      <section className="finanzas-superficie" aria-labelledby="lista-impuestos">
        <div className="finanzas-acciones">
          <h2 id="lista-impuestos">Catálogo de impuestos</h2>
          <button type="button" className="primary-button" disabled={ocupado} onClick={() => abrir('alta')}>Nuevo impuesto</button>
        </div>
        <div className="finanzas-campos">
          <CampoModulo etiqueta="Buscar por código, nombre o país">{campo => <input id={campo} type="search" value={buscar}
            onChange={event => { buscarPor(event.target.value); irAPagina(1); }} />}</CampoModulo>
          <CampoModulo etiqueta="Tipo de impuesto">{campo => <select id={campo} value={tipo} onChange={event => {
            filtrarTipo(event.target.value); irAPagina(1);
          }}><option value="">Todos</option>{tiposImpuesto.map(opcion => <option key={opcion}>{opcion}</option>)}</select>}</CampoModulo>
          <CampoModulo etiqueta="Actividad">{campo => <select id={campo} value={estado} onChange={event => {
            filtrarEstado(event.target.value); irAPagina(1);
          }}><option value="">Todos</option><option value="activo">Activos</option><option value="inactivo">Inactivos</option></select>}</CampoModulo>
        </div>
        {lista.cargando && <p role="status">Cargando impuestos…</p>}
        {lista.error && <p role="alert" className="field-error">{lista.error}</p>}
        {lista.datos && filas.length === 0 && <p>No hay impuestos para estos filtros.</p>}
        {filas.length > 0 && <TablaPaginada
          caption="Impuestos y tasas vigentes" etiquetaRegion="Impuestos" columnas={columnas}
          elementos={filas.slice((paginaActual - 1) * porPagina, paginaActual * porPagina)}
          obtenerClave={item => item.id} pagina={paginaActual} totalPaginas={totalPaginas} cambiarPagina={irAPagina}
          renderizarFila={item => <tr>
            <td><button type="button" className="secondary-button" disabled={ocupado} onClick={() => {
              seleccionar(item.id); abrir(null);
            }}>{item.codigo}</button></td>
            <td>{item.nombre}</td><td>{item.tipo}</td><td>{item.pais}</td>
            <td>{item.porcentajeVigente === null ? '—' : pct(item.porcentajeVigente)}</td>
            <td>{item.activo ? 'Activo' : 'Inactivo'}</td>
          </tr>}
        />}
      </section>
      <PanelDetalle ariaLabelledBy="detalle-impuesto" className="finanzas-superficie">
        <h2 id="detalle-impuesto">{modo === 'alta' ? 'Nuevo impuesto' : impuesto?.nombre ?? 'Detalle del impuesto'}</h2>
        {detalle.cargando && modo !== 'alta' && <p role="status">Cargando detalle…</p>}
        {detalle.error && modo !== 'alta' && <p role="alert" className="field-error">{detalle.error}</p>}
        {id === null && !modo && <p>Selecciona un impuesto para ver su historial.</p>}
        {mensaje && <p role="status">{mensaje}</p>}
        {error && <p role="alert" className="field-error">{error}</p>}
        {impuesto && !modo && <>
          <p>{impuesto.codigo} · {impuesto.tipo} · {impuesto.pais} · {impuesto.activo ? 'Activo' : 'Inactivo'}</p>
          {impuesto.descripcion && <p>{impuesto.descripcion}</p>}
          <div className="finanzas-acciones">
            <button type="button" className="secondary-button" disabled={ocupado} onClick={() => abrir('edicion')}>Editar impuesto</button>
            {impuesto.activo && <>
              <button type="button" className="secondary-button" disabled={ocupado} onClick={() => abrir('tasa')}>Registrar tasa</button>
              <button type="button" className="secondary-button" disabled={ocupado} onClick={() => abrir('calculo')}>Calcular impuesto</button>
            </>}
            <button type="button" className="secondary-button" disabled={ocupado} onClick={() => {
              avisarError(''); avisar(''); confirmarCambio(impuesto.activo ? 'desactivar' : 'activar');
            }}>{impuesto.activo ? 'Desactivar impuesto' : 'Activar impuesto'}</button>
          </div>
          {cambio && <div role="group" className="finanzas-confirmacion" aria-label="Confirmar cambio de actividad">
            <p>¿Confirmas {cambio} {impuesto.codigo}?</p>
            <button type="button" className="secondary-button" disabled={ocupado} onClick={() => confirmarCambio(null)}>Cancelar</button>
            <button type="button" className="primary-button" disabled={ocupado} onClick={() => {
              const { codigo, nombre, tipo: tipoActual, pais, descripcion } = impuesto;
              void ejecutar(() => cambio === 'desactivar' ? desactivarImpuesto(impuesto.id) :
                actualizarImpuesto(impuesto.id, { codigo, nombre, tipo: tipoActual, pais, descripcion, activo: true }),
                cambio === 'desactivar' ? 'Impuesto desactivado.' : 'Impuesto activado.');
            }}>Confirmar</button>
          </div>}
          <h3>Historial de tasas</h3>
          {impuesto.tasas.length === 0 ? <p>Sin tasas registradas.</p> : <ul className="finanzas-historial">
            {impuesto.tasas.map(tasa => <li key={tasa.id}>
              <strong>{pct(tasa.porcentaje)}</strong> · {tasa.fechaInicio.slice(0, 10)} a {tasa.fechaFin?.slice(0, 10) ?? 'vigencia abierta'}
              {' · '}{tasa.activo ? 'Activa' : 'Inactiva'}{tasa.observacion && <p>{tasa.observacion}</p>}
            </li>)}
          </ul>}
        </>}
        {calculo && <dl className="finanzas-historial" aria-label="Resultado del cálculo">
          <dt>Porcentaje aplicado</dt><dd>{pct(calculo.porcentajeAplicado)}</dd>
          <dt>Importe de impuesto</dt><dd>{calculo.importeImpuesto.toFixed(2)}</dd>
          <dt>Total</dt><dd>{calculo.total.toFixed(2)}</dd>
        </dl>}
        {modo && <form key={modo + String(id)} onSubmit={guardar} aria-label={modo === 'tasa' ? 'Registrar tasa' :
          modo === 'calculo' ? 'Calcular impuesto' : 'Datos del impuesto'}>
          <fieldset disabled={ocupado} className="finanzas-campos">
            {(modo === 'edicion' || modo === 'alta') && <>
              <CampoModulo etiqueta="Código">{campo => <input id={campo} name="codigo" required maxLength={30}
                defaultValue={modo === 'edicion' ? impuesto?.codigo : ''} />}</CampoModulo>
              <CampoModulo etiqueta="Nombre">{campo => <input id={campo} name="nombre" required maxLength={120}
                defaultValue={modo === 'edicion' ? impuesto?.nombre : ''} />}</CampoModulo>
              <CampoModulo etiqueta="Tipo">{campo => <select id={campo} name="tipo"
                defaultValue={modo === 'edicion' ? impuesto?.tipo : 'IGV'}>
                {tiposImpuesto.map(opcion => <option key={opcion}>{opcion}</option>)}
              </select>}</CampoModulo>
              <CampoModulo etiqueta="País">{campo => <input id={campo} name="pais" minLength={3} maxLength={3} required
                defaultValue={modo === 'edicion' ? impuesto?.pais : 'PER'} />}</CampoModulo>
              <CampoModulo etiqueta="Descripción">{campo => <textarea id={campo} name="descripcion" maxLength={250}
                defaultValue={modo === 'edicion' ? impuesto?.descripcion ?? '' : ''} />}</CampoModulo>
              <label><input type="checkbox" name="activo" defaultChecked={modo === 'alta' || impuesto?.activo} /> Activo</label>
            </>}
            {modo === 'tasa' && <>
              <CampoModulo etiqueta="Porcentaje">{campo => <input id={campo} name="porcentaje" type="number" required
                step="0.000001" min="0.000001" max="100" />}</CampoModulo>
              <CampoModulo etiqueta="Fecha de inicio">{campo => <input id={campo} name="fechaInicio" type="date" required defaultValue={diaDePeru()} />}</CampoModulo>
              <CampoModulo etiqueta="Fecha final (opcional)">{campo => <input id={campo} name="fechaFin" type="date" />}</CampoModulo>
              <CampoModulo etiqueta="Observación">{campo => <textarea id={campo} name="observacion" maxLength={250} />}</CampoModulo>
            </>}
            {modo === 'calculo' && <>
              <CampoModulo etiqueta="Base imponible">{campo => <input id={campo} name="base" type="number" required min="0.005" step="any" />}</CampoModulo>
              <CampoModulo etiqueta="Fecha de operación">{campo => <input id={campo} name="fechaOperacion" type="date" required defaultValue={diaDePeru()} />}</CampoModulo>
            </>}
          </fieldset>
          <div className="finanzas-acciones">
            <button type="button" className="secondary-button" disabled={ocupado} onClick={() => abrir(null)}>Cancelar</button>
            <button type="submit" className="primary-button" disabled={ocupado}>{ocupado ? 'Procesando…' :
              modo === 'calculo' ? 'Calcular' : modo === 'tasa' ? 'Guardar tasa' : 'Guardar impuesto'}</button>
          </div>
        </form>}
      </PanelDetalle>
    </div>
  </PaginaModulo>;
}
