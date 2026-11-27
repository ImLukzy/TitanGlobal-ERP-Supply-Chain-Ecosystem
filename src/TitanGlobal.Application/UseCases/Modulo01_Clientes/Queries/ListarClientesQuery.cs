using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo01_Clientes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo01_Clientes.Queries;

public class ListarClientesQuery : IRequest<PaginaClientesDto>
{
    public string? Busqueda { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarClientesQueryHandler : IRequestHandler<ListarClientesQuery, PaginaClientesDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarClientesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaClientesDto> Handle(ListarClientesQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<Cliente> consulta = _unitOfWork.Repository<Cliente>().AsQueryable();
        string busquedaNormalizada = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            consulta = consulta.Where(cliente =>
                cliente.Nombre.ToLower().Contains(busquedaNormalizada)
                || cliente.Ruc.Contains(busquedaNormalizada));
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<ClienteResumenDto> items = await consulta
            .OrderBy(cliente => cliente.Nombre)
            .ThenBy(cliente => cliente.Id)
            .Skip(saltar)
            .Take(request.TamanoPagina)
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
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
