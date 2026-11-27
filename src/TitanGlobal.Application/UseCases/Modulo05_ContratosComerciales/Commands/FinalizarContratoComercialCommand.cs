using MediatR;
using TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Queries;
using TitanGlobal.Application.DTOs.Modulo05_ContratosComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Commands;

public class FinalizarContratoComercialCommand : IRequest<ContratoDetalleDto>
{
    public int Id { get; set; }
}

public class FinalizarContratoComercialCommandHandler : IRequestHandler<FinalizarContratoComercialCommand, ContratoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ISender _sender;

    public FinalizarContratoComercialCommandHandler(IUnitOfWork unitOfWork, ISender sender)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<ContratoDetalleDto> Handle(FinalizarContratoComercialCommand request, CancellationToken cancellationToken)
    {
        ContratoComercial contrato = await OperacionesContratosComerciales.ObtenerEntidadAsync(_unitOfWork, request.Id);
        OperacionesContratosComerciales.ValidarEstado(contrato, "Vigente");
        contrato.Estado = "Finalizado";
        contrato.FechaCierre = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ContratoComercial>().Update(contrato);
        await _unitOfWork.CompleteAsync();
        return await _sender.Send(new ObtenerContratoComercialQuery { Id = request.Id }, cancellationToken);
    }
}
