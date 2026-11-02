using System.Text.RegularExpressions;
using TitanGlobal.Application.DTOs.Modulo02_SegmentacionClientes;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo02_SegmentacionClientes;

internal static class ValidacionSegmentoCliente
{
    private static readonly Regex FormatoCodigo = new Regex(
        @"^[A-Z0-9-]{2,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static SegmentoClienteNormalizado Normalizar(GuardarSegmentoRequestDto solicitud)
    {
        string codigo = solicitud.Codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El código es obligatorio.");
        }

        if (!FormatoCodigo.IsMatch(codigo))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El código solo admite letras, números y guiones (2 a 20 caracteres).");
        }

        string nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El nombre del segmento es obligatorio.");
        }

        if (nombre.Length > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El nombre del segmento no puede superar 100 caracteres.");
        }

        string descripcionNormalizada = solicitud.Descripcion?.Trim() ?? string.Empty;
        string? descripcion = string.IsNullOrWhiteSpace(descripcionNormalizada) ? null : descripcionNormalizada;
        if (descripcion is not null && descripcion.Length > 300)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La descripción no puede superar 300 caracteres.");
        }

        if (solicitud.Prioridad < 1 || solicitud.Prioridad > 999)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La prioridad debe estar entre 1 y 999.");
        }

        if (solicitud.MontoMinimo < 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El monto mínimo no puede ser negativo.");
        }

        if (solicitud.OrdenesMinimas < 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Las órdenes mínimas no pueden ser negativas.");
        }

        if (solicitud.MontoMinimo == 0 && solicitud.OrdenesMinimas == 0)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Define al menos un criterio: monto mínimo u órdenes mínimas.");
        }

        return new SegmentoClienteNormalizado(
            codigo,
            nombre,
            descripcion,
            solicitud.Prioridad,
            solicitud.MontoMinimo,
            solicitud.OrdenesMinimas,
            solicitud.Activo);
    }

    public static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }
    }
}

internal sealed record SegmentoClienteNormalizado(
    string Codigo,
    string Nombre,
    string? Descripcion,
    int Prioridad,
    decimal MontoMinimo,
    int OrdenesMinimas,
    bool Activo);
