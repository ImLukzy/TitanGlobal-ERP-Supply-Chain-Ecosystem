import { textoNivel, textoOtorgadoPor } from './presentacion';
import type { PermisoEfectivo } from './tipos';

interface GrupoDominio {
  dominio: string;
  permisos: PermisoEfectivo[];
}

export function agruparPorDominio(permisos: PermisoEfectivo[]): GrupoDominio[] {
  const grupos: GrupoDominio[] = [];
  for (const permiso of permisos) {
    const existente = grupos.find((grupo) => grupo.dominio === permiso.dominio);
    if (existente) {
      existente.permisos.push(permiso);
    } else {
      grupos.push({ dominio: permiso.dominio, permisos: [permiso] });
    }
  }
  return grupos;
}

function conAcceso(permisos: PermisoEfectivo[]): number {
  return permisos.filter((permiso) => permiso.permiso !== 'Ninguno').length;
}

export function PermisosEfectivos({ permisos }: { permisos: PermisoEfectivo[] }) {
  const grupos = agruparPorDominio(permisos);
  return (
    <div className="usuario-dominios">
      {grupos.map((grupo) => (
        <details className="usuario-dominio" key={grupo.dominio}>
          <summary>
            <span>{grupo.dominio}</span>
            <small>{conAcceso(grupo.permisos)} de {grupo.permisos.length} con acceso</small>
          </summary>
          <ul>
            {grupo.permisos.map((permiso) => (
              <li key={permiso.moduloNumero}>
                <span className="usuario-modulo-nombre">
                  {String(permiso.moduloNumero).padStart(2, '0')} · {permiso.moduloNombre}
                </span>
                <span className={`usuario-permiso is-${permiso.permiso.toLowerCase()}`}>
                  {textoNivel(permiso.permiso)}
                </span>
                {permiso.otorgadoPor.length > 0 && <small>{textoOtorgadoPor(permiso.otorgadoPor)}</small>}
              </li>
            ))}
          </ul>
        </details>
      ))}
    </div>
  );
}
