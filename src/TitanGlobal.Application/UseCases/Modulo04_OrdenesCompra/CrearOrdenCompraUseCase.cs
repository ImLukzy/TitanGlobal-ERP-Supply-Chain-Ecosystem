using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo04_OrdenesCompra;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo04_OrdenesCompra;

public class CrearOrdenCompraUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearOrdenCompraUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(CrearOrdenCompraRequestDto request)
    {
        string numeroOrden = request.NumeroOrden?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(numeroOrden))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El número de orden es obligatorio.");
        }

        if (request.ClienteId <= 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El cliente es obligatorio.");
        }

        if (request.Total < 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El total no puede ser negativo.");
        }

        bool clientExists = await _unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .AnyAsync(client => client.Id == request.ClienteId);
        if (!clientExists)
        {
            throw CreateMissingClientException(request.ClienteId);
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
            Total = request.Total,
            Descripcion = request.Descripcion,
            ClienteId = request.ClienteId
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
            throw CreateMissingClientException(request.ClienteId);
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
