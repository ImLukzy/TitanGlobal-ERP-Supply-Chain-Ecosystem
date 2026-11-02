using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo11_Proveedores;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo11_Proveedores;

public class ListarProveedoresUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarProveedoresUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaProveedoresDto> ExecuteAsync(
        string? busqueda,
        string? pais,
        bool? activo,
        int pagina,
        int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<Proveedor> consulta = _unitOfWork.Repository<Proveedor>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
            consulta = consulta.Where(proveedor =>
                proveedor.RazonSocial.ToLower().Contains(busquedaNormalizada)
                || proveedor.Ruc.Contains(busquedaNormalizada));
        }

        string paisNormalizado = pais?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(paisNormalizado))
        {
            consulta = consulta.Where(proveedor => proveedor.Pais.ToLower() == paisNormalizado);
        }

        if (activo.HasValue)
        {
            consulta = consulta.Where(proveedor => proveedor.Activo == activo.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<ProveedorListadoDto> items = await consulta
            .OrderBy(proveedor => proveedor.RazonSocial)
            .ThenBy(proveedor => proveedor.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
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
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
