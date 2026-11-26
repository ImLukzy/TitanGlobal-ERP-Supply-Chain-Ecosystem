using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo34_OrdenesProduccion;

internal static class ValidacionOrdenProduccion
{
    public const int MaximoUnidades = 1000000;
    public const string ReferenciaProducto = "FK_OrdenesProduccion_Productos_ProductoId";

    public const string Planificada = "Planificada";
    public const string Liberada = "Liberada";
    public const string EnProceso = "EnProceso";
    public const string Terminada = "Terminada";
    public const string Cancelada = "Cancelada";

    public static readonly string[] Estados = { Planificada, Liberada, EnProceso, Terminada, Cancelada };

    public static void ValidarPaginacion(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw Invalida("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw Invalida("El tamaño de página debe estar entre 1 y 100.");
        }
    }

    public static void ValidarIdProducto(int productoId)
    {
        if (productoId <= 0)
        {
            throw Invalida("El identificador del producto debe ser mayor que 0.");
        }
    }

    public static string NormalizarEstado(string estado)
    {
        string valor = estado.Trim();
        string? encontrado = Estados.FirstOrDefault(
            existente => existente.Equals(valor, StringComparison.OrdinalIgnoreCase));
        if (encontrado == null)
        {
            throw Invalida("El estado de la orden de producción no es válido.");
        }

        return encontrado;
    }

    public static void ValidarRangoFechas(DateOnly? desde, DateOnly? hasta)
    {
        if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
        {
            throw Invalida("La fecha desde no puede ser posterior a la fecha hasta.");
        }
    }

    public static string? NormalizarBusqueda(string? busqueda)
    {
        string valor = busqueda?.Trim() ?? string.Empty;
        if (valor.Length > 20)
        {
            throw Invalida("La búsqueda no puede superar 20 caracteres.");
        }

        if (valor.Length == 0)
        {
            return null;
        }

        return valor.ToUpperInvariant();
    }

    public static int ValidarCantidadPlanificada(int cantidad)
    {
        if (cantidad < 1 || cantidad > MaximoUnidades)
        {
            throw Invalida("La cantidad planificada debe estar entre 1 y 1000000.");
        }

        return cantidad;
    }

    public static (DateOnly Inicio, DateOnly Fin) ValidarFechasPlanificadas(DateOnly? inicio, DateOnly? fin)
    {
        if (!inicio.HasValue || !fin.HasValue)
        {
            throw Invalida("Las fechas planificadas son obligatorias.");
        }

        if (fin.Value < inicio.Value)
        {
            throw Invalida("La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        return (inicio.Value, fin.Value);
    }

    public static string NumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    public static string NumeroDefinitivo(int id)
    {
        return $"OP-{id:D6}";
    }

    public static async Task<OrdenProduccion> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        OrdenProduccion? orden = await unidad.Repository<OrdenProduccion>().AsQueryable()
            .SingleOrDefaultAsync(orden => orden.Id == id);
        return orden ?? throw NoEncontrada(id);
    }

    public static ReglaNegocioException ProductoInexistente(int productoId)
    {
        return Invalida($"El producto {productoId} no existe.");
    }

    public static ReglaNegocioException NoEncontrada(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"La orden de producción {id} no existe.");
    }

    public static ReglaNegocioException Invalida(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    public static ReglaNegocioException Conflicto(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }
}
