using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos.Queries;

public class ListarPrestamosQuery : IRequest<PaginaPrestamosDto>
{
    public string? Busqueda { get; set; }

    public int? EmpleadoId { get; set; }

    public string? Tipo { get; set; }

    public string? Estado { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarPrestamosQueryHandler : IRequestHandler<ListarPrestamosQuery, PaginaPrestamosDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarPrestamosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaPrestamosDto> Handle(ListarPrestamosQuery request, CancellationToken cancellationToken)
    {
        string tipoNormalizado = request.Tipo?.Trim() ?? string.Empty;
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        ValidacionPrestamo.ValidarListado(request.Pagina, request.TamanoPagina, tipoNormalizado, estadoNormalizado);
        if (request.EmpleadoId is not null)
        {
            await ValidacionPrestamo.ObtenerEmpleadoAsync(_unidad, request.EmpleadoId.Value);
        }

        IQueryable<Prestamo> consulta = _unidad.Repository<Prestamo>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(prestamo => prestamo.Numero.ToLower().Contains(termino)
                || prestamo.Empleado.Nombres.ToLower().Contains(termino)
                || prestamo.Empleado.Apellidos.ToLower().Contains(termino)
                || prestamo.Empleado.Legajo.ToLower().Contains(termino));
        }

        if (request.EmpleadoId is not null)
        {
            int idEmpleado = request.EmpleadoId.Value;
            consulta = consulta.Where(prestamo => prestamo.EmpleadoId == idEmpleado);
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(prestamo => prestamo.Tipo == tipoNormalizado);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(prestamo => prestamo.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        List<PrestamoListadoDto> items = await consulta
            .OrderByDescending(prestamo => prestamo.FechaOtorgamiento)
            .ThenByDescending(prestamo => prestamo.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(prestamo => new PrestamoListadoDto
            {
                Id = prestamo.Id,
                Numero = prestamo.Numero,
                EmpleadoId = prestamo.EmpleadoId,
                EmpleadoLegajo = prestamo.Empleado.Legajo,
                EmpleadoNombre = prestamo.Empleado.Apellidos + ", " + prestamo.Empleado.Nombres,
                Tipo = prestamo.Tipo,
                Monto = prestamo.Monto,
                NumeroCuotas = prestamo.NumeroCuotas,
                FechaOtorgamiento = prestamo.FechaOtorgamiento,
                Saldo = prestamo.Saldo,
                Estado = prestamo.Estado
            })
            .ToListAsync();
        return new PaginaPrestamosDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
