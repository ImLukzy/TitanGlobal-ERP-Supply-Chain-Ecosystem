using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo01_Clientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo01_Clientes;

public class ResumenClientesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ResumenClientesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResumenClientesDto> ExecuteAsync()
    {
        IQueryable<Cliente> clientes = _unitOfWork.Repository<Cliente>().AsQueryable();
        int totalClientes = await clientes.CountAsync();
        int clientesConOrdenes = await clientes.CountAsync(cliente => cliente.OrdenesCompra.Any());
        int clientesSinDescripcion = await clientes.CountAsync(cliente => string.IsNullOrEmpty(cliente.Descripcion));
        List<ClienteMontoDto> topClientesPorMonto = await clientes
            .Select(cliente => new ClienteMontoDto
            {
                ClienteId = cliente.Id,
                Nombre = cliente.Nombre,
                MontoTotal = cliente.OrdenesCompra.Sum(orden => (decimal?)orden.Total) ?? 0m,
                TotalOrdenes = cliente.OrdenesCompra.Count()
            })
            .Where(cliente => cliente.TotalOrdenes > 0)
            .OrderByDescending(cliente => cliente.MontoTotal)
            .ThenBy(cliente => cliente.ClienteId)
            .Take(5)
            .ToListAsync();

        return new ResumenClientesDto
        {
            TotalClientes = totalClientes,
            ClientesConOrdenes = clientesConOrdenes,
            ClientesSinDescripcion = clientesSinDescripcion,
            TopClientesPorMonto = topClientesPorMonto
        };
    }
}
