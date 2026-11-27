using MediatR;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores.Commands;

public class CrearVendedorCommand : IRequest<IdDto>
{
    public required GuardarVendedorRequestDto Solicitud { get; set; }
}

public class CrearVendedorCommandHandler : IRequestHandler<CrearVendedorCommand, IdDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearVendedorCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IdDto> Handle(CrearVendedorCommand request, CancellationToken cancellationToken)
    {
        GuardarVendedorRequestDto datos = ValidacionComisiones.ValidarVendedor(request.Solicitud);
        await OperacionesComisionesVendedores.ValidarCodigoUnicoAsync(_unitOfWork, datos.Codigo, null);
        Vendedor vendedor = CrearEntidad(datos);
        await _unitOfWork.Repository<Vendedor>().AddAsync(vendedor);
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

    private static Vendedor CrearEntidad(GuardarVendedorRequestDto datos)
    {
        Vendedor vendedor = new Vendedor();
        OperacionesComisionesVendedores.AplicarDatos(vendedor, datos);
        return vendedor;
    }
}
