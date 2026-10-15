import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import type { GuardarSegmentoRequest, SegmentoDetalle } from './tipos';
import { actualizarSegmento, crearSegmento } from './api';

interface Errores {
  codigo?: string;
  nombre?: string;
  descripcion?: string;
  prioridad?: string;
  montoMinimo?: string;
  ordenesMinimas?: string;
}

function validar(request: GuardarSegmentoRequest): Errores {
  const errores: Errores = {};
  const codigo = request.codigo.trim().toUpperCase();
  const nombre = request.nombre.trim();
  if (codigo === '') {
    errores.codigo = 'El código es obligatorio.';
  } else if (!/^[A-Z0-9-]{2,20}$/.test(codigo)) {
    errores.codigo = 'El código solo admite letras, números y guiones (2 a 20 caracteres).';
  }
  if (nombre === '') {
    errores.nombre = 'El nombre del segmento es obligatorio.';
  } else if (nombre.length > 100) {
    errores.nombre = 'El nombre del segmento no puede superar 100 caracteres.';
  }
  if ((request.descripcion?.length ?? 0) > 300) {
    errores.descripcion = 'La descripción no puede superar 300 caracteres.';
  }
  if (request.prioridad < 1 || request.prioridad > 999) {
    errores.prioridad = 'La prioridad debe estar entre 1 y 999.';
  }
  if (request.montoMinimo < 0) {
    errores.montoMinimo = 'El monto mínimo no puede ser negativo.';
  }
  if (request.ordenesMinimas < 0) {
    errores.ordenesMinimas = 'Las órdenes mínimas no pueden ser negativas.';
  }
  if (request.montoMinimo === 0 && request.ordenesMinimas === 0) {
    errores.montoMinimo = 'Define al menos un criterio: monto mínimo u órdenes mínimas.';
  }
  return errores;
}

interface Props {
  segmento: SegmentoDetalle | null;
  cancelar: () => void;
  guardado: () => Promise<void>;
}

export function SegmentoFormulario({ segmento, cancelar, guardado }: Props) {
  const [codigo, setCodigo] = useState(segmento?.codigo ?? '');
  const [nombre, setNombre] = useState(segmento?.nombre ?? '');
  const [descripcion, setDescripcion] = useState(segmento?.descripcion ?? '');
  const [prioridad, setPrioridad] = useState(segmento?.prioridad ?? 1);
  const [montoMinimo, setMontoMinimo] = useState(segmento?.montoMinimo ?? 0);
  const [ordenesMinimas, setOrdenesMinimas] = useState(segmento?.ordenesMinimas ?? 0);
  const [activo, setActivo] = useState(segmento?.activo ?? true);
  const [errores, setErrores] = useState<Errores>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setMensaje('');
    const request: GuardarSegmentoRequest = {
      codigo: codigo.trim().toUpperCase(),
      nombre: nombre.trim(),
      descripcion: descripcion.trim() === '' ? null : descripcion.trim(),
      prioridad,
      montoMinimo,
      ordenesMinimas,
      activo,
    };
    const nuevosErrores = validar(request);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) {
      return;
    }
    setGuardando(true);
    try {
      if (segmento === null) {
        await crearSegmento(request);
      } else {
        await actualizarSegmento(segmento.id, request);
      }
      await guardado();
    } catch (error: unknown) {
      setMensaje(error instanceof ApiError ? error.message : 'No se pudo guardar el segmento.');
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="segmento-form-title" className="clientes-form-panel segment-form-panel">
      <div className="clientes-panel-heading">
        <div><div className="page-eyebrow">SEGMENTACIÓN DE CLIENTES</div><h2 id="segmento-form-title">{segmento === null ? 'Nuevo segmento' : 'Editar segmento'}</h2></div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="segment-form" noValidate onSubmit={(event) => void enviar(event)}>
        <CampoConError etiqueta="Código" htmlFor="segmento-codigo" error={errores.codigo} idError="segmento-codigo-error">
          <input aria-describedby={errores.codigo ? 'segmento-codigo-error' : undefined} aria-invalid={Boolean(errores.codigo)}
            autoComplete="off" id="segmento-codigo" maxLength={20} onChange={(event) => setCodigo(event.currentTarget.value.toUpperCase())} value={codigo} />
        </CampoConError>
        <CampoConError etiqueta="Nombre" htmlFor="segmento-nombre" error={errores.nombre} idError="segmento-nombre-error">
          <input aria-describedby={errores.nombre ? 'segmento-nombre-error' : undefined} aria-invalid={Boolean(errores.nombre)}
            id="segmento-nombre" maxLength={100} onChange={(event) => setNombre(event.currentTarget.value)} value={nombre} />
        </CampoConError>
        <CampoConError etiqueta="Descripción (opcional)" htmlFor="segmento-descripcion" error={errores.descripcion} idError="segmento-descripcion-error">
          <textarea aria-describedby={errores.descripcion ? 'segmento-descripcion-error' : undefined} aria-invalid={Boolean(errores.descripcion)}
            id="segmento-descripcion" maxLength={301} onChange={(event) => setDescripcion(event.currentTarget.value)} value={descripcion} />
        </CampoConError>
        <CampoConError etiqueta="Prioridad" htmlFor="segmento-prioridad" error={errores.prioridad} idError="segmento-prioridad-error">
          <input aria-describedby={errores.prioridad ? 'segmento-prioridad-error' : undefined} aria-invalid={Boolean(errores.prioridad)}
            id="segmento-prioridad" onChange={(event) => setPrioridad(Number(event.currentTarget.value))} type="number" value={prioridad} />
        </CampoConError>
        <CampoConError etiqueta="Monto mínimo (S/)" htmlFor="segmento-monto" error={errores.montoMinimo} idError="segmento-monto-error">
          <input aria-describedby={errores.montoMinimo ? 'segmento-monto-error' : undefined} aria-invalid={Boolean(errores.montoMinimo)}
            id="segmento-monto" onChange={(event) => setMontoMinimo(Number(event.currentTarget.value))} step="0.01" type="number" value={montoMinimo} />
        </CampoConError>
        <CampoConError etiqueta="Órdenes mínimas" htmlFor="segmento-ordenes" error={errores.ordenesMinimas} idError="segmento-ordenes-error">
          <input aria-describedby={errores.ordenesMinimas ? 'segmento-ordenes-error' : undefined} aria-invalid={Boolean(errores.ordenesMinimas)}
            id="segmento-ordenes" onChange={(event) => setOrdenesMinimas(Number(event.currentTarget.value))} type="number" value={ordenesMinimas} />
        </CampoConError>
        <label className="segment-active-field"><input checked={activo} onChange={(event) => setActivo(event.currentTarget.checked)} type="checkbox" />Activo</label>
        <div className="segment-form-actions">
          <p className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar segmento'}</button>
        </div>
      </form>
    </section>
  );
}
