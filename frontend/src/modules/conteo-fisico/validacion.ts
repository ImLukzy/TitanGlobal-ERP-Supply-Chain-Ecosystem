import type {
  ActualizarTomaInventarioRequest,
  CrearTomaInventarioRequest,
  LineaTomaInventarioRequest,
} from './tipos';

export interface ErroresTomaInventario {
  codigo?: string;
  almacenId?: string;
  responsable?: string;
  observaciones?: string;
  lineas?: string;
}

function validarLineas(lineas: LineaTomaInventarioRequest[]): string | undefined {
  if (lineas.length === 0) {
    return 'La toma de inventario debe incluir al menos un producto.';
  }
  const vistos = new Set<number>();
  for (const linea of lineas) {
    if (!Number.isInteger(linea.productoId) || linea.productoId < 1) {
      return 'El identificador del producto es obligatorio en cada línea.';
    }
    if (vistos.has(linea.productoId)) {
      return `El producto ${linea.productoId} está duplicado en la toma de inventario.`;
    }
    vistos.add(linea.productoId);
    if (!Number.isInteger(linea.cantidadContada) || linea.cantidadContada < 0 || linea.cantidadContada > 1000000) {
      return 'La cantidad contada debe estar entre 0 y 1000000.';
    }
    if (linea.observacion !== undefined && linea.observacion.trim().length > 250) {
      return 'La observación de la línea no puede superar 250 caracteres.';
    }
  }
  return undefined;
}

export function validarNuevaToma(valores: CrearTomaInventarioRequest): ErroresTomaInventario {
  const errores: ErroresTomaInventario = {};
  const codigo = valores.codigo.trim().toUpperCase();

  if (codigo === '') {
    errores.codigo = 'El código de la toma de inventario es obligatorio.';
  } else if (codigo.length > 20) {
    errores.codigo = 'El código de la toma de inventario no puede superar 20 caracteres.';
  } else if (codigo.includes(' ')) {
    errores.codigo = 'El código de la toma de inventario no puede contener espacios.';
  }

  if (!Number.isInteger(valores.almacenId) || valores.almacenId < 1) {
    errores.almacenId = 'El almacén es obligatorio.';
  }

  const responsable = valores.responsable.trim();
  if (responsable === '') {
    errores.responsable = 'El responsable de la toma de inventario es obligatorio.';
  } else if (responsable.length > 100) {
    errores.responsable = 'El responsable no puede superar 100 caracteres.';
  }

  if (valores.observaciones !== undefined && valores.observaciones.trim().length > 500) {
    errores.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  }

  const errorLineas = validarLineas(valores.lineas);
  if (errorLineas !== undefined) {
    errores.lineas = errorLineas;
  }

  return errores;
}

export function validarEdicionToma(
  valores: ActualizarTomaInventarioRequest,
): ErroresTomaInventario {
  const errores: ErroresTomaInventario = {};
  const responsable = valores.responsable.trim();

  if (responsable === '') {
    errores.responsable = 'El responsable de la toma de inventario es obligatorio.';
  } else if (responsable.length > 100) {
    errores.responsable = 'El responsable no puede superar 100 caracteres.';
  }

  if (valores.observaciones !== undefined && valores.observaciones.trim().length > 500) {
    errores.observaciones = 'Las observaciones no pueden superar 500 caracteres.';
  }

  const errorLineas = validarLineas(valores.lineas);
  if (errorLineas !== undefined) {
    errores.lineas = errorLineas;
  }

  return errores;
}
