using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo07_PromocionesDescuentos.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo07_PromocionesDescuentos.UseCases;

internal static class ValidacionPromocion
{
    public static async Task ValidarAsync(GuardarPromocionRequestDto solicitud, int? idActual, IUnitOfWork unitOfWork)
    {
        solicitud.Codigo = NormalizarCodigo(solicitud.Codigo);
        solicitud.Nombre = NormalizarNombre(solicitud.Nombre);
        solicitud.Tipo = NormalizarTipo(solicitud.Tipo);

        if (solicitud.Tipo == "Producto")
        {
            if (!solicitud.ProductoId.HasValue)
            {
                throw Error(StatusCodes.Status400BadRequest, "La promoción de producto requiere un producto.");
            }

            bool productoExiste = await unitOfWork.Repository<Producto>().AsQueryable()
                .AnyAsync(producto => producto.Id == solicitud.ProductoId.Value);
            if (!productoExiste)
            {
                throw Error(StatusCodes.Status400BadRequest, $"El producto {solicitud.ProductoId.Value} no existe.");
            }

            if (solicitud.MontoMinimo.HasValue)
            {
                throw Error(StatusCodes.Status400BadRequest, "Una promoción de producto no lleva monto mínimo.");
            }
        }
        else
        {
            if (!solicitud.MontoMinimo.HasValue || solicitud.MontoMinimo.Value <= 0m)
            {
                throw Error(StatusCodes.Status400BadRequest, "El monto mínimo debe ser mayor que 0.");
            }

            if (solicitud.ProductoId.HasValue)
            {
                throw Error(StatusCodes.Status400BadRequest, "Una promoción por monto mínimo no lleva producto.");
            }
        }

        if (solicitud.PorcentajeDescuento <= 0m || solicitud.PorcentajeDescuento > 100m)
        {
            throw Error(StatusCodes.Status400BadRequest, "El descuento debe ser mayor que 0 y como máximo 100.");
        }

        if (solicitud.SegmentoClienteId.HasValue && !await unitOfWork.Repository<SegmentoCliente>().AsQueryable()
            .AnyAsync(segmento => segmento.Id == solicitud.SegmentoClienteId.Value))
        {
            throw Error(StatusCodes.Status400BadRequest, $"El segmento {solicitud.SegmentoClienteId.Value} no existe.");
        }

        DateTime inicio = solicitud.FechaInicio.Date;
        DateTime fin = solicitud.FechaFin.Date;
        if (fin < inicio)
        {
            throw Error(StatusCodes.Status400BadRequest, "La fecha de fin no puede ser anterior a la de inicio.");
        }

        solicitud.FechaInicio = DateTime.SpecifyKind(inicio, DateTimeKind.Utc);
        solicitud.FechaFin = DateTime.SpecifyKind(fin, DateTimeKind.Utc);

        bool codigoRepetido = await unitOfWork.Repository<Promocion>().AsQueryable()
            .AnyAsync(promocion => promocion.Codigo == solicitud.Codigo && promocion.Id != idActual);
        if (codigoRepetido)
        {
            throw Error(StatusCodes.Status409Conflict, $"Ya existe una promoción con el código {solicitud.Codigo}.");
        }
    }

    private static string NormalizarCodigo(string? codigo)
    {
        string normalizado = codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw Error(StatusCodes.Status400BadRequest, "El código es obligatorio.");
        }

        if (!Regex.IsMatch(normalizado, "^[A-Z0-9-]{2,20}$"))
        {
            throw Error(StatusCodes.Status400BadRequest, "El código solo admite letras, números y guiones (2 a 20 caracteres).");
        }

        return normalizado;
    }

    private static string NormalizarNombre(string? nombre)
    {
        string normalizado = nombre?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw Error(StatusCodes.Status400BadRequest, "El nombre de la promoción es obligatorio.");
        }

        if (normalizado.Length > 100)
        {
            throw Error(StatusCodes.Status400BadRequest, "El nombre de la promoción no puede superar 100 caracteres.");
        }

        return normalizado;
    }

    private static string NormalizarTipo(string? tipo)
    {
        string normalizado = tipo?.Trim() ?? string.Empty;
        if (normalizado.Equals("Producto", StringComparison.OrdinalIgnoreCase))
        {
            return "Producto";
        }

        if (normalizado.Equals("MontoMinimo", StringComparison.OrdinalIgnoreCase))
        {
            return "MontoMinimo";
        }

        throw Error(StatusCodes.Status400BadRequest, "El tipo de promoción no es válido.");
    }

    private static ReglaNegocioException Error(int codigo, string mensaje)
    {
        return new ReglaNegocioException(codigo, mensaje);
    }
}
