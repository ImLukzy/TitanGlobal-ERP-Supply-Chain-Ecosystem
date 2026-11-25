using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo68_EvaluacionDesempeno;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo68_EvaluacionDesempeno;

internal static class ValidacionEvaluacion
{
    public const string PeriodoSemestral = "Semestral";

    public const string PeriodoAnual = "Anual";

    public const string CicloConfiguracion = "Configuracion";

    public const string CicloAbierto = "Abierto";

    public const string CicloCerrado = "Cerrado";

    public const string EvaluacionBorrador = "Borrador";

    public const string EvaluacionEnviada = "Enviada";

    public const string EvaluacionCerrada = "Cerrada";

    public const string EmpleadoActivo = "Activo";

    public const string ContratoAnulado = "Anulado";

    public const string IndiceCodigoUnico = "IX_CiclosEvaluacion_Codigo";

    public const string IndiceCompetenciaUnica = "IX_CompetenciasEvaluacion_CicloEvaluacionId_Nombre";

    public const string IndiceEvaluacionUnica = "IX_Evaluaciones_CicloEvaluacionId_EmpleadoId";

    public const int MaximoCodigo = 20;

    public const int MaximoNombre = 100;

    public const int MaximoDescripcion = 300;

    public const int MaximoComentario = 500;

    public const decimal PesoTotal = 100m;

    public static readonly string[] Etiquetas = { "Insuficiente", "En desarrollo", "Competente", "Destacado", "Excepcional" };

    private static readonly string[] Periodos = { PeriodoSemestral, PeriodoAnual };

    private static readonly string[] EstadosCiclo = { CicloConfiguracion, CicloAbierto, CicloCerrado };

    private static readonly string[] EstadosEvaluacion = { EvaluacionBorrador, EvaluacionEnviada, EvaluacionCerrada };

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

    public static DateOnly Hoy()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public static string Fecha(DateOnly fecha)
    {
        return fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static string Decimales(decimal valor)
    {
        return valor.ToString("0.00", CultureInfo.InvariantCulture);
    }

    public static decimal Redondear(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    // D3: la etiqueta se deduce del puntaje final.
    public static string Etiqueta(decimal puntaje)
    {
        if (puntaje < 2m)
        {
            return Etiquetas[0];
        }

        if (puntaje < 3m)
        {
            return Etiquetas[1];
        }

        if (puntaje < 4m)
        {
            return Etiquetas[2];
        }

        if (puntaje < 4.5m)
        {
            return Etiquetas[3];
        }

        return Etiquetas[4];
    }

    // D3: suma de (peso / 100 x calificacion) con 2 decimales.
    public static decimal CalcularPuntaje(IEnumerable<(decimal Peso, int Calificacion)> notas)
    {
        decimal suma = 0m;
        foreach ((decimal peso, int calificacion) in notas)
        {
            suma += peso * calificacion / PesoTotal;
        }

        return Redondear(suma);
    }

    public static ReglaNegocioException CicloInexistente(int id)
    {
        return Error404($"El ciclo de evaluación {id} no existe.");
    }

    public static ReglaNegocioException EmpleadoInexistente(int empleadoId)
    {
        return Error404($"El empleado {empleadoId} no existe.");
    }

    public static ReglaNegocioException EvaluacionInexistente(int id)
    {
        return Error404($"La evaluación {id} no existe.");
    }

    public static ReglaNegocioException CodigoDuplicado(string codigo)
    {
        return Error409($"Ya existe un ciclo con el código {codigo}.");
    }

    public static ReglaNegocioException CicloNoEnConfiguracion(string codigo)
    {
        return Error409($"El ciclo {codigo} ya no está en configuración.");
    }

    public static ReglaNegocioException CompetenciaDuplicada(string nombre)
    {
        return Error409($"Ya existe la competencia {nombre} en el ciclo.");
    }

    public static ReglaNegocioException CicloNoAbierto(string codigo)
    {
        return Error409($"El ciclo {codigo} no está abierto.");
    }

    public static ReglaNegocioException EvaluacionDuplicada(int empleadoId, string codigo)
    {
        return Error409($"El empleado {empleadoId} ya tiene una evaluación en el ciclo {codigo}.");
    }

    public static ReglaNegocioException FiltroInvalido(string nombre)
    {
        return Error400($"El filtro {nombre} no es válido.");
    }

    public static bool EsCodigoDuplicado(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad && excepcion.Restriccion == IndiceCodigoUnico;
    }

    public static bool EsCompetenciaDuplicada(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad && excepcion.Restriccion == IndiceCompetenciaUnica;
    }

    public static bool EsEvaluacionDuplicada(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad && excepcion.Restriccion == IndiceEvaluacionUnica;
    }

    public static void ValidarPagina(int pagina, int tamanoPagina)
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

    public static void ValidarEstadoCiclo(string estado)
    {
        if (estado.Length > 0 && !EstadosCiclo.Contains(estado))
        {
            throw FiltroInvalido("estado");
        }
    }

    public static void ValidarEstadoEvaluacion(string estado)
    {
        if (estado.Length > 0 && !EstadosEvaluacion.Contains(estado))
        {
            throw FiltroInvalido("estado");
        }
    }

    // Reglas 1 a 7 en el orden de la spec. Devuelve el ciclo normalizado (sin guardar).
    public static CicloEvaluacion ValidarCiclo(GuardarCicloEvaluacionRequestDto solicitud)
    {
        string codigo = solicitud.Codigo?.Trim() ?? string.Empty;
        if (codigo.Length == 0 || codigo.Length > MaximoCodigo)
        {
            throw Error400("El código es obligatorio y no puede superar 20 caracteres.");
        }

        string nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        if (nombre.Length == 0 || nombre.Length > MaximoNombre)
        {
            throw Error400("El nombre es obligatorio y no puede superar 100 caracteres.");
        }

        string periodo = solicitud.Periodo?.Trim() ?? string.Empty;
        if (!Periodos.Contains(periodo))
        {
            throw Error400("El periodo debe ser Semestral o Anual.");
        }

        if (solicitud.FechaInicioPeriodo is null || solicitud.FechaFinPeriodo is null
            || solicitud.FechaApertura is null || solicitud.FechaCierre is null)
        {
            throw Error400("Las fechas del periodo, de apertura y de cierre son obligatorias.");
        }

        DateOnly inicio = solicitud.FechaInicioPeriodo.Value;
        DateOnly fin = solicitud.FechaFinPeriodo.Value;
        DateOnly apertura = solicitud.FechaApertura.Value;
        DateOnly cierre = solicitud.FechaCierre.Value;
        int duracion = fin.DayNumber - inicio.DayNumber + 1;
        bool duracionValida = periodo == PeriodoSemestral
            ? duracion >= 170 && duracion <= 190
            : duracion >= 360 && duracion <= 370;
        if (!duracionValida)
        {
            throw Error400($"La duración del periodo no corresponde a un ciclo {periodo}.");
        }

        if (apertura < fin.AddDays(-30))
        {
            throw Error400("La apertura no puede ser anterior a 30 días antes del fin del periodo.");
        }

        if (cierre <= apertura)
        {
            throw Error400("La fecha de cierre debe ser posterior a la de apertura.");
        }

        return new CicloEvaluacion
        {
            Codigo = codigo.ToUpperInvariant(),
            Nombre = nombre,
            Periodo = periodo,
            FechaInicioPeriodo = inicio,
            FechaFinPeriodo = fin,
            FechaApertura = apertura,
            FechaCierre = cierre,
            Estado = CicloConfiguracion
        };
    }

    // Mensajes 2, 34 y 11 en ese orden. Devuelve la competencia normalizada (sin guardar).
    public static CompetenciaEvaluacion ValidarCompetencia(GuardarCompetenciaEvaluacionRequestDto solicitud)
    {
        string nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        if (nombre.Length == 0 || nombre.Length > MaximoNombre)
        {
            throw Error400("El nombre es obligatorio y no puede superar 100 caracteres.");
        }

        string descripcion = solicitud.Descripcion?.Trim() ?? string.Empty;
        if (descripcion.Length > MaximoDescripcion)
        {
            throw Error400("La descripción no puede superar 300 caracteres.");
        }

        decimal? peso = solicitud.Peso;
        if (peso is null || peso.Value <= 0m || peso.Value > PesoTotal || Redondear(peso.Value) != peso.Value)
        {
            throw Error400("El peso debe ser mayor que 0 y como máximo 100, con 2 decimales.");
        }

        return new CompetenciaEvaluacion
        {
            Nombre = nombre,
            Descripcion = descripcion.Length == 0 ? null : descripcion,
            Peso = peso.Value
        };
    }

    public static async Task<CicloEvaluacion> ObtenerCicloAsync(IUnitOfWork unidad, int id)
    {
        CicloEvaluacion? ciclo = await unidad.Repository<CicloEvaluacion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (ciclo is null)
        {
            throw CicloInexistente(id);
        }

        return ciclo;
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

    public static void ExigirCicloAbierto(CicloEvaluacion ciclo)
    {
        if (ciclo.Estado != CicloAbierto)
        {
            throw CicloNoAbierto(ciclo.Codigo);
        }
    }

    // Mensaje 16: se registra y se envía solo entre la apertura y el cierre del ciclo.
    public static void ExigirVentana(CicloEvaluacion ciclo)
    {
        DateOnly hoy = Hoy();
        if (hoy < ciclo.FechaApertura || hoy > ciclo.FechaCierre)
        {
            throw Error409($"El ciclo {ciclo.Codigo} solo admite evaluaciones entre el {Fecha(ciclo.FechaApertura)} y el {Fecha(ciclo.FechaCierre)}.");
        }
    }

    // D5: contrato no anulado con al menos un día dentro del periodo (regla de vigencia del módulo 62).
    public static async Task<bool> TuvoContratoEnPeriodoAsync(IUnitOfWork unidad, int empleadoId, DateOnly inicio, DateOnly fin)
    {
        List<ContratoLaboral> contratos = await unidad.Repository<ContratoLaboral>().AsQueryable()
            .AsNoTracking()
            .Where(contrato => contrato.EmpleadoId == empleadoId
                && contrato.Estado != ContratoAnulado
                && contrato.FechaInicio <= fin)
            .ToListAsync();
        return contratos.Any(contrato => (contrato.FechaCierre ?? contrato.FechaFin ?? DateOnly.MaxValue) >= inicio);
    }

    public static async Task<decimal> SumarPesosAsync(IUnitOfWork unidad, int cicloId)
    {
        List<decimal> pesos = await unidad.Repository<CompetenciaEvaluacion>().AsQueryable()
            .Where(competencia => competencia.CicloEvaluacionId == cicloId)
            .Select(competencia => competencia.Peso)
            .ToListAsync();
        return pesos.Sum();
    }
}
