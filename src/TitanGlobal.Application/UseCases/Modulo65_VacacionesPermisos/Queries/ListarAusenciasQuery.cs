using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo65_VacacionesPermisos;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos.Queries;

public class ListarAusenciasQuery : IRequest<PaginaAusenciasDto>
{
    public string? Busqueda { get; set; }

    public int? EmpleadoId { get; set; }

    public string? Tipo { get; set; }

    public string? Estado { get; set; }

    public DateOnly? Desde { get; set; }

    public DateOnly? Hasta { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarAusenciasQueryHandler : IRequestHandler<ListarAusenciasQuery, PaginaAusenciasDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarAusenciasQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaAusenciasDto> Handle(ListarAusenciasQuery request, CancellationToken cancellationToken)
    {
        string tipoNormalizado = request.Tipo?.Trim() ?? string.Empty;
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        ValidacionAusencia.ValidarListado(request.Pagina, request.TamanoPagina, request.Desde, request.Hasta, tipoNormalizado, estadoNormalizado);
        if (request.EmpleadoId is not null)
        {
            await ValidacionAusencia.ObtenerEmpleadoAsync(_unidad, request.EmpleadoId.Value);
        }

        IQueryable<Ausencia> consulta = _unidad.Repository<Ausencia>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(ausencia => ausencia.Numero.ToLower().Contains(termino)
                || ausencia.Empleado.Nombres.ToLower().Contains(termino)
                || ausencia.Empleado.Apellidos.ToLower().Contains(termino)
                || ausencia.Empleado.Legajo.ToLower().Contains(termino));
        }

        if (request.EmpleadoId is not null)
        {
            int idEmpleado = request.EmpleadoId.Value;
            consulta = consulta.Where(ausencia => ausencia.EmpleadoId == idEmpleado);
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(ausencia => ausencia.Tipo == tipoNormalizado);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(ausencia => ausencia.Estado == estadoNormalizado);
        }

        if (request.Desde is not null)
        {
            DateOnly inicio = request.Desde.Value;
            consulta = consulta.Where(ausencia => ausencia.FechaFin >= inicio);
        }

        if (request.Hasta is not null)
        {
            DateOnly fin = request.Hasta.Value;
            consulta = consulta.Where(ausencia => ausencia.FechaInicio <= fin);
        }

        int total = await consulta.CountAsync();
        List<AusenciaListadoDto> items = await consulta
            .OrderByDescending(ausencia => ausencia.FechaInicio)
            .ThenByDescending(ausencia => ausencia.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(ausencia => new AusenciaListadoDto
            {
                Id = ausencia.Id,
                Numero = ausencia.Numero,
                EmpleadoId = ausencia.EmpleadoId,
                EmpleadoLegajo = ausencia.Empleado.Legajo,
                EmpleadoNombre = ausencia.Empleado.Apellidos + ", " + ausencia.Empleado.Nombres,
                Tipo = ausencia.Tipo,
                FechaInicio = ausencia.FechaInicio,
                FechaFin = ausencia.FechaFin,
                Dias = ausencia.Dias,
                Estado = ausencia.Estado,
                FechaSolicitud = ausencia.FechaSolicitud
            })
            .ToListAsync();
        return new PaginaAusenciasDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
