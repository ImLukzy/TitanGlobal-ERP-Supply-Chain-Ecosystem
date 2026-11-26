import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';
import { ApiError } from '../../api/client';
import * as api from './api';
import { CentrosCostoPage } from './CentrosCostoPage';
import type { CentroDetalle, CentroFila } from './tipos';

const sesion = vi.hoisted(() => ({ roles: ['ADMIN'] }));
vi.mock('../../layout/SesionContext', () => ({ useSesionOpcional: () => ({ sesion }) }));
vi.mock('./api');

const padre: CentroFila = { id: 1, codigo: 'C01', nombre: 'Administración', centroCostoPadreId: null, responsableEmpleadoId: null, activo: true };
const centro: CentroDetalle = { id: 2, codigo: 'C02', nombre: 'Operaciones', centroCostoPadreId: 1,
  responsableEmpleadoId: 7, activo: true, descripcion: 'Trabajo interno', nivel: 2,
  fechaRegistro: '2026-11-26T10:00:00Z', fechaDesactivacion: null };
const hijo: CentroFila = { id: 3, codigo: 'C03', nombre: 'Depósito', centroCostoPadreId: 2, responsableEmpleadoId: null, activo: true };
const pagina = { items: [centro], pagina: 1, tamanoPagina: 20, totalRegistros: 21, totalPaginas: 2 };

afterEach(cleanup);
beforeEach(() => {
  vi.resetAllMocks();
  sesion.roles = ['ADMIN'];
  vi.mocked(api.catalogoCentros).mockResolvedValue([padre, centro, hijo]);
  vi.mocked(api.listarCentros).mockResolvedValue(pagina);
  vi.mocked(api.obtenerCentro).mockResolvedValue(centro);
  vi.mocked(api.responsables).mockResolvedValue([{ id: 7, legajo: 'EMP-7', tipoDocumento: 'DNI', numeroDocumento: '00000000',
    nombres: 'Ana', apellidos: 'Pérez', cargo: 'Encargada', area: 'Operaciones', fechaIngreso: '2026-01-01', estado: 'Activo' }]);
});

function abrir() {
  render(<MemoryRouter><CentrosCostoPage /></MemoryRouter>);
}

async function consultar() {
  fireEvent.click(await screen.findByRole('button', { name: 'C02' }));
  await screen.findByText('Trabajo interno');
}

it('muestra carga, padre y nivel y filtra con paginación del servidor', async () => {
  abrir();
  expect(screen.getByText('Cargando centros…')).toBeInTheDocument();
  const filaVisible = await screen.findByRole('button', { name: 'C02' });
  expect(filaVisible.closest('tbody > tr')).not.toBeNull();
  fireEvent.change(screen.getByLabelText('Actividad'), { target: { value: 'true' } });
  fireEvent.change(screen.getByLabelText('Filtrar por padre'), { target: { value: '1' } });
  expect(api.listarCentros).toHaveBeenLastCalledWith('true', '1', 1);
  fireEvent.click(await screen.findByRole('button', { name: 'Siguiente' }));
  expect(api.listarCentros).toHaveBeenLastCalledWith('true', '1', 2);
});

it('muestra el estado vacío y esconde las escrituras de CONSULTA', async () => {
  sesion.roles = ['CONSULTA'];
  vi.mocked(api.listarCentros).mockResolvedValue({ ...pagina, items: [] });
  abrir();
  expect(await screen.findByText('No hay centros para estos filtros.')).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Nuevo centro' })).not.toBeInTheDocument();
});

it('edita código y responsable, impide elegirse a sí mismo y conserva el padre válido', async () => {
  vi.mocked(api.guardarCentro).mockResolvedValue(centro);
  abrir();
  await consultar();
  expect(screen.getByText('Administración / Operaciones')).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Editar centro' }));
  const form = await screen.findByRole('form', { name: 'Datos del centro' });
  const opciones = within(form);
  expect(opciones.getByLabelText('Centro padre')).toHaveValue('1');
  expect(opciones.queryByRole('option', { name: /C02|C03/ })).not.toBeInTheDocument();
  expect(opciones.getByLabelText('Responsable')).toHaveValue('7');
  fireEvent.submit(form);
  await screen.findByText('Centro de costo guardado.');
  expect(api.guardarCentro).toHaveBeenCalledWith({ codigo: 'C02', nombre: 'Operaciones', descripcion: 'Trabajo interno',
    centroCostoPadreId: 1, responsableEmpleadoId: 7 }, 2);
});

it('valida el alta con campos obligatorios y muestra el resultado del servidor', async () => {
  vi.mocked(api.guardarCentro).mockResolvedValue(centro);
  abrir();
  fireEvent.click(screen.getByRole('button', { name: 'Nuevo centro' }));
  const form = await screen.findByRole('form', { name: 'Datos del centro' });
  fireEvent.submit(form);
  expect(screen.getByRole('alert')).toHaveTextContent('El código debe tener entre 2 y 20 caracteres');
  fireEvent.change(within(form).getByLabelText('Código'), { target: { value: 'C02' } });
  fireEvent.change(within(form).getByLabelText('Nombre'), { target: { value: 'Operaciones' } });
  fireEvent.submit(form);
  await screen.findByText('Centro de costo guardado.');
  expect(api.guardarCentro).toHaveBeenCalledWith(expect.objectContaining({ codigo: 'C02', centroCostoPadreId: null }), undefined);
});

it('conserva el 409 de hijos activos y refresca vigencia después de confirmar', async () => {
  vi.mocked(api.cambiarVigencia)
    .mockRejectedValueOnce(new ApiError(409, 'El centro de costo tiene hijos activos.'))
    .mockResolvedValueOnce({ ...centro, activo: false });
  abrir();
  await consultar();
  fireEvent.click(screen.getByRole('button', { name: 'Desactivar centro' }));
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar' }));
  expect(await screen.findByRole('alert')).toHaveTextContent('El centro de costo tiene hijos activos.');
  vi.mocked(api.obtenerCentro).mockResolvedValue({ ...centro, activo: false });
  fireEvent.click(screen.getByRole('button', { name: 'Confirmar' }));
  await screen.findByText('Centro desactivado.');
  expect(await screen.findByRole('button', { name: 'Reactivar centro' })).toBeInTheDocument();
});

it.each([400, 401, 403, 404, 409])('mantiene el literal de la API HTTP %s', async codigo => {
  vi.mocked(api.listarCentros).mockRejectedValue(new ApiError(codigo, 'Literal ' + codigo));
  abrir();
  expect(await screen.findByRole('alert')).toHaveTextContent('Literal ' + codigo);
});
