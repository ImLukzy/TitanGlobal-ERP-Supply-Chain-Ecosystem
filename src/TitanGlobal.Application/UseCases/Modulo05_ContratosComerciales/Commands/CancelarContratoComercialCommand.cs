using MediatR;
using TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Queries;
using TitanGlobal.Application.DTOs.Modulo05_ContratosComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Common;

namespace TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Commands;

public class CancelarContratoComercialCommand : IRequest<ContratoDetalleDto>
{
    public int Id { get; set; }

    public required CancelarContratoRequestDto Solicitud { get; set; }
}

public class CancelarContratoComercialCommandHandler : IRequestHandler<CancelarContratoComercialCommand, ContratoDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly ISender _sender;

    public CancelarContratoComercialCommandHandler(IUnitOfWork unitOfWork, ISender sender)
    {
        _unitOfWork = unitOfWork;
        _sender = sender;
    }

    public async Task<ContratoDetalleDto> Handle(CancelarContratoComercialCommand request, CancellationToken cancellationToken)
    {
        ContratoComercial contrato = await OperacionesContratosComerciales.ObtenerEntidadAsync(_unitOfWork, request.Id);
        if (contrato.Estado != "Borrador" && contrato.Estado != "Vigente")
        {
            throw OperacionesContratosComerciales.ConflictoEstado(contrato);
        }

        string motivo = ValidacionContratoComercial.ValidarMotivo(request.Solicitud.Motivo);
        contrato.Estado = "Cancelado";
        contrato.MotivoCancelacion = motivo;
        contrato.FechaCierre = FechasUtc.Normalizar(DateTime.UtcNow);
        _unitOfWork.Repository<ContratoComercial>().Update(contrato);
        await _unitOfWork.CompleteAsync();
        return await _sender.Send(new ObtenerContratoComercialQuery { Id = request.Id }, cancellationToken);
    }
}
