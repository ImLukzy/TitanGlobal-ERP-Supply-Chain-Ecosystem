import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react';
import { CampoConError } from '../../components/CampoConError';
import { analizarCsvReloj, type ResultadoCsv } from './csvReloj';
import { mensajeError, useDialogoModal } from './presentacion';
import type { ImportarMarcacionesRequest, ResultadoImportacion } from './tipos';
import { validarDispositivo } from './validacionMarcacion';

interface Props {
  cerrar: () => void;
  importar: (request: ImportarMarcacionesRequest) => Promise<ResultadoImportacion>;
}

const FILAS_VISTA_PREVIA = 5;

// Diálogo modal propio: lee el CSV del reloj en el navegador, muestra una vista previa y lo envía como JSON.
export function DialogoImportacion({ cerrar, importar }: Props) {
  const dialogoRef = useRef<HTMLDivElement>(null);
  const primerCampoRef = useRef<HTMLInputElement>(null);
  const lecturaRef = useRef(0);
  const [dispositivo, setDispositivo] = useState('');
  const [errorDispositivo, setErrorDispositivo] = useState<string | undefined>(undefined);
  const [analisis, setAnalisis] = useState<ResultadoCsv | null>(null);
  const [errorArchivo, setErrorArchivo] = useState('');
  const [mensaje, setMensaje] = useState('');
  const [enviando, setEnviando] = useState(false);
  const [resultado, setResultado] = useState<ResultadoImportacion | null>(null);

  useEffect(() => {
    primerCampoRef.current?.focus();
  }, []);

  useDialogoModal(dialogoRef, cerrar, enviando);

  // Si se elige otro archivo mientras se lee el anterior, solo cuenta la última lectura.
  async function elegirArchivo(evento: ChangeEvent<HTMLInputElement>): Promise<void> {
    const archivo = evento.currentTarget.files?.[0];
    lecturaRef.current += 1;
    const lectura = lecturaRef.current;
    setErrorArchivo('');
    setMensaje('');
    if (archivo === undefined) {
      setAnalisis(null);
      return;
    }
    try {
      const texto = await archivo.text();
      if (lectura === lecturaRef.current) {
        setAnalisis(analizarCsvReloj(texto));
      }
    } catch {
      if (lectura === lecturaRef.current) {
        setAnalisis(null);
        setErrorArchivo('No se pudo leer el archivo.');
      }
    }
  }

  async function enviar(evento: FormEvent<HTMLFormElement>): Promise<void> {
    evento.preventDefault();
    setMensaje('');
    const nuevoError = validarDispositivo(dispositivo);
    setErrorDispositivo(nuevoError);
    if (analisis === null) {
      setErrorArchivo('Selecciona el archivo CSV del reloj.');
    }
    if (nuevoError !== undefined || analisis === null || !analisis.valido) {
      return;
    }
    setEnviando(true);
    try {
      setResultado(await importar({ dispositivo: dispositivo.trim(), registros: analisis.registros }));
    } catch (falla: unknown) {
      setMensaje(mensajeError(falla, 'No se pudo importar el archivo.'));
    } finally {
      setEnviando(false);
    }
  }

  const mensajeArchivo = analisis !== null && !analisis.valido ? analisis.error : errorArchivo;
  const vistaPrevia = analisis !== null && analisis.valido ? analisis.registros.slice(0, FILAS_VISTA_PREVIA) : [];

  return (
    <div className="clientes-dialog-backdrop">
      <div
        aria-labelledby="importacion-dialog-title"
        aria-modal="true"
        className="clientes-dialog asistencia-dialog asistencia-dialog-ancho"
        ref={dialogoRef}
        role="dialog"
        tabIndex={-1}
      >
        <div className="page-eyebrow">IMPORTACIÓN</div>
        <h2 id="importacion-dialog-title">Importar archivo del reloj</h2>
        {resultado === null && (
          <form className="asistencia-dialog-form" noValidate onSubmit={(evento) => void enviar(evento)}>
            <CampoConError etiqueta="Dispositivo" htmlFor="importacion-dispositivo" error={errorDispositivo} idError="importacion-dispositivo-error">
              <input
                aria-describedby={errorDispositivo ? 'importacion-dispositivo-error' : undefined}
                aria-invalid={Boolean(errorDispositivo)}
                id="importacion-dispositivo"
                onChange={(event) => setDispositivo(event.currentTarget.value)}
                ref={primerCampoRef}
                type="text"
                value={dispositivo}
              />
            </CampoConError>
            <CampoConError etiqueta="Archivo del reloj (.csv)" htmlFor="importacion-archivo" error={mensajeArchivo === '' ? undefined : mensajeArchivo} idError="importacion-archivo-error">
              <input
                accept=".csv,text/csv"
                aria-describedby={mensajeArchivo === '' ? 'importacion-formato' : 'importacion-archivo-error importacion-formato'}
                aria-invalid={mensajeArchivo !== ''}
                id="importacion-archivo"
                onChange={(event) => void elegirArchivo(event)}
                type="file"
              />
            </CampoConError>
            <p className="asistencia-aviso" id="importacion-formato">
              Cabecera: numeroRegistro,legajo,tipo,instante (instante ISO 8601 con zona). Máximo 1000 registros.
            </p>
            {analisis !== null && analisis.valido && (
              <section aria-label="Vista previa del archivo" className="asistencia-preview">
                <p><strong>{`Total de registros: ${analisis.registros.length}`}</strong></p>
                <div className="asistencia-scroll">
                  <table className="asistencia-mini-tabla">
                    <caption>{`Primeras ${vistaPrevia.length} filas`}</caption>
                    <thead>
                      <tr>
                        <th scope="col">Registro</th>
                        <th scope="col">Legajo</th>
                        <th scope="col">Tipo</th>
                        <th scope="col">Instante</th>
                      </tr>
                    </thead>
                    <tbody>
                      {vistaPrevia.map((registro, indice) => (
                        <tr key={`${registro.numeroRegistro}-${indice}`}>
                          <td>{registro.numeroRegistro}</td>
                          <td>{registro.legajo}</td>
                          <td>{registro.tipo}</td>
                          <td className="asistencia-numero">{registro.instante ?? '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            )}
            {mensaje !== '' && (
              <p aria-live="polite" className="clientes-error clientes-dialog-error" role="alert">
                <span aria-hidden="true">!</span>
                {mensaje}
              </p>
            )}
            <div className="clientes-dialog-actions">
              <button className="secondary-button" disabled={enviando} onClick={cerrar} type="button">
                Cancelar
              </button>
              <button className="primary-button" disabled={enviando} type="submit">
                {enviando ? 'Importando…' : 'Importar'}
              </button>
            </div>
          </form>
        )}
        {resultado !== null && (
          <>
            <dl aria-label="Resultado de la importación" className="asistencia-resultado">
              <div>
                <dt>Recibidos</dt>
                <dd>{resultado.recibidos}</dd>
              </div>
              <div>
                <dt>Importados</dt>
                <dd>{resultado.importados}</dd>
              </div>
              <div>
                <dt>Ya importados</dt>
                <dd>{resultado.yaImportados}</dd>
              </div>
              <div>
                <dt>Duplicados</dt>
                <dd>{resultado.duplicados}</dd>
              </div>
            </dl>
            {resultado.rechazados.length > 0 && (
              <section aria-labelledby="importacion-rechazados-title" className="asistencia-preview">
                <h3 id="importacion-rechazados-title">{`Rechazados (${resultado.rechazados.length})`}</h3>
                <div className="asistencia-scroll">
                  <table className="asistencia-mini-tabla">
                    <caption>Registros rechazados</caption>
                    <thead>
                      <tr>
                        <th scope="col">Registro</th>
                        <th scope="col">Motivo</th>
                      </tr>
                    </thead>
                    <tbody>
                      {resultado.rechazados.map((rechazado, indice) => (
                        <tr key={`${rechazado.numeroRegistro}-${indice}`}>
                          <td>{rechazado.numeroRegistro}</td>
                          <td>{rechazado.motivo}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            )}
            <div className="clientes-dialog-actions">
              <button className="primary-button" onClick={cerrar} type="button">Cerrar</button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
