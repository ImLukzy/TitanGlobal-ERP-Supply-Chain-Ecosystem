using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo66_Planillas;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Commands;

public class GuardarAfiliacionPensionCommand : IRequest<AfiliacionPensionIdDto>
{
    public int EmpleadoId { get; set; }

    public required GuardarAfiliacionPensionRequestDto Solicitud { get; set; }
}

public class GuardarAfiliacionPensionCommandHandler : IRequestHandler<GuardarAfiliacionPensionCommand, AfiliacionPensionIdDto>
{
    private readonly IUnitOfWork _unidad;

    public GuardarAfiliacionPensionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    // Crea o reemplaza la afiliación del empleado. Las reglas siguen el orden de la spec: 22, 23, 24, 25 y 17.
    public async Task<AfiliacionPensionIdDto> Handle(GuardarAfiliacionPensionCommand request, CancellationToken cancellationToken)
    {
        string codigo = request.Solicitud.CodigoPension?.Trim() ?? string.Empty;
        if (codigo.Length == 0)
        {
            throw ValidacionPlanilla.Error400("El sistema de pensiones es obligatorio.");
        }

        ParametroPension? parametro = await _unidad.Repository<ParametroPension>().AsQueryable()
            .AsNoTracking()
            .SingleOrDefaultAsync(entidad => entidad.Codigo == codigo);
        if (parametro is null)
        {
            throw ValidacionPlanilla.Error404($"El sistema de pensiones {codigo} no existe.");
        }

        string? cuspp = request.Solicitud.Cuspp?.Trim();
        bool esAfp = parametro.Sistema == ValidacionPlanilla.SistemaAfp;
        if (esAfp && (cuspp is null || cuspp.Length != ValidacionPlanilla.LongitudCuspp))
        {
            throw ValidacionPlanilla.Error400("El CUSPP es obligatorio en una AFP y tiene 12 caracteres.");
        }

        if (request.Solicitud.FechaAfiliacion is null)
        {
            throw ValidacionPlanilla.Error400("La fecha de afiliación es obligatoria.");
        }

        await ValidacionPlanilla.ObtenerEmpleadoAsync(_unidad, request.EmpleadoId);
        AfiliacionPension? existente = await _unidad.Repository<AfiliacionPension>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.EmpleadoId == request.EmpleadoId);
        string? cusppGuardado = esAfp ? cuspp : null;
        if (existente is null)
        {
            existente = new AfiliacionPension
            {
                EmpleadoId = request.EmpleadoId,
                ParametroPensionId = parametro.Id,
                Cuspp = cusppGuardado,
                FechaAfiliacion = request.Solicitud.FechaAfiliacion.Value
            };
            await _unidad.Repository<AfiliacionPension>().AddAsync(existente);
        }
        else
        {
            existente.ParametroPensionId = parametro.Id;
            existente.Cuspp = cusppGuardado;
            existente.FechaAfiliacion = request.Solicitud.FechaAfiliacion.Value;
        }

        await _unidad.CompleteAsync();
        return new AfiliacionPensionIdDto { Id = existente.Id };
    }
}
