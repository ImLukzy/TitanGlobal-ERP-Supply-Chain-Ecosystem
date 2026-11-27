using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales.Queries;

public class ListarListasMaterialesQuery : IRequest<PaginaListasMaterialesDto>
{
    public int? ProductoId { get; set; }

    public string? Estado { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListarListasMaterialesQueryHandler : IRequestHandler<ListarListasMaterialesQuery, PaginaListasMaterialesDto>
{
    private readonly IUnitOfWork _unidad;

    public ListarListasMaterialesQueryHandler(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaListasMaterialesDto> Handle(ListarListasMaterialesQuery request, CancellationToken cancellationToken)
    {
        if (request.Pagina < 1)
        {
            throw ValidacionListaMaterial.SolicitudInvalida("La página debe ser mayor o igual a 1.");
        }

        if (request.TamanoPagina < 1 || request.TamanoPagina > 100)
        {
            throw ValidacionListaMaterial.SolicitudInvalida("El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<ListaMateriales> consulta = _unidad.Repository<ListaMateriales>().AsQueryable();
        if (request.Estado != null)
        {
            string estadoNormalizado = ValidacionListaMaterial.NormalizarEstado(request.Estado);
            consulta = consulta.Where(lista => lista.Estado == estadoNormalizado);
        }

        if (request.ProductoId.HasValue)
        {
            ValidacionListaMaterial.ValidarIdProducto(request.ProductoId.Value);
            consulta = consulta.Where(lista => lista.ProductoId == request.ProductoId.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)request.Pagina - 1) * request.TamanoPagina;
        if (inicio >= total)
        {
            return new PaginaListasMaterialesDto(new List<ListaMaterialListadoDto>(), total, request.Pagina, request.TamanoPagina);
        }

        List<ListaMaterialListadoDto> items = await consulta
            .OrderBy(lista => lista.ProductoId)
            .ThenByDescending(lista => lista.Version)
            .Skip((int)inicio)
            .Take(request.TamanoPagina)
            .Select(lista => new ListaMaterialListadoDto(
                lista.Id, lista.ProductoId, lista.Producto.Nombre, lista.Version, lista.Estado,
                lista.DetallesListaMaterial.Count))
            .ToListAsync();

        return new PaginaListasMaterialesDto(items, total, request.Pagina, request.TamanoPagina);
    }
}
