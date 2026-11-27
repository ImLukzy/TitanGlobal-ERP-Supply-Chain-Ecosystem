using MediatR;
using System.Transactions;
using TitanGlobal.Application.DTOs.Modulo05_ContratosComerciales;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo05_ContratosComerciales.Commands;

public class CrearContratoComercialCommand : IRequest<ContratoCreadoDto>
{
    public required GuardarContratoRequestDto Solicitud { get; set; }
}

public class CrearContratoComercialCommandHandler : IRequestHandler<CrearContratoComercialCommand, ContratoCreadoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearContratoComercialCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ContratoCreadoDto> Handle(CrearContratoComercialCommand request, CancellationToken cancellationToken)
    {
        GuardarContratoRequestDto datos = await ValidacionContratoComercial.ValidarAsync(request.Solicitud, _unitOfWork);
        ContratoComercial contrato = CrearEntidad(datos);
        using (TransactionScope transaccion = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            contrato.Numero = $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
            await _unitOfWork.Repository<ContratoComercial>().AddAsync(contrato);
            await _unitOfWork.CompleteAsync();
            contrato.Numero = CrearNumero(contrato.Id);
            _unitOfWork.Repository<ContratoComercial>().Update(contrato);
            await _unitOfWork.CompleteAsync();
            transaccion.Complete();
        }

        return new ContratoCreadoDto { Id = contrato.Id, Numero = contrato.Numero };
    }

    private static ContratoComercial CrearEntidad(GuardarContratoRequestDto datos)
    {
        ContratoComercial contrato = new ContratoComercial();
        OperacionesContratosComerciales.AplicarDatos(contrato, datos);
        contrato.Estado = "Borrador";
        return contrato;
    }

    private static string CrearNumero(int id)
    {
        return $"CC-{id:D6}";
    }
}
