using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo01_Clientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo01_Clientes.Commands;

public class ActualizarClienteCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarClienteRequestDto Solicitud { get; set; }
}

public class ActualizarClienteCommandHandler : IRequestHandler<ActualizarClienteCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarClienteCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ActualizarClienteCommand request, CancellationToken cancellationToken)
    {
        Cliente? cliente = await _unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (cliente is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El cliente {request.Id} no existe.");
        }

        string nombre = ValidacionCliente.NormalizarNombre(request.Solicitud.Nombre);
        string ruc = ValidacionCliente.NormalizarRuc(request.Solicitud.Ruc);
        bool rucExiste = await _unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .AnyAsync(entidad => entidad.Id != request.Id && entidad.Ruc == ruc);
        if (rucExiste)
        {
            throw CrearConflictoRucException(ruc);
        }

        cliente.Nombre = nombre;
        cliente.Ruc = ruc;
        cliente.Descripcion = ValidacionCliente.NormalizarDescripcion(request.Solicitud.Descripcion);
        _unitOfWork.Repository<Cliente>().Update(cliente);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CrearConflictoRucException(ruc);
        }

        return cliente.Id;
    }

    private static ReglaNegocioException CrearConflictoRucException(string ruc)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Ya existe un cliente con el RUC {ruc}.");
    }
}
