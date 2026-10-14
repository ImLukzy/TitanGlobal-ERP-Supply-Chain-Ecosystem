using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo01_Clientes.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo01_Clientes.UseCases;

public class ListarClientesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarClientesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaClientesDto> ExecuteAsync(string? busqueda, int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<Cliente> consulta = _unitOfWork.Repository<Cliente>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            consulta = consulta.Where(cliente =>
                cliente.Nombre.ToLower().Contains(busquedaNormalizada)
                || cliente.Ruc.Contains(busquedaNormalizada));
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<ClienteResumenDto> items = await consulta
            .OrderBy(cliente => cliente.Nombre)
            .ThenBy(cliente => cliente.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(cliente => new ClienteResumenDto
            {
                Id = cliente.Id,
                Nombre = cliente.Nombre,
                Ruc = cliente.Ruc,
                TotalOrdenes = cliente.OrdenesCompra.Count()
            })
            .ToListAsync();

        return new PaginaClientesDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
