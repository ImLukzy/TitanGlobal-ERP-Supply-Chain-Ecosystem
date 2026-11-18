export interface ValoresCentroTrabajo {
  codigo: string;
  nombre: string;
  tipo: string;
  planta: string;
  capacidadHorasDia: string;
  costoHora: string;
  responsable: string;
  activo: boolean;
}

export type ErroresCentroTrabajo = Partial<Record<keyof ValoresCentroTrabajo, string>>;

// Se valida el texto antes de convertirlo a number para no ocultar decimales ni
// redondear el límite decimal(18,2) de PostgreSQL durante la validación local.
function masDeDosDecimales(texto: string): boolean {
  const fraccion = texto.trim().split('.')[1] ?? '';
  return /[1-9]/.test(fraccion.slice(2));
}

function excedeCostoMaximo(texto: string): boolean {
  const [entera = '', decimal = ''] = texto.trim().replace(/^\+/, '').split('.');
  const digitos = entera.replace(/^0+/, '') || '0';
  if (digitos.length !== 16) {
    return digitos.length > 16;
  }
  return BigInt(digitos) * 100n + BigInt(decimal.padEnd(2, '0').slice(0, 2)) > 999999999999999999n;
}

export function validarCentroTrabajo(valores: ValoresCentroTrabajo): ErroresCentroTrabajo {
  const errores: ErroresCentroTrabajo = {};
  const codigo = valores.codigo.trim().toUpperCase();
  const nombre = valores.nombre.trim();
  const planta = valores.planta.trim();
  const capacidad = valores.capacidadHorasDia.trim();
  const costo = valores.costoHora.trim();
  const formatoDecimal = /^[+-]?(?:\d+(?:\.\d*)?|\.\d+)$/;

  if (codigo === '') {
    errores.codigo = 'El código es obligatorio.';
  } else if (!/^[A-Z0-9-]{2,20}$/.test(codigo)) {
    errores.codigo = 'El código solo admite letras, números y guiones (2 a 20 caracteres).';
  }
  if (nombre === '') {
    errores.nombre = 'El nombre del centro de trabajo es obligatorio.';
  } else if (nombre.length > 200) {
    errores.nombre = 'El nombre del centro de trabajo no puede superar 200 caracteres.';
  }
  if (!['Linea', 'Celda'].includes(valores.tipo)) {
    errores.tipo = 'El tipo de centro de trabajo no es válido.';
  }
  if (planta === '') {
    errores.planta = 'La planta es obligatoria.';
  } else if (planta.length > 100) {
    errores.planta = 'La planta no puede superar 100 caracteres.';
  }
  if (!formatoDecimal.test(capacidad) || !Number.isFinite(Number(capacidad)) || Number(capacidad) <= 0 || Number(capacidad) > 24) {
    errores.capacidadHorasDia = 'La capacidad debe ser mayor que 0 y como máximo 24 horas por día.';
  } else if (masDeDosDecimales(capacidad)) {
    errores.capacidadHorasDia = 'La capacidad admite como máximo 2 decimales.';
  }
  if (costo === '' || !formatoDecimal.test(costo)) {
    errores.costoHora = 'Introduce un costo por hora válido.';
  } else if (Number(costo) < 0) {
    errores.costoHora = 'El costo por hora no puede ser negativo.';
  } else if (masDeDosDecimales(costo)) {
    errores.costoHora = 'El costo por hora admite como máximo 2 decimales.';
  } else if (excedeCostoMaximo(costo)) {
    errores.costoHora = 'El costo por hora no puede superar 9999999999999999.99.';
  } else if (!Number.isSafeInteger(Math.round(Number(costo) * 100))) {
    // El contrato JSON usa number. Evita enviar un importe distinto del escrito.
    errores.costoHora = 'El costo por hora no puede representarse con precisión. Introduce un importe menor.';
  }
  if (valores.responsable.trim().length > 100) {
    errores.responsable = 'El responsable no puede superar 100 caracteres.';
  }
  return errores;
}
