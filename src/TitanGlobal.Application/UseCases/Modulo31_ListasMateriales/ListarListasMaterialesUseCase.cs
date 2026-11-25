using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales;

public class ListarListasMaterialesUseCase
{
    private readonly IUnitOfWork _unidad;

    public ListarListasMaterialesUseCase(IUnitOfWork unidad)
    {
        _unidad = unidad;
    }

    public async Task<PaginaListasMaterialesDto> ExecuteAsync(
        int? productoId, string? estado, int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw ValidacionListaMaterial.SolicitudInvalida("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw ValidacionListaMaterial.SolicitudInvalida("El tamaño de página debe estar entre 1 y 100.");
        }

        IQueryable<ListaMateriales> consulta = _unidad.Repository<ListaMateriales>().AsQueryable();
        if (estado != null)
        {
            string estadoNormalizado = ValidacionListaMaterial.NormalizarEstado(estado);
            consulta = consulta.Where(lista => lista.Estado == estadoNormalizado);
        }

        if (productoId.HasValue)
        {
            ValidacionListaMaterial.ValidarIdProducto(productoId.Value);
            consulta = consulta.Where(lista => lista.ProductoId == productoId.Value);
        }

        int total = await consulta.CountAsync();
        long inicio = ((long)pagina - 1) * tamanoPagina;
        if (inicio >= total)
        {
            return new PaginaListasMaterialesDto(new List<ListaMaterialListadoDto>(), total, pagina, tamanoPagina);
        }

        List<ListaMaterialListadoDto> items = await consulta
            .OrderBy(lista => lista.ProductoId)
            .ThenByDescending(lista => lista.Version)
            .Skip((int)inicio)
            .Take(tamanoPagina)
            .Select(lista => new ListaMaterialListadoDto(
                lista.Id, lista.ProductoId, lista.Producto.Nombre, lista.Version, lista.Estado,
                lista.DetallesListaMaterial.Count))
            .ToListAsync();

        return new PaginaListasMaterialesDto(items, total, pagina, tamanoPagina);
    }
}
