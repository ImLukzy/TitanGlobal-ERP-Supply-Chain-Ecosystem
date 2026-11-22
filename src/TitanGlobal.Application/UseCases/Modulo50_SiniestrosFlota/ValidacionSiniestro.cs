using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo50_SiniestrosFlota;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo50_SiniestrosFlota;

internal static class ValidacionSiniestro
{
    public static ReglaNegocioException Error(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }
    public static ReglaNegocioException Conflicto(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }
    public static string Texto(string? valor, string etiqueta, int maximo)
    {
        string texto = valor?.Trim() ?? string.Empty;
        if (texto.Length == 0) { throw Error($"El campo {etiqueta} es obligatorio."); }
        if (texto.Length > maximo) { throw Error($"El campo {etiqueta} no puede superar {maximo} caracteres."); }
        return texto;
    }
    public static string Tipo(string? valor)
    {
        string? tipo = new[] { "Accidente", "Robo", "DanoMaterial" }.FirstOrDefault(t => t.Equals(valor?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (tipo is null)
        {
            throw Error("El tipo debe ser Accidente, Robo o DanoMaterial.");
        }
        return tipo;
    }
    public static void Monto(decimal monto, string etiqueta, decimal minimo)
    {
        if (monto < minimo || monto > 9999999999.99m)
        {
            string limite = minimo == 0 ? "0" : "0.01";
            throw Error($"El {etiqueta} debe estar entre {limite} y 9999999999.99.");
        }
        if (decimal.Round(monto, 2) != monto)
        {
            throw Error($"El {etiqueta} no puede tener más de 2 decimales.");
        }
    }
    public static void Validar(GuardarSiniestroRequestDto datos)
    {
        datos.Codigo = Texto(datos.Codigo, "código", 20);
        datos.Placa = Texto(datos.Placa, "placa", 20).ToUpperInvariant();
        datos.Lugar = Texto(datos.Lugar, "lugar", 200);
        datos.Descripcion = Texto(datos.Descripcion, "descripción", 500);
        datos.Tipo = Tipo(datos.Tipo);
        if (!datos.FechaIncidente.HasValue || datos.FechaIncidente.Value == default)
        {
            throw Error("La fecha del incidente es obligatoria.");
        }
        datos.FechaIncidente = FechasUtc.Normalizar(datos.FechaIncidente.Value);
        datos.Observaciones = datos.Observaciones?.Trim();
        if (datos.Observaciones?.Length > 500)
        {
            throw Error("Las observaciones no pueden superar 500 caracteres.");
        }
        if (string.IsNullOrEmpty(datos.Observaciones))
        {
            datos.Observaciones = null;
        }
    }
    public static async Task<SiniestroFlota> BuscarAsync(IUnitOfWork unidad, int id)
    {
        SiniestroFlota? siniestro = await unidad.Repository<SiniestroFlota>().AsQueryable().FirstOrDefaultAsync(r => r.Id == id);
        if (siniestro is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El siniestro {id} no existe."); }
        return siniestro;
    }
    public static async Task CodigoLibreAsync(IUnitOfWork unidad, string codigo, int id = 0)
    {
        string normalizado = codigo.ToLowerInvariant();
        if (await unidad.Repository<SiniestroFlota>().AsQueryable().AnyAsync(r => r.Id != id && r.Codigo.Trim().ToLower() == normalizado)) { throw Conflicto($"Ya existe un siniestro con el código {codigo}."); }
    }
    public static async Task GuardarAsync(IUnitOfWork unidad, string codigo)
    {
        try
        {
            await unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad) { throw Conflicto($"Ya existe un siniestro con el código {codigo}."); }
    }
    public static void Asignar(SiniestroFlota siniestro, GuardarSiniestroRequestDto datos)
    {
        siniestro.Codigo = datos.Codigo;
        siniestro.Placa = datos.Placa;
        siniestro.Lugar = datos.Lugar;
        siniestro.Descripcion = datos.Descripcion;
        siniestro.Tipo = datos.Tipo;
        siniestro.Observaciones = datos.Observaciones;
        siniestro.FechaIncidente = datos.FechaIncidente!.Value;
    }
}
