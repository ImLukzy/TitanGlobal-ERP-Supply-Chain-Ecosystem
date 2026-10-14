using Microsoft.EntityFrameworkCore;
using Npgsql;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.UseCases;

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
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El número de orden es obligatorio.");
        }

        if (request.ClienteId <= 0)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El cliente es obligatorio.");
        }

        if (request.Total < 0)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "El total no puede ser negativo.");
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
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw CreateDuplicateOrderException(numeroOrden);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw CreateMissingClientException(request.ClienteId);
        }

        return order.Id;
    }

    private static ReglaNegocioException CreateMissingClientException(int clientId)
    {
        return new ReglaNegocioException(
            StatusCodes.Status400BadRequest,
            $"El cliente {clientId} no existe.");
    }

    private static ReglaNegocioException CreateDuplicateOrderException(string orderNumber)
    {
        return new ReglaNegocioException(
            StatusCodes.Status409Conflict,
            $"Ya existe una orden de compra con el número {orderNumber}.");
    }
}
