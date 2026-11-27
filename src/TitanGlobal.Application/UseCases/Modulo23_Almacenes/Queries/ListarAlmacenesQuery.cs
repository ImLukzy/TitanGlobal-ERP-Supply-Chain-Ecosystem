using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo23_Almacenes;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo23_Almacenes.Queries;

public class ListarAlmacenesQuery : IRequest<PaginaAlmacenesDto>
{
    public string? Busqueda { get; set; }

    public string? Ciudad { get; set; }

    public string? Tipo { get; set; }

    public bool? Activo { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarAlmacenesQueryHandler : IRequestHandler<ListarAlmacenesQuery, PaginaAlmacenesDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ListarAlmacenesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginaAlmacenesDto> Handle(ListarAlmacenesQuery request, CancellationToken cancellationToken)
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

        IQueryable<Almacen> consulta = _unitOfWork.Repository<Almacen>().AsQueryable();
        string busquedaNormalizada = request.Busqueda?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(busquedaNormalizada))
        {
            // PATRÓN LINQ 1: búsqueda textual insensible a mayúsculas.
            consulta = consulta.Where(almacen =>
                almacen.Codigo.ToLower().Contains(busquedaNormalizada)
                || almacen.Nombre.ToLower().Contains(busquedaNormalizada));
        }

        string ciudadNormalizada = request.Ciudad?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(ciudadNormalizada))
        {
            consulta = consulta.Where(almacen => almacen.Ciudad.ToLower() == ciudadNormalizada);
        }

        if (!string.IsNullOrWhiteSpace(request.Tipo))
        {
            string tipoNormalizado = ValidacionAlmacen.NormalizarTipo(request.Tipo);
            consulta = consulta.Where(almacen => almacen.Tipo == tipoNormalizado);
        }

        if (request.Activo.HasValue)
        {
            consulta = consulta.Where(almacen => almacen.Activo == request.Activo.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)request.Pagina - 1) * request.TamanoPagina;
        int saltar = inicio > int.MaxValue ? int.MaxValue : (int)inicio;
        List<AlmacenListadoDto> items = await consulta
            .OrderBy(almacen => almacen.Codigo)
            .ThenBy(almacen => almacen.Id)
            .Skip(saltar)
            .Take(request.TamanoPagina)
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
            Pagina = request.Pagina,
            TamanoPagina = request.TamanoPagina
        };
    }
}
