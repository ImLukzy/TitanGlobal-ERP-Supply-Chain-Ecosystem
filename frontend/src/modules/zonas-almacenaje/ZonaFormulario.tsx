import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import type { AlmacenListado } from '../almacenes/tipos';
import { actualizarZona, crearZona } from './api';
import type { GuardarZonaRequest, TemperaturaZona, TipoZona, ZonaDetalle } from './tipos';

interface Errores {
  almacenId?: string;
  codigo?: string;
  nombre?: string;
  tipo?: string;
  temperatura?: string;
  capacidadM3?: string;
}

function validar(request: GuardarZonaRequest): Errores {
  const errores: Errores = {};
  const codigo = request.codigo.trim().toUpperCase();
  const nombre = request.nombre.trim();
  if (request.almacenId <= 0) { errores.almacenId = 'Selecciona un almacén.'; }
  if (codigo === '') { errores.codigo = 'El código de zona es obligatorio.'; }
  else if (!/^[A-Z0-9-]{1,20}$/.test(codigo)) {
    errores.codigo = 'El código de zona solo admite letras, números y guiones (1 a 20 caracteres).';
  }
  if (nombre === '') { errores.nombre = 'El nombre de la zona es obligatorio.'; }
  else if (nombre.length > 100) { errores.nombre = 'El nombre de la zona no puede superar 100 caracteres.'; }
  if (!['Recepcion', 'Almacenamiento', 'Picking', 'Despacho', 'Cuarentena'].includes(request.tipo)) {
    errores.tipo = 'El tipo de zona no es válido.';
  }
  if (!['Ambiente', 'Refrigerada', 'Congelada'].includes(request.temperatura)) {
    errores.temperatura = 'La temperatura no es válida.';
  }
  if (request.capacidadM3 <= 0) { errores.capacidadM3 = 'La capacidad debe ser mayor que 0.'; }
  return errores;
}

interface Props {
  zona: ZonaDetalle | null;
  almacenes: AlmacenListado[];
  almacenIdInicial: number | '';
  cancelar: () => void;
  guardado: () => Promise<void>;
}

export function ZonaFormulario({ zona, almacenes, almacenIdInicial, cancelar, guardado }: Props) {
  const [almacenId, setAlmacenId] = useState(zona?.almacenId ?? (almacenIdInicial || almacenes[0]?.id || 0));
  const [codigo, setCodigo] = useState(zona?.codigo ?? '');
  const [nombre, setNombre] = useState(zona?.nombre ?? '');
  const [tipo, setTipo] = useState<TipoZona>(zona?.tipo ?? 'Almacenamiento');
  const [temperatura, setTemperatura] = useState<TemperaturaZona>(zona?.temperatura ?? 'Ambiente');
  const [capacidadM3, setCapacidadM3] = useState(zona?.capacidadM3 ?? 0);
  const [activa, setActiva] = useState(zona?.activa ?? true);
  const [errores, setErrores] = useState<Errores>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const request: GuardarZonaRequest = {
      almacenId,
      codigo: codigo.trim().toUpperCase(),
      nombre: nombre.trim(),
      tipo,
      temperatura,
      capacidadM3,
      activa,
    };
    const nuevosErrores = validar(request);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) { return; }
    setGuardando(true);
    try {
      if (zona === null) { await crearZona(request); }
      else { await actualizarZona(zona.id, request); }
      await guardado();
    } catch (error: unknown) {
      setMensaje(error instanceof ApiError ? error.message : 'No se pudo guardar la zona.');
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="zona-form-title" className="clientes-form-panel zonas-form-panel">
      <div className="clientes-panel-heading">
        <div><div className="page-eyebrow">ZONA DE ALMACENAJE</div>
          <h2 id="zona-form-title">{zona === null ? 'Nueva zona' : 'Editar zona'}</h2></div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="zona-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Almacén" htmlFor="zona-almacen" error={errores.almacenId} idError="zona-almacen-error">
          <select aria-describedby={errores.almacenId ? 'zona-almacen-error' : undefined} aria-invalid={Boolean(errores.almacenId)}
            id="zona-almacen" onChange={(event) => setAlmacenId(Number(event.currentTarget.value))} value={almacenId || ''}>
            <option value="">Selecciona un almacén</option>
            {almacenes.map((almacen) => <option key={almacen.id} value={almacen.id}>{almacen.codigo} · {almacen.nombre}</option>)}
          </select>
        </CampoConError>
        <CampoConError etiqueta="Código" htmlFor="zona-codigo" error={errores.codigo} idError="zona-codigo-error">
          <input aria-describedby={errores.codigo ? 'zona-codigo-error' : undefined} aria-invalid={Boolean(errores.codigo)}
            autoComplete="off" id="zona-codigo" maxLength={20} onChange={(event) => setCodigo(event.currentTarget.value.toUpperCase())} value={codigo} />
        </CampoConError>
        <CampoConError etiqueta="Nombre" htmlFor="zona-nombre" error={errores.nombre} idError="zona-nombre-error">
          <input aria-describedby={errores.nombre ? 'zona-nombre-error' : undefined} aria-invalid={Boolean(errores.nombre)}
            id="zona-nombre" maxLength={100} onChange={(event) => setNombre(event.currentTarget.value)} value={nombre} />
        </CampoConError>
        <CampoConError etiqueta="Tipo" htmlFor="zona-tipo" error={errores.tipo} idError="zona-tipo-error">
          <select aria-describedby={errores.tipo ? 'zona-tipo-error' : undefined} aria-invalid={Boolean(errores.tipo)}
            id="zona-tipo" onChange={(event) => setTipo(event.currentTarget.value as TipoZona)} value={tipo}>
            <option value="Recepcion">Recepción</option><option value="Almacenamiento">Almacenamiento</option>
            <option value="Picking">Picking</option><option value="Despacho">Despacho</option><option value="Cuarentena">Cuarentena</option>
          </select>
        </CampoConError>
        <CampoConError etiqueta="Temperatura" htmlFor="zona-temperatura" error={errores.temperatura} idError="zona-temperatura-error">
          <select aria-describedby={errores.temperatura ? 'zona-temperatura-error' : undefined} aria-invalid={Boolean(errores.temperatura)}
            id="zona-temperatura" onChange={(event) => setTemperatura(event.currentTarget.value as TemperaturaZona)} value={temperatura}>
            <option value="Ambiente">Ambiente</option><option value="Refrigerada">Refrigerada</option><option value="Congelada">Congelada</option>
          </select>
        </CampoConError>
        <CampoConError etiqueta="Capacidad (m³)" htmlFor="zona-capacidad" error={errores.capacidadM3} idError="zona-capacidad-error">
          <input aria-describedby={errores.capacidadM3 ? 'zona-capacidad-error' : undefined} aria-invalid={Boolean(errores.capacidadM3)}
            id="zona-capacidad" min="0" onChange={(event) => setCapacidadM3(Number(event.currentTarget.value))} step="0.01" type="number" value={capacidadM3} />
        </CampoConError>
        <label className="zona-active-field"><input checked={activa} onChange={(event) => setActiva(event.currentTarget.checked)} type="checkbox" />Activa</label>
        <div className="zona-form-actions"><p className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar zona'}</button>
        </div>
      </form>
    </section>
  );
}
