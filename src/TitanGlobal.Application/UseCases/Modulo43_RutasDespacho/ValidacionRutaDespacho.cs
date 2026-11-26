using System.Text.RegularExpressions;
using TitanGlobal.Application.DTOs.Modulo43_RutasDespacho;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo43_RutasDespacho;

internal static class ValidacionRutaDespacho
{
    private static string Texto(string? texto, int maximo, string obligatorio, string longitud)
    {
        string valor = texto?.Trim() ?? string.Empty;
        if (valor.Length == 0) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, obligatorio); }
        if (valor.Length > maximo) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, longitud); }
        return valor;
    }

    public static GuardarRutaDespachoRequestDto Normalizar(GuardarRutaDespachoRequestDto datos)
    {
        string codigo = Texto(datos.Codigo, 20, "El código de la ruta es obligatorio.", "El código de la ruta no puede superar 20 caracteres.");
        string nombre = Texto(datos.Nombre, 100, "El nombre de la ruta es obligatorio.", "El nombre de la ruta no puede superar 100 caracteres.");
        string zona = Texto(datos.Zona, 100, "La zona es obligatoria.", "La zona no puede superar 100 caracteres.");
        string origen = Texto(datos.Origen, 120, "El origen es obligatorio.", "El origen no puede superar 120 caracteres.");
        string destino = Texto(datos.Destino, 120, "El destino es obligatorio.", "El destino no puede superar 120 caracteres.");
        if (datos.DistanciaKm <= 0) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La distancia debe ser mayor que cero."); }
        if (datos.DistanciaKm > 99999999.99m) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La distancia no puede superar 99999999.99 km."); }
        if (decimal.Round(datos.DistanciaKm, 2) != datos.DistanciaKm) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La distancia no puede tener más de 2 decimales."); }
        if (datos.PeajeEstimado < 0) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El peaje estimado no puede ser negativo."); }
        if (datos.PeajeEstimado > 9999999999999999.99m) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El peaje estimado no puede superar 9999999999999999.99."); }
        if (decimal.Round(datos.PeajeEstimado, 2) != datos.PeajeEstimado) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El peaje estimado no puede tener más de 2 decimales."); }
        string moneda = datos.Moneda?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!Regex.IsMatch(moneda, "^[A-Z]{3}$")) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La moneda debe tener tres letras."); }
        string observaciones = datos.Observaciones?.Trim() ?? string.Empty;
        if (observaciones.Length > 500) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "Las observaciones no pueden superar 500 caracteres."); }
        return new GuardarRutaDespachoRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            Zona = zona,
            Origen = origen,
            Destino = destino,
            DistanciaKm = datos.DistanciaKm,
            PeajeEstimado = datos.PeajeEstimado,
            Moneda = moneda,
            Activo = datos.Activo,
            Observaciones = observaciones.Length == 0 ? null : observaciones
        };
    }

    public static void Pagina(int pagina, int tamanoPagina)
    {
        if (pagina < 1) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El número de página debe ser mayor que cero."); }
        if (tamanoPagina < 1 || tamanoPagina > 100) { throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tamaño de página debe estar entre 1 y 100."); }
    }

    public static ReglaNegocioException Duplicado(string codigo)
    { return new ReglaNegocioException(CodigosEstado.Status409Conflict, $"Ya existe una ruta de despacho con el código {codigo}."); }
}
