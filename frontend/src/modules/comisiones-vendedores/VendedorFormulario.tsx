import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import type { GuardarVendedorRequest, VendedorDetalle } from './tipos';

interface Props {
  vendedor: VendedorDetalle | null;
  cancelar: () => void;
  guardar: (request: GuardarVendedorRequest) => Promise<void>;
}

interface Errores {
  codigo?: string;
  nombre?: string;
  email?: string;
  porcentajeComision?: string;
}

function validar(request: GuardarVendedorRequest): Errores {
  const errores: Errores = {};
  if (request.codigo === '') {
    errores.codigo = 'El código es obligatorio.';
  } else if (!/^[A-Z0-9-]{2,20}$/.test(request.codigo)) {
    errores.codigo = 'El código solo admite letras, números y guiones (2 a 20 caracteres).';
  }
  if (request.nombre === '') {
    errores.nombre = 'El nombre del vendedor es obligatorio.';
  } else if (request.nombre.length > 100) {
    errores.nombre = 'El nombre del vendedor no puede superar 100 caracteres.';
  }
  if (request.email !== null && (request.email.length > 150 || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(request.email))) {
    errores.email = 'El email no tiene un formato válido.';
  }
  if (request.porcentajeComision < 0 || request.porcentajeComision > 20) {
    errores.porcentajeComision = 'La comisión debe estar entre 0 y 20 %.';
  }
  return errores;
}

export function VendedorFormulario({ vendedor, cancelar, guardar }: Props) {
  const [codigo, setCodigo] = useState(vendedor?.codigo ?? '');
  const [nombre, setNombre] = useState(vendedor?.nombre ?? '');
  const [email, setEmail] = useState(vendedor?.email ?? '');
  const [porcentaje, setPorcentaje] = useState(vendedor?.porcentajeComision ?? 5);
  const [activo, setActivo] = useState(vendedor?.activo ?? true);
  const [errores, setErrores] = useState<Errores>({});
  const [errorApi, setErrorApi] = useState('');
  const [ocupado, setOcupado] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const request: GuardarVendedorRequest = {
      codigo: codigo.trim().toUpperCase(),
      nombre: nombre.trim(),
      email: email.trim() === '' ? null : email.trim(),
      porcentajeComision: porcentaje,
      activo,
    };
    const nuevosErrores = validar(request);
    setErrores(nuevosErrores);
    setErrorApi('');
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setOcupado(true);
    try {
      await guardar(request);
    } catch (error: unknown) {
      setErrorApi(error instanceof ApiError ? error.message : 'No se pudo guardar el vendedor.');
    } finally {
      setOcupado(false);
    }
  }

  return (
    <div className="clientes-dialog-backdrop">
      <section aria-labelledby="vendedor-form-title" aria-modal="true" className="clientes-dialog comision-dialog" role="dialog">
        <div className="page-eyebrow">MAESTRO DE VENDEDORES</div>
        <h2 id="vendedor-form-title">{vendedor === null ? 'Nuevo vendedor' : 'Editar vendedor'}</h2>
        <form className="comision-form" noValidate onSubmit={(event) => void enviar(event)}>
          <CampoConError error={errores.codigo} htmlFor="vendedor-codigo" etiqueta="Código" idError="vendedor-codigo-error">
            <input aria-describedby={errores.codigo ? 'vendedor-codigo-error' : undefined} aria-invalid={Boolean(errores.codigo)} autoComplete="off" id="vendedor-codigo" maxLength={20} onChange={(event) => setCodigo(event.currentTarget.value.toUpperCase())} value={codigo} />
          </CampoConError>
          <CampoConError error={errores.nombre} htmlFor="vendedor-nombre" etiqueta="Nombre" idError="vendedor-nombre-error">
            <input aria-describedby={errores.nombre ? 'vendedor-nombre-error' : undefined} aria-invalid={Boolean(errores.nombre)} id="vendedor-nombre" maxLength={100} onChange={(event) => setNombre(event.currentTarget.value)} value={nombre} />
          </CampoConError>
          <CampoConError error={errores.email} htmlFor="vendedor-email" etiqueta="Email (opcional)" idError="vendedor-email-error">
            <input aria-describedby={errores.email ? 'vendedor-email-error' : undefined} aria-invalid={Boolean(errores.email)} autoComplete="email" id="vendedor-email" maxLength={150} onChange={(event) => setEmail(event.currentTarget.value)} type="email" value={email} />
          </CampoConError>
          <CampoConError error={errores.porcentajeComision} htmlFor="vendedor-porcentaje" etiqueta="Porcentaje de comisión" idError="vendedor-porcentaje-error">
            <input aria-describedby={errores.porcentajeComision ? 'vendedor-porcentaje-error' : undefined} aria-invalid={Boolean(errores.porcentajeComision)} id="vendedor-porcentaje" max="20" min="0" onChange={(event) => setPorcentaje(Number(event.currentTarget.value))} step="0.01" type="number" value={porcentaje} />
          </CampoConError>
          <label className="comision-active-field"><input checked={activo} onChange={(event) => setActivo(event.currentTarget.checked)} type="checkbox" />Activo</label>
          {errorApi !== '' && <p className="clientes-error" role="alert">{errorApi}</p>}
          <div className="clientes-dialog-actions">
            <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
            <button className="primary-button" disabled={ocupado} type="submit">{ocupado ? 'Guardando…' : 'Guardar vendedor'}</button>
          </div>
        </form>
      </section>
    </div>
  );
}
