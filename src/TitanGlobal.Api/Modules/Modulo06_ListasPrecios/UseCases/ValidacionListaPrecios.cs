using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo06_ListasPrecios.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo06_ListasPrecios.UseCases;

internal static class ValidacionListaPrecios
{
    public static async Task ValidarAsync(GuardarListaRequestDto solicitud, int? idActual, IUnitOfWork unitOfWork)
    {
        solicitud.Codigo = NormalizarCodigo(solicitud.Codigo);
        solicitud.Nombre = NormalizarNombre(solicitud.Nombre);
        if (solicitud.SegmentoClienteId.HasValue && !await unitOfWork.Repository<SegmentoCliente>()
            .AsQueryable().AnyAsync(segmento => segmento.Id == solicitud.SegmentoClienteId.Value))
        {
            throw Error(StatusCodes.Status400BadRequest, $"El segmento {solicitud.SegmentoClienteId.Value} no existe.");
        }

        if (solicitud.Prioridad < 1 || solicitud.Prioridad > 999)
        {
            throw Error(StatusCodes.Status400BadRequest, "La prioridad debe estar entre 1 y 999.");
        }

        DateTime inicio = solicitud.FechaInicio.Date;
        DateTime? fin = solicitud.FechaFin?.Date;
        if (fin.HasValue && fin.Value < inicio)
        {
            throw Error(StatusCodes.Status400BadRequest, "La fecha de fin no puede ser anterior a la de inicio.");
        }

        solicitud.FechaInicio = DateTime.SpecifyKind(inicio, DateTimeKind.Utc);
        solicitud.FechaFin = fin.HasValue ? DateTime.SpecifyKind(fin.Value, DateTimeKind.Utc) : null;
        if (solicitud.Items is null || solicitud.Items.Count == 0)
        {
            throw Error(StatusCodes.Status400BadRequest, "La lista debe tener al menos un producto.");
        }

        foreach (GuardarItemListaDto item in solicitud.Items)
        {
            if (item.Precio <= 0m)
            {
                throw Error(StatusCodes.Status400BadRequest, "El precio debe ser mayor que 0.");
            }
        }

        int? repetido = solicitud.Items.GroupBy(item => item.ProductoId)
            .Where(grupo => grupo.Count() > 1).Select(grupo => (int?)grupo.Key).FirstOrDefault();
        if (repetido.HasValue)
        {
            throw Error(StatusCodes.Status400BadRequest, $"El producto {repetido.Value} está repetido en la lista.");
        }

        HashSet<int> productosIds = solicitud.Items.Select(item => item.ProductoId).ToHashSet();
        List<int> existentes = await unitOfWork.Repository<Producto>().AsQueryable()
            .Where(producto => productosIds.Contains(producto.Id)).Select(producto => producto.Id).ToListAsync();
        int? inexistente = productosIds.Except(existentes).OrderBy(productoId => productoId).Cast<int?>().FirstOrDefault();
        if (inexistente.HasValue)
        {
            throw Error(StatusCodes.Status400BadRequest, $"El producto {inexistente.Value} no existe.");
        }

        bool codigoRepetido = await unitOfWork.Repository<ListaPrecios>().AsQueryable()
            .AnyAsync(lista => lista.Codigo == solicitud.Codigo && lista.Id != idActual);
        if (codigoRepetido)
        {
            throw Error(StatusCodes.Status409Conflict, $"Ya existe una lista de precios con el código {solicitud.Codigo}.");
        }

        bool prioridadRepetida = await unitOfWork.Repository<ListaPrecios>().AsQueryable()
            .AnyAsync(lista => lista.Prioridad == solicitud.Prioridad && lista.Id != idActual);
        if (prioridadRepetida)
        {
            throw Error(StatusCodes.Status409Conflict, $"Ya existe una lista de precios con la prioridad {solicitud.Prioridad}.");
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
            throw Error(StatusCodes.Status400BadRequest, "El nombre de la lista es obligatorio.");
        }

        if (normalizado.Length > 100)
        {
            throw Error(StatusCodes.Status400BadRequest, "El nombre de la lista no puede superar 100 caracteres.");
        }

        return normalizado;
    }

    private static ReglaNegocioException Error(int codigo, string mensaje)
    {
        return new ReglaNegocioException(codigo, mensaje);
    }
}
