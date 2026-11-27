using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Queries;

public class ListarDetallesQuery : IRequest<PaginaPlanillaEmpleadosDto>
{
    public int Id { get; set; }

    public string? Busqueda { get; set; }

    public bool SoloIncidencias { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarDetallesQueryHandler : IRequestHandler<ListarDetallesQuery, PaginaPlanillaEmpleadosDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarDetallesQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaPlanillaEmpleadosDto> Handle(ListarDetallesQuery request, CancellationToken cancellationToken)
    {
        ValidacionPlanilla.ValidarListado(request.Pagina, request.TamanoPagina);
        bool existe = await _unidad.Repository<Planilla>().AsQueryable().AnyAsync(planilla => planilla.Id == request.Id);
        if (!existe)
        {
            throw ValidacionPlanilla.PlanillaInexistente(request.Id);
        }

        IQueryable<PlanillaDetalle> consulta = _unidad.Repository<PlanillaDetalle>().AsQueryable()
            .AsNoTracking()
            .Where(detalle => detalle.PlanillaId == request.Id);
        string termino = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (termino.Length > 0)
        {
            consulta = consulta.Where(detalle => detalle.Empleado.Nombres.ToLower().Contains(termino)
                || detalle.Empleado.Apellidos.ToLower().Contains(termino)
                || detalle.Empleado.Legajo.ToLower().Contains(termino));
        }

        if (request.SoloIncidencias)
        {
            consulta = consulta.Where(detalle => detalle.Incidencia != null);
        }

        int total = await consulta.CountAsync();
        List<PlanillaEmpleadoDto> items = await consulta
            .OrderBy(detalle => detalle.EmpleadoId)
            .Skip((request.Pagina - 1) * request.TamanoPagina)
            .Take(request.TamanoPagina)
            .Select(detalle => new PlanillaEmpleadoDto
            {
                Id = detalle.Id,
                EmpleadoId = detalle.EmpleadoId,
                EmpleadoLegajo = detalle.Empleado.Legajo,
                EmpleadoNombre = detalle.Empleado.Apellidos + ", " + detalle.Empleado.Nombres,
                Cargo = detalle.Cargo,
                SueldoBasico = detalle.SueldoBasico,
                DiasContrato = detalle.DiasContrato,
                DiasSinGoce = detalle.DiasSinGoce,
                DiasFalta = detalle.DiasFalta,
                DiasLaborados = detalle.DiasLaborados,
                IngresoBasico = detalle.IngresoBasico,
                MinutosExtra = detalle.MinutosExtra,
                IngresoHorasExtra = detalle.IngresoHorasExtra,
                IngresoBonos = detalle.IngresoBonos,
                TotalIngresos = detalle.TotalIngresos,
                CodigoPension = detalle.CodigoPension,
                DescuentoPension = detalle.DescuentoPension,
                DescuentoQuinta = detalle.DescuentoQuinta,
                DescuentoPrestamos = detalle.DescuentoPrestamos,
                TotalDescuentos = detalle.TotalDescuentos,
                Neto = detalle.Neto,
                AporteEssalud = detalle.AporteEssalud,
                Incidencia = detalle.Incidencia,
                NumeroBoleta = detalle.NumeroBoleta
            })
            .ToListAsync();
        return new PaginaPlanillaEmpleadosDto { Items = items, Total = total, Pagina = request.Pagina, TamanoPagina = request.TamanoPagina };
    }
}
