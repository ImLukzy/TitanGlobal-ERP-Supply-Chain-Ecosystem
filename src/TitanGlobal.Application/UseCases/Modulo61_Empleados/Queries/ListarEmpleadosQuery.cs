using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo61_Empleados;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo61_Empleados.Queries;

public class ListarEmpleadosQuery : IRequest<PaginaEmpleadosDto>
{
    public string? Busqueda { get; set; }

    public string? Estado { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarEmpleadosQueryHandler : IRequestHandler<ListarEmpleadosQuery, PaginaEmpleadosDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarEmpleadosQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaEmpleadosDto> Handle(ListarEmpleadosQuery request, CancellationToken cancellationToken)
    {
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        ValidacionEmpleado.ValidarListado(request.Pagina, request.TamanoPagina, estadoNormalizado);
        IQueryable<Empleado> consulta = _unidad.Repository<Empleado>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(empleado => empleado.Nombres.ToLower().Contains(termino)
                || empleado.Apellidos.ToLower().Contains(termino)
                || empleado.NumeroDocumento.ToLower().Contains(termino)
                || empleado.Legajo.ToLower().Contains(termino));
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(empleado => empleado.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        List<EmpleadoListadoDto> items = await consulta.OrderBy(empleado => empleado.Apellidos)
            .ThenBy(empleado => empleado.Nombres)
            .ThenBy(empleado => empleado.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(empleado => new EmpleadoListadoDto
            {
                Id = empleado.Id,
                Legajo = empleado.Legajo,
                TipoDocumento = empleado.TipoDocumento,
                NumeroDocumento = empleado.NumeroDocumento,
                Nombres = empleado.Nombres,
                Apellidos = empleado.Apellidos,
                Cargo = empleado.Cargo,
                Area = empleado.Area,
                FechaIngreso = empleado.FechaIngreso,
                Estado = empleado.Estado
            })
            .ToListAsync();
        return new PaginaEmpleadosDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
