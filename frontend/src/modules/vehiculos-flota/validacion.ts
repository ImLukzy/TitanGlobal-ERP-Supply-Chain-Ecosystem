export interface ValoresVehiculoFlota {
  placa: string;
  marca: string;
  modelo: string;
  anio: string;
  tipo: string;
  capacidadKg: string;
  activo: boolean;
  observaciones: string;
}

export type ErroresVehiculoFlota = Partial<Record<keyof ValoresVehiculoFlota, string>>;

export function validarVehiculoFlota(valores: ValoresVehiculoFlota): ErroresVehiculoFlota {
  const errores: ErroresVehiculoFlota = {};
  const textos = [
    ['placa', 20, 'La placa es obligatoria.', 'La placa no puede superar 20 caracteres.'],
    ['marca', 100, 'La marca es obligatoria.', 'La marca no puede superar 100 caracteres.'],
    ['modelo', 100, 'El modelo es obligatorio.', 'El modelo no puede superar 100 caracteres.'],
  ] as const;
  for (const [campo, maximo, obligatorio, longitud] of textos) {
    const valor = valores[campo].trim();
    if (valor === '') { errores[campo] = obligatorio; }
    else if (valor.length > maximo) { errores[campo] = longitud; }
  }
  const anio = Number(valores.anio);
  if (!/^\d+$/.test(valores.anio.trim()) || !Number.isInteger(anio) || anio < 1900 || anio > new Date().getUTCFullYear() + 1) {
    errores.anio = 'El año del vehículo no es válido.';
  }
  if (valores.tipo.trim() === '') {
    errores.tipo = 'El tipo es obligatorio.';
  } else if (!['Camion', 'Furgon', 'Moto', 'Otro'].includes(valores.tipo)) {
    errores.tipo = 'El tipo debe ser Camion, Furgon, Moto u Otro.';
  }
  const capacidad = valores.capacidadKg.trim();
  if (!/^\+?(?:\d+(?:\.\d*)?|\.\d+)$/.test(capacidad) || !Number.isFinite(Number(capacidad)) || Number(capacidad) <= 0) {
    errores.capacidadKg = 'La capacidad debe ser mayor que cero.';
  } else {
    const [entera = '', decimal = ''] = capacidad.replace(/^\+/, '').split('.');
    const digitos = entera.replace(/^0+/, '') || '0';
    if (digitos.length > 16 || (digitos.length === 16 && BigInt(digitos) * 100n + BigInt(decimal.padEnd(2, '0').slice(0, 2)) > 999999999999999999n)) {
      errores.capacidadKg = 'La capacidad no puede superar 9999999999999999.99.';
    } else if (/[1-9]/.test(decimal.slice(2))) {
      errores.capacidadKg = 'La capacidad no puede tener más de 2 decimales.';
    } else if (!Number.isSafeInteger(Math.round(Number(capacidad) * 100))) {
      errores.capacidadKg = 'La capacidad no puede representarse con precisión. Introduce una cantidad menor.';
    }
  }
  if (valores.observaciones.trim().length > 500) {
    errores.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  }
  return errores;
}
