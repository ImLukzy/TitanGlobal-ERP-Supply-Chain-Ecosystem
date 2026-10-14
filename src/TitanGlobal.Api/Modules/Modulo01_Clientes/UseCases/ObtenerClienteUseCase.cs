using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo01_Clientes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo01_Clientes.UseCases;

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
            throw new ReglaNegocioException(StatusCodes.Status404NotFound, $"El cliente {id} no existe.");
        }

        return cliente;
    }
}
