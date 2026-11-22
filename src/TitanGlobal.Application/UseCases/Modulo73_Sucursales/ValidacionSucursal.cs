using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo73_Sucursales;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Common;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;
namespace TitanGlobal.Application.UseCases.Modulo73_Sucursales;

internal static class ValidacionSucursal
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
    private static string? Opcional(string? valor, int maximo, string mensaje)
    {
        string? texto = valor?.Trim();
        if (texto?.Length > maximo)
        {
            throw Error(mensaje);
        }
        return string.IsNullOrEmpty(texto) ? null : texto;
    }
    public static string Tipo(string? valor)
    {
        string? tipo = new[] { "Filial", "Planta", "Oficina" }.FirstOrDefault(t => t.Equals(valor?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (tipo is null)
        {
            throw Error("El tipo debe ser Filial, Planta u Oficina.");
        }
        return tipo;
    }
    public static void Validar(GuardarSucursalRequestDto datos)
    {
        datos.Codigo = Texto(datos.Codigo, "código", 20);
        datos.Nombre = Texto(datos.Nombre, "nombre", 150);
        datos.Pais = Texto(datos.Pais, "país", 100);
        datos.Ciudad = Texto(datos.Ciudad, "ciudad", 100);
        datos.Direccion = Texto(datos.Direccion, "dirección", 300);
        datos.Tipo = Tipo(datos.Tipo);
        datos.Contacto = Opcional(datos.Contacto, 100, "El contacto no puede superar 100 caracteres.");
        datos.Email = Opcional(datos.Email, 150, "El email no puede superar 150 caracteres.");
        if (datos.Email is not null && !System.Text.RegularExpressions.Regex.IsMatch(datos.Email, @"^[^\s@<>]+@[^\s@<>]+$"))
        {
            throw Error("El email no es válido.");
        }
        datos.Telefono = Opcional(datos.Telefono, 30, "El teléfono no puede superar 30 caracteres.");
        datos.Observaciones = Opcional(datos.Observaciones, 500, "Las observaciones no pueden superar 500 caracteres.");
        if (datos.FechaApertura.HasValue)
        {
            if (datos.FechaApertura.Value == default)
            {
                throw Error("La fecha de apertura no es válida.");
            }
            datos.FechaApertura = FechasUtc.Normalizar(datos.FechaApertura.Value);
        }
    }
    public static async Task<Sucursal> BuscarAsync(IUnitOfWork unidad, int id)
    {
        Sucursal? sucursal = await unidad.Repository<Sucursal>().AsQueryable().FirstOrDefaultAsync(s => s.Id == id);
        if (sucursal is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La sucursal {id} no existe.");
        }
        return sucursal;
    }
    public static async Task CodigoLibreAsync(IUnitOfWork unidad, string codigo, int id = 0)
    {
        string normalizado = codigo.ToLowerInvariant();
        if (await unidad.Repository<Sucursal>().AsQueryable().AnyAsync(s => s.Id != id && s.Codigo.Trim().ToLower() == normalizado))
        {
            throw Conflicto($"Ya existe una sucursal con el código {codigo}.");
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
            throw Conflicto($"Ya existe una sucursal con el código {codigo}.");
        }
    }
    public static async Task<int> CambiarEstadoAsync(IUnitOfWork unidad, int id, bool activa)
    {
        Sucursal sucursal = await BuscarAsync(unidad, id);
        if (sucursal.Activa == activa)
        {
            throw Conflicto(activa ? "La sucursal ya está activa." : "La sucursal ya está inactiva.");
        }
        sucursal.Activa = activa;
        sucursal.FechaCambioEstado = FechasUtc.Normalizar(DateTime.UtcNow);
        await unidad.CompleteAsync();
        return id;
    }
    public static void Asignar(Sucursal sucursal, GuardarSucursalRequestDto datos)
    {
        sucursal.Codigo = datos.Codigo;
        sucursal.Nombre = datos.Nombre;
        sucursal.Tipo = datos.Tipo;
        sucursal.Pais = datos.Pais;
        sucursal.Ciudad = datos.Ciudad;
        sucursal.Direccion = datos.Direccion;
        sucursal.Contacto = datos.Contacto;
        sucursal.Email = datos.Email;
        sucursal.Telefono = datos.Telefono;
        sucursal.Observaciones = datos.Observaciones;
        sucursal.FechaApertura = datos.FechaApertura;
    }
}
