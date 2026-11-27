using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores.Queries;

public class ListarProveedoresQuery : IRequest<PaginaProveedoresDto>
{
    public string? Busqueda { get; set; }

    public string? Pais { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarProveedoresQueryHandler : IRequestHandler<ListarProveedoresQuery, PaginaProveedoresDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarProveedoresQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaProveedoresDto> Handle(ListarProveedoresQuery request, CancellationToken cancellationToken)
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

        IQueryable<Proveedor> consulta = _unitOfWork.Repository<Proveedor>().AsQueryable();
        string busquedaNormalizada = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
            consulta = consulta.Where(proveedor =>
                proveedor.RazonSocial.ToLower().Contains(busquedaNormalizada)
                || proveedor.Ruc.Contains(busquedaNormalizada));
        }

        string paisNormalizado = request.Pais?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(paisNormalizado))
        {
            consulta = consulta.Where(proveedor => proveedor.Pais.ToLower() == paisNormalizado);
        }

        if (request.Activo.HasValue)
        {
            consulta = consulta.Where(proveedor => proveedor.Activo == request.Activo.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<ProveedorListadoDto> items = await consulta
            .OrderBy(proveedor => proveedor.RazonSocial)
            .ThenBy(proveedor => proveedor.Id)
            .Skip(saltar)
            .Take(request.TamanoPagina)
            .Select(proveedor => new ProveedorListadoDto
            {
                Id = proveedor.Id,
                RazonSocial = proveedor.RazonSocial,
                Ruc = proveedor.Ruc,
                Pais = proveedor.Pais,
                Activo = proveedor.Activo
            })
            .ToListAsync();

        return new PaginaProveedoresDto
        {
            Items = items,
            Total = total,
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
