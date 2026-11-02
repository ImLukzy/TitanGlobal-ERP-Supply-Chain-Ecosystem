using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo23_Almacenes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo23_Almacenes;

public class ListarAlmacenesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarAlmacenesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaAlmacenesDto> ExecuteAsync(
        string? busqueda,
        string? ciudad,
        string? tipo,
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

        IQueryable<Almacen> consulta = _unitOfWork.Repository<Almacen>().AsQueryable();
        string busquedaNormalizada = busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
            consulta = consulta.Where(almacen =>
                almacen.Codigo.ToLower().Contains(busquedaNormalizada)
                || almacen.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        string ciudadNormalizada = ciudad?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(ciudadNormalizada))
        {
            consulta = consulta.Where(almacen => almacen.Ciudad.ToLower() == ciudadNormalizada);
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            string tipoNormalizado = ValidacionAlmacen.NormalizarTipo(tipo);
            consulta = consulta.Where(almacen => almacen.Tipo == tipoNormalizado);
        }

        if (activo.HasValue)
        {
            consulta = consulta.Where(almacen => almacen.Activo == activo.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<AlmacenListadoDto> items = await consulta
            .OrderBy(almacen => almacen.Codigo)
            .ThenBy(almacen => almacen.Id)
            .Skip(saltar)
            .Take(tamanoPagina)
            .Select(almacen => new AlmacenListadoDto
            {
                Id = almacen.Id,
                Codigo = almacen.Codigo,
                Nombre = almacen.Nombre,
                Tipo = almacen.Tipo,
                Ciudad = almacen.Ciudad,
                CapacidadM3 = almacen.CapacidadM3,
                Activo = almacen.Activo
            })
            .ToListAsync();

        return new PaginaAlmacenesDto
        {
            Items = items,
            Total = total,
            Pagina = pagina,
            TamanoPagina = tamanoPagina
        };
    }
}
