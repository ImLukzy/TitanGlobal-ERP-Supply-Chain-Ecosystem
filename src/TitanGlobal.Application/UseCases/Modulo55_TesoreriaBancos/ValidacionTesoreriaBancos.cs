using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo55_TesoreriaBancos;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo55_TesoreriaBancos;

internal static class ValidacionTesoreriaBancos
{
    internal const string Ingreso = "Ingreso";
    internal const string Egreso = "Egreso";
    private const string CuentaDuplicada = "La cuenta ya está registrada para ese banco y número.";
    private const decimal ImporteMaximo = 9999999999999999.99m;

    internal static CuentaBancaria DatosCuenta(GuardarCuentaBancariaRequestDto solicitud)
    {
        string banco = solicitud.Banco?.Trim().ToUpperInvariant() ?? string.Empty;
        string numero = solicitud.Numero?.Trim().ToUpperInvariant() ?? string.Empty;
        string nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        if (!TextoValido(banco, 1, 120) || !TextoValido(numero, 1, 40) || !TextoValido(nombre, 1, 120))
        {
            throw DatosInvalidos();
        }

        return new CuentaBancaria
        {
            Banco = banco,
            Numero = numero,
            Nombre = nombre,
            Tipo = TipoCuenta(solicitud.Tipo),
            Moneda = Moneda(solicitud.Moneda)
        };
    }

    internal static string TipoCuenta(string? valor)
    {
        return Opcion(valor, "Corriente", "Ahorros");
    }

    internal static string TipoMovimiento(string? valor)
    {
        return Opcion(valor, Ingreso, Egreso);
    }

    internal static string Moneda(string? valor)
    {
        return Opcion(valor, "PEN", "USD");
    }

    internal static DatosOperacionBancaria Operacion(decimal importe, DateOnly? fecha, string? referencia)
    {
        string texto = referencia?.Trim() ?? string.Empty;
        if (importe <= 0m || importe > ImporteMaximo || decimal.Round(importe, 2, MidpointRounding.AwayFromZero) != importe
            || !fecha.HasValue || fecha > DateOnly.FromDateTime(DateTime.UtcNow) || !TextoValido(texto, 0, 100))
        {
            throw DatosInvalidos();
        }

        return new DatosOperacionBancaria(importe, fecha.Value, texto.Length == 0 ? null : texto);
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

    internal static void ValidarPagina(int pagina, int tamanoPagina)
    {
        if (pagina < 1 || tamanoPagina is < 1 or > 100)
        {
            throw DatosInvalidos();
        }
    }

    internal static void ExigirSaldo(decimal saldo, decimal importe)
    {
        if (importe > saldo)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "La cuenta bancaria no tiene saldo suficiente.");
        }
    }

    internal static async Task ValidarUnicidadAsync(IGenericRepository<CuentaBancaria> repositorio, CuentaBancaria datos)
    {
        if (await repositorio.AsQueryable().AnyAsync(cuenta =>
            cuenta.Banco == datos.Banco && cuenta.Numero == datos.Numero && cuenta.Id != datos.Id))
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, CuentaDuplicada);
        }
    }

    internal static async Task GuardarAsync(IUnitOfWork unitOfWork)
    {
        try
        {
            await unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad
            && excepcion.Restriccion == "IX_CuentasBancarias_BancoNumero")
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, CuentaDuplicada);
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea
            && excepcion.Restriccion is "FK_MovimientosBancarios_CuentaBancaria" or "FK_MovimientosBancarios_TransferenciaBancaria"
                or "FK_TransferenciasBancarias_CuentaOrigen" or "FK_TransferenciasBancarias_CuentaDestino")
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se pudo guardar la operación bancaria porque una referencia ya no existe.");
        }
    }

    internal static ReglaNegocioException DatosInvalidos()
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, "Los datos de tesorería no son válidos.");
    }

    private static string Opcion(string? valor, string primero, string segundo)
    {
        string? texto = valor?.Trim();
        if (string.Equals(texto, primero, StringComparison.OrdinalIgnoreCase))
        {
            return primero;
        }
        if (string.Equals(texto, segundo, StringComparison.OrdinalIgnoreCase))
        {
            return segundo;
        }

        throw DatosInvalidos();
    }

    private static bool TextoValido(string texto, int minimo, int maximo)
    {
        int longitud = texto.EnumerateRunes().Count();
        return longitud >= minimo && longitud <= maximo && !texto.Contains('\0');
    }
}

internal sealed record DatosOperacionBancaria(decimal Importe, DateOnly FechaOperacion, string? Referencia)
{
    internal MovimientoBancario CrearMovimiento(int cuentaId, string tipo, DateTime fechaRegistro)
    {
        return new MovimientoBancario
        {
            CuentaBancariaId = cuentaId,
            Tipo = tipo,
            Importe = Importe,
            FechaOperacion = FechaOperacion,
            Referencia = Referencia,
            FechaRegistro = fechaRegistro
        };
    }
}
