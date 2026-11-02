import { fireEvent, screen, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import {
  limpiarEmpleados,
  montarEmpleadosCargados,
  prepararApiEmpleados,
  urlsDeListado,
  type OpcionesApiEmpleados,
} from './empleados.testUtils';
import { validarCese, validarEmpleado, type CamposEmpleado } from './validacionEmpleado';

afterEach(() => {
  limpiarEmpleados();
});

const camposValidos: CamposEmpleado = {
  tipoDocumento: 'DNI',
  numeroDocumento: '10000009',
  nombres: 'Rosa',
  apellidos: 'Paredes Lima',
  fechaNacimiento: '1992-04-10',
  fechaIngreso: '2024-02-01',
  cargo: 'Analista',
  area: 'Finanzas',
  email: '',
  telefono: '',
};

const etiquetas: Record<keyof CamposEmpleado, string> = {
  tipoDocumento: 'Tipo de documento',
  numeroDocumento: 'Número de documento',
  nombres: 'Nombres',
  apellidos: 'Apellidos',
  fechaNacimiento: 'Fecha de nacimiento',
  fechaIngreso: 'Fecha de ingreso',
  cargo: 'Cargo',
  area: 'Área',
  email: 'Email',
  telefono: 'Teléfono',
};

function llenarFormulario(cambios: Partial<CamposEmpleado> = {}): void {
  const campos = { ...camposValidos, ...cambios };
  for (const clave of Object.keys(etiquetas) as Array<keyof CamposEmpleado>) {
    fireEvent.change(screen.getByLabelText(etiquetas[clave]), { target: { value: campos[clave] } });
  }
}

async function abrirNuevoEmpleado(opciones?: OpcionesApiEmpleados) {
  const api = prepararApiEmpleados(opciones);
  await montarEmpleadosCargados(api.fetchMock);
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo empleado' }));
  return api;
}

function guardar(): void {
  fireEvent.click(screen.getByRole('button', { name: 'Guardar empleado' }));
}

function envios(llamadas: Array<{ metodo: string }>): number {
  return llamadas.filter((llamada) => llamada.metodo === 'POST' || llamada.metodo === 'PUT').length;
}

describe('formulario de empleado: validación local', () => {
  it('muestra todos los obligatorios vacíos sin enviar nada', async () => {
    const { llamadas } = await abrirNuevoEmpleado();
    guardar();
    for (const mensaje of [
      'El número de documento es obligatorio.',
      'Los nombres son obligatorios.',
      'Los apellidos son obligatorios.',
      'La fecha de nacimiento es obligatoria.',
      'La fecha de ingreso es obligatoria.',
      'El cargo es obligatorio.',
      'El área es obligatoria.',
    ]) {
      expect(await screen.findByText(mensaje)).toBeInTheDocument();
    }
    expect(envios(llamadas)).toBe(0);
  });

  it('marca el campo inválido y lo asocia a su mensaje', async () => {
    await abrirNuevoEmpleado();
    guardar();
    const nombres = await screen.findByLabelText('Nombres');
    expect(nombres).toHaveAttribute('aria-invalid', 'true');
    expect(nombres).toHaveAccessibleDescription('Los nombres son obligatorios.');
  });

  it.each<[string, Partial<CamposEmpleado>, string]>([
    ['número de documento de más de 20 caracteres', { numeroDocumento: 'A'.repeat(21) }, 'El número de documento no puede superar 20 caracteres.'],
    ['número de documento con caracteres no alfanuméricos', { numeroDocumento: 'AB-12' }, 'El número de documento solo admite letras y números.'],
    ['nombres de más de 100 caracteres', { nombres: 'N'.repeat(101) }, 'Los nombres no pueden superar 100 caracteres.'],
    ['apellidos de más de 100 caracteres', { apellidos: 'A'.repeat(101) }, 'Los apellidos no pueden superar 100 caracteres.'],
    ['cargo de más de 100 caracteres', { cargo: 'C'.repeat(101) }, 'El cargo no puede superar 100 caracteres.'],
    ['área de más de 100 caracteres', { area: 'A'.repeat(101) }, 'El área no puede superar 100 caracteres.'],
    ['email con formato inválido', { email: 'sin-arroba' }, 'El email no tiene un formato válido.'],
    ['teléfono de más de 20 caracteres', { telefono: '9'.repeat(21) }, 'El teléfono no puede superar 20 caracteres.'],
    ['menor de 18 años a la fecha de ingreso', { fechaNacimiento: '2010-01-01', fechaIngreso: '2024-01-01' }, 'El empleado debe tener al menos 18 años a la fecha de ingreso.'],
  ])('rechaza en local: %s', async (_nombre, cambios, mensaje) => {
    const { llamadas } = await abrirNuevoEmpleado();
    llenarFormulario(cambios);
    guardar();
    expect(await screen.findByText(mensaje)).toBeInTheDocument();
    expect(envios(llamadas)).toBe(0);
  });
});

describe('formulario de empleado: guardado', () => {
  it('crea con POST, el cuerpo exacto y las fechas en yyyy-MM-dd, y selecciona el id devuelto', async () => {
    const { fetchMock, llamadas } = await abrirNuevoEmpleado({ idCreado: 2 });
    const listadosAntes = urlsDeListado(fetchMock).length;
    llenarFormulario({
      tipoDocumento: 'CE',
      numeroDocumento: ' 00123abc ',
      nombres: ' Rosa ',
      apellidos: ' Paredes Lima ',
      cargo: ' Analista ',
      area: ' Finanzas ',
      email: ' Rosa.Paredes@TitanGlobal.example ',
      telefono: ' 999111222 ',
    });
    guardar();
    await waitFor(() => expect(llamadas.some((llamada) => llamada.metodo === 'POST')).toBe(true));
    const post = llamadas.find((llamada) => llamada.metodo === 'POST');
    expect(post?.url).toBe('/api/v1/empleados');
    expect(post?.cuerpo).toEqual({
      tipoDocumento: 'CE',
      numeroDocumento: '00123abc',
      nombres: 'Rosa',
      apellidos: 'Paredes Lima',
      fechaNacimiento: '1992-04-10',
      fechaIngreso: '2024-02-01',
      cargo: 'Analista',
      area: 'Finanzas',
      email: 'rosa.paredes@titanglobal.example',
      telefono: '999111222',
    });
    expect(await screen.findByRole('heading', { name: 'Luis Rojas Mendoza' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Nuevo empleado' })).not.toBeInTheDocument();
    await waitFor(() => expect(urlsDeListado(fetchMock)).toHaveLength(listadosAntes + 1));
    expect(llamadas.some((llamada) => llamada.url === '/api/v1/empleados/2' && llamada.metodo === 'GET')).toBe(true);
  });

  it('envía null en email y teléfono cuando se dejan vacíos', async () => {
    const { llamadas } = await abrirNuevoEmpleado({ idCreado: 2 });
    llenarFormulario();
    guardar();
    await waitFor(() => expect(llamadas.some((llamada) => llamada.metodo === 'POST')).toBe(true));
    const post = llamadas.find((llamada) => llamada.metodo === 'POST');
    expect(post?.cuerpo).toMatchObject({ email: null, telefono: null });
  });

  it('muestra el 400 del servidor junto al formulario conservando lo escrito', async () => {
    const { llamadas } = await abrirNuevoEmpleado({ errorGuardar: 'invalido' });
    llenarFormulario({ email: 'rosa@titanglobal.example' });
    guardar();
    const alerta = await screen.findByText('El email no tiene un formato válido.');
    expect(alerta).toHaveAttribute('role', 'alert');
    expect(screen.getByLabelText('Nombres')).toHaveValue('Rosa');
    expect(screen.getByLabelText('Email')).toHaveValue('rosa@titanglobal.example');
    expect(screen.getByRole('heading', { name: 'Nuevo empleado' })).toBeInTheDocument();
    expect(llamadas.filter((llamada) => llamada.metodo === 'POST')).toHaveLength(1);
  });
});

describe('validarEmpleado', () => {
  it('no devuelve errores con campos válidos y opcionales vacíos', () => {
    expect(validarEmpleado(camposValidos)).toEqual({});
  });

  it.each<[string, Partial<CamposEmpleado>, keyof CamposEmpleado, string]>([
    ['tipo de documento fuera de lista', { tipoDocumento: 'RUC' }, 'tipoDocumento', 'El tipo de documento no es válido.'],
    ['tipo de documento con otra capitalización', { tipoDocumento: 'dni' }, 'tipoDocumento', 'El tipo de documento no es válido.'],
    ['número de documento solo con espacios', { numeroDocumento: '   ' }, 'numeroDocumento', 'El número de documento es obligatorio.'],
    ['nombres solo con espacios', { nombres: '   ' }, 'nombres', 'Los nombres son obligatorios.'],
    ['apellidos solo con espacios', { apellidos: '   ' }, 'apellidos', 'Los apellidos son obligatorios.'],
    ['cargo solo con espacios', { cargo: '   ' }, 'cargo', 'El cargo es obligatorio.'],
    ['área solo con espacios', { area: '   ' }, 'area', 'El área es obligatoria.'],
    ['email con más de 150 caracteres', { email: `${'a'.repeat(145)}@x.com` }, 'email', 'El email no tiene un formato válido.'],
    ['email sin punto en el dominio', { email: 'ana@titan' }, 'email', 'El email no tiene un formato válido.'],
    ['email con espacios internos', { email: 'ana torres@titan.com' }, 'email', 'El email no tiene un formato válido.'],
  ])('rechaza %s', (_nombre, cambios, campo, mensaje) => {
    expect(validarEmpleado({ ...camposValidos, ...cambios })[campo]).toBe(mensaje);
  });

  it.each<[string, Partial<CamposEmpleado>]>([
    ['documento de exactamente 20 caracteres', { numeroDocumento: 'A'.repeat(20) }],
    ['nombres de exactamente 100 caracteres', { nombres: 'N'.repeat(100) }],
    ['teléfono de exactamente 20 caracteres', { telefono: '9'.repeat(20) }],
    ['email en mayúsculas', { email: 'ANA@TITAN.COM' }],
    ['email de exactamente 150 caracteres', { email: `${'a'.repeat(144)}@x.com` }],
  ])('acepta %s', (_nombre, cambios) => {
    expect(validarEmpleado({ ...camposValidos, ...cambios })).toEqual({});
  });

  const MENSAJE_EDAD = 'El empleado debe tener al menos 18 años a la fecha de ingreso.';

  it.each<[string, string, string, boolean]>([
    ['cumple 18 el mismo día del ingreso', '2006-06-15', '2024-06-15', true],
    ['un día antes de cumplir 18', '2006-06-15', '2024-06-14', false],
    ['nacido un 29 de febrero, ingreso el 28 de febrero del año común en que cumple 18', '2004-02-29', '2022-02-28', true],
    ['nacido un 29 de febrero, ingreso el 27 de febrero del año común en que cumple 18', '2004-02-29', '2022-02-27', false],
    ['nacido un 29 de febrero, ingreso el 1 de marzo del año común en que cumple 18', '2004-02-29', '2022-03-01', true],
    ['nacido un 29 de febrero del 2000, ingreso el 28 de febrero de 2018', '2000-02-29', '2018-02-28', true],
  ])('edad mínima: %s', (_nombre, nacimiento, ingreso, valido) => {
    const errores = validarEmpleado({ ...camposValidos, fechaNacimiento: nacimiento, fechaIngreso: ingreso });
    expect(errores.fechaIngreso).toBe(valido ? undefined : MENSAJE_EDAD);
  });

  it('no evalúa la edad si falta una de las dos fechas', () => {
    const errores = validarEmpleado({ ...camposValidos, fechaNacimiento: '', fechaIngreso: '' });
    expect(errores.fechaNacimiento).toBe('La fecha de nacimiento es obligatoria.');
    expect(errores.fechaIngreso).toBe('La fecha de ingreso es obligatoria.');
  });
});

describe('validarCese', () => {
  const base = { fechaCese: '2026-10-31', motivo: 'Renuncia', fechaIngreso: '2021-03-01' };

  it('no devuelve errores con fecha y motivo válidos', () => {
    expect(validarCese(base)).toEqual({});
  });

  it('acepta la fecha de cese igual a la de ingreso', () => {
    expect(validarCese({ ...base, fechaCese: '2021-03-01' })).toEqual({});
  });

  it.each<[string, Partial<typeof base>, string]>([
    ['fecha vacía', { fechaCese: '' }, 'La fecha de cese es obligatoria.'],
    ['fecha anterior al ingreso', { fechaCese: '2021-02-28' }, 'La fecha de cese no puede ser anterior a la fecha de ingreso.'],
  ])('rechaza %s', (_nombre, cambios, mensaje) => {
    expect(validarCese({ ...base, ...cambios }).fechaCese).toBe(mensaje);
  });

  it.each<[string, string, string | undefined]>([
    ['motivo vacío', '   ', 'El motivo de cese es obligatorio.'],
    ['motivo de 301 caracteres', 'M'.repeat(301), 'El motivo de cese no puede superar 300 caracteres.'],
    ['motivo de exactamente 300 caracteres', 'M'.repeat(300), undefined],
  ])('motivo: %s', (_nombre, motivo, mensaje) => {
    expect(validarCese({ ...base, motivo }).motivo).toBe(mensaje);
  });
});
