using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo61_Empleados;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo61_Empleados;

internal static class ValidacionEmpleado
{
    public const string EstadoActivo = "Activo";

    public const string EstadoCesado = "Cesado";

    private const int MayoriaEdad = 18;

    private const string IndiceDocumentoUnico = "IX_Empleados_TipoDocumento_NumeroDocumento";

    private static readonly string[] TiposDocumento = { "DNI", "CE", "Pasaporte" };

    private static readonly Regex FormatoNumeroDocumento = new Regex(
        "^[A-Za-z0-9]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FormatoEmail = new Regex(
        "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static DatosEmpleado Normalizar(GuardarEmpleadoRequestDto solicitud)
    {
        string tipoDocumento = solicitud.TipoDocumento?.Trim() ?? string.Empty;
        if (!TiposDocumento.Contains(tipoDocumento))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El tipo de documento no es válido.");
        }

        string numeroDocumento = solicitud.NumeroDocumento?.Trim() ?? string.Empty;
        if (numeroDocumento.Length == 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El número de documento es obligatorio.");
        }

        if (numeroDocumento.Length > 20)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El número de documento no puede superar 20 caracteres.");
        }

        if (!FormatoNumeroDocumento.IsMatch(numeroDocumento))
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El número de documento solo admite letras y números.");
        }

        string nombres = solicitud.Nombres?.Trim() ?? string.Empty;
        if (nombres.Length == 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "Los nombres son obligatorios.");
        }

        if (nombres.Length > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "Los nombres no pueden superar 100 caracteres.");
        }

        string apellidos = solicitud.Apellidos?.Trim() ?? string.Empty;
        if (apellidos.Length == 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "Los apellidos son obligatorios.");
        }

        if (apellidos.Length > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Los apellidos no pueden superar 100 caracteres.");
        }

        if (solicitud.FechaNacimiento is not DateOnly fechaNacimiento)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La fecha de nacimiento es obligatoria.");
        }

        if (solicitud.FechaIngreso is not DateOnly fechaIngreso)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La fecha de ingreso es obligatoria.");
        }

        string cargo = solicitud.Cargo?.Trim() ?? string.Empty;
        if (cargo.Length == 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El cargo es obligatorio.");
        }

        if (cargo.Length > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El cargo no puede superar 100 caracteres.");
        }

        string area = solicitud.Area?.Trim() ?? string.Empty;
        if (area.Length == 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El área es obligatoria.");
        }

        if (area.Length > 100)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El área no puede superar 100 caracteres.");
        }

        string? email = solicitud.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email))
        {
            email = null;
        }
        else if (email.Length > 150 || !FormatoEmail.IsMatch(email))
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El email no tiene un formato válido.");
        }

        string? telefono = solicitud.Telefono?.Trim();
        if (string.IsNullOrEmpty(telefono))
        {
            telefono = null;
        }
        else if (telefono.Length > 20)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El teléfono no puede superar 20 caracteres.");
        }

        return new DatosEmpleado
        {
            TipoDocumento = tipoDocumento,
            NumeroDocumento = numeroDocumento,
            Nombres = nombres,
            Apellidos = apellidos,
            FechaNacimiento = fechaNacimiento,
            FechaIngreso = fechaIngreso,
            Cargo = cargo,
            Area = area,
            Email = email,
            Telefono = telefono
        };
    }

    public static void ValidarEdadMinima(DateOnly fechaNacimiento, DateOnly fechaIngreso)
    {
        // Si el año de mayoría excede 9999, DateOnly.AddYears lanzaría excepción; en ese caso el ingreso nunca alcanza la mayoría.
        bool esMenor = fechaNacimiento.Year + MayoriaEdad > 9999
            || fechaIngreso < fechaNacimiento.AddYears(MayoriaEdad);
        if (esMenor)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El empleado debe tener al menos 18 años a la fecha de ingreso.");
        }
    }

    public static async Task ValidarDocumentoUnicoAsync(
        IUnitOfWork unidad,
        string tipoDocumento,
        string numeroDocumento,
        int? excluirId)
    {
        bool existe = await unidad.Repository<Empleado>().AsQueryable()
            .AnyAsync(empleado => empleado.TipoDocumento == tipoDocumento
                && empleado.NumeroDocumento == numeroDocumento
                && (!excluirId.HasValue || empleado.Id != excluirId.Value));
        if (existe)
        {
            throw CrearConflictoDocumento(tipoDocumento, numeroDocumento);
        }
    }

    public static bool EsDuplicadoDocumento(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad
            && excepcion.Restriccion == IndiceDocumentoUnico;
    }

    public static ReglaNegocioException CrearConflictoDocumento(string tipoDocumento, string numeroDocumento)
    {
        return new ReglaNegocioException(
            CodigosEstado.Status409Conflict,
            $"Ya existe un empleado con el documento {tipoDocumento} {numeroDocumento}.");
    }

    public static async Task<Empleado> ObtenerAsync(IUnitOfWork unidad, int id)
    {
        Empleado? empleado = await unidad.Repository<Empleado>().AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == id);
        if (empleado is null)
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, $"El empleado {id} no existe.");
        }

        return empleado;
    }

    public static void ValidarListado(int pagina, int tamanoPagina, string estado)
    {
        if (pagina < 1)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La página debe ser mayor o igual a 1.");
        }

        if (tamanoPagina < 1 || tamanoPagina > 100)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El tamaño de página debe estar entre 1 y 100.");
        }

        if (estado.Length > 0 && estado != EstadoActivo && estado != EstadoCesado)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El estado debe ser Activo o Cesado.");
        }
    }

    public static void ValidarEditable(Empleado empleado)
    {
        if (empleado.Estado == EstadoCesado)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se puede editar un empleado cesado.");
        }
    }

    public static void ValidarCesable(Empleado empleado)
    {
        if (empleado.Estado == EstadoCesado)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status409Conflict,
                $"El empleado {empleado.Id} ya está cesado.");
        }
    }

    public static DateOnly ObtenerFechaCese(DateOnly? fechaCese)
    {
        if (fechaCese is not DateOnly fecha)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "La fecha de cese es obligatoria.");
        }

        return fecha;
    }

    public static string NormalizarMotivoCese(string? motivo)
    {
        string motivoNormalizado = motivo?.Trim() ?? string.Empty;
        if (motivoNormalizado.Length == 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, "El motivo de cese es obligatorio.");
        }

        if (motivoNormalizado.Length > 300)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "El motivo de cese no puede superar 300 caracteres.");
        }

        return motivoNormalizado;
    }

    public static void ValidarFechaCese(DateOnly fechaCese, DateOnly fechaIngreso)
    {
        if (fechaCese < fechaIngreso)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "La fecha de cese no puede ser anterior a la fecha de ingreso.");
        }
    }

    internal sealed class DatosEmpleado
    {
        public string TipoDocumento { get; set; } = string.Empty;

        public string NumeroDocumento { get; set; } = string.Empty;

        public string Nombres { get; set; } = string.Empty;

        public string Apellidos { get; set; } = string.Empty;

        public DateOnly FechaNacimiento { get; set; }

        public DateOnly FechaIngreso { get; set; }

        public string Cargo { get; set; } = string.Empty;

        public string Area { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Telefono { get; set; }
    }
}
