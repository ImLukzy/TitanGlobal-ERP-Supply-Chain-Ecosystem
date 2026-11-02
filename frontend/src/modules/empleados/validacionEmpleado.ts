// Mensajes literales de la spec 47 §4 (reglas 1 a 17, 20 a 22 y 24). El backend repite las reglas y tiene la última palabra.
export interface CamposEmpleado {
  tipoDocumento: string;
  numeroDocumento: string;
  nombres: string;
  apellidos: string;
  fechaNacimiento: string;
  fechaIngreso: string;
  cargo: string;
  area: string;
  email: string;
  telefono: string;
}

export type ErroresEmpleado = Partial<Record<keyof CamposEmpleado, string>>;

export interface CamposCese {
  fechaCese: string;
  motivo: string;
  fechaIngreso: string;
}

export type ErroresCese = Partial<Record<'fechaCese' | 'motivo', string>>;

export const LIMITE_MOTIVO_CESE = 300;

const tiposDocumento = ['DNI', 'CE', 'Pasaporte'];
const formatoNumeroDocumento = /^[A-Za-z0-9]+$/;
const formatoEmail = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
const EDAD_MINIMA = 18;

// Fecha de nacimiento más 18 años en calendario, en yyyy-MM-dd. El 29 de febrero pasa al 28 si el año no es bisiesto (como AddYears).
function fechaMayorDeEdad(fechaNacimiento: string): string {
  const [anio, mes, dia] = fechaNacimiento.split('-').map(Number);
  const cumpleanos = new Date(Date.UTC(anio + EDAD_MINIMA, mes - 1, dia));
  if (cumpleanos.getUTCMonth() !== mes - 1) {
    return new Date(Date.UTC(anio + EDAD_MINIMA, mes, 0)).toISOString().slice(0, 10);
  }
  return cumpleanos.toISOString().slice(0, 10);
}

export function validarEmpleado(campos: CamposEmpleado): ErroresEmpleado {
  const errores: ErroresEmpleado = {};
  const numeroDocumento = campos.numeroDocumento.trim();
  const nombres = campos.nombres.trim();
  const apellidos = campos.apellidos.trim();
  const cargo = campos.cargo.trim();
  const area = campos.area.trim();
  const email = campos.email.trim().toLowerCase();
  const telefono = campos.telefono.trim();

  if (!tiposDocumento.includes(campos.tipoDocumento)) {
    errores.tipoDocumento = 'El tipo de documento no es válido.';
  }

  if (numeroDocumento === '') {
    errores.numeroDocumento = 'El número de documento es obligatorio.';
  } else if (numeroDocumento.length > 20) {
    errores.numeroDocumento = 'El número de documento no puede superar 20 caracteres.';
  } else if (!formatoNumeroDocumento.test(numeroDocumento)) {
    errores.numeroDocumento = 'El número de documento solo admite letras y números.';
  }

  if (nombres === '') {
    errores.nombres = 'Los nombres son obligatorios.';
  } else if (nombres.length > 100) {
    errores.nombres = 'Los nombres no pueden superar 100 caracteres.';
  }

  if (apellidos === '') {
    errores.apellidos = 'Los apellidos son obligatorios.';
  } else if (apellidos.length > 100) {
    errores.apellidos = 'Los apellidos no pueden superar 100 caracteres.';
  }

  if (campos.fechaNacimiento === '') {
    errores.fechaNacimiento = 'La fecha de nacimiento es obligatoria.';
  }

  if (campos.fechaIngreso === '') {
    errores.fechaIngreso = 'La fecha de ingreso es obligatoria.';
  }

  if (cargo === '') {
    errores.cargo = 'El cargo es obligatorio.';
  } else if (cargo.length > 100) {
    errores.cargo = 'El cargo no puede superar 100 caracteres.';
  }

  if (area === '') {
    errores.area = 'El área es obligatoria.';
  } else if (area.length > 100) {
    errores.area = 'El área no puede superar 100 caracteres.';
  }

  if (email !== '' && (email.length > 150 || !formatoEmail.test(email))) {
    errores.email = 'El email no tiene un formato válido.';
  }

  if (telefono.length > 20) {
    errores.telefono = 'El teléfono no puede superar 20 caracteres.';
  }

  const edadValida = campos.fechaNacimiento === '' || campos.fechaIngreso === ''
    || campos.fechaIngreso >= fechaMayorDeEdad(campos.fechaNacimiento);
  if (!edadValida) {
    errores.fechaIngreso = 'El empleado debe tener al menos 18 años a la fecha de ingreso.';
  }

  return errores;
}

export function validarCese(campos: CamposCese): ErroresCese {
  const errores: ErroresCese = {};
  const motivo = campos.motivo.trim();

  if (campos.fechaCese === '') {
    errores.fechaCese = 'La fecha de cese es obligatoria.';
  } else if (campos.fechaCese < campos.fechaIngreso) {
    errores.fechaCese = 'La fecha de cese no puede ser anterior a la fecha de ingreso.';
  }

  if (motivo === '') {
    errores.motivo = 'El motivo de cese es obligatorio.';
  } else if (motivo.length > LIMITE_MOTIVO_CESE) {
    errores.motivo = 'El motivo de cese no puede superar 300 caracteres.';
  }

  return errores;
}
