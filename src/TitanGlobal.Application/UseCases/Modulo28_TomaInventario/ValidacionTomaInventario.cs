using TitanGlobal.Application.DTOs.Modulo28_TomaInventario;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo28_TomaInventario;

internal static class ValidacionTomaInventario
{
    public static string NormalizarCodigo(string? codigo)
    {
        string valor = codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código de la toma de inventario es obligatorio.");
        }

        if (valor.Length > 20)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código de la toma de inventario no puede superar 20 caracteres.");
        }

        if (valor.Contains(' '))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código de la toma de inventario no puede contener espacios.");
        }

        return valor;
    }

    public static string NormalizarResponsable(string? responsable)
    {
        string valor = responsable?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El responsable de la toma de inventario es obligatorio.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El responsable no puede superar 100 caracteres.");
        }

        return valor;
    }

    public static string? NormalizarObservaciones(string? observaciones)
    {
        string valor = observaciones?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        if (valor.Length > 500)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Las observaciones no pueden superar 500 caracteres.");
        }

        return valor;
    }

    public static string? NormalizarObservacionLinea(string? observacion)
    {
        string valor = observacion?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        if (valor.Length > 250)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La observación de la línea no puede superar 250 caracteres.");
        }

        return valor;
    }

    public static DateTime NormalizarFechaToma(DateTime? fechaToma)
    {
        if (!fechaToma.HasValue)
        {
            return DateTime.UtcNow;
        }

        DateTime valor = FechasUtc.Normalizar(fechaToma.Value);
        if (valor > DateTime.UtcNow)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha de la toma de inventario no puede ser futura.");
        }

        return valor;
    }

    public static List<LineaTomaInventarioRequestDto> ValidarLineas(List<LineaTomaInventarioRequestDto>? lineas)
    {
        if (lineas is null || lineas.Count == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La toma de inventario debe incluir al menos un producto.");
        }

        HashSet<int> productosVistos = new HashSet<int>();
        foreach (LineaTomaInventarioRequestDto linea in lineas)
        {
            if (linea.ProductoId <= 0)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    "El identificador del producto es obligatorio en cada línea.");
            }

            if (!productosVistos.Add(linea.ProductoId))
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    $"El producto {linea.ProductoId} está duplicado en la toma de inventario.");
            }

            if (linea.CantidadContada < 0 || linea.CantidadContada > 1000000)
            {
                throw new ReglaNegocioException(
                    CodigosEstado.Status400BadRequest,
                    "La cantidad contada debe estar entre 0 y 1000000.");
            }

            NormalizarObservacionLinea(linea.Observacion);
        }

        return lineas;
    }

    public static TomaInventario ValidarTomaExistente(TomaInventario? toma, int id)
    {
        if (toma is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La toma de inventario {id} no existe.");
        }

        return toma;
    }

    public static void ValidarEstadoAbierta(TomaInventario toma)
    {
        if (!toma.Estado.Equals("Abierta", StringComparison.OrdinalIgnoreCase))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La toma de inventario {toma.Codigo} está {toma.Estado.ToLowerInvariant()} y no permite esta operación.");
        }
    }

    public static string? NormalizarFiltroEstado(string? estado)
    {
        string valor = estado?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        string[] estadosValidos = { "Abierta", "Cerrada", "Anulada" };
        foreach (string estadoValido in estadosValidos)
        {
            if (valor.Equals(estadoValido, StringComparison.OrdinalIgnoreCase))
            {
                return estadoValido;
            }
        }

        throw new ReglaNegocioException(
            CodigosEstado.Status400BadRequest,
            "El estado de toma de inventario no es válido.");
    }

    public static TomaInventarioDetalleDto MapearDetalle(TomaInventario toma)
    {
        List<DetalleTomaInventarioDto> lineasDto = toma.DetallesTomaInventario
            .OrderBy(detalle => detalle.Id)
            .Select(detalle => new DetalleTomaInventarioDto
            {
                Id = detalle.Id,
                ProductoId = detalle.ProductoId,
                ProductoNombre = detalle.Producto?.Nombre ?? string.Empty,
                StockSistema = detalle.StockSistema,
                CantidadContada = detalle.CantidadContada,
                Diferencia = detalle.Diferencia,
                Observacion = detalle.Observacion
            })
            .ToList();

        return new TomaInventarioDetalleDto
        {
            Id = toma.Id,
            Codigo = toma.Codigo,
            AlmacenId = toma.AlmacenId,
            AlmacenCodigo = toma.Almacen?.Codigo ?? string.Empty,
            AlmacenNombre = toma.Almacen?.Nombre ?? string.Empty,
            FechaToma = toma.FechaToma,
            Estado = toma.Estado,
            Responsable = toma.Responsable,
            Observaciones = toma.Observaciones,
            Lineas = lineasDto
        };
    }
}
