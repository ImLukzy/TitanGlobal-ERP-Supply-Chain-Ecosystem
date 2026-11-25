using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional;

internal static class ValidacionSst
{
    public const string EmpleadoActivo = "Activo";

    public const string ExamenIngreso = "Ingreso";

    public const string ExamenPeriodico = "Periodico";

    public const string ExamenRetiro = "Retiro";

    public const string ResultadoApto = "Apto";

    public const string ResultadoAptoConRestricciones = "AptoConRestricciones";

    public const string ResultadoNoApto = "NoApto";

    public const string IncidenteAccidenteSinBaja = "AccidenteSinBaja";

    public const string IncidenteAccidenteConBaja = "AccidenteConBaja";

    public const string IncidenteSinLesion = "IncidenteSinLesion";

    public const string IncidenteEnfermedadOcupacional = "EnfermedadOcupacional";

    public const string SeveridadLeve = "Leve";

    public const string SeveridadModerada = "Moderada";

    public const string SeveridadGrave = "Grave";

    public const string SeveridadMortal = "Mortal";

    public const string EstadoAbierto = "Abierto";

    public const string EstadoCerrado = "Cerrado";

    public const string IndiceCodigoEquipoUnico = "IX_EquiposProteccion_Codigo";

    public const int MaximoCodigo = 20;

    public const int MaximoNombre = 100;

    public const int MinimoVidaUtil = 1;

    public const int MaximoVidaUtil = 3650;

    public const int MinimoCantidad = 1;

    public const int MaximoCantidad = 100;

    public const int MaximoRestricciones = 300;

    public const int MaximoClinica = 150;

    public const int MaximoLugar = 150;

    public const int MaximoDescripcionIncidente = 1000;

    public const int MaximoDescripcionAccion = 500;

    public const int MaximoResponsable = 150;

    public const int DiasRetiroTrasCese = 30;

    private static readonly string[] TiposExamen = { ExamenIngreso, ExamenPeriodico, ExamenRetiro };

    private static readonly string[] Resultados = { ResultadoApto, ResultadoAptoConRestricciones, ResultadoNoApto };

    private static readonly string[] TiposIncidente =
    {
        IncidenteAccidenteSinBaja,
        IncidenteAccidenteConBaja,
        IncidenteSinLesion,
        IncidenteEnfermedadOcupacional
    };

    private static readonly string[] Severidades = { SeveridadLeve, SeveridadModerada, SeveridadGrave, SeveridadMortal };

    private static readonly string[] Estados = { EstadoAbierto, EstadoCerrado };

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

    public static string NuevoNumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    public static string NumeroDefinitivo(int id)
    {
        return $"INC-{id:D6}";
    }

    public static string NombreEmpleado(Empleado empleado)
    {
        return empleado.Apellidos + ", " + empleado.Nombres;
    }

    public static ReglaNegocioException CodigoEquipoDuplicado(string codigo)
    {
        return Error409($"Ya existe un equipo de protección con el código {codigo}.");
    }

    public static ReglaNegocioException EquipoInexistente(int id)
    {
        return Error404($"El equipo de protección {id} no existe.");
    }

    public static ReglaNegocioException EquipoYaEnEstado(string codigo, bool activo)
    {
        string estado = activo ? "activo" : "inactivo";
        return Error409($"El equipo de protección {codigo} ya está {estado}.");
    }

    public static ReglaNegocioException EquipoInactivo(string codigo)
    {
        return Error409($"El equipo de protección {codigo} está inactivo y no se entrega.");
    }

    public static ReglaNegocioException EmpleadoObligatorio()
    {
        return Error400("El empleado es obligatorio.");
    }

    public static ReglaNegocioException EmpleadoInexistente(int empleadoId)
    {
        return Error404($"El empleado {empleadoId} no existe.");
    }

    public static ReglaNegocioException EmpleadoCesado(int empleadoId)
    {
        return Error409($"El empleado {empleadoId} está cesado.");
    }

    public static ReglaNegocioException FechaInvalida()
    {
        return Error400("La fecha es obligatoria y no puede ser posterior a hoy.");
    }

    public static ReglaNegocioException IncidenteInexistente(int id)
    {
        return Error404($"El incidente {id} no existe.");
    }

    public static ReglaNegocioException IncidenteCerrado(string numero)
    {
        return Error409($"El incidente {numero} está cerrado y no admite cambios.");
    }

    public static ReglaNegocioException AccionInexistente(int id)
    {
        return Error404($"La acción correctiva {id} no existe en el incidente.");
    }

    public static ReglaNegocioException AccionYaCompletada(int id)
    {
        return Error409($"La acción correctiva {id} ya está completada.");
    }

    public static ReglaNegocioException FiltroInvalido(string nombre)
    {
        return Error400($"El filtro {nombre} no es válido.");
    }

    public static bool EsCodigoEquipoDuplicado(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad && excepcion.Restriccion == IndiceCodigoEquipoUnico;
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

    public static void ValidarTipoExamenFiltro(string tipo)
    {
        if (tipo.Length > 0 && !TiposExamen.Contains(tipo))
        {
            throw FiltroInvalido("tipo");
        }
    }

    public static void ValidarResultadoFiltro(string resultado)
    {
        if (resultado.Length > 0 && !Resultados.Contains(resultado))
        {
            throw FiltroInvalido("resultado");
        }
    }

    public static void ValidarFiltrosIncidente(string tipo, string severidad, string estado, DateOnly? desde, DateOnly? hasta)
    {
        if (tipo.Length > 0 && !TiposIncidente.Contains(tipo))
        {
            throw FiltroInvalido("tipo");
        }

        if (severidad.Length > 0 && !Severidades.Contains(severidad))
        {
            throw FiltroInvalido("severidad");
        }

        if (estado.Length > 0 && !Estados.Contains(estado))
        {
            throw FiltroInvalido("estado");
        }

        if (desde is not null && hasta is not null && desde.Value > hasta.Value)
        {
            throw FiltroInvalido("hasta");
        }
    }

    // Mensajes 1 a 3 en el orden de la spec. Devuelve el equipo normalizado (sin guardar).
    public static EquipoProteccion ValidarEquipo(GuardarEquipoProteccionRequestDto solicitud)
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

        int? vidaUtil = solicitud.VidaUtilDias;
        if (vidaUtil is null || vidaUtil.Value < MinimoVidaUtil || vidaUtil.Value > MaximoVidaUtil)
        {
            throw Error400("La vida útil debe estar entre 1 y 3650 días.");
        }

        return new EquipoProteccion
        {
            Codigo = codigo.ToUpperInvariant(),
            Nombre = nombre,
            VidaUtilDias = vidaUtil.Value,
            Activo = true
        };
    }

    // Mensajes 8, 11 y 12 en el orden de la spec.
    public static void ValidarEntrega(RegistrarEntregaEppRequestDto solicitud)
    {
        if (solicitud.EmpleadoId is null)
        {
            throw EmpleadoObligatorio();
        }

        ValidarFechaNoFutura(solicitud.FechaEntrega);
        int? cantidad = solicitud.Cantidad;
        if (cantidad is null || cantidad.Value < MinimoCantidad || cantidad.Value > MaximoCantidad)
        {
            throw Error400("La cantidad debe estar entre 1 y 100.");
        }
    }

    public static DateOnly ValidarFechaNoFutura(DateOnly? fecha)
    {
        if (fecha is null || fecha.Value > Hoy())
        {
            throw FechaInvalida();
        }

        return fecha.Value;
    }

    // Mensajes 8, 11, 13 a 17 y 19 en el orden de la spec. Devuelve el examen normalizado (sin guardar).
    public static ExamenMedico ValidarExamen(RegistrarExamenMedicoRequestDto solicitud)
    {
        if (solicitud.EmpleadoId is null)
        {
            throw EmpleadoObligatorio();
        }

        DateOnly fecha = ValidarFechaNoFutura(solicitud.Fecha);
        string tipo = solicitud.Tipo?.Trim() ?? string.Empty;
        if (!TiposExamen.Contains(tipo))
        {
            throw Error400("El tipo de examen debe ser Ingreso, Periodico o Retiro.");
        }

        string resultado = solicitud.Resultado?.Trim() ?? string.Empty;
        if (!Resultados.Contains(resultado))
        {
            throw Error400("El resultado debe ser Apto, AptoConRestricciones o NoApto.");
        }

        string restricciones = solicitud.Restricciones?.Trim() ?? string.Empty;
        if (restricciones.Length > MaximoRestricciones
            || (resultado == ResultadoAptoConRestricciones && restricciones.Length == 0))
        {
            throw Error400("Las restricciones son obligatorias si el resultado es AptoConRestricciones y no pueden superar 300 caracteres.");
        }

        DateOnly? vencimiento = solicitud.FechaVencimiento;
        if (tipo == ExamenRetiro)
        {
            if (vencimiento is not null)
            {
                throw Error400("El examen de retiro no tiene fecha de vencimiento.");
            }
        }
        else if (vencimiento is null || vencimiento.Value <= fecha)
        {
            throw Error400("La fecha de vencimiento es obligatoria y posterior al examen en los exámenes de ingreso y periódicos.");
        }

        string clinica = solicitud.Clinica?.Trim() ?? string.Empty;
        if (clinica.Length > MaximoClinica)
        {
            throw Error400("La clínica no puede superar 150 caracteres.");
        }

        return new ExamenMedico
        {
            EmpleadoId = solicitud.EmpleadoId.Value,
            Tipo = tipo,
            Fecha = fecha,
            Resultado = resultado,
            Restricciones = restricciones.Length == 0 ? null : restricciones,
            FechaVencimiento = vencimiento,
            Clinica = clinica.Length == 0 ? null : clinica
        };
    }

    // Mensajes 8, 11 y 20 a 25 en el orden de la spec. Devuelve el incidente normalizado (sin guardar).
    public static IncidenteSst ValidarIncidente(RegistrarIncidenteSstRequestDto solicitud)
    {
        if (solicitud.EmpleadoId is null)
        {
            throw EmpleadoObligatorio();
        }

        string tipo = solicitud.Tipo?.Trim() ?? string.Empty;
        if (!TiposIncidente.Contains(tipo))
        {
            throw Error400("El tipo de incidente debe ser AccidenteSinBaja, AccidenteConBaja, IncidenteSinLesion o EnfermedadOcupacional.");
        }

        string severidad = solicitud.Severidad?.Trim() ?? string.Empty;
        if (!Severidades.Contains(severidad))
        {
            throw Error400("La severidad debe ser Leve, Moderada, Grave o Mortal.");
        }

        DateOnly fecha = ValidarFechaNoFutura(solicitud.Fecha);
        int diasBaja = solicitud.DiasBaja ?? 0;
        if (tipo == IncidenteAccidenteConBaja && diasBaja < 1)
        {
            throw Error400("Un accidente con baja exige al menos 1 día de baja.");
        }

        if (tipo != IncidenteAccidenteConBaja && diasBaja != 0)
        {
            throw Error400("Solo un accidente con baja tiene días de baja.");
        }

        string descripcion = solicitud.Descripcion?.Trim() ?? string.Empty;
        if (descripcion.Length == 0 || descripcion.Length > MaximoDescripcionIncidente)
        {
            throw Error400("La descripción es obligatoria y no puede superar 1000 caracteres.");
        }

        string lugar = solicitud.Lugar?.Trim() ?? string.Empty;
        if (lugar.Length > MaximoLugar)
        {
            throw Error400("El lugar no puede superar 150 caracteres.");
        }

        return new IncidenteSst
        {
            EmpleadoId = solicitud.EmpleadoId.Value,
            Tipo = tipo,
            Severidad = severidad,
            Fecha = fecha,
            Lugar = lugar.Length == 0 ? null : lugar,
            Descripcion = descripcion,
            DiasBaja = diasBaja,
            Estado = EstadoAbierto
        };
    }

    // Mensaje 28. Devuelve la acción normalizada (sin guardar y sin incidente).
    public static AccionCorrectivaSst ValidarAccion(GuardarAccionCorrectivaRequestDto solicitud)
    {
        string descripcion = solicitud.Descripcion?.Trim() ?? string.Empty;
        string responsable = solicitud.Responsable?.Trim() ?? string.Empty;
        if (descripcion.Length == 0
            || descripcion.Length > MaximoDescripcionAccion
            || responsable.Length == 0
            || responsable.Length > MaximoResponsable
            || solicitud.FechaCompromiso is null)
        {
            throw Error400("La acción correctiva necesita descripción (máximo 500), responsable (máximo 150) y fecha de compromiso.");
        }

        return new AccionCorrectivaSst
        {
            Descripcion = descripcion,
            Responsable = responsable,
            FechaCompromiso = solicitud.FechaCompromiso.Value,
            Completada = false
        };
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

    public static async Task<EquipoProteccion> ObtenerEquipoAsync(IUnitOfWork unidad, int id)
    {
        EquipoProteccion? equipo = await unidad.Repository<EquipoProteccion>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (equipo is null)
        {
            throw EquipoInexistente(id);
        }

        return equipo;
    }

    public static async Task<IncidenteSst> ObtenerIncidenteAsync(IUnitOfWork unidad, int id)
    {
        IncidenteSst? incidente = await unidad.Repository<IncidenteSst>().AsQueryable()
            .Include(entidad => entidad.Empleado)
            .Include(entidad => entidad.AccionesCorrectivasSst)
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (incidente is null)
        {
            throw IncidenteInexistente(id);
        }

        return incidente;
    }
}
