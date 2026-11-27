using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Queries;

public class ResumenPlanillasQuery : IRequest<ResumenPlanillasDto>
{
}

public class ResumenPlanillasQueryHandler : IRequestHandler<ResumenPlanillasQuery, ResumenPlanillasDto>
{
    private readonly IUnitOfWork _unidad;

    public ResumenPlanillasQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<ResumenPlanillasDto> Handle(ResumenPlanillasQuery request, CancellationToken cancellationToken)
    {
        Planilla? ultima = await _unidad.Repository<Planilla>().AsQueryable()
            .AsNoTracking()
            .Where(planilla => planilla.Tipo == ValidacionPlanilla.TipoMensual)
            .OrderByDescending(planilla => planilla.Anio)
            .ThenByDescending(planilla => planilla.Mes)
            .FirstOrDefaultAsync();
        int pendientes = await _unidad.Repository<Planilla>().AsQueryable()
            .CountAsync(planilla => planilla.Estado == ValidacionPlanilla.EstadoBorrador || planilla.Estado == ValidacionPlanilla.EstadoCalculada);
        ResumenPlanillasDto resumen = new ResumenPlanillasDto { BorradoresYCalculadas = pendientes };
        if (ultima is not null)
        {
            resumen.UltimoPeriodo = $"{ultima.Anio}-{ultima.Mes:D2}";
            resumen.EstadoUltimo = ultima.Estado;
            resumen.NetoUltimo = ultima.TotalNeto;
            resumen.EmpleadosUltimo = ultima.Empleados;
            resumen.IncidenciasUltimo = ultima.Incidencias;
        }

        return resumen;
    }
}
