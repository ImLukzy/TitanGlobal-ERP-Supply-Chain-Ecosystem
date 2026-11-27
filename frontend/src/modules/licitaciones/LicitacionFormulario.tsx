import { useState, type FormEvent } from 'react';
import { ApiError } from '../../api/client';
import { CampoConError } from '../../components/CampoConError';
import { aFechaHoraLocal, etiquetasTipo } from './plazo';
import type { CrearLicitacionRequest, LicitacionDetalle, TipoLicitacion } from './tipos';
import { validarLicitacion, type CampoLicitacion, type ErroresLicitacion } from './validacion';

interface LicitacionFormularioProps {
  licitacion: LicitacionDetalle | null;
  cancelar: () => void;
  guardar: (datos: CrearLicitacionRequest) => Promise<void>;
}

const campos: CampoLicitacion[] = ['numero', 'titulo', 'presupuesto', 'fechaCierre', 'descripcion'];

function describir(errores: ErroresLicitacion, campo: CampoLicitacion): string | undefined {
  return errores[campo] ? `licitacion-${campo}-error` : undefined;
}

export function LicitacionFormulario({ licitacion, cancelar, guardar }: LicitacionFormularioProps) {
  const edicion = licitacion !== null;
  const [numero, setNumero] = useState(licitacion?.numero ?? '');
  const [titulo, setTitulo] = useState(licitacion?.titulo ?? '');
  const [descripcion, setDescripcion] = useState(licitacion?.descripcion ?? '');
  const [tipo, setTipo] = useState<TipoLicitacion>(licitacion?.tipo ?? 'ConcursoPrecios');
  const [presupuesto, setPresupuesto] = useState(licitacion === null ? '' : String(licitacion.presupuestoReferencial));
  const [fechaCierre, setFechaCierre] = useState(licitacion === null ? '' : aFechaHoraLocal(licitacion.fechaCierre));
  const [errores, setErrores] = useState<ErroresLicitacion>({});
  const [errorApi, setErrorApi] = useState('');
  const [guardando, setGuardando] = useState(false);

  async function enviar(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const nuevos = validarLicitacion({ numero, titulo, descripcion, presupuesto, fechaCierre }, edicion);
    setErrores(nuevos);
    if (campos.some((campo) => nuevos[campo] !== undefined)) {
      return;
    }
    const datos: CrearLicitacionRequest = {
      numero: numero.trim(),
      titulo: titulo.trim(),
      tipo,
      presupuestoReferencial: Number(presupuesto),
      fechaCierre: new Date(fechaCierre).toISOString(),
    };
    if (descripcion.trim() !== '') {
      datos.descripcion = descripcion.trim();
    }
    setErrorApi('');
    setGuardando(true);
    try {
      await guardar(datos);
    } catch (error: unknown) {
      setErrorApi(error instanceof ApiError ? error.message : 'No se pudo guardar la licitación.');
    } finally {
      setGuardando(false);
    }
  }

  return (
    <section aria-labelledby="licitacion-form-title" className="licitaciones-panel licitacion-form">
      <div className="licitaciones-panel-heading">
        <div>
          <span className="page-eyebrow">LICITACIÓN</span>
          <h2 id="licitacion-form-title">{edicion ? `Editar licitación ${licitacion.numero}` : 'Nueva licitación'}</h2>
        </div>
        <button className="secondary-button" onClick={cancelar} type="button">Cancelar</button>
      </div>
      <form noValidate onSubmit={(event) => void enviar(event)}>
        <div className="licitacion-form-fields">
          {edicion ? (
            <dl className="licitacion-form-readonly">
              <div><dt>Número</dt><dd>{licitacion.numero}</dd></div>
              <div><dt>Tipo</dt><dd>{etiquetasTipo[licitacion.tipo]}</dd></div>
            </dl>
          ) : (
            <>
              <CampoConError etiqueta="Número" htmlFor="licitacion-numero" error={errores.numero} idError="licitacion-numero-error">
                <input
                  id="licitacion-numero"
                  aria-describedby={describir(errores, 'numero')}
                  aria-invalid={Boolean(errores.numero)}
                  maxLength={20}
                  onChange={(event) => setNumero(event.currentTarget.value.toUpperCase())}
                  value={numero}
                />
              </CampoConError>
              <CampoConError etiqueta="Tipo" htmlFor="licitacion-tipo" idError="licitacion-tipo-error">
                <select id="licitacion-tipo" onChange={(event) => setTipo(event.currentTarget.value as TipoLicitacion)} value={tipo}>
                  {(Object.keys(etiquetasTipo) as TipoLicitacion[]).map((valor) => <option key={valor} value={valor}>{etiquetasTipo[valor]}</option>)}
                </select>
              </CampoConError>
            </>
          )}
          <CampoConError etiqueta="Título" htmlFor="licitacion-titulo" error={errores.titulo} idError="licitacion-titulo-error">
            <input
              id="licitacion-titulo"
              aria-describedby={describir(errores, 'titulo')}
              aria-invalid={Boolean(errores.titulo)}
              onChange={(event) => setTitulo(event.currentTarget.value)}
              value={titulo}
            />
          </CampoConError>
          <CampoConError etiqueta="Presupuesto referencial" htmlFor="licitacion-presupuesto" error={errores.presupuesto} idError="licitacion-presupuesto-error">
            <input
              id="licitacion-presupuesto"
              aria-describedby={describir(errores, 'presupuesto')}
              aria-invalid={Boolean(errores.presupuesto)}
              inputMode="decimal"
              onChange={(event) => setPresupuesto(event.currentTarget.value)}
              value={presupuesto}
            />
          </CampoConError>
          <CampoConError etiqueta="Fecha de cierre" htmlFor="licitacion-fecha-cierre" error={errores.fechaCierre} idError="licitacion-fechaCierre-error">
            <input
              id="licitacion-fecha-cierre"
              aria-describedby={describir(errores, 'fechaCierre')}
              aria-invalid={Boolean(errores.fechaCierre)}
              onChange={(event) => setFechaCierre(event.currentTarget.value)}
              type="datetime-local"
              value={fechaCierre}
            />
          </CampoConError>
          <CampoConError etiqueta="Descripción" htmlFor="licitacion-descripcion" error={errores.descripcion} idError="licitacion-descripcion-error">
            <textarea
              id="licitacion-descripcion"
              aria-describedby={describir(errores, 'descripcion')}
              aria-invalid={Boolean(errores.descripcion)}
              maxLength={520}
              onChange={(event) => setDescripcion(event.currentTarget.value)}
              value={descripcion}
            />
            <span className="licitacion-contador">{descripcion.length}/500</span>
          </CampoConError>
        </div>
        {errorApi && <p className="licitaciones-error" role="alert">{errorApi}</p>}
        <div className="licitacion-form-actions">
          <button className="primary-button" disabled={guardando} type="submit">{guardando ? 'Guardando…' : 'Guardar licitación'}</button>
        </div>
      </form>
    </section>
  );
}
