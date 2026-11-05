import type { PermisoModulo } from './tipos';

export const DOMINIOS_PRUEBA = [
  'Comercial, Ventas y CRM',
  'Compras y Abastecimiento Global',
  'Inventarios y Almacenes (WMS)',
  'Manufactura y Producción (MRP)',
  'Logística, Flota y Distribución (TMS)',
  'Finanzas, Contabilidad y Tesorería',
  'Capital Humano y Nóminas (HCM)',
  'Gobierno Corporativo, Seguridad y Auditoría',
];

export type ReglaPermisoPrueba = (moduloNumero: number) => 'escritura' | 'lectura' | 'ninguno';

/** Los 80 módulos del catálogo, diez por dominio, con el permiso que da la regla indicada. */
export function crearPermisosPrueba(regla: ReglaPermisoPrueba): PermisoModulo[] {
  const permisos: PermisoModulo[] = [];
  for (let numero = 1; numero <= 80; numero += 1) {
    const nivel = regla(numero);
    permisos.push({
      moduloNumero: numero,
      moduloNombre: `Módulo ${numero}`,
      dominio: DOMINIOS_PRUEBA[Math.floor((numero - 1) / 10)] ?? '',
      puedeLeer: nivel !== 'ninguno',
      puedeEscribir: nivel === 'escritura',
    });
  }
  return permisos;
}

/** Como el seed: VENTAS escribe en 01–10 y lee 21. */
export function permisosVentasPrueba(): PermisoModulo[] {
  return crearPermisosPrueba((numero) => {
    if (numero <= 10) {
      return 'escritura';
    }
    return numero === 21 ? 'lectura' : 'ninguno';
  });
}

/** Como el seed: ADMIN escribe en los 80 módulos. */
export function permisosAdminPrueba(): PermisoModulo[] {
  return crearPermisosPrueba(() => 'escritura');
}
