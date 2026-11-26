using TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo29_AjustesInventario;

internal static class ValidacionAjustesInventario
{
    public static readonly string[] TiposValidos = { "Incremento", "Decremento" };

    public static readonly string[] MotivosValidos =
    {
        "ConteoFisico",
        "Merma",
        "Dano",
        "Caducidad",
        "Correccion"
    };

    public static string NormalizarCodigo(string? codigo)
    {
        string valor = codigo?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código del ajuste de inventario es obligatorio.");
        }

        if (valor.Length > 20)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código del ajuste de inventario no puede superar 20 caracteres.");
        }

        return valor;
    }

    public static string NormalizarTipo(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;
        foreach (string tipoValido in TiposValidos)
        {
            if (valor.Equals(tipoValido, StringComparison.OrdinalIgnoreCase))
            {
                return tipoValido;
            }
        }

        throw new ReglaNegocioException(
            CodigosEstado.Status400BadRequest,
            "El tipo de ajuste debe ser Incremento o Decremento.");
    }

    public static string? NormalizarTipoFiltro(string? tipo)
    {
        string valor = tipo?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        return NormalizarTipo(valor);
    }

    public static string NormalizarMotivo(string? motivo)
    {
        string valor = motivo?.Trim() ?? string.Empty;
        foreach (string motivoValido in MotivosValidos)
        {
            if (valor.Equals(motivoValido, StringComparison.OrdinalIgnoreCase))
            {
                return motivoValido;
            }
        }

        throw new ReglaNegocioException(
            CodigosEstado.Status400BadRequest,
            "El motivo del ajuste no es válido.");
    }

    public static string? NormalizarMotivoFiltro(string? motivo)
    {
        string valor = motivo?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            return null;
        }

        return NormalizarMotivo(valor);
    }

    public static string NormalizarResponsable(string? responsable)
    {
        string valor = responsable?.Trim() ?? string.Empty;
        if (valor.Length == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El responsable del ajuste es obligatorio.");
        }

        if (valor.Length > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El responsable del ajuste no puede superar 100 caracteres.");
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

    public static decimal ValidarCostoIncrementoExplicito(decimal costo)
    {
        if (costo <= 0m || costo > 1000000m || decimal.Round(costo, 4) != costo)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El costo unitario debe ser mayor que 0, no superar 1000000 y tener hasta 4 decimales.");
        }

        return costo;
    }

    public static AjusteInventario ValidarAjusteExistente(AjusteInventario? ajuste, int id)
    {
        if (ajuste is null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                $"El ajuste de inventario {id} no existe.");
        }

        return ajuste;
    }

    public static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }
    }

    public static void ValidarRangoFechas(DateTime? fechaDesde, DateTime? fechaHasta)
    {
        if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde.Value > fechaHasta.Value)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha inicial no puede ser mayor que la fecha final.");
        }
    }

    public static AjusteInventarioItemDto MapearItem(AjusteInventario ajuste)
    {
        return new AjusteInventarioItemDto
        {
            Id = ajuste.Id,
            Codigo = ajuste.Codigo,
            AlmacenId = ajuste.AlmacenId,
            AlmacenCodigo = ajuste.Almacen.Codigo,
            AlmacenNombre = ajuste.Almacen.Nombre,
            ProductoId = ajuste.ProductoId,
            ProductoCategoria = ajuste.Producto.Categoria,
            ProductoNombre = ajuste.Producto.Nombre,
            Tipo = ajuste.Tipo,
            Motivo = ajuste.Motivo,
            Cantidad = ajuste.Cantidad,
            CostoUnitario = ajuste.CostoUnitario,
            ValorTotal = Math.Round(ajuste.Cantidad * ajuste.CostoUnitario, 2),
            Fecha = ajuste.Fecha,
            Responsable = ajuste.Responsable,
            Observaciones = ajuste.Observaciones
        };
    }
}
