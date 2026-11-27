using MediatR;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Commands;

public class ActualizarVendedorCommand : IRequest<IdDto>
{
    public int Id { get; set; }

    public required GuardarVendedorRequestDto Solicitud { get; set; }
}

public class ActualizarVendedorCommandHandler : IRequestHandler<ActualizarVendedorCommand, IdDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarVendedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IdDto> Handle(ActualizarVendedorCommand request, CancellationToken cancellationToken)
    {
        GuardarVendedorRequestDto datos = ValidacionComisiones.ValidarVendedor(request.Solicitud);
        Vendedor vendedor = await OperacionesComisionesVendedores.ObtenerVendedorEntidadAsync(_unitOfWork, request.Id);
        await OperacionesComisionesVendedores.ValidarCodigoUnicoAsync(_unitOfWork, datos.Codigo, request.Id);
        OperacionesComisionesVendedores.AplicarDatos(vendedor, datos);
        _unitOfWork.Repository<Vendedor>().Update(vendedor);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (OperacionesComisionesVendedores.EsViolacionUnica(excepcion))
        {
            throw new ReglaNegocioException(409, $"Ya existe un vendedor con el código {datos.Codigo}.");
        }

        return new IdDto { Id = vendedor.Id };
    }
}
