import type { Dominio, Modulo } from './catalogo';
import { pantallas } from './pantallas';

export function modulosConPantalla(dominio: Dominio): Modulo[] {
  return dominio.modulos.filter((modulo) => pantallas[modulo.slug] !== undefined);
}
