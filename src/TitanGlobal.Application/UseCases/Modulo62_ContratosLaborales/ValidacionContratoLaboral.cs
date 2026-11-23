using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo62_ContratosLaborales;

internal static class ValidacionContratoLaboral
{
    public const string EstadoVigente = "Vigente";

    public const string EstadoRenovado = "Renovado";

    public const string EstadoVencido = "Vencido";

    public const string EstadoCerrado = "Cerrado";

    public const string EstadoAnulado = "Anulado";

    public const string ModalidadIndeterminado = "Indeterminado";

    public const string ModalidadPlazoFijo = "PlazoFijo";

    public const string ModalidadFormativo = "Formativo";

    public const string IndiceVigenteUnico = "IX_ContratosLaborales_EmpleadoId_Vigente";

    public const string IndiceAdendaFecha = "IX_AdendasContrato_ContratoLaboralId_FechaVigencia";

    public const string ClaveForaneaEmpleado = "FK_ContratosLaborales_Empleados_EmpleadoId";

    private const decimal SueldoMaximo = 9999999999.99m;

    private static readonly string[] Modalidades = { ModalidadIndeterminado, ModalidadPlazoFijo, ModalidadFormativo };

    private static readonly string[] Estados = { EstadoVigente, EstadoRenovado, EstadoVencido, EstadoCerrado, EstadoAnulado };

    public static string ValidarModalidad(string? modalidad)
    {
        string normalizada = modalidad?.Trim() ?? string.Empty;
        if (!Modalidades.Contains(normalizada))
        {
            throw Error400("La modalidad no es válida.");
        }

        return normalizada;
    }

    public static decimal ValidarSueldo(decimal? sueldo)
    {
        if (sueldo is null)
        {
            throw Error400("El sueldo básico es obligatorio.");
        }

        return ValidarSueldoPresente(sueldo.Value);
    }

    public static decimal ValidarSueldoPresente(decimal sueldo)
    {
        if (sueldo <= 0)
        {
            throw Error400("El sueldo básico debe ser mayor que cero.");
        }

        if (sueldo > SueldoMaximo)
        {
            throw Error400("El sueldo básico no puede superar 9999999999.99.");
        }

        if (decimal.Round(sueldo, 2) != sueldo)
        {
            throw Error400("El sueldo básico admite como máximo 2 decimales.");
        }

        return sueldo;
    }

    public static string ValidarCargo(string? cargo)
    {
        string normalizado = cargo?.Trim() ?? string.Empty;
        if (normalizado.Length == 0)
        {
            throw Error400("El cargo es obligatorio.");
        }

        return ValidarLongitudCargo(normalizado);
    }

    public static string ValidarLongitudCargo(string cargo)
    {
        if (cargo.Length > 100)
        {
            throw Error400("El cargo no puede superar 100 caracteres.");
        }

        return cargo;
    }

    // Reglas 10 y 11 (C1): la fecha de fin depende de la modalidad.
    public static void ValidarPresenciaFechaFin(string modalidad, DateOnly? fechaFin)
    {
        if (modalidad == ModalidadIndeterminado && fechaFin is not null)
        {
            throw Error400("El contrato indeterminado no tiene fecha de fin.");
        }

        if (modalidad != ModalidadIndeterminado && fechaFin is null)
        {
            throw Error400("La fecha de fin es obligatoria para esta modalidad.");
        }
    }

    // Regla 12 (C3): la fecha de fin debe ser posterior a la de inicio.
    public static void ValidarOrdenFechas(DateOnly fechaInicio, DateOnly? fechaFin)
    {
        if (fechaFin is not null && fechaFin.Value <= fechaInicio)
        {
            throw Error400("La fecha de fin debe ser posterior a la fecha de inicio.");
        }
    }

    public static string ValidarMotivoAnulacion(string? motivo)
    {
        string normalizado = motivo?.Trim() ?? string.Empty;
        if (normalizado.Length == 0)
        {
            throw Error400("El motivo de anulación es obligatorio.");
        }

        if (normalizado.Length > 300)
        {
            throw Error400("El motivo de anulación no puede superar 300 caracteres.");
        }

        return normalizado;
    }

    public static string ValidarListado(int pagina, int tamanoPagina, string estado, string modalidad)
    {
        if (pagina < 1)
        {
            throw Error400("La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw Error400("El tamaño de página debe estar entre 1 y 100.");
        }

        if (estado.Length > 0 && !Estados.Contains(estado))
        {
            throw Error400("El estado debe ser Vigente, Renovado, Vencido, Cerrado o Anulado.");
        }

        if (modalidad.Length > 0 && !Modalidades.Contains(modalidad))
        {
            throw Error400("La modalidad debe ser Indeterminado, PlazoFijo o Formativo.");
        }

        return estado;
    }

    public static bool EsConflictoVigente(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad
            && excepcion.Restriccion == IndiceVigenteUnico;
    }

    public static bool EsEmpleadoInexistente(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea
            && excepcion.Restriccion == ClaveForaneaEmpleado;
    }

    public static bool EsConflictoAdenda(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad
            && excepcion.Restriccion == IndiceAdendaFecha;
    }

    public static ReglaNegocioException ConflictoVigente(int empleadoId)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"El empleado {empleadoId} ya tiene un contrato vigente.");
    }

    public static ReglaNegocioException EmpleadoInexistente(int empleadoId)
    {
        return new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El empleado {empleadoId} no existe.");
    }

    public static ReglaNegocioException EmpleadoCesado(int empleadoId)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"El empleado {empleadoId} está cesado y no admite contratos nuevos.");
    }

    public static ReglaNegocioException ConflictoAdenda()
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            "Ya existe una adenda con fecha de vigencia igual o posterior.");
    }

    public static async Task<ContratoLaboral> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        ContratoLaboral? contrato = await unidad.Repository<ContratoLaboral>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (contrato is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El contrato laboral {id} no existe.");
        }

        return contrato;
    }

    public static string NuevoNumeroTemporal()
    {
        return $"TMP-{Guid.NewGuid().ToString("N").Substring(0, 16)}";
    }

    public static string NumeroDefinitivo(int id)
    {
        return $"CTR-{id:D6}";
    }

    public static DateOnly Hoy()
    {
        return DateOnly.FromDateTime(DateTime.UtcNow);
    }

    private static ReglaNegocioException Error400(string mensaje)
    {
        return new ReglaNegocioException(CodigosEstado.Status400BadRequest, mensaje);
    }
}
