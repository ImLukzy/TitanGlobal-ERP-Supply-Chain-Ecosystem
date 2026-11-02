using System.Text.RegularExpressions;
using TitanGlobal.Application.DTOs.Modulo08_ComisionesVendedores;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo08_ComisionesVendedores;

public static class ValidacionComisiones
{
    public static GuardarVendedorRequestDto ValidarVendedor(GuardarVendedorRequestDto solicitud)
    {
        string codigo = solicitud.Codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        string nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        string? email = string.IsNullOrWhiteSpace(solicitud.Email) ? null : solicitud.Email.Trim();

        if (codigo.Length == 0)
        {
            throw new ReglaNegocioException(400, "El código es obligatorio.");
        }

        if (!Regex.IsMatch(codigo, "^[A-Z0-9-]{2,20}$"))
        {
            throw new ReglaNegocioException(400, "El código solo admite letras, números y guiones (2 a 20 caracteres).");
        }

        if (nombre.Length == 0)
        {
            throw new ReglaNegocioException(400, "El nombre del vendedor es obligatorio.");
        }

        if (nombre.Length > 100)
        {
            throw new ReglaNegocioException(400, "El nombre del vendedor no puede superar 100 caracteres.");
        }

        if (email is not null && (email.Length > 150 || !Regex.IsMatch(email, "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$")))
        {
            throw new ReglaNegocioException(400, "El email no tiene un formato válido.");
        }

        if (solicitud.PorcentajeComision < 0m || solicitud.PorcentajeComision > 20m)
        {
            throw new ReglaNegocioException(400, "La comisión debe estar entre 0 y 20 %.");
        }

        return new GuardarVendedorRequestDto
        {
            Codigo = codigo,
            Nombre = nombre,
            Email = email,
            PorcentajeComision = solicitud.PorcentajeComision,
            Activo = solicitud.Activo
        };
    }

    public static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(400, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(400, "El tamaño de página debe estar entre 1 y 100.");
        }
    }

    public static void ValidarPeriodo(int anio, int mes)
    {
        if (anio < 2000 || anio > 2100)
        {
            throw new ReglaNegocioException(400, "El año no es válido.");
        }

        if (mes < 1 || mes > 12)
        {
            throw new ReglaNegocioException(400, "El mes no es válido.");
        }
    }
}
