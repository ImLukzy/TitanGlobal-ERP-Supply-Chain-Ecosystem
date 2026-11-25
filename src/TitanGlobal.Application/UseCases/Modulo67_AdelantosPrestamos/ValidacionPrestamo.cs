using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo67_AdelantosPrestamos;

internal static class ValidacionPrestamo
{
    public const string TipoAdelanto = "Adelanto";

    public const string TipoPrestamo = "Prestamo";

    public const string EstadoActivo = "Activo";

    public const string EstadoCancelado = "Cancelado";

    public const string EstadoAnulado = "Anulado";

    public const string CuotaPendiente = "Pendiente";

    public const string CuotaDescontada = "Descontada";

    public const string EstadoEmpleadoActivo = "Activo";

    public const string EstadoContratoAnulado = "Anulado";

    public const int MaximoMotivo = 300;

    public const int MaximoCuotas = 24;

    // Descuento previsional típico del 13 %, hasta que exista ParametrosSistema (módulo 79).
    public const decimal FactorNetoEstimado = 0.87m;

    public const decimal PorcentajeMaximoCuota = 0.30m;

    private const decimal MontoMaximo = 9999999999.99m;

    private const int AnioMinimo = 2000;

    private const int AnioMaximo = 2100;

    private static readonly string[] Tipos = { TipoAdelanto, TipoPrestamo };

    private static readonly string[] Estados = { EstadoActivo, EstadoCancelado, EstadoAnulado };

    public static ReglaNegocioException Error400(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    public static ReglaNegocioException Error409(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }

    public static ReglaNegocioException EmpleadoInexistente(int empleadoId)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El empleado {empleadoId} no existe.");
    }

    public static ReglaNegocioException PrestamoInexistente(int id)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El préstamo {id} no existe.");
    }

    public static DateOnly Hoy()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public static string NuevoNumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    public static string NumeroDefinitivo(int id)
    {
        return $"PRE-{id:D6}";
    }

    public static decimal NetoEstimado(decimal sueldo)
    {
        return Math.Round(sueldo * FactorNetoEstimado, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CuotaMaxima(decimal neto)
    {
        return Math.Round(neto * PorcentajeMaximoCuota, 2, MidpointRounding.AwayFromZero);
    }

    public static string Importe(decimal valor)
    {
        return valor.ToString("0.00", CultureInfo.InvariantCulture);
    }

    // Reglas 1 a 12 del registro, en el orden de la spec. Devuelve el tipo, el número de cuotas y el motivo normalizados.
    public static void ValidarSolicitud(RegistrarPrestamoRequestDto solicitud, out string tipo, out int numeroCuotas, out string motivo)
    {
        if (solicitud.EmpleadoId is null)
        {
            throw Error400("El empleado es obligatorio.");
        }

        tipo = solicitud.Tipo?.Trim() ?? string.Empty;
        if (!Tipos.Contains(tipo))
        {
            throw Error400("El tipo debe ser Adelanto o Prestamo.");
        }

        if (solicitud.Monto is null)
        {
            throw Error400("El monto es obligatorio.");
        }

        decimal monto = solicitud.Monto.Value;
        if (monto <= 0m)
        {
            throw Error400("El monto debe ser mayor que cero.");
        }

        if (monto > MontoMaximo || decimal.Round(monto, 2) != monto)
        {
            throw Error400("El monto admite como máximo 2 decimales y no puede superar 9999999999.99.");
        }

        if (tipo == TipoAdelanto)
        {
            if (solicitud.NumeroCuotas is not null && solicitud.NumeroCuotas.Value != 1)
            {
                throw Error400("Un adelanto se descuenta en una sola cuota.");
            }

            numeroCuotas = 1;
        }
        else
        {
            if (solicitud.NumeroCuotas is null || solicitud.NumeroCuotas.Value < 1 || solicitud.NumeroCuotas.Value > MaximoCuotas)
            {
                throw Error400("El número de cuotas debe estar entre 1 y 24.");
            }

            numeroCuotas = solicitud.NumeroCuotas.Value;
        }

        if (solicitud.FechaOtorgamiento is null)
        {
            throw Error400("La fecha de otorgamiento es obligatoria.");
        }

        if (solicitud.PrimerAnio is null
            || solicitud.PrimerMes is null
            || solicitud.PrimerAnio.Value < AnioMinimo
            || solicitud.PrimerAnio.Value > AnioMaximo
            || solicitud.PrimerMes.Value < 1
            || solicitud.PrimerMes.Value > 12)
        {
            throw Error400("El primer mes de descuento es obligatorio y debe ser un mes válido.");
        }

        DateOnly otorgamiento = solicitud.FechaOtorgamiento.Value;
        if (solicitud.PrimerAnio.Value * 12 + solicitud.PrimerMes.Value < otorgamiento.Year * 12 + otorgamiento.Month)
        {
            throw Error400("El primer mes de descuento no puede ser anterior al mes de otorgamiento.");
        }

        motivo = solicitud.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length == 0)
        {
            throw Error400("El motivo es obligatorio.");
        }

        if (motivo.Length > MaximoMotivo)
        {
            throw Error400("El motivo no puede superar 300 caracteres.");
        }
    }

    public static string ValidarMotivoAnulacion(string? motivo)
    {
        string normalizado = motivo?.Trim() ?? string.Empty;
        if (normalizado.Length == 0)
        {
            throw Error400("El motivo de la anulación es obligatorio.");
        }

        if (normalizado.Length > MaximoMotivo)
        {
            throw Error400("El motivo de la anulación no puede superar 300 caracteres.");
        }

        return normalizado;
    }

    public static void ValidarListado(int pagina, int tamanoPagina, string tipo, string estado)
    {
        if (pagina < 1)
        {
            throw Error400("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw Error400("El tamaño de página debe estar entre 1 y 100.");
        }

        if (tipo.Length > 0 && !Tipos.Contains(tipo))
        {
            throw Error400("El filtro tipo no es válido.");
        }

        if (estado.Length > 0 && !Estados.Contains(estado))
        {
            throw Error400("El filtro estado no es válido.");
        }
    }

    public static async Task<Empleado> ObtenerEmpleadoAsync(IUnitOfWork unidad, int empleadoId)
    {
        Empleado? empleado = await unidad.Repository<Empleado>().AsQueryable()
            .AsNoTracking()
            .SingleOrDefaultAsync(entidad => entidad.Id == empleadoId);
        if (empleado is null)
        {
            throw EmpleadoInexistente(empleadoId);
        }

        return empleado;
    }

    // Contrato no anulado con FechaInicio <= fecha <= FechaCierre ?? FechaFin ?? infinito (regla de vigencia del módulo 62).
    // Devuelve el sueldo vigente a la fecha (la última adenda con FechaVigencia <= fecha, o el sueldo del contrato) o null.
    public static async Task<decimal?> LeerSueldoVigenteAsync(IUnitOfWork unidad, int empleadoId, DateOnly fecha)
    {
        List<ContratoLaboral> contratos = await unidad.Repository<ContratoLaboral>().AsQueryable()
            .AsNoTracking()
            .Include(contrato => contrato.AdendasContrato)
            .Where(contrato => contrato.EmpleadoId == empleadoId
                && contrato.Estado != EstadoContratoAnulado
                && contrato.FechaInicio <= fecha)
            .ToListAsync();
        ContratoLaboral? vigente = contratos
            .Where(contrato => fecha <= (contrato.FechaCierre ?? contrato.FechaFin ?? DateOnly.MaxValue))
            .OrderByDescending(contrato => contrato.FechaInicio)
            .FirstOrDefault();
        if (vigente is null)
        {
            return null;
        }

        AdendaContrato? adenda = vigente.AdendasContrato
            .Where(candidata => candidata.FechaVigencia <= fecha)
            .OrderBy(candidata => candidata.FechaVigencia)
            .LastOrDefault();
        return adenda?.SueldoBasico ?? vigente.SueldoBasico;
    }

    // Cuotas pendientes de préstamos activos del empleado, agrupadas por mes, desde el mes indicado (inclusive).
    public static async Task<Dictionary<(int Anio, int Mes), decimal>> LeerComprometidoPorMesAsync(IUnitOfWork unidad, int empleadoId, int anioDesde, int mesDesde)
    {
        int desde = anioDesde * 12 + mesDesde;
        List<CuotaPrestamo> cuotas = await unidad.Repository<CuotaPrestamo>().AsQueryable()
            .AsNoTracking()
            .Where(cuota => cuota.Estado == CuotaPendiente
                && cuota.Prestamo.EmpleadoId == empleadoId
                && cuota.Prestamo.Estado == EstadoActivo
                && cuota.Anio * 12 + cuota.Mes >= desde)
            .ToListAsync();
        return cuotas
            .GroupBy(cuota => (cuota.Anio, cuota.Mes))
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(cuota => cuota.Monto - cuota.MontoDescontado));
    }

    public static async Task<Prestamo> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        Prestamo? prestamo = await unidad.Repository<Prestamo>().AsQueryable()
            .Include(entidad => entidad.CuotasPrestamo)
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (prestamo is null)
        {
            throw PrestamoInexistente(id);
        }

        return prestamo;
    }
}
