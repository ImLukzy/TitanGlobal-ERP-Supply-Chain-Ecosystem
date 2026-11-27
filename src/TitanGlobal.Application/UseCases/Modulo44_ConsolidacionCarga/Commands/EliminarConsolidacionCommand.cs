using MediatR;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo44_ConsolidacionCarga.Commands;

public class EliminarConsolidacionCommand : IRequest<int>
{
    public int Id { get; set; }
}

public class EliminarConsolidacionCommandHandler : IRequestHandler<EliminarConsolidacionCommand, int>
{
    private readonly IUnitOfWork _unidad;

    public EliminarConsolidacionCommandHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<int> Handle(EliminarConsolidacionCommand request, CancellationToken cancellationToken)
    {
        ConsolidacionCarga carga = await ValidacionCarga.ObtenerAsync(_unidad, request.Id);
        ValidacionCarga.ExigirBorrador(carga, "eliminar");
        _unidad.Repository<ConsolidacionCarga>().Remove(carga);
        try { await _unidad.CompleteAsync(); }
        catch (ViolacionRestriccionException error) when (error.Tipo == TipoViolacionRestriccion.ReferenciaForanea) { throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede eliminar la consolidación porque está en uso."); }
        return request.Id;
    }
}
