import { useEffect, useRef } from 'react';
import { formatearCantidad } from '../../lib/formato';
import { agruparPorDominio, casillaBloqueada, contarPermisos, type CasillaPermiso } from './permisos';
import type { RolDetalle } from './tipos';
import { usePermisosRol } from './usePermisosRol';

interface EditorPermisosProps {
  rol: RolDetalle;
  /** Se avisa tras guardar para que la lista y la ficha actualicen sus totales. */
  guardado: () => void;
  cerrar: () => void;
}

const ID_NOTA_SISTEMA = 'permisos-nota-sistema';

export function EditorPermisos({ rol, guardado, cerrar }: EditorPermisosProps) {
  const datos = usePermisosRol(rol.id, rol.esSistema, guardado);
  const tituloRef = useRef<HTMLHeadingElement>(null);

  useEffect(() => {
    tituloRef.current?.focus();
  }, []);

  const grupos = agruparPorDominio(datos.actual);
  const hayCambios = datos.cambios > 0;

  return (
    <section aria-labelledby="permisos-editor-title" className="clientes-form-panel roles-permisos-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">PERMISOS POR MÓDULO</div>
          <h2 id="permisos-editor-title" ref={tituloRef} tabIndex={-1}>Permisos de {rol.nombre}</h2>
        </div>
        <button className="secondary-button" onClick={cerrar} type="button">Cerrar</button>
      </div>
      {datos.error !== '' && <p className="clientes-error" role="alert">{datos.error}</p>}
      {datos.cargando && <p aria-live="polite" className="clientes-status" role="status">Cargando permisos…</p>}
      {datos.error === '' && !datos.cargando && (
        <>
          {rol.esSistema && (
            <p className="rol-form-nota" id={ID_NOTA_SISTEMA}>
              Este rol es de sistema: no se le pueden quitar permisos, por eso algunas casillas están deshabilitadas.
            </p>
          )}
          <div className="permisos-dominios">
            {grupos.map((grupo, indice) => {
              const totales = contarPermisos(grupo.modulos);
              return (
                <details className="permisos-dominio" key={grupo.dominio} open={indice === 0}>
                  <summary>
                    <span className="permisos-dominio-nombre">{grupo.dominio}</span>
                    <span className="permisos-dominio-resumen">
                      {totales.lectura} de {grupo.modulos.length} con lectura · {totales.escritura} con escritura
                    </span>
                  </summary>
                  <div aria-label={`Atajos de ${grupo.dominio}`} className="permisos-atajos" role="group">
                    <button
                      aria-label={`Todo lectura en ${grupo.dominio}`}
                      className="secondary-button"
                      onClick={() => datos.aplicarAtajo(grupo.dominio, 'lectura')}
                      type="button"
                    >
                      Todo lectura
                    </button>
                    <button
                      aria-label={`Todo escritura en ${grupo.dominio}`}
                      className="secondary-button"
                      onClick={() => datos.aplicarAtajo(grupo.dominio, 'escritura')}
                      type="button"
                    >
                      Todo escritura
                    </button>
                    <button
                      aria-label={`Ninguno en ${grupo.dominio}`}
                      className="secondary-button"
                      disabled={rol.esSistema}
                      onClick={() => datos.aplicarAtajo(grupo.dominio, 'ninguno')}
                      type="button"
                    >
                      Ninguno
                    </button>
                  </div>
                  <ul className="permisos-lista">
                    {grupo.modulos.map((modulo) => (
                      <li className="permiso-fila" key={modulo.moduloNumero}>
                        <span className="permiso-modulo">
                          <span className="permiso-numero">{String(modulo.moduloNumero).padStart(2, '0')}</span>
                          {modulo.moduloNombre}
                        </span>
                        {(['leer', 'escribir'] as CasillaPermiso[]).map((casilla) => {
                          const bloqueada = casillaBloqueada(datos.original, modulo.moduloNumero, casilla, rol.esSistema);
                          const marcada = casilla === 'leer' ? modulo.puedeLeer : modulo.puedeEscribir;
                          const etiqueta = casilla === 'leer' ? 'Leer' : 'Escribir';
                          return (
                            <label className="permiso-casilla" key={casilla}>
                              <input
                                aria-describedby={bloqueada ? ID_NOTA_SISTEMA : undefined}
                                aria-label={`${etiqueta} ${modulo.moduloNombre}`}
                                checked={marcada}
                                disabled={bloqueada}
                                onChange={(evento) => datos.cambiarCasilla(modulo.moduloNumero, casilla, evento.currentTarget.checked)}
                                type="checkbox"
                              />
                              <span aria-hidden="true">{etiqueta}</span>
                            </label>
                          );
                        })}
                      </li>
                    ))}
                  </ul>
                </details>
              );
            })}
          </div>
          <div className="roles-form-actions permisos-acciones">
            <div>
              <p aria-live="polite" className="permisos-contador" role="status">
                {hayCambios ? `${formatearCantidad(datos.cambios, 'cambio', 'cambios')} sin guardar` : 'Sin cambios'}
              </p>
              {hayCambios && (
                <p className="rol-form-nota">Si cierras o cambias de rol sin guardar, se pierden estos cambios.</p>
              )}
              {datos.errorGuardar !== '' && <p className="permisos-error" role="alert">{datos.errorGuardar}</p>}
            </div>
            <div className="permisos-botones">
              <button className="secondary-button" disabled={!hayCambios || datos.guardando} onClick={datos.descartar} type="button">
                Descartar
              </button>
              <button
                className="primary-button"
                disabled={!hayCambios || datos.guardando}
                onClick={() => void datos.guardar()}
                type="button"
              >
                {datos.guardando ? 'Guardando…' : 'Guardar permisos'}
              </button>
            </div>
          </div>
        </>
      )}
    </section>
  );
}
