using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Configuration;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo15_OrdenesAbastecimiento.UseCases;

public static class ValidacionOrdenAbastecimiento
{
    public static async Task<GuardarOrdenAbastecimientoRequestDto> ValidarAsync(
        GuardarOrdenAbastecimientoRequestDto orden,
        IUnitOfWork unitOfWork)
    {
        await ValidarProveedorAsync(orden.ProveedorId, unitOfWork);
        orden.FechaEntregaEstimada = ValidarFechaEntrega(orden.FechaEntregaEstimada);
        orden.Observaciones = NormalizarObservaciones(orden.Observaciones);
        List<LineaOrdenAbastecimientoRequestDto> lineas = ValidarLineas(orden.Lineas);
        orden.Lineas = lineas;
        await ValidarProductosAsync(lineas, unitOfWork);

        return orden;
    }

    public static async Task ValidarProveedorAsync(int proveedorId, IUnitOfWork unitOfWork)
    {
        Proveedor? proveedor = await unitOfWork.Repository<Proveedor>()
            .AsQueryable()
            .Where(entidad => entidad.Id == proveedorId)
            .Select(entidad => new Proveedor
            {
                Id = entidad.Id,
                Activo = entidad.Activo
            })
            .SingleOrDefaultAsync();
        if (proveedor is null)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                $"El proveedor {proveedorId} no existe.");
        }

        if (!proveedor.Activo)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                $"El proveedor {proveedorId} está inactivo.");
        }
    }

    public static DateTime ValidarFechaEntrega(DateTime fechaEntregaEstimada)
    {
        DateTime fechaUtc = FechasUtc.Normalizar(fechaEntregaEstimada);
        if (fechaUtc.Date < DateTime.UtcNow.Date)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "La fecha de entrega estimada no puede ser anterior a hoy.");
        }

        return fechaUtc;
    }

    public static string? NormalizarObservaciones(string? observaciones)
    {
        if (string.IsNullOrWhiteSpace(observaciones))
        {
            return null;
        }

        string valor = observaciones.Trim();
        if (valor.Length > 500)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "Las observaciones no pueden superar 500 caracteres.");
        }

        return valor;
    }

    public static List<LineaOrdenAbastecimientoRequestDto> ValidarLineas(
        List<LineaOrdenAbastecimientoRequestDto>? lineas)
    {
        if (lineas is null || lineas.Count == 0)
        {
            throw new ReglaNegocioException(
                StatusCodes.Status400BadRequest,
                "La orden debe tener al menos una línea.");
        }

        HashSet<int> productosUnicos = new HashSet<int>();
        foreach (LineaOrdenAbastecimientoRequestDto? linea in lineas)
        {
            if (linea is null)
            {
                throw new ReglaNegocioException(
                    StatusCodes.Status400BadRequest,
                    "La orden debe tener al menos una línea.");
            }

            if (linea.Cantidad <= 0)
            {
                throw new ReglaNegocioException(StatusCodes.Status400BadRequest, "La cantidad debe ser mayor que 0.");
            }

            if (linea.PrecioUnitario <= 0)
            {
                throw new ReglaNegocioException(
                    StatusCodes.Status400BadRequest,
                    "El precio unitario debe ser mayor que 0.");
            }

            if (!productosUnicos.Add(linea.ProductoId))
            {
                throw new ReglaNegocioException(
                    StatusCodes.Status400BadRequest,
                    $"El producto {linea.ProductoId} está repetido en la orden.");
            }
        }

        return lineas;
    }

    private static async Task ValidarProductosAsync(
        List<LineaOrdenAbastecimientoRequestDto> lineas,
        IUnitOfWork unitOfWork)
    {
        List<int> productosSolicitados = lineas.Select(linea => linea.ProductoId).Distinct().ToList();
        List<int> productosExistentes = await unitOfWork.Repository<Producto>()
            .AsQueryable()
            .Where(producto => productosSolicitados.Contains(producto.Id))
            .Select(producto => producto.Id)
            .ToListAsync();
        HashSet<int> productosExistentesSet = productosExistentes.ToHashSet();
        foreach (int productoId in productosSolicitados)
        {
            if (!productosExistentesSet.Contains(productoId))
            {
                throw new ReglaNegocioException(
                    StatusCodes.Status400BadRequest,
                    $"El producto {productoId} no existe.");
            }
        }
    }
}
