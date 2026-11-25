using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo31_ListasMateriales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo31_ListasMateriales;

internal static class ValidacionListaMaterial
{
    public const string Borrador = "Borrador";

    public const string Activa = "Activa";

    public const string Retirada = "Retirada";

    public static string NormalizarEstado(string estado)
    {
        switch (estado.Trim().ToUpperInvariant())
        {
            case "BORRADOR":
                return Borrador;
            case "ACTIVA":
                return Activa;
            case "RETIRADA":
                return Retirada;
            default:
                throw SolicitudInvalida("El estado de la lista de materiales no es válido.");
        }
    }

    public static async Task ValidarProductoAsync(IUnitOfWork unidad, int productoId)
    {
        ValidarIdProducto(productoId);
        if (!await unidad.Repository<Producto>().AsQueryable().AnyAsync(producto => producto.Id == productoId))
        {
            throw SolicitudInvalida($"El producto {productoId} no existe.");
        }
    }

    public static void ValidarIdProducto(int productoId)
    {
        if (productoId <= 0)
        {
            throw SolicitudInvalida("El identificador del producto debe ser mayor que 0.");
        }
    }

    public static async Task<List<DetalleListaMaterial>> ValidarComponentesAsync(
        IUnitOfWork unidad,
        int productoId,
        List<ComponenteListaMaterialRequestDto?>? componentes)
    {
        if (componentes == null || componentes.Count < 1 || componentes.Count > 100)
        {
            throw SolicitudInvalida("La lista de materiales debe tener entre 1 y 100 componentes.");
        }

        List<int> ids = componentes.OfType<ComponenteListaMaterialRequestDto>()
            .Select(componente => componente.ComponenteProductoId)
            .Distinct()
            .ToList();
        HashSet<int> existentes = await ConsultarProductosAsync(unidad, ids);
        HashSet<int> vistos = new HashSet<int>();
        List<DetalleListaMaterial> detalles = new List<DetalleListaMaterial>();

        foreach (ComponenteListaMaterialRequestDto? componente in componentes)
        {
            if (componente == null || componente.ComponenteProductoId <= 0)
            {
                throw SolicitudInvalida("El identificador del componente debe ser mayor que 0.");
            }

            int id = componente.ComponenteProductoId;
            if (id == productoId)
            {
                throw SolicitudInvalida("El producto fabricado no puede ser componente de su propia lista.");
            }

            if (!vistos.Add(id))
            {
                throw SolicitudInvalida($"El componente {id} está repetido.");
            }

            ValidarCantidad(componente.Cantidad);
            string unidadMedida = NormalizarUnidad(componente.UnidadMedida);
            if (!existentes.Contains(id))
            {
                throw SolicitudInvalida($"El producto componente {id} no existe.");
            }

            detalles.Add(new DetalleListaMaterial
            {
                ComponenteProductoId = id,
                Cantidad = componente.Cantidad,
                UnidadMedida = unidadMedida
            });
        }

        return detalles;
    }

    public static async Task<HashSet<int>> ConsultarProductosAsync(IUnitOfWork unidad, List<int> ids)
    {
        List<int> existentes = await unidad.Repository<Producto>().AsQueryable()
            .Where(producto => ids.Contains(producto.Id))
            .Select(producto => producto.Id)
            .ToListAsync();
        return existentes.ToHashSet();
    }

    public static ReglaNegocioException SolicitudInvalida(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    public static ReglaNegocioException NoEncontrada(int id)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status404NotFound, $"La lista de materiales {id} no existe.");
    }

    public static ReglaNegocioException Conflicto(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }

    private static void ValidarCantidad(decimal cantidad)
    {
        if (cantidad <= 0 || cantidad > 999999999999.999999m)
        {
            throw SolicitudInvalida("La cantidad debe ser mayor que 0 y no superar 999999999999.999999.");
        }

        if (decimal.Round(cantidad, 6) != cantidad)
        {
            throw SolicitudInvalida("La cantidad admite como máximo 6 decimales.");
        }
    }

    private static string NormalizarUnidad(string? unidadMedida)
    {
        string unidad = unidadMedida?.Trim() ?? string.Empty;
        if (unidad.Length == 0)
        {
            throw SolicitudInvalida("La unidad de medida es obligatoria.");
        }

        if (unidad.Length > 20)
        {
            throw SolicitudInvalida("La unidad de medida no puede superar 20 caracteres.");
        }

        return unidad;
    }
}
