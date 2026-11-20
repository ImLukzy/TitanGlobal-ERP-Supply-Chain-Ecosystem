import { useState, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { actualizarCentroTrabajo, crearCentroTrabajo } from './api';
import { mensajeError } from './presentacion';
import type { CentroTrabajoDetalle, GuardarCentroTrabajoRequest, TipoCentroTrabajo } from './tipos';
import { validarCentroTrabajo, type ErroresCentroTrabajo, type ValoresCentroTrabajo } from './validacion';

interface Props {
  centro: CentroTrabajoDetalle | null;
  plantas: string[];
  cancelar: () => void;
  guardado: (id: number) => void;
}

export function CentroTrabajoFormulario({ centro, plantas, cancelar, guardado }: Props) {
  const [valores, setValores] = useState<ValoresCentroTrabajo>({
    codigo: centro?.codigo ?? '', nombre: centro?.nombre ?? '', tipo: centro?.tipo ?? 'Linea',
    planta: centro?.planta ?? '', capacidadHorasDia: String(centro?.capacidadHorasDia ?? ''),
    costoHora: String(centro?.costoHora ?? 0), responsable: centro?.responsable ?? '', activo: centro?.activo ?? true,
  });
  const [errores, setErrores] = useState<ErroresCentroTrabajo>({});
  const [mensaje, setMensaje] = useState('');
  const [guardando, setGuardando] = useState(false);

  function cambiar(campo: keyof ValoresCentroTrabajo, valor: string | boolean): void {
    setValores((actual) => ({ ...actual, [campo]: valor }));
  }

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (guardando) { return; }
    setMensaje('');
    const nuevosErrores = validarCentroTrabajo(valores);
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) { return; }
    const request: GuardarCentroTrabajoRequest = {
      codigo: valores.codigo.trim().toUpperCase(), nombre: valores.nombre.trim(),
      tipo: valores.tipo as TipoCentroTrabajo, planta: valores.planta.trim(),
      capacidadHorasDia: Number(valores.capacidadHorasDia), costoHora: Number(valores.costoHora),
      activo: valores.activo,
    };
    if (valores.responsable.trim() !== '') { request.responsable = valores.responsable.trim(); }
    setGuardando(true);
    try {
      const respuesta = centro === null
        ? await crearCentroTrabajo(request)
        : await actualizarCentroTrabajo(centro.id, request);
      guardado(respuesta.id);
    } catch (error: unknown) {
      setMensaje(mensajeError(error, 'No se pudo guardar el centro de trabajo.'));
    } finally {
      setGuardando(false);
    }
  }

  const campos: { campo: 'codigo' | 'nombre' | 'planta' | 'capacidadHorasDia' | 'costoHora' | 'responsable'; etiqueta: string; maximo?: number }[] = [
    { campo: 'codigo', etiqueta: 'Código', maximo: 20 },
    { campo: 'nombre', etiqueta: 'Nombre', maximo: 200 },
    { campo: 'planta', etiqueta: 'Planta', maximo: 100 },
    { campo: 'capacidadHorasDia', etiqueta: 'Capacidad (h/día)' },
    { campo: 'costoHora', etiqueta: 'Costo por hora (S/)' },
    { campo: 'responsable', etiqueta: 'Responsable (opcional)', maximo: 100 },
  ];

  return (
    <section className="clientes-form-panel centros-trabajo-form-panel" aria-labelledby="centro-form-title">
      <div className="clientes-panel-heading">
        <div><div className="page-eyebrow">CENTRO DE TRABAJO</div>
          <h2 id="centro-form-title">{centro === null ? 'Nuevo centro de trabajo' : 'Editar centro de trabajo'}</h2></div>
        <button className="secondary-button" disabled={guardando} onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form className="centros-trabajo-form" noValidate onSubmit={(event) => void enviar(event)}>
        {campos.map(({ campo, etiqueta, maximo }) => {
          const id = `centro-${campo}`;
          const decimal = campo === 'capacidadHorasDia' || campo === 'costoHora';
          return (
            <CampoConError key={campo} etiqueta={etiqueta} htmlFor={id} error={errores[campo]} idError={`${id}-error`}>
              <input id={id} aria-describedby={errores[campo] ? `${id}-error` : undefined}
                aria-invalid={Boolean(errores[campo])} autoComplete="off" disabled={guardando}
                inputMode={decimal ? 'decimal' : undefined} list={campo === 'planta' ? 'centro-plantas' : undefined}
                maxLength={maximo} value={valores[campo]}
                onChange={(event) => cambiar(campo, campo === 'codigo' ? event.currentTarget.value.toUpperCase() : event.currentTarget.value)} />
            </CampoConError>
          );
        })}
        <datalist id="centro-plantas">{plantas.map((planta) => <option key={planta} value={planta} />)}</datalist>
        <CampoConError etiqueta="Tipo" htmlFor="centro-tipo" error={errores.tipo} idError="centro-tipo-error">
          <select id="centro-tipo" disabled={guardando} value={valores.tipo}
            aria-invalid={Boolean(errores.tipo)} aria-describedby={errores.tipo ? 'centro-tipo-error' : undefined}
            onChange={(event) => cambiar('tipo', event.currentTarget.value)}>
            <option value="Linea">Línea</option><option value="Celda">Celda</option>
          </select>
        </CampoConError>
        <label className="centros-trabajo-active-field">
          <input type="checkbox" checked={valores.activo} disabled={guardando}
            onChange={(event) => cambiar('activo', event.currentTarget.checked)} />Activo
        </label>
        <div className="centros-trabajo-form-actions">
          <p className="clientes-form-message" role={mensaje ? 'alert' : 'status'}>{mensaje}</p>
          <button className="primary-button" disabled={guardando} type="submit">
            {guardando ? 'Guardando…' : 'Guardar centro de trabajo'}
          </button>
        </div>
      </form>
    </section>
  );
}
