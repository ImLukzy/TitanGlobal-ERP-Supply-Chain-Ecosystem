import { useEffect, useState } from 'react';
import { formatearCantidad } from '../../lib/formato';
import { obtenerMatrizPermisos } from './api';
import { mensajeError } from './errores';
import type { MatrizPermisos as MatrizDatos, ModuloMatriz, NivelPermiso } from './tipos';

interface GrupoMatriz {
  dominio: string;
  modulos: ModuloMatriz[];
}

function agruparMatriz(modulos: ModuloMatriz[]): GrupoMatriz[] {
  const grupos: GrupoMatriz[] = [];
  for (const modulo of modulos) {
    const grupo = grupos.find((candidato) => candidato.dominio === modulo.dominio);
    if (grupo === undefined) {
      grupos.push({ dominio: modulo.dominio, modulos: [modulo] });
    } else {
      grupo.modulos.push(modulo);
    }
  }
  return grupos;
}

function CeldaNivel({ nivel }: { nivel: NivelPermiso | undefined }) {
  if (nivel === 'Escritura' || nivel === 'Lectura') {
    return <span className={`matriz-nivel is-${nivel.toLowerCase()}`}>{nivel}</span>;
  }
  return (
    <span className="matriz-nivel is-ninguno">
      <span aria-hidden="true">—</span>
      <span className="sr-only">Sin acceso</span>
    </span>
  );
}

export function MatrizPermisos() {
  const [matriz, setMatriz] = useState<MatrizDatos | null>(null);
  const [error, setError] = useState('');
  const [dominioFiltro, setDominioFiltro] = useState('');

  useEffect(() => {
    let activo = true;
    void obtenerMatrizPermisos().then((datos) => {
      if (activo) {
        setMatriz(datos);
      }
    }).catch((falla: unknown) => {
      if (activo) {
        setError(mensajeError(falla, 'No se pudo cargar la matriz de permisos.'));
      }
    });
    return () => {
      activo = false;
    };
  }, []);

  const todosLosGrupos = agruparMatriz(matriz?.modulos ?? []);
  const grupos = todosLosGrupos.filter((grupo) => dominioFiltro === '' || grupo.dominio === dominioFiltro);
  const totalModulos = grupos.reduce((suma, grupo) => suma + grupo.modulos.length, 0);
  const columnas = (matriz?.roles.length ?? 0) + 1;

  return (
    <section aria-labelledby="matriz-title" className="clientes-list-panel roles-matriz-panel">
      <div className="clientes-panel-heading">
        <div>
          <div className="page-eyebrow">ROLES × MÓDULOS</div>
          <h2 id="matriz-title">Matriz de permisos</h2>
        </div>
      </div>
      {error !== '' && <p aria-live="polite" className="clientes-error" role="alert">{error}</p>}
      {matriz === null && error === '' && (
        <p aria-live="polite" className="clientes-status" role="status">Cargando matriz de permisos…</p>
      )}
      {matriz !== null && error === '' && matriz.roles.length === 0 && (
        <p aria-live="polite" className="clientes-status" role="status">No hay roles para mostrar en la matriz.</p>
      )}
      {matriz !== null && error === '' && matriz.roles.length > 0 && (
        <>
          <div className="roles-filters matriz-filtros">
            <div>
              <label htmlFor="matriz-dominio">Filtrar por dominio</label>
              <select id="matriz-dominio" onChange={(evento) => setDominioFiltro(evento.currentTarget.value)} value={dominioFiltro}>
                <option value="">Todos los dominios</option>
                {todosLosGrupos.map((grupo) => (
                  <option key={grupo.dominio} value={grupo.dominio}>{grupo.dominio}</option>
                ))}
              </select>
            </div>
            <p aria-live="polite" className="matriz-resumen" role="status">
              {formatearCantidad(totalModulos, 'módulo', 'módulos')} · {formatearCantidad(matriz.roles.length, 'rol', 'roles')}
            </p>
          </div>
          <div aria-label="Matriz de permisos por rol" className="matriz-scroll" role="region" tabIndex={0}>
            <table className="matriz-tabla">
              <caption className="sr-only">Nivel de acceso de cada rol a cada módulo</caption>
              <thead>
                <tr>
                  <th scope="col">Módulo</th>
                  {matriz.roles.map((rol) => (
                    <th key={rol.id} scope="col">{rol.codigo}</th>
                  ))}
                </tr>
              </thead>
              {grupos.map((grupo) => (
                <tbody key={grupo.dominio}>
                  <tr className="matriz-dominio">
                    <th colSpan={columnas} scope="rowgroup">{grupo.dominio}</th>
                  </tr>
                  {grupo.modulos.map((modulo) => (
                    <tr key={modulo.moduloNumero}>
                      <th scope="row">
                        <span className="permiso-numero">{String(modulo.moduloNumero).padStart(2, '0')}</span>{' '}
                        {modulo.moduloNombre}
                      </th>
                      {matriz.roles.map((rol, indice) => (
                        <td key={rol.id}>
                          <CeldaNivel nivel={modulo.permisos[indice]} />
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              ))}
            </table>
          </div>
        </>
      )}
    </section>
  );
}
