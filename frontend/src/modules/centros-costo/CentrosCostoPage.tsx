import { useCallback, useState, type FormEvent } from 'react';
import { CampoModulo } from '../../components/CampoModulo';
import { PaginaModulo } from '../../components/PaginaModulo';
import { PanelDetalle } from '../../components/PanelDetalle';
import { TablaPaginada } from '../../components/TablaPaginada';
import { textoFormulario, useEscrituraFinanzas, useOperacionFinanzas } from '../../lib/accionesFinanzas';
import { useConsultaPantalla } from '../../lib/consultaPantalla';
import { cambiarVigencia, catalogoCentros, guardarCentro, listarCentros, obtenerCentro, responsables } from './api';
import type { CentroFila } from './tipos';

const columnas = [
  { id: 'codigo', etiqueta: 'Código' }, { id: 'nombre', etiqueta: 'Nombre' },
  { id: 'nivel', etiqueta: 'Nivel' }, { id: 'padre', etiqueta: 'Centro padre' },
  { id: 'estado', etiqueta: 'Estado' },
];

function linaje(id: number, catalogo: CentroFila[]): CentroFila[] {
  const camino: CentroFila[] = [];
  let centro = catalogo.find(item => item.id === id);
  while (centro && !camino.some(item => item.id === centro?.id)) {
    camino.unshift(centro);
    const padre: number | null = centro.centroCostoPadreId;
    centro = catalogo.find(item => item.id === padre);
  }
  return camino;
}

export function CentrosCostoPage() {
  const [activo, filtrarActivo] = useState('');
  const [padre, filtrarPadre] = useState('');
  const [pagina, navegar] = useState(1);
  const [id, seleccionar] = useState<number | null>(null);
  const [modo, cambiarModo] = useState<'alta' | 'editar' | 'vigencia' | null>(null);
  const puedeEscribir = useEscrituraFinanzas();
  const operacion = useOperacionFinanzas();
  const cargar = useCallback(() => listarCentros(activo, padre, pagina), [activo, padre, pagina]);
  const cargarDetalle = useCallback(() => obtenerCentro(id ?? 0), [id]);
  const lista = useConsultaPantalla(cargar, operacion.revision);
  const detalle = useConsultaPantalla(id === null ? null : cargarDetalle, operacion.revision);
  const centros = useConsultaPantalla(catalogoCentros, operacion.revision);
  const empleados = useConsultaPantalla((puedeEscribir && modo !== null && modo !== 'vigencia') ? responsables : null);
  const centro = detalle.datos;
  const opciones = centros.datos ?? [];
  const padres = opciones.filter(item => {
    const ruta = linaje(item.id, opciones);
    return ruta.length < 3 && (modo !== 'editar' || !ruta.some(ancestro => ancestro.id === centro?.id));
  });

  function abrir(nuevo: typeof modo) {
    operacion.limpiar();
    cambiarModo(nuevo);
  }

  function guardar(evento: FormEvent<HTMLFormElement>) {
    evento.preventDefault();
    const datos = new FormData(evento.currentTarget);
    const codigo = textoFormulario(datos, 'codigo');
    const nombre = textoFormulario(datos, 'nombre');
    if (codigo.length < 2 || codigo.length > 20 || nombre === '') {
      operacion.indicarError('El código debe tener entre 2 y 20 caracteres y los datos del centro deben ser válidos.');
      return;
    }
    void operacion.ejecutar(async () => {
      const nuevo = await guardarCentro({
        codigo, nombre, descripcion: textoFormulario(datos, 'descripcion') || null,
        responsableEmpleadoId: Number(datos.get('responsable')) || null, centroCostoPadreId: Number(datos.get('padre')) || null,
      }, modo === 'editar' ? id ?? undefined : undefined);
      seleccionar(nuevo.id);
    }, 'Centro de costo guardado.', () => cambiarModo(null));
  }

  return <PaginaModulo modulo={59} className="centros-costo-page">
    <div className="finanzas-columnas">
      <section aria-labelledby="lista-centros-costo" className="finanzas-superficie">
        <div className="finanzas-acciones">
          <h2 id="lista-centros-costo">Centros de costo</h2>
          {puedeEscribir && <button type="button" className="primary-button" disabled={operacion.ocupado}
            onClick={() => abrir('alta')}>Nuevo centro</button>}
        </div>
        <div className="finanzas-campos">
          <CampoModulo etiqueta="Actividad">{campo => <select id={campo} value={activo}
            onChange={e => { filtrarActivo(e.target.value); navegar(1); }}>
            <option value="">Todos</option><option value="true">Activos</option><option value="false">Inactivos</option>
          </select>}</CampoModulo>
          <CampoModulo etiqueta="Filtrar por padre">{campo => <select id={campo} value={padre}
            onChange={e => { filtrarPadre(e.target.value); navegar(1); }}>
            <option value="">Cualquier padre</option>
            {opciones.map(item => <option key={item.id} value={item.id}>{item.codigo} · {item.nombre}</option>)}
          </select>}</CampoModulo>
        </div>
        {centros.error && <p role="alert">{centros.error}</p>}
        {lista.cargando && <p role="status">Cargando centros…</p>}
        {lista.error && <p role="alert">{lista.error}</p>}
        {lista.datos?.items.length === 0 && <p>No hay centros para estos filtros.</p>}
        {lista.datos && (pagina > 1 || lista.datos.items.length > 0) && <TablaPaginada
          caption="Centros de costo y jerarquía" etiquetaRegion="Centros de costo" columnas={columnas}
          elementos={lista.datos.items} obtenerClave={item => item.id} pagina={pagina} cambiarPagina={navegar}
          totalPaginas={Math.max(1, lista.datos.totalPaginas)} renderizarFila={item => <tr>
            <td><button type="button" className="secondary-button" disabled={operacion.ocupado}
              onClick={() => { seleccionar(item.id); abrir(null); }}>{item.codigo}</button></td>
            <td>{item.nombre}</td><td>{linaje(item.id, opciones).length || '—'}</td>
            <td>{opciones.find(padreItem => padreItem.id === item.centroCostoPadreId)?.codigo ?? '—'}</td>
            <td>{item.activo ? 'Activo' : 'Inactivo'}</td>
          </tr>} />}
      </section>
      <PanelDetalle ariaLabelledBy="detalle-centro-costo" className="finanzas-superficie">
        <h2 id="detalle-centro-costo">{modo === 'alta' ? 'Nuevo centro de costo' : centro?.nombre ?? 'Detalle del centro'}</h2>
        {detalle.cargando && modo !== 'alta' && <p role="status">Cargando detalle…</p>}
        {detalle.error && modo !== 'alta' && <p role="alert">{detalle.error}</p>}
        {id === null && !modo && <p>Selecciona un centro para consultar su jerarquía.</p>}
        {operacion.aviso && <p role="status">{operacion.aviso}</p>}
        {operacion.error && <p role="alert" className="field-error">{operacion.error}</p>}
        {centro && modo !== 'alta' && modo !== 'editar' && <>
          <p>{centro.codigo} · Nivel {centro.nivel} · {centro.activo ? 'Activo' : 'Inactivo'}</p>
          <p>{linaje(centro.id, opciones).map(item => item.nombre).join(' / ') || centro.nombre}</p>
          {centro.descripcion && <p>{centro.descripcion}</p>}
          <p>Responsable: {centro.responsableEmpleadoId === null ? 'Sin responsable' : 'empleado ' + centro.responsableEmpleadoId}</p>
          {centro.fechaDesactivacion && <p>Desactivación {centro.fechaDesactivacion.slice(0, 10)}</p>}
          {puedeEscribir && modo === null && <div className="finanzas-acciones">
            <button type="button" className="secondary-button" onClick={() => abrir('editar')}>Editar centro</button>
            <button type="button" className="secondary-button" onClick={() => abrir('vigencia')}>
              {centro.activo ? 'Desactivar centro' : 'Reactivar centro'}
            </button>
          </div>}
          {puedeEscribir && modo === 'vigencia' && <div className="finanzas-confirmacion" role="group" aria-label="Confirmar actividad">
            <p>¿{centro.activo ? 'Desactivar' : 'Reactivar'} {centro.codigo}?</p>
            <button type="button" className="secondary-button" disabled={operacion.ocupado} onClick={() => abrir(null)}>Cancelar</button>
            <button type="button" className="primary-button" disabled={operacion.ocupado}
              onClick={() => void operacion.ejecutar(() => cambiarVigencia(centro.id, !centro.activo),
                centro.activo ? 'Centro desactivado.' : 'Centro reactivado.', () => cambiarModo(null))}>Confirmar</button>
          </div>}
        </>}
        {puedeEscribir && (modo === 'alta' || modo === 'editar') && <>
          {(centros.cargando || empleados.cargando) && <p role="status">Cargando padres y responsables…</p>}
          {empleados.error && <p role="alert">{empleados.error}</p>}
          {centros.datos && empleados.datos && <form aria-label="Datos del centro" key={modo + String(id)} onSubmit={guardar}>
            <fieldset className="finanzas-campos" disabled={operacion.ocupado}>
              <CampoModulo etiqueta="Código">{campo => <input id={campo} name="codigo" required minLength={2} maxLength={20}
                defaultValue={modo === 'editar' ? centro?.codigo : ''} />}</CampoModulo>
              <CampoModulo etiqueta="Nombre">{campo => <input id={campo} name="nombre" required maxLength={120}
                defaultValue={modo === 'editar' ? centro?.nombre : ''} />}</CampoModulo>
              <CampoModulo etiqueta="Descripción">{campo => <textarea id={campo} name="descripcion" maxLength={300}
                defaultValue={modo === 'editar' ? centro?.descripcion ?? '' : ''} />}</CampoModulo>
              <CampoModulo etiqueta="Centro padre">{campo => <select id={campo} name="padre"
                defaultValue={modo === 'editar' ? centro?.centroCostoPadreId ?? '' : ''}>
                <option value="">Sin padre</option>
                {padres.map(item => <option key={item.id} value={item.id}>{item.codigo} · {item.nombre}{item.activo ? '' : ' (inactivo)'}</option>)}
              </select>}</CampoModulo>
              <CampoModulo etiqueta="Responsable">{campo => <select id={campo} name="responsable"
                defaultValue={modo === 'editar' ? centro?.responsableEmpleadoId ?? '' : ''}>
                <option value="">Sin responsable</option>
                {empleados.datos?.map(item => <option key={item.id} value={item.id}>{item.nombres} {item.apellidos} · {item.legajo}</option>)}
              </select>}</CampoModulo>
            </fieldset>
            <div className="finanzas-acciones">
              <button type="button" className="secondary-button" disabled={operacion.ocupado} onClick={() => abrir(null)}>Cancelar</button>
              <button type="submit" className="primary-button" disabled={operacion.ocupado}>Guardar centro</button>
            </div>
          </form>}
        </>}
      </PanelDetalle>
    </div>
  </PaginaModulo>;
}
