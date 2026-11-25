using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas;

internal static class ValidacionPlanilla
{
    public const string TipoMensual = "Mensual";

    public const string TipoComplementaria = "Complementaria";

    public const string EstadoBorrador = "Borrador";

    public const string EstadoCalculada = "Calculada";

    public const string EstadoAprobada = "Aprobada";

    public const string EstadoPagada = "Pagada";

    public const string EstadoContratoAnulado = "Anulado";

    public const string EmpresaBoleta = "TitanGlobal";

    public const int MaximoConcepto = 100;

    public const int LongitudCuspp = 12;

    public const string SistemaAfp = "AFP";

    private const decimal MontoMaximo = 9999999999.99m;

    private const int AnioMinimo = 2000;

    private const int AnioMaximo = 2100;

    private static readonly string[] Tipos = { TipoMensual, TipoComplementaria };

    private static readonly string[] Estados = { EstadoBorrador, EstadoCalculada, EstadoAprobada, EstadoPagada };

    public static ReglaNegocioException Error400(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }

    public static ReglaNegocioException Error404(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, mensaje);
    }

    public static ReglaNegocioException Error409(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status409Conflict, mensaje);
    }

    public static ReglaNegocioException PlanillaInexistente(int id)
    {
        return Error404($"La planilla {id} no existe.");
    }

    public static ReglaNegocioException EmpleadoInexistente(int empleadoId)
    {
        return Error404($"El empleado {empleadoId} no existe.");
    }

    public static DateOnly Hoy()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public static string SufijoTipo(string tipo)
    {
        return tipo == TipoMensual ? "M" : "C";
    }

    public static string NumeroPlanilla(int anio, int mes, string tipo)
    {
        return $"PLA-{anio}{mes:D2}-{SufijoTipo(tipo)}";
    }

    public static string NumeroBoleta(int anio, int mes, string tipo, int empleadoId)
    {
        return $"BOL-{anio}{mes:D2}-{SufijoTipo(tipo)}-{empleadoId:D6}";
    }

    public static bool EstaCerrada(string estado)
    {
        return estado == EstadoAprobada || estado == EstadoPagada;
    }

    public static int ValidarAnio(int? anio)
    {
        if (anio is null || anio.Value < AnioMinimo || anio.Value > AnioMaximo)
        {
            throw Error400("El año es obligatorio y debe estar entre 2000 y 2100.");
        }

        return anio.Value;
    }

    public static int ValidarMes(int? mes)
    {
        if (mes is null || mes.Value < 1 || mes.Value > 12)
        {
            throw Error400("El mes es obligatorio y debe estar entre 1 y 12.");
        }

        return mes.Value;
    }

    public static string ValidarTipo(string? tipo)
    {
        string normalizado = tipo?.Trim() ?? string.Empty;
        if (!Tipos.Contains(normalizado))
        {
            throw Error400("El tipo de planilla debe ser Mensual o Complementaria.");
        }

        return normalizado;
    }

    public static void ValidarListado(int pagina, int tamanoPagina)
    {
        if (pagina < 1)
        {
            throw Error400("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw Error400("El tamaño de página debe estar entre 1 y 100.");
        }
    }

    public static void ValidarFiltroTipo(string tipo)
    {
        if (tipo.Length > 0 && !Tipos.Contains(tipo))
        {
            throw Error400("El filtro tipo no es válido.");
        }
    }

    public static void ValidarFiltroEstado(string estado)
    {
        if (estado.Length > 0 && !Estados.Contains(estado))
        {
            throw Error400("El filtro estado no es válido.");
        }
    }

    public static string ValidarConcepto(string? concepto)
    {
        string normalizado = concepto?.Trim() ?? string.Empty;
        if (normalizado.Length == 0 || normalizado.Length > MaximoConcepto)
        {
            throw Error400("El concepto es obligatorio y no puede superar 100 caracteres.");
        }

        return normalizado;
    }

    public static decimal ValidarMonto(decimal? monto)
    {
        if (monto is null || monto.Value <= 0m || monto.Value > MontoMaximo || decimal.Round(monto.Value, 2) != monto.Value)
        {
            throw Error400("El monto debe ser mayor que cero, con 2 decimales como máximo y no superar 9999999999.99.");
        }

        return monto.Value;
    }

    public static async Task<Planilla> ObtenerPlanillaAsync(IUnitOfWork unidad, int id)
    {
        Planilla? planilla = await unidad.Repository<Planilla>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (planilla is null)
        {
            throw PlanillaInexistente(id);
        }

        return planilla;
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

    // Lee los parámetros legales de la tabla ParametrosPlanilla y los tramos de la quinta categoría. Si falta una clave
    // se usa el valor por defecto del seed (hasta que exista ParametrosSistema, módulo 79).
    public static async Task<ParametrosRemuneracion> LeerParametrosAsync(IUnitOfWork unidad)
    {
        Dictionary<string, decimal> valores = await unidad.Repository<ParametroPlanilla>().AsQueryable()
            .AsNoTracking()
            .ToDictionaryAsync(parametro => parametro.Clave, parametro => parametro.Valor);
        ParametrosRemuneracion parametros = new ParametrosRemuneracion
        {
            Uit = Leer(valores, "UIT", 5350m),
            TasaEssalud = Leer(valores, "TASA_ESSALUD", 0.09m),
            HorasJornada = Leer(valores, "HORAS_JORNADA", 8m),
            MinutosRecargo25 = (int)Leer(valores, "MINUTOS_RECARGO_25", 120m),
            RecargoExtra25 = Leer(valores, "RECARGO_EXTRA_25", 0.25m),
            RecargoExtra35 = Leer(valores, "RECARGO_EXTRA_35", 0.35m),
            DeduccionUitQuinta = Leer(valores, "DEDUCCION_UIT_QUINTA", 7m)
        };
        List<TramoQuintaCategoria> tramos = await unidad.Repository<TramoQuintaCategoria>().AsQueryable()
            .AsNoTracking()
            .OrderBy(tramo => tramo.Orden)
            .ToListAsync();
        parametros.Tramos = tramos
            .Select(tramo => new TramoQuintaRemuneracion { HastaUit = tramo.HastaUit, Tasa = tramo.Tasa })
            .ToList();
        if (parametros.Tramos.Count == 0)
        {
            parametros.Tramos = new List<TramoQuintaRemuneracion>
            {
                new TramoQuintaRemuneracion { HastaUit = 5m, Tasa = 0.08m },
                new TramoQuintaRemuneracion { HastaUit = 20m, Tasa = 0.14m },
                new TramoQuintaRemuneracion { HastaUit = 35m, Tasa = 0.17m },
                new TramoQuintaRemuneracion { HastaUit = 45m, Tasa = 0.20m },
                new TramoQuintaRemuneracion { HastaUit = null, Tasa = 0.30m }
            };
        }

        return parametros;
    }

    private static decimal Leer(Dictionary<string, decimal> valores, string clave, decimal porDefecto)
    {
        return valores.TryGetValue(clave, out decimal valor) ? valor : porDefecto;
    }
}
