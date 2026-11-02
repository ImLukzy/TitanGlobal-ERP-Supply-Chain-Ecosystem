using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo01_Clientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo01_Clientes;

public class ObtenerClienteUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerClienteUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ClienteDetalleDto> ExecuteAsync(int id)
    {
        ClienteDetalleDto? cliente = await _unitOfWork.Repository<Cliente>()
            .AsQueryable()
            .Where(entidad => entidad.Id == id)
            .Select(entidad => new ClienteDetalleDto
            {
                Id = entidad.Id,
                Nombre = entidad.Nombre,
                Ruc = entidad.Ruc,
                Descripcion = entidad.Descripcion,
                TotalOrdenes = entidad.OrdenesCompra.Count(),
                MontoTotal = entidad.OrdenesCompra.Sum(orden => (decimal?)orden.Total) ?? 0m,
                UltimaOrdenFecha = entidad.OrdenesCompra.Max(orden => (DateTime?)orden.Fecha)
            })
            .SingleOrDefaultAsync();

        if (cliente is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El cliente {id} no existe.");
        }

        return cliente;
    }
}
