using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas.Commands;

public class EliminarBonoPlanillaCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarBonoPlanillaCommandHandler : IRequestHandler<EliminarBonoPlanillaCommand>
{
    private readonly IUnitOfWork _unidad;

    public EliminarBonoPlanillaCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task Handle(EliminarBonoPlanillaCommand request, CancellationToken cancellationToken)
    {
        BonoPlanilla? bono = await _unidad.Repository<BonoPlanilla>().AsQueryable()
            .Include(entidad => entidad.Planilla)
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (bono is null)
        {
            throw ValidacionPlanilla.Error404($"El bono {request.Id} no existe.");
        }

        if (bono.Planilla is not null && ValidacionPlanilla.EstaCerrada(bono.Planilla.Estado))
        {
            throw ValidacionPlanilla.Error409("El bono está en una planilla aprobada y no se puede eliminar.");
        }

        _unidad.Repository<BonoPlanilla>().Remove(bono);
        await _unidad.CompleteAsync();
    }
}
