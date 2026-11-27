using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Queries;

public class ListarAfiliacionesPensionQuery : IRequest<PaginaAfiliacionesPensionDto>
{
    public int? EmpleadoId { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarAfiliacionesPensionQueryHandler : IRequestHandler<ListarAfiliacionesPensionQuery, PaginaAfiliacionesPensionDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarAfiliacionesPensionQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaAfiliacionesPensionDto> Handle(ListarAfiliacionesPensionQuery request, CancellationToken cancellationToken)
    {
        ValidacionPlanilla.ValidarListado(request.Pagina, request.TamanoPagina);
        IQueryable<AfiliacionPension> consulta = _unidad.Repository<AfiliacionPension>().AsQueryable().AsNoTracking();
        if (request.EmpleadoId is not null)
        {
            await ValidacionPlanilla.ObtenerEmpleadoAsync(_unidad, request.EmpleadoId.Value);
            int empleadoFiltro = request.EmpleadoId.Value;
            consulta = consulta.Where(afiliacion => afiliacion.EmpleadoId == empleadoFiltro);
        }

        int total = await consulta.CountAsync();
        List<AfiliacionPensionDto> items = await consulta
            .OrderBy(afiliacion => afiliacion.EmpleadoId)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(afiliacion => new AfiliacionPensionDto
            {
                Id = afiliacion.Id,
                EmpleadoId = afiliacion.EmpleadoId,
                EmpleadoLegajo = afiliacion.Empleado.Legajo,
                EmpleadoNombre = afiliacion.Empleado.Apellidos + ", " + afiliacion.Empleado.Nombres,
                CodigoPension = afiliacion.ParametroPension.Codigo,
                Sistema = afiliacion.ParametroPension.Sistema,
                Cuspp = afiliacion.Cuspp,
                FechaAfiliacion = afiliacion.FechaAfiliacion
            })
            .ToListAsync();
        return new PaginaAfiliacionesPensionDto { Items = items, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }
}
