using TitanGlobal.Application.DTOs.Modulo30_ReabastecimientoAutomatico;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo30_ReabastecimientoAutomatico;

public static class ValidacionReabastecimiento
{
    public static string NormalizarCodigo(string? codigo)
    {
        string valor = codigo?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código de la regla de reabastecimiento es obligatorio.");
        }

        if (valor.Length > 20)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código de la regla de reabastecimiento no puede superar 20 caracteres.");
        }

        if (valor.Any(char.IsWhiteSpace))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código de la regla de reabastecimiento no puede contener espacios.");
        }

        return valor;
    }

    public static void ValidarParametrosStock(
        int stockActual,
        int stockMinimo,
        int puntoReorden,
        int stockMaximo,
        int cantidadLoteCompra)
    {
        if (stockActual < 0 || stockActual > 1000000)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El stock actual debe estar entre 0 y 1000000.");
        }

        if (stockMinimo < 0 || stockMinimo > 1000000)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El stock mínimo debe estar entre 0 y 1000000.");
        }

        if (puntoReorden < stockMinimo || puntoReorden > 1000000)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El punto de reorden debe ser mayor o igual al stock mínimo y no superar 1000000.");
        }

        if (stockMaximo <= puntoReorden || stockMaximo > 1000000)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El stock máximo debe ser mayor al punto de reorden y no superar 1000000.");
        }

        if (cantidadLoteCompra < 1 || cantidadLoteCompra > 1000000)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La cantidad de lote de compra debe estar entre 1 y 1000000.");
        }
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

    public static string? NormalizarMotivoDescarte(string? motivo)
    {
        string valor = motivo?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        if (valor.Length > 300)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El motivo de descarte no puede superar 300 caracteres.");
        }

        return valor;
    }

    public static string? NormalizarFiltroEstadoSugerencia(string? estado)
    {
        string valor = estado?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        string[] estadosValidos = { "Pendiente", "SolicitudGenerada", "Descartada" };
        foreach (string estadoValido in estadosValidos)
        {
            if (valor.Equals(estadoValido, StringComparison.OrdinalIgnoreCase))
            {
                return estadoValido;
            }
        }

        throw new ReglaNegocioException(
            CodigosEstado.Status400BadRequest,
            "El estado de sugerencia de reabastecimiento no es válido.");
    }

    public static Producto ValidarProductoExistente(Producto? producto, int productoId)
    {
        if (productoId < 1)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El identificador del producto es obligatorio.");
        }

        if (producto is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                $"El producto {productoId} no existe.");
        }

        return producto;
    }

    public static Almacen ValidarAlmacenExistente(Almacen? almacen, int almacenId)
    {
        if (almacenId < 1)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El identificador del almacén es obligatorio.");
        }

        if (almacen is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                $"El almacén {almacenId} no existe.");
        }

        return almacen;
    }

    public static void ValidarAlmacenActivo(Almacen almacen)
    {
        if (!almacen.Activo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El almacén {almacen.Codigo} está inactivo.");
        }
    }

    public static Proveedor? ValidarProveedorOpcional(Proveedor? proveedor, int? proveedorId)
    {
        if (!proveedorId.HasValue)
        {
            return null;
        }

        if (proveedorId.Value < 1 || proveedor is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                $"El proveedor {proveedorId.Value} no existe.");
        }

        if (!proveedor.Activo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El proveedor {proveedor.RazonSocial} está inactivo.");
        }

        return proveedor;
    }

    public static ReglaReabastecimiento ValidarReglaExistente(ReglaReabastecimiento? regla, int id)
    {
        if (regla is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La regla de reabastecimiento {id} no existe.");
        }

        return regla;
    }

    public static EjecucionReabastecimiento ValidarEjecucionExistente(EjecucionReabastecimiento? ejecucion, int id)
    {
        if (ejecucion is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"La sugerencia de reabastecimiento {id} no existe.");
        }

        return ejecucion;
    }

    public static void ValidarEjecucionPendiente(EjecucionReabastecimiento ejecucion)
    {
        if (!ejecucion.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"La sugerencia de reabastecimiento {ejecucion.Id} ya fue procesada en estado {ejecucion.Estado}.");
        }
    }

    public static int CalcularCantidadSugerida(ReglaReabastecimiento regla)
    {
        if (regla.StockActual > regla.PuntoReorden)
        {
            return 0;
        }

        int necesidadBase = regla.StockMaximo - regla.StockActual;
        if (necesidadBase < 1)
        {
            necesidadBase = 1;
        }

        int lote = regla.CantidadLoteCompra < 1 ? 1 : regla.CantidadLoteCompra;
        int lotesNecesarios = (necesidadBase + lote - 1) / lote;
        return lotesNecesarios * lote;
    }

    public static ReglaReabastecimientoListadoDto MapearListado(ReglaReabastecimiento regla)
    {
        int cantidadSugerida = CalcularCantidadSugerida(regla);
        return new ReglaReabastecimientoListadoDto
        {
            Id = regla.Id,
            Codigo = regla.Codigo,
            ProductoId = regla.ProductoId,
            ProductoNombre = regla.Producto?.Nombre ?? string.Empty,
            ProductoPrecio = regla.Producto?.Precio ?? 0m,
            AlmacenId = regla.AlmacenId,
            AlmacenCodigo = regla.Almacen?.Codigo ?? string.Empty,
            AlmacenNombre = regla.Almacen?.Nombre ?? string.Empty,
            ProveedorPreferidoId = regla.ProveedorPreferidoId,
            ProveedorPreferidoNombre = regla.ProveedorPreferido?.RazonSocial,
            StockActual = regla.StockActual,
            StockMinimo = regla.StockMinimo,
            PuntoReorden = regla.PuntoReorden,
            StockMaximo = regla.StockMaximo,
            CantidadLoteCompra = regla.CantidadLoteCompra,
            CantidadSugeridaActual = cantidadSugerida,
            RequiereReabastecimiento = regla.Activa && regla.StockActual <= regla.PuntoReorden,
            Activa = regla.Activa,
            UltimaEvaluacion = regla.UltimaEvaluacion
        };
    }

    public static EjecucionReabastecimientoItemDto MapearEjecucion(EjecucionReabastecimiento ejecucion)
    {
        ReglaReabastecimiento? regla = ejecucion.ReglaReabastecimiento;
        return new EjecucionReabastecimientoItemDto
        {
            Id = ejecucion.Id,
            ReglaReabastecimientoId = ejecucion.ReglaReabastecimientoId,
            ReglaCodigo = regla?.Codigo ?? string.Empty,
            ProductoId = regla?.ProductoId ?? 0,
            ProductoNombre = regla?.Producto?.Nombre ?? string.Empty,
            AlmacenId = regla?.AlmacenId ?? 0,
            AlmacenCodigo = regla?.Almacen?.Codigo ?? string.Empty,
            AlmacenNombre = regla?.Almacen?.Nombre ?? string.Empty,
            ProveedorPreferidoId = regla?.ProveedorPreferidoId,
            ProveedorPreferidoNombre = regla?.ProveedorPreferido?.RazonSocial,
            FechaEjecucion = ejecucion.FechaEjecucion,
            StockActual = ejecucion.StockActual,
            PuntoReorden = regla?.PuntoReorden ?? 0,
            CantidadSugerida = ejecucion.CantidadSugerida,
            Estado = ejecucion.Estado,
            SolicitudCompraId = ejecucion.SolicitudCompraId,
            SolicitudCompraNumero = ejecucion.SolicitudCompra?.Numero,
            Observacion = ejecucion.Observacion
        };
    }
}
