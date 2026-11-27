using MediatR;
using TitanGlobal.Application.DTOs.Modulo05_ContratosComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Commands;

public class ActualizarContratoComercialCommand : IRequest<ContratoCreadoDto>
{
    public int Id { get; set; }

    public required GuardarContratoRequestDto Solicitud { get; set; }
}

public class ActualizarContratoComercialCommandHandler : IRequestHandler<ActualizarContratoComercialCommand, ContratoCreadoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarContratoComercialCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ContratoCreadoDto> Handle(ActualizarContratoComercialCommand request, CancellationToken cancellationToken)
    {
        GuardarContratoRequestDto datos = await ValidacionContratoComercial.ValidarAsync(request.Solicitud, _unitOfWork);
        ContratoComercial contrato = await OperacionesContratosComerciales.ObtenerEntidadAsync(_unitOfWork, request.Id);
        OperacionesContratosComerciales.ValidarEstado(contrato, "Borrador");
        OperacionesContratosComerciales.AplicarDatos(contrato, datos);
        _unitOfWork.Repository<ContratoComercial>().Update(contrato);
        await _unitOfWork.CompleteAsync();
        return new ContratoCreadoDto { Id = contrato.Id, Numero = contrato.Numero };
    }
}
