import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import type { AlmacenDetalle, GuardarAlmacenRequest, TipoAlmacen } from './tipos';
import { actualizarAlmacen, crearAlmacen } from './api';

interface Errores {
  codigo?: string;
  nombre?: string;
  tipo?: string;
  direccion?: string;
  ciudad?: string;
  capacidadM3?: string;
  responsable?: string;
}

function validar(valores: GuardarAlmacenRequest): Errores {
  const errores: Errores = {};
  const codigo = valores.codigo.trim().toUpperCase();
  const nombre = valores.nombre.trim();
  const direccion = valores.direccion.trim();
  const ciudad = valores.ciudad.trim();
  const responsable = valores.responsable?.trim() ?? '';
  if (codigo === '') { errores.codigo = 'El código es obligatorio.'; }
  else if (!/^[A-Z0-9-]{2,20}$/.test(codigo)) { errores.codigo = 'El código solo admite letras, números y guiones (2 a 20 caracteres).'; }
  if (nombre === '') { errores.nombre = 'El nombre del almacén es obligatorio.'; }
  else if (nombre.length > 200) { errores.nombre = 'El nombre del almacén no puede superar 200 caracteres.'; }
  if (!['Central', 'Regional', 'Transito'].includes(valores.tipo)) { errores.tipo = 'El tipo de almacén no es válido.'; }
  if (direccion === '') { errores.direccion = 'La dirección es obligatoria.'; }
  else if (direccion.length > 300) { errores.direccion = 'La dirección no puede superar 300 caracteres.'; }
  if (ciudad === '') { errores.ciudad = 'La ciudad es obligatoria.'; }
  else if (ciudad.length > 100) { errores.ciudad = 'La ciudad no puede superar 100 caracteres.'; }
  if (valores.capacidadM3 <= 0) { errores.capacidadM3 = 'La capacidad debe ser mayor que 0.'; }
  if (responsable.length > 100) { errores.responsable = 'El responsable no puede superar 100 caracteres.'; }
  return errores;
}

interface Props {
  almacen: AlmacenDetalle | null;
  ciudades: string[];
  cancelar: () => void;
  guardado: () => Promise<void>;
}

export function AlmacenFormulario({ almacen, ciudades, cancelar, guardado }: Props) {
  const [codigo, setCodigo] = useState(almacen?.codigo ?? '');
  const [nombre, setNombre] = useState(almacen?.nombre ?? '');
  const [tipo, setTipo] = useState<TipoAlmacen>(almacen?.tipo ?? 'Central');
  const [direccion, setDireccion] = useState(almacen?.direccion ?? '');
  const [ciudad, setCiudad] = useState(almacen?.ciudad ?? '');
  const [capacidadM3, setCapacidadM3] = useState(almacen?.capacidadM3 ?? 0);
  const [responsable, setResponsable] = useState(almacen?.responsable ?? '');
  const [activo, setActivo] = useState(almacen?.activo ?? true);
  const [errores, setErrores] = useState<Errores>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const request: GuardarAlmacenRequest = {
      codigo: codigo.trim().toUpperCase(), nombre: nombre.trim(), tipo,
      direccion: direccion.trim(), ciudad: ciudad.trim(), capacidadM3, activo,
    };
    if (responsable.trim() !== '') { request.responsable = responsable.trim(); }
    const nuevosErrores = validar(request);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) { return; }
    setGuardando(true);
    try {
      if (almacen === null) { await crearAlmacen(request); }
      else { await actualizarAlmacen(almacen.id, request); }
      await guardado();
    } catch (error: unknown) {
      setMensaje(error instanceof ApiError ? error.message : 'No se pudo guardar el almacén.');
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section className="clientes-form-panel almacenes-form-panel" aria-labelledby="almacen-form-title">
      <div className="clientes-panel-heading"><div><div className="page-eyebrow">ALMACÉN</div>
        <h2 id="almacen-form-title">{almacen === null ? 'Nuevo almacén' : 'Editar almacén'}</h2></div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button></div>
      <form className="almacen-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Código" htmlFor="almacen-codigo" error={errores.codigo} idError="almacen-codigo-error">
          <input aria-describedby={errores.codigo ? 'almacen-codigo-error' : undefined} aria-invalid={Boolean(errores.codigo)}
            autoComplete="off" id="almacen-codigo" maxLength={20} onChange={(event) => setCodigo(event.currentTarget.value.toUpperCase())} value={codigo} />
        </CampoConError>
        <CampoConError etiqueta="Nombre" htmlFor="almacen-nombre" error={errores.nombre} idError="almacen-nombre-error">
          <input aria-describedby={errores.nombre ? 'almacen-nombre-error' : undefined} aria-invalid={Boolean(errores.nombre)}
            id="almacen-nombre" maxLength={200} onChange={(event) => setNombre(event.currentTarget.value)} value={nombre} />
        </CampoConError>
        <CampoConError etiqueta="Tipo" htmlFor="almacen-tipo" error={errores.tipo} idError="almacen-tipo-error">
          <select aria-describedby={errores.tipo ? 'almacen-tipo-error' : undefined} aria-invalid={Boolean(errores.tipo)}
            id="almacen-tipo" onChange={(event) => setTipo(event.currentTarget.value as TipoAlmacen)} value={tipo}>
            <option value="Central">Central</option><option value="Regional">Regional</option><option value="Transito">Tránsito</option>
          </select>
        </CampoConError>
        <CampoConError etiqueta="Dirección" htmlFor="almacen-direccion" error={errores.direccion} idError="almacen-direccion-error">
          <input aria-describedby={errores.direccion ? 'almacen-direccion-error' : undefined} aria-invalid={Boolean(errores.direccion)}
            id="almacen-direccion" maxLength={300} onChange={(event) => setDireccion(event.currentTarget.value)} value={direccion} />
        </CampoConError>
        <CampoConError etiqueta="Ciudad" htmlFor="almacen-ciudad" error={errores.ciudad} idError="almacen-ciudad-error">
          <input aria-describedby={errores.ciudad ? 'almacen-ciudad-error' : undefined} aria-invalid={Boolean(errores.ciudad)}
            id="almacen-ciudad" list="almacen-ciudades" maxLength={100} onChange={(event) => setCiudad(event.currentTarget.value)} value={ciudad} />
          <datalist id="almacen-ciudades">{ciudades.map((item) => <option key={item} value={item} />)}</datalist>
        </CampoConError>
        <CampoConError etiqueta="Capacidad (m³)" htmlFor="almacen-capacidad" error={errores.capacidadM3} idError="almacen-capacidad-error">
          <input aria-describedby={errores.capacidadM3 ? 'almacen-capacidad-error' : undefined} aria-invalid={Boolean(errores.capacidadM3)}
            id="almacen-capacidad" min="0" onChange={(event) => setCapacidadM3(Number(event.currentTarget.value))} step="0.01" type="number" value={capacidadM3} />
        </CampoConError>
        <CampoConError etiqueta="Responsable (opcional)" htmlFor="almacen-responsable" error={errores.responsable} idError="almacen-responsable-error">
          <input aria-describedby={errores.responsable ? 'almacen-responsable-error' : undefined} aria-invalid={Boolean(errores.responsable)}
            id="almacen-responsable" maxLength={100} onChange={(event) => setResponsable(event.currentTarget.value)} value={responsable} />
        </CampoConError>
        <label className="almacen-active-field"><input checked={activo} onChange={(event) => setActivo(event.currentTarget.checked)} type="checkbox" />Activo</label>
        <div className="almacen-form-actions"><p className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar almacén'}</button></div>
      </form>
    </section>
  );
}
