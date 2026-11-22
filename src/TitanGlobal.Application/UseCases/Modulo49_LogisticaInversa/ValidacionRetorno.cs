using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo49_LogisticaInversa;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo49_LogisticaInversa;

internal static class ValidacionRetorno
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
        string? tipo = new[] { "Pallet", "Retornable", "DevolucionRuta" }.FirstOrDefault(t => t.Equals(valor?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (tipo is null)
        {
            throw Error("El tipo debe ser Pallet, Retornable o DevolucionRuta.");
        }
        return tipo;
    }
    public static void Cantidad(decimal cantidad, string etiqueta)
    {
        if (cantidad < 0.001m || cantidad > 999999999.999m) { throw Error($"La {etiqueta} debe estar entre 0.001 y 999999999.999."); }
        if (decimal.Round(cantidad, 3) != cantidad) { throw Error($"La {etiqueta} no puede tener más de 3 decimales."); }
    }
    public static void Validar(GuardarRetornoRequestDto datos)
    {
        datos.Codigo = Texto(datos.Codigo, "código", 20);
        datos.ReferenciaOrigen = Texto(datos.ReferenciaOrigen, "referencia de origen", 40);
        datos.Contraparte = Texto(datos.Contraparte, "contraparte", 200);
        datos.PuntoRecogida = Texto(datos.PuntoRecogida, "punto de recogida", 200);
        datos.PuntoRecepcion = Texto(datos.PuntoRecepcion, "punto de recepción", 200);
        datos.Descripcion = Texto(datos.Descripcion, "descripción", 200);
        datos.Unidad = Texto(datos.Unidad, "unidad", 20);
        datos.Tipo = Tipo(datos.Tipo);
        if (!datos.FechaProgramada.HasValue || datos.FechaProgramada.Value == default)
        {
            throw Error("La fecha programada es obligatoria.");
        }
        datos.FechaProgramada = FechasUtc.Normalizar(datos.FechaProgramada.Value);
        Cantidad(datos.CantidadProgramada, "cantidad programada");
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
    public static async Task<RetornoLogistico> BuscarAsync(IUnitOfWork unidad, int id)
    {
        RetornoLogistico? retorno = await unidad.Repository<RetornoLogistico>().AsQueryable().FirstOrDefaultAsync(r => r.Id == id);
        if (retorno is null) { throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El retorno {id} no existe."); }
        return retorno;
    }
    public static async Task CodigoLibreAsync(IUnitOfWork unidad, string codigo, int id = 0)
    {
        string normalizado = codigo.ToLowerInvariant();
        if (await unidad.Repository<RetornoLogistico>().AsQueryable().AnyAsync(r => r.Id != id && r.Codigo.Trim().ToLower() == normalizado)) { throw Conflicto($"Ya existe un retorno con el código {codigo}."); }
    }
    public static async Task GuardarAsync(IUnitOfWork unidad, string codigo)
    {
        try
        {
            await unidad.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad) { throw Conflicto($"Ya existe un retorno con el código {codigo}."); }
    }
    public static void Asignar(RetornoLogistico retorno, GuardarRetornoRequestDto datos)
    {
        retorno.Codigo = datos.Codigo;
        retorno.ReferenciaOrigen = datos.ReferenciaOrigen;
        retorno.Contraparte = datos.Contraparte;
        retorno.PuntoRecogida = datos.PuntoRecogida;
        retorno.PuntoRecepcion = datos.PuntoRecepcion;
        retorno.Descripcion = datos.Descripcion;
        retorno.Unidad = datos.Unidad;
        retorno.Tipo = datos.Tipo;
        retorno.CantidadProgramada = datos.CantidadProgramada;
        retorno.Observaciones = datos.Observaciones;
        retorno.FechaProgramada = datos.FechaProgramada!.Value;
    }
}
