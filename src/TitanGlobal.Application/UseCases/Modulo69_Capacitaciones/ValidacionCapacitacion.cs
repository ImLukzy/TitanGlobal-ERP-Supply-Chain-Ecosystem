using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo69_Capacitaciones;

internal static class ValidacionCapacitacion
{
    public const string TipoInterno = "Interno";

    public const string TipoExterno = "Externo";

    public const string TipoVirtual = "Virtual";

    public const string ProgramacionProgramada = "Programada";

    public const string ProgramacionRealizada = "Realizada";

    public const string ProgramacionCancelada = "Cancelada";

    public const string InscripcionInscrito = "Inscrito";

    public const string InscripcionAprobado = "Aprobado";

    public const string InscripcionDesaprobado = "Desaprobado";

    public const string InscripcionRetirado = "Retirado";

    public const string EmpleadoActivo = "Activo";

    public const string IndiceCodigoCursoUnico = "IX_Cursos_Codigo";

    public const string IndiceInscripcionUnica = "IX_InscripcionesCurso_ProgramacionCursoId_EmpleadoId";

    public const string IndiceCertificacionInscripcionUnica = "IX_CertificacionesCurso_InscripcionCursoId";

    public const string IndiceCertificacionCodigoUnico = "IX_CertificacionesCurso_Codigo";

    public const int MaximoCodigoCurso = 20;

    public const int MaximoNombre = 150;

    public const int MaximoProveedor = 150;

    public const int MaximoLugar = 150;

    public const int MaximoCodigoCertificacion = 40;

    public const decimal MaximoHoras = 500m;

    public const decimal MaximoNota = 20m;

    public const int MinimoCupo = 1;

    public const int MaximoCupo = 500;

    private static readonly string[] Tipos = { TipoInterno, TipoExterno, TipoVirtual };

    private static readonly string[] EstadosProgramacion = { ProgramacionProgramada, ProgramacionRealizada, ProgramacionCancelada };

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

    public static string NombreEmpleado(Empleado empleado)
    {
        return empleado.Apellidos + ", " + empleado.Nombres;
    }

    public static ReglaNegocioException CodigoCursoDuplicado(string codigo)
    {
        return Error409($"Ya existe un curso con el código {codigo}.");
    }

    public static ReglaNegocioException CursoInexistente(int id)
    {
        return Error404($"El curso {id} no existe.");
    }

    public static ReglaNegocioException CursoInactivo(string codigo)
    {
        return Error409($"El curso {codigo} está inactivo y no admite programaciones.");
    }

    public static ReglaNegocioException CursoYaEnEstado(string codigo, bool activo)
    {
        string estado = activo ? "activo" : "inactivo";
        return Error409($"El curso {codigo} ya está {estado}.");
    }

    public static ReglaNegocioException ProgramacionInexistente(int id)
    {
        return Error404($"La programación {id} no existe.");
    }

    public static ReglaNegocioException ProgramacionNoProgramada(int id)
    {
        return Error409($"La programación {id} no está programada.");
    }

    public static ReglaNegocioException EmpleadoInexistente(int empleadoId)
    {
        return Error404($"El empleado {empleadoId} no existe.");
    }

    public static ReglaNegocioException EmpleadoCesado(int empleadoId)
    {
        return Error409($"El empleado {empleadoId} está cesado y no se puede inscribir.");
    }

    public static ReglaNegocioException InscripcionDuplicada(int empleadoId)
    {
        return Error409($"El empleado {empleadoId} ya está inscrito en esta programación.");
    }

    public static ReglaNegocioException SinCupo(int programacionId)
    {
        return Error409($"La programación {programacionId} no tiene cupo libre.");
    }

    public static ReglaNegocioException InscripcionInexistente(int id)
    {
        return Error404($"La inscripción {id} no existe en esta programación.");
    }

    public static ReglaNegocioException CertificacionDuplicada(int inscripcionId)
    {
        return Error409($"La inscripción {inscripcionId} ya tiene certificación.");
    }

    public static ReglaNegocioException CodigoCertificacionDuplicado(string codigo)
    {
        return Error409($"Ya existe una certificación con el código {codigo}.");
    }

    public static ReglaNegocioException FiltroInvalido(string nombre)
    {
        return Error400($"El filtro {nombre} no es válido.");
    }

    public static bool EsCodigoCursoDuplicado(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad && excepcion.Restriccion == IndiceCodigoCursoUnico;
    }

    public static bool EsInscripcionDuplicada(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad && excepcion.Restriccion == IndiceInscripcionUnica;
    }

    public static bool EsCertificacionDuplicada(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad && excepcion.Restriccion == IndiceCertificacionInscripcionUnica;
    }

    public static bool EsCodigoCertificacionDuplicado(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad && excepcion.Restriccion == IndiceCertificacionCodigoUnico;
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

    public static int ResolverAnio(int? anio)
    {
        if (anio is null)
        {
            return Hoy().Year;
        }

        if (anio.Value < 1900 || anio.Value > 2200)
        {
            throw FiltroInvalido("anio");
        }

        return anio.Value;
    }

    public static void ValidarTipoFiltro(string tipo)
    {
        if (tipo.Length > 0 && !Tipos.Contains(tipo))
        {
            throw FiltroInvalido("tipo");
        }
    }

    public static void ValidarEstadoProgramacionFiltro(string estado)
    {
        if (estado.Length > 0 && !EstadosProgramacion.Contains(estado))
        {
            throw FiltroInvalido("estado");
        }
    }

    // Mensajes 1 a 5 en el orden de la spec. Devuelve el curso normalizado (sin guardar).
    public static Curso ValidarCurso(GuardarCursoRequestDto solicitud)
    {
        string codigo = solicitud.Codigo?.Trim() ?? string.Empty;
        if (codigo.Length == 0 || codigo.Length > MaximoCodigoCurso)
        {
            throw Error400("El código es obligatorio y no puede superar 20 caracteres.");
        }

        string nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        if (nombre.Length == 0 || nombre.Length > MaximoNombre)
        {
            throw Error400("El nombre es obligatorio y no puede superar 150 caracteres.");
        }

        decimal? horas = solicitud.Horas;
        if (horas is null || horas.Value <= 0m || horas.Value > MaximoHoras || Math.Round(horas.Value, 1) != horas.Value)
        {
            throw Error400("Las horas deben ser mayores que 0 y como máximo 500, con un decimal.");
        }

        string tipo = solicitud.Tipo?.Trim() ?? string.Empty;
        if (!Tipos.Contains(tipo))
        {
            throw Error400("El tipo debe ser Interno, Externo o Virtual.");
        }

        string proveedor = solicitud.Proveedor?.Trim() ?? string.Empty;
        if (proveedor.Length > MaximoProveedor)
        {
            throw Error400("El proveedor no puede superar 150 caracteres.");
        }

        return new Curso
        {
            Codigo = codigo.ToUpperInvariant(),
            Nombre = nombre,
            Horas = horas.Value,
            Tipo = tipo,
            Proveedor = proveedor.Length == 0 ? null : proveedor,
            Activo = true
        };
    }

    // Mensajes 10 a 13 en el orden de la spec. Devuelve la programación normalizada (sin guardar y sin curso).
    public static ProgramacionCurso ValidarProgramacion(GuardarProgramacionRequestDto solicitud)
    {
        if (solicitud.FechaInicio is null || solicitud.FechaFin is null)
        {
            throw Error400("Las fechas de inicio y fin son obligatorias.");
        }

        DateOnly inicio = solicitud.FechaInicio.Value;
        DateOnly fin = solicitud.FechaFin.Value;
        if (fin < inicio)
        {
            throw Error400("La fecha de fin no puede ser anterior a la de inicio.");
        }

        int? cupo = solicitud.Cupo;
        if (cupo is null || cupo.Value < MinimoCupo || cupo.Value > MaximoCupo)
        {
            throw Error400("El cupo debe estar entre 1 y 500.");
        }

        string lugar = solicitud.Lugar?.Trim() ?? string.Empty;
        if (lugar.Length > MaximoLugar)
        {
            throw Error400("El lugar no puede superar 150 caracteres.");
        }

        return new ProgramacionCurso
        {
            Anio = inicio.Year,
            FechaInicio = inicio,
            FechaFin = fin,
            Cupo = cupo.Value,
            Lugar = lugar.Length == 0 ? null : lugar,
            Estado = ProgramacionProgramada
        };
    }

    public static void ValidarNota(decimal? nota)
    {
        if (nota is not null && (nota.Value < 0m || nota.Value > MaximoNota || Math.Round(nota.Value, 1) != nota.Value))
        {
            throw Error400("La nota debe estar entre 0 y 20, con un decimal.");
        }
    }

    public static async Task<Curso> ObtenerCursoAsync(IUnitOfWork unidad, int id)
    {
        Curso? curso = await unidad.Repository<Curso>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (curso is null)
        {
            throw CursoInexistente(id);
        }

        return curso;
    }

    public static async Task<ProgramacionCurso> ObtenerProgramacionAsync(IUnitOfWork unidad, int id)
    {
        ProgramacionCurso? programacion = await unidad.Repository<ProgramacionCurso>().AsQueryable()
            .Include(entidad => entidad.Curso)
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (programacion is null)
        {
            throw ProgramacionInexistente(id);
        }

        return programacion;
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

    public static void ExigirProgramada(ProgramacionCurso programacion)
    {
        if (programacion.Estado != ProgramacionProgramada)
        {
            throw ProgramacionNoProgramada(programacion.Id);
        }
    }

    public static async Task<int> ContarInscritosAsync(IUnitOfWork unidad, int programacionId)
    {
        return await unidad.Repository<InscripcionCurso>().AsQueryable()
            .CountAsync(inscripcion => inscripcion.ProgramacionCursoId == programacionId
                && inscripcion.Estado != InscripcionRetirado);
    }
}
