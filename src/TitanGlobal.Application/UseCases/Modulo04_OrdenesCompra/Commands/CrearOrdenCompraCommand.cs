using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra.Commands;

public class CrearOrdenCompraCommand : IRequest<int>
{
    public required CrearOrdenCompraRequestDto Request { get; set; }
}

public class CrearOrdenCompraCommandHandler : IRequestHandler<CrearOrdenCompraCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearOrdenCompraCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearOrdenCompraCommand request, CancellationToken cancellationToken)
    {
        string numeroOrden = request.Request.NumeroOrden?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(numeroOrden))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El número de orden es obligatorio.");
        }

        if (request.Request.ClienteId <= 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El cliente es obligatorio.");
        }

        if (request.Request.Total < 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El total no puede ser negativo.");
        }

        bool clientExists = await _unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .AnyAsync(client => client.Id == request.Request.ClienteId);
        if (!clientExists)
        {
            throw CreateMissingClientException(request.Request.ClienteId);
        }

        bool orderExists = await _unitOfWork.Repository<OrdenCompra>()
            .AsQueryable()
            .AnyAsync(order => order.NumeroOrden == numeroOrden);
        if (orderExists)
        {
            throw CreateDuplicateOrderException(numeroOrden);
        }

        OrdenCompra order = new OrdenCompra
        {
            NumeroOrden = numeroOrden,
            Fecha = DateTime.UtcNow,
            Total = request.Request.Total,
            Descripcion = request.Request.Descripcion,
            ClienteId = request.Request.ClienteId
        };

        await _unitOfWork.Repository<OrdenCompra>().AddAsync(order);
        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException exception) when (exception.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw CreateDuplicateOrderException(numeroOrden);
        }
        catch (ViolacionRestriccionException exception) when (exception.Tipo == TipoViolacionRestriccion.ReferenciaForanea)
        {
            throw CreateMissingClientException(request.Request.ClienteId);
        }

        return order.Id;
    }

    private static ReglaNegocioException CreateMissingClientException(int clientId)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status400BadRequest,
            $"El cliente {clientId} no existe.");
    }

    private static ReglaNegocioException CreateDuplicateOrderException(string orderNumber)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe una orden de compra con el número {orderNumber}.");
    }
}
