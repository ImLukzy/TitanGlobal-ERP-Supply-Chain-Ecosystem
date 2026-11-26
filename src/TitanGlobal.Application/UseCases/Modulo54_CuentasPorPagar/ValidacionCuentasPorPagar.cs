using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo54_CuentasPorPagar;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo54_CuentasPorPagar;

internal static class ValidacionCuentasPorPagar
{
    private const decimal ImporteMaximo = 9999999999999999.99m;
    internal const string FacturaDuplicada = "La factura del proveedor ya está registrada.";

    internal static DateOnly CorteLima(TimeProvider reloj)
    {
        return FechaNegocioPeru.DesdeUtc(reloj.GetUtcNow().UtcDateTime);
    }

    internal static CuentaPorPagar DatosCuenta(RegistrarCuentaPorPagarRequestDto solicitud, DateOnly corte)
    {
        string serie = NormalizarClave(solicitud.Serie);
        string numero = NormalizarClave(solicitud.NumeroDocumento);
        if (!TextoValido(serie, 1, 10) || !TextoValido(numero, 1, 40))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La serie y el número de la factura del proveedor son obligatorios y deben respetar su longitud.");
        }
        if (!solicitud.FechaVencimiento.HasValue)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La fecha de vencimiento es obligatoria.");
        }
        if (solicitud.ProveedorId <= 0 || solicitud.OrdenAbastecimientoId <= 0 || !solicitud.FechaDocumento.HasValue
            || solicitud.FechaDocumento.Value > corte || solicitud.BaseImponible is not decimal baseImponible
            || solicitud.ImporteImpuesto is not decimal impuesto || !ImporteValido(baseImponible) || !ImporteValido(impuesto))
        {
            throw DatosInvalidos();
        }

        decimal total = baseImponible + impuesto;
        if (total <= 0m || total > ImporteMaximo)
        {
            throw DatosInvalidos();
        }

        return new CuentaPorPagar
        {
            ProveedorId = solicitud.ProveedorId,
            OrdenAbastecimientoId = solicitud.OrdenAbastecimientoId,
            Serie = serie,
            NumeroDocumento = numero,
            FechaDocumento = solicitud.FechaDocumento.Value,
            FechaVencimiento = solicitud.FechaVencimiento.Value,
            Moneda = Moneda(solicitud.Moneda),
            BaseImponible = baseImponible,
            ImporteImpuesto = impuesto,
            Total = total
        };
    }

    internal static async Task ValidarReferenciasAsync(IUnitOfWork unitOfWork, CuentaPorPagar cuenta)
    {
        Proveedor proveedor = await unitOfWork.Repository<Proveedor>().AsQueryable().AsNoTracking()
            .SingleOrDefaultAsync(fila => fila.Id == cuenta.ProveedorId)
            ?? throw new ReglaNegocioException(CodigosEstado.Status404NotFound, "El proveedor no existe.");
        if (!proveedor.Activo)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "El proveedor debe estar activo para registrar la factura.");
        }

        if (cuenta.OrdenAbastecimientoId.HasValue)
        {
            OrdenAbastecimiento orden = await unitOfWork.Repository<OrdenAbastecimiento>().AsQueryable().AsNoTracking()
                .SingleOrDefaultAsync(fila => fila.Id == cuenta.OrdenAbastecimientoId)
                ?? throw new ReglaNegocioException(CodigosEstado.Status404NotFound, "La orden de abastecimiento no existe.");
            if (orden.ProveedorId != cuenta.ProveedorId)
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "La orden de abastecimiento no pertenece al proveedor de la factura.");
            }
        }
    }

    internal static DateOnly ValidarPago(RegistrarAplicacionPagoRequestDto solicitud, DateOnly corte)
    {
        if (solicitud.Importe <= 0m || !ImporteValido(solicitud.Importe) || !solicitud.FechaPago.HasValue
            || solicitud.FechaPago.Value > corte)
        {
            throw DatosInvalidos();
        }

        return solicitud.FechaPago.Value;
    }

    internal static void ExigirVigente(CuentaPorPagar cuenta)
    {
        if (cuenta.FechaAnulacion.HasValue)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "La cuenta por pagar está anulada.");
        }
    }

    internal static string Motivo(string? valor)
    {
        string motivo = valor?.Trim() ?? string.Empty;
        if (!TextoValido(motivo, 3, 300))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El motivo debe tener entre 3 y 300 caracteres.");
        }

        return motivo;
    }

    internal static string Moneda(string? valor)
    {
        string moneda = valor?.Trim().ToUpperInvariant() ?? string.Empty;
        if (moneda is not ("PEN" or "USD"))
        {
            throw DatosInvalidos();
        }

        return moneda;
    }

    internal static void ValidarPagina(int pagina, int tamanoPagina)
    {
        if (pagina < 1 || tamanoPagina is < 1 or > 100)
        {
            throw DatosInvalidos();
        }
    }

    internal static async Task GuardarAsync(IUnitOfWork unitOfWork)
    {
        try
        {
            await unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad
            && excepcion.Restriccion == "IX_CuentasPorPagar_ProveedorSerieNumero")
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, FacturaDuplicada);
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea
            && excepcion.Restriccion is "FK_CuentasPorPagar_Proveedor" or "FK_CuentasPorPagar_OrdenAbastecimiento" or "FK_AplicacionesPago_CuentaPorPagar")
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se pudo guardar la operación porque una referencia ya no existe.");
        }
    }

    internal static ReglaNegocioException DatosInvalidos()
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, "Los datos de la cuenta por pagar no son válidos.");
    }

    private static bool ImporteValido(decimal importe)
    {
        return importe >= 0m && importe <= ImporteMaximo && decimal.Round(importe, 2, MidpointRounding.AwayFromZero) == importe;
    }

    private static bool TextoValido(string valor, int minimo, int maximo)
    {
        int longitud = valor.EnumerateRunes().Count();
        return longitud >= minimo && longitud <= maximo && !valor.Contains('\0');
    }

    private static string NormalizarClave(string? valor)
    {
        return string.Concat((valor ?? string.Empty).Where(caracter => !char.IsWhiteSpace(caracter))).ToUpperInvariant();
    }
}
