using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo32_RutasProduccion;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo32_RutasProduccion.Queries;

public class ListarRutasProduccionQuery : IRequest<PaginaRutasProduccionDto>
{
    public int? ProductoId { get; set; }

    public string? Estado { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarRutasProduccionQueryHandler : IRequestHandler<ListarRutasProduccionQuery, PaginaRutasProduccionDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarRutasProduccionQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaRutasProduccionDto> Handle(ListarRutasProduccionQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1)
        {
            throw ValidacionRutaProduccion.Invalida("La página debe ser mayor o igual a 1.");
        }

        if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
        {
            throw ValidacionRutaProduccion.Invalida("El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<RutaProduccion> consulta = _unidad.Repository<RutaProduccion>().AsQueryable();
        if (request.ProductoId.HasValue)
        {
            ValidacionRutaProduccion.ValidarIdProducto(request.ProductoId.Value);
            consulta = consulta.Where(ruta => ruta.ProductoId == request.ProductoId.Value);
        }

        if (request.Estado != null)
        {
            string estadoNormalizado = ValidacionRutaProduccion.NormalizarEstado(request.Estado);
            consulta = consulta.Where(ruta => ruta.Estado == estadoNormalizado);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)request.Pagina - 1) * request.TamanoPagina;
        if (inicio >= total)
        {
            return new PaginaRutasProduccionDto(new List<RutaProduccionListadoDto>(), total, request.Pagina, request.TamanoPagina);
        }

        List<RutaProduccionListadoDto> items = await consulta
            .OrderBy(ruta => ruta.ProductoId)
            .ThenByDescending(ruta => ruta.Version)
            .Skip((int)inicio)
            .Take(request.TamanoPagina)
            .Select(ruta => new RutaProduccionListadoDto(
                ruta.Id, ruta.ProductoId, ruta.Producto.Nombre, ruta.Version, ruta.Estado,
                ruta.OperacionesRutaProduccion.Count))
            .ToListAsync();
        return new PaginaRutasProduccionDto(items, total, request.Pagina, request.TamanoPagina);
    }
}
