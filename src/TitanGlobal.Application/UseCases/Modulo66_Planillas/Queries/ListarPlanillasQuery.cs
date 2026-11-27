using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Queries;

public class ListarPlanillasQuery : IRequest<PaginaPlanillasDto>
{
    public int? Anio { get; set; }

    public string? Tipo { get; set; }

    public string? Estado { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarPlanillasQueryHandler : IRequestHandler<ListarPlanillasQuery, PaginaPlanillasDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarPlanillasQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaPlanillasDto> Handle(ListarPlanillasQuery request, CancellationToken cancellationToken)
    {
        string tipoNormalizado = request.Tipo?.Trim() ?? string.Empty;
        string estadoNormalizado = request.Estado?.Trim() ?? string.Empty;
        ValidacionPlanilla.ValidarListado(request.Pagina, request.TamanoPagina);
        ValidacionPlanilla.ValidarFiltroTipo(tipoNormalizado);
        ValidacionPlanilla.ValidarFiltroEstado(estadoNormalizado);
        IQueryable<Planilla> consulta = _unidad.Repository<Planilla>().AsQueryable().AsNoTracking();
        if (request.Anio is not null)
        {
            int anioFiltro = request.Anio.Value;
            consulta = consulta.Where(planilla => planilla.Anio == anioFiltro);
        }

        if (tipoNormalizado.Length > 0)
        {
            consulta = consulta.Where(planilla => planilla.Tipo == tipoNormalizado);
        }

        if (estadoNormalizado.Length > 0)
        {
            consulta = consulta.Where(planilla => planilla.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        List<PlanillaListadoDto> items = await consulta
            .OrderByDescending(planilla => planilla.Anio)
            .ThenByDescending(planilla => planilla.Mes)
            .ThenBy(planilla => planilla.Tipo)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(planilla => new PlanillaListadoDto
            {
                Id = planilla.Id,
                Numero = planilla.Numero,
                Anio = planilla.Anio,
                Mes = planilla.Mes,
                Tipo = planilla.Tipo,
                Estado = planilla.Estado,
                Empleados = planilla.Empleados,
                Incidencias = planilla.Incidencias,
                TotalIngresos = planilla.TotalIngresos,
                TotalDescuentos = planilla.TotalDescuentos,
                TotalNeto = planilla.TotalNeto
            })
            .ToListAsync();
        return new PaginaPlanillasDto { Items = items, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }
}
