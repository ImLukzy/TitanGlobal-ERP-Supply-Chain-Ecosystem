using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Queries;

public class ObtenerBoletaQuery : IRequest<BoletaPagoDto>
{
    public int DetalleId { get; set; }
}

public class ObtenerBoletaQueryHandler : IRequestHandler<ObtenerBoletaQuery, BoletaPagoDto>
{
    private readonly IUnitOfWork _unidad;

    public ObtenerBoletaQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<BoletaPagoDto> Handle(ObtenerBoletaQuery request, CancellationToken cancellationToken)
    {
        PlanillaDetalle? detalle = await _unidad.Repository<PlanillaDetalle>().AsQueryable()
            .AsNoTracking()
            .Include(entidad => entidad.Planilla)
            .Include(entidad => entidad.Empleado)
            .SingleOrDefaultAsync(entidad => entidad.Id == request.DetalleId);
        if (detalle is null)
        {
            throw ValidacionPlanilla.Error404($"El detalle de planilla {request.DetalleId} no existe.");
        }

        if (detalle.NumeroBoleta is null)
        {
            throw ValidacionPlanilla.Error409("La boleta se genera al aprobar la planilla.");
        }

        return new BoletaPagoDto
        {
            Empresa = ValidacionPlanilla.EmpresaBoleta,
            NumeroBoleta = detalle.NumeroBoleta,
            NumeroPlanilla = detalle.Planilla.Numero,
            Tipo = detalle.Planilla.Tipo,
            Anio = detalle.Planilla.Anio,
            Mes = detalle.Planilla.Mes,
            EmpleadoId = detalle.EmpleadoId,
            EmpleadoLegajo = detalle.Empleado.Legajo,
            EmpleadoNombre = detalle.Empleado.Apellidos + ", " + detalle.Empleado.Nombres,
            Cargo = detalle.Cargo,
            CodigoPension = detalle.CodigoPension,
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
            DescuentoPension = detalle.DescuentoPension,
            DescuentoQuinta = detalle.DescuentoQuinta,
            DescuentoPrestamos = detalle.DescuentoPrestamos,
            TotalDescuentos = detalle.TotalDescuentos,
            AporteEssalud = detalle.AporteEssalud,
            Neto = detalle.Neto,
            FechaPago = detalle.Planilla.FechaPago
        };
    }
}
