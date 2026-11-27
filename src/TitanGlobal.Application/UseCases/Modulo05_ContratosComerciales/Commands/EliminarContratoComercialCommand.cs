using MediatR;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Commands;

public class EliminarContratoComercialCommand : IRequest
{
    public int Id { get; set; }
}

public class EliminarContratoComercialCommandHandler : IRequestHandler<EliminarContratoComercialCommand>
{
    private readonly IUnitOfWork _unitOfWork;

    public EliminarContratoComercialCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(EliminarContratoComercialCommand request, CancellationToken cancellationToken)
    {
        ContratoComercial contrato = await OperacionesContratosComerciales.ObtenerEntidadAsync(_unitOfWork, request.Id);
        OperacionesContratosComerciales.ValidarEstado(contrato, "Borrador");
        _unitOfWork.Repository<ContratoComercial>().Remove(contrato);
        await _unitOfWork.CompleteAsync();
    }
}
