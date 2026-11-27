using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Queries;

public class ListarBonosPlanillaQuery : IRequest<PaginaBonosPlanillaDto>
{
    public int? Anio { get; set; }

    public int? Mes { get; set; }

    public int? EmpleadoId { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarBonosPlanillaQueryHandler : IRequestHandler<ListarBonosPlanillaQuery, PaginaBonosPlanillaDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarBonosPlanillaQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaBonosPlanillaDto> Handle(ListarBonosPlanillaQuery request, CancellationToken cancellationToken)
    {
        ValidacionPlanilla.ValidarListado(request.Pagina, request.TamanoPagina);
        IQueryable<BonoPlanilla> consulta = _unidad.Repository<BonoPlanilla>().AsQueryable().AsNoTracking();
        if (request.Anio is not null)
        {
            int anioFiltro = request.Anio.Value;
            consulta = consulta.Where(bono => bono.Anio == anioFiltro);
        }

        if (request.Mes is not null)
        {
            int mesFiltro = request.Mes.Value;
            consulta = consulta.Where(bono => bono.Mes == mesFiltro);
        }

        if (request.EmpleadoId is not null)
        {
            int empleadoFiltro = request.EmpleadoId.Value;
            consulta = consulta.Where(bono => bono.EmpleadoId == empleadoFiltro);
        }

        int total = await consulta.CountAsync();
        List<BonoPlanillaDto> items = await consulta
            .OrderByDescending(bono => bono.Anio)
            .ThenByDescending(bono => bono.Mes)
            .ThenBy(bono => bono.EmpleadoId)
            .ThenBy(bono => bono.Id)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(bono => new BonoPlanillaDto
            {
                Id = bono.Id,
                EmpleadoId = bono.EmpleadoId,
                EmpleadoLegajo = bono.Empleado.Legajo,
                EmpleadoNombre = bono.Empleado.Apellidos + ", " + bono.Empleado.Nombres,
                Anio = bono.Anio,
                Mes = bono.Mes,
                TipoPlanilla = bono.TipoPlanilla,
                Concepto = bono.Concepto,
                Monto = bono.Monto,
                PlanillaId = bono.PlanillaId,
                PlanillaNumero = bono.Planilla == null ? null : bono.Planilla.Numero
            })
            .ToListAsync();
        return new PaginaBonosPlanillaDto { Items = items, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }
}
