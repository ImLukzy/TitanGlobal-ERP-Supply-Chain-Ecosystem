using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo47_ControlCombustible;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo47_ControlCombustible;

internal static class ValidacionVale
{
    public static GuardarValeRequestDto Normalizar(GuardarValeRequestDto solicitud)
    {
        GuardarValeRequestDto datos = new GuardarValeRequestDto
        {
            Codigo = Texto(solicitud.Codigo, "código", 20),
            Placa = Texto(solicitud.Placa, "placa", 20).ToUpperInvariant(),
            Estacion = Texto(solicitud.Estacion, "estación", 150)
        };
        if (!solicitud.FechaCarga.HasValue || solicitud.FechaCarga.Value == default)
        {
            throw Error("La fecha de carga es obligatoria.");
        }
        datos.FechaCarga = FechasUtc.Normalizar(solicitud.FechaCarga.Value);
        datos.TipoCombustible = Tipo(solicitud.TipoCombustible);
        ValidarDecimal(solicitud.Litros, "Los litros deben estar entre 0.001 y 9999999.999.", "Los litros no pueden tener más de 3 decimales.");
        ValidarDecimal(solicitud.PrecioLitro, "El precio por litro debe estar entre 0.001 y 9999999.999.", "El precio por litro no puede tener más de 3 decimales.");
        datos.Litros = solicitud.Litros;
        datos.PrecioLitro = solicitud.PrecioLitro;
        datos.Moneda = solicitud.Moneda?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!System.Text.RegularExpressions.Regex.IsMatch(datos.Moneda, "^[A-Z]{3}$"))
        {
            throw Error("La moneda debe contener 3 letras mayúsculas.");
        }
        if (solicitud.OdometroInicial < 0 || solicitud.OdometroInicial > 999999999)
        {
            throw Error("El odómetro inicial debe estar entre 0 y 999999999.");
        }
        if (solicitud.OdometroFinal.HasValue)
        {
            if (solicitud.OdometroFinal.Value < 0 || solicitud.OdometroFinal.Value > 999999999)
            {
                throw Error("El odómetro final debe estar entre 0 y 999999999.");
            }
            if (solicitud.OdometroFinal.Value <= solicitud.OdometroInicial)
            {
                throw Error("El odómetro final debe ser mayor que el inicial.");
            }
        }
        datos.OdometroInicial = solicitud.OdometroInicial;
        datos.OdometroFinal = solicitud.OdometroFinal;
        datos.Observaciones = solicitud.Observaciones?.Trim();
        if (datos.Observaciones?.Length > 500)
        {
            throw Error("Las observaciones no pueden superar 500 caracteres.");
        }
        if (string.IsNullOrEmpty(datos.Observaciones))
        {
            datos.Observaciones = null;
        }
        return datos;
    }

    public static string Tipo(string? entrada)
    {
        string? tipo = new[] { "Diesel", "Gasolina" }.FirstOrDefault(t => t.Equals(entrada?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (tipo is null)
        {
            throw Error("El combustible debe ser Diesel o Gasolina.");
        }
        return tipo;
    }

    private static void ValidarDecimal(decimal valor, string rango, string escala)
    {
        if (valor < 0.001m || valor > 9999999.999m)
        {
            throw Error(rango);
        }
        if (decimal.Round(valor, 3) != valor)
        {
            throw Error(escala);
        }
    }

    private static string Texto(string? entrada, string etiqueta, int maximo)
    {
        string texto = entrada?.Trim() ?? string.Empty;
        if (texto.Length == 0)
        {
            throw Error($"El campo {etiqueta} es obligatorio.");
        }
        if (texto.Length > maximo)
        {
            throw Error($"El campo {etiqueta} no puede superar {maximo} caracteres.");
        }
        return texto;
    }

    public static ReglaNegocioException Error(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    public static ReglaNegocioException Conflicto(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }

    public static void ExigirBorrador(ValeCombustible vale, string accion)
    {
        if (vale.Estado != "Borrador")
        {
            throw Conflicto($"Solo se puede {accion} un vale en borrador.");
        }
    }

    public static async Task<ValeCombustible> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        ValeCombustible? vale = await unidad.Repository<ValeCombustible>().AsQueryable().FirstOrDefaultAsync(v => v.Id == id);
        if (vale is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El vale de combustible {id} no existe.");
        }
        return vale;
    }

    public static async Task ExigirCodigoLibreAsync(IUnitOfWork unidad, string codigo, int id = 0)
    {
        string normalizado = codigo.ToLowerInvariant();
        if (await unidad.Repository<ValeCombustible>().AsQueryable().AnyAsync(v => v.Id != id && v.Codigo.Trim().ToLower() == normalizado))
        {
            throw Conflicto($"Ya existe un vale con el código {codigo}.");
        }
    }

    public static async Task GuardarAsync(IUnitOfWork unidad, string codigo)
    {
        try
        {
            await unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad)
        {
            throw Conflicto($"Ya existe un vale con el código {codigo}.");
        }
    }

    public static void Asignar(ValeCombustible vale, GuardarValeRequestDto datos)
    {
        vale.Codigo = datos.Codigo;
        vale.Placa = datos.Placa;
        vale.Estacion = datos.Estacion;
        vale.FechaCarga = datos.FechaCarga!.Value;
        vale.TipoCombustible = datos.TipoCombustible;
        vale.Litros = datos.Litros;
        vale.PrecioLitro = datos.PrecioLitro;
        vale.Moneda = datos.Moneda;
        vale.OdometroInicial = datos.OdometroInicial;
        vale.OdometroFinal = datos.OdometroFinal;
        vale.Observaciones = datos.Observaciones;
    }
}
