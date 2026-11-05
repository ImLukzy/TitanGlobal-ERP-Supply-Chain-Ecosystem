import type { GuardarPermisosRequest, PermisoModulo } from './tipos';

export type AtajoDominio = 'lectura' | 'escritura' | 'ninguno';

export interface GrupoDominio {
  dominio: string;
  modulos: PermisoModulo[];
}

export type CasillaPermiso = 'leer' | 'escribir';

/** Agrupa los módulos por dominio conservando el orden en que llegan de la API. */
export function agruparPorDominio(permisos: PermisoModulo[]): GrupoDominio[] {
  const grupos: GrupoDominio[] = [];
  for (const permiso of permisos) {
    const grupo = grupos.find((candidato) => candidato.dominio === permiso.dominio);
    if (grupo === undefined) {
      grupos.push({ dominio: permiso.dominio, modulos: [permiso] });
    } else {
      grupo.modulos.push(permiso);
    }
  }
  return grupos;
}

/** Marcar «Escribir» marca «Leer»; desmarcar «Leer» desmarca «Escribir». */
export function cambiarCasilla(
  permisos: PermisoModulo[],
  moduloNumero: number,
  casilla: CasillaPermiso,
  marcada: boolean,
): PermisoModulo[] {
  return permisos.map((permiso) => {
    if (permiso.moduloNumero !== moduloNumero) {
      return permiso;
    }
    if (casilla === 'escribir') {
      return { ...permiso, puedeEscribir: marcada, puedeLeer: marcada ? true : permiso.puedeLeer };
    }
    return { ...permiso, puedeLeer: marcada, puedeEscribir: marcada ? permiso.puedeEscribir : false };
  });
}

/**
 * En un rol de sistema no se puede quitar ningún permiso que ya tenga: esa casilla queda deshabilitada.
 * En un rol personalizado nunca hay casillas bloqueadas.
 */
export function casillaBloqueada(
  original: PermisoModulo[],
  moduloNumero: number,
  casilla: CasillaPermiso,
  esSistema: boolean,
): boolean {
  if (!esSistema) {
    return false;
  }
  const inicial = original.find((permiso) => permiso.moduloNumero === moduloNumero);
  if (inicial === undefined) {
    return false;
  }
  return casilla === 'leer' ? inicial.puedeLeer : inicial.puedeEscribir;
}

/**
 * Aplica «Todo lectura», «Todo escritura» o «Ninguno» a los módulos de un dominio.
 * En un rol de sistema el atajo nunca quita permisos que ya tenía.
 */
export function aplicarAtajoDominio(
  permisos: PermisoModulo[],
  original: PermisoModulo[],
  dominio: string,
  atajo: AtajoDominio,
  esSistema: boolean,
): PermisoModulo[] {
  return permisos.map((permiso) => {
    if (permiso.dominio !== dominio) {
      return permiso;
    }
    let puedeLeer = atajo !== 'ninguno';
    let puedeEscribir = atajo === 'escritura';
    if (esSistema) {
      const inicial = original.find((candidato) => candidato.moduloNumero === permiso.moduloNumero);
      puedeLeer = puedeLeer || inicial?.puedeLeer === true;
      puedeEscribir = puedeEscribir || inicial?.puedeEscribir === true;
    }
    return { ...permiso, puedeLeer, puedeEscribir };
  });
}

/** Número de módulos cuyo permiso difiere del último estado guardado. */
export function contarCambios(original: PermisoModulo[], actual: PermisoModulo[]): number {
  let cambios = 0;
  for (const permiso of actual) {
    const inicial = original.find((candidato) => candidato.moduloNumero === permiso.moduloNumero);
    if (inicial === undefined || inicial.puedeLeer !== permiso.puedeLeer || inicial.puedeEscribir !== permiso.puedeEscribir) {
      cambios += 1;
    }
  }
  return cambios;
}

/** Cuerpo del PUT: siempre el conjunto completo de módulos, en el orden recibido. */
export function construirRequestPermisos(permisos: PermisoModulo[]): GuardarPermisosRequest {
  return {
    permisos: permisos.map((permiso) => ({
      moduloNumero: permiso.moduloNumero,
      puedeLeer: permiso.puedeLeer,
      puedeEscribir: permiso.puedeEscribir,
    })),
  };
}

export function contarPermisos(permisos: PermisoModulo[]): { lectura: number; escritura: number } {
  return {
    lectura: permisos.filter((permiso) => permiso.puedeLeer).length,
    escritura: permisos.filter((permiso) => permiso.puedeEscribir).length,
  };
}
