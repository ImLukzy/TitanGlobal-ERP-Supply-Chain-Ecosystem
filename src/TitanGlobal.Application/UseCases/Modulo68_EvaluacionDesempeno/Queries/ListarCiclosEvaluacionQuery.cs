using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno.Queries;

public class ListarCiclosEvaluacionQuery : IRequest<PaginaCiclosEvaluacionDto>
{
    public string? Busqueda { get; set; }

    public string? Estado { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarCiclosEvaluacionQueryHandler : IRequestHandler<ListarCiclosEvaluacionQuery, PaginaCiclosEvaluacionDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarCiclosEvaluacionQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaCiclosEvaluacionDto> Handle(ListarCiclosEvaluacionQuery request, CancellationToken cancellationToken)
    {
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        ValidacionEvaluacion.ValidarPagina(request.Pagina, request.TamanoPagina);
        ValidacionEvaluacion.ValidarEstadoCiclo(estadoNormalizado);
        IQueryable<CicloEvaluacion> consulta = _unidad.Repository<CicloEvaluacion>().AsQueryable();
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(ciclo => ciclo.Codigo.ToLower().Contains(termino)
                || ciclo.Nombre.ToLower().Contains(termino));
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(ciclo => ciclo.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        List<CicloEvaluacionListadoDto> items = await consulta
            .OrderByDescending(ciclo => ciclo.FechaInicioPeriodo)
            .ThenByDescending(ciclo => ciclo.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(ciclo => new CicloEvaluacionListadoDto
            {
                Id = ciclo.Id,
                Codigo = ciclo.Codigo,
                Nombre = ciclo.Nombre,
                Periodo = ciclo.Periodo,
                FechaInicioPeriodo = ciclo.FechaInicioPeriodo,
                FechaFinPeriodo = ciclo.FechaFinPeriodo,
                FechaApertura = ciclo.FechaApertura,
                FechaCierre = ciclo.FechaCierre,
                Estado = ciclo.Estado
            })
            .ToListAsync();
        return new PaginaCiclosEvaluacionDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
