using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo59_CentrosCosto;
using TitanGlobal.Application.Exceptions;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo59_CentrosCosto;

internal static class ValidacionCentroCosto
{
    internal const string DatosInvalidos = "El código debe tener entre 2 y 20 caracteres y los datos del centro deben ser válidos.";
    internal const string CentroInexistente = "El centro de costo no existe.";
    private const string CodigoDuplicado = "Ya existe un centro de costo con ese código.";
    private const string JerarquiaInvalida = "La jerarquía de centros debe evitar ciclos y tener como máximo tres niveles.";
    private const int NivelMaximo = 3;

    internal static async Task AplicarDatosAsync(IUnitOfWork unitOfWork, CentroCosto centro, GuardarCentroCostoRequestDto solicitud)
    {
        string codigo = solicitud.Codigo?.Trim().ToUpperInvariant() ?? string.Empty;
        string nombre = solicitud.Nombre?.Trim() ?? string.Empty;
        string? descripcion = solicitud.Descripcion?.Trim();
        if (!TextoValido(codigo, 2, 20) || !TextoValido(nombre, 1, 120)
            || !TextoValido(descripcion ?? string.Empty, 0, 300)
            || solicitud.ResponsableEmpleadoId <= 0 || solicitud.CentroCostoPadreId <= 0)
        {
            throw new ReglaNegocioException(CodigosEstado.Status400BadRequest, DatosInvalidos);
        }

        IQueryable<CentroCosto> consulta = unitOfWork.Repository<CentroCosto>().AsQueryable().AsNoTracking();
        if (await consulta.AnyAsync(otro => otro.Codigo == codigo && otro.Id != centro.Id))
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, CodigoDuplicado);
        }

        if (solicitud.ResponsableEmpleadoId.HasValue
            && !await unitOfWork.Repository<Empleado>().AsQueryable().AnyAsync(empleado => empleado.Id == solicitud.ResponsableEmpleadoId))
        {
            throw new ReglaNegocioException(CodigosEstado.Status404NotFound, "El empleado responsable no existe.");
        }

        bool cambiaPadre = centro.CentroCostoPadreId != solicitud.CentroCostoPadreId;
        int nivel = await ValidarAncestrosAsync(consulta, centro.Id, solicitud.CentroCostoPadreId, centro.Activo || cambiaPadre);
        if (cambiaPadre && centro.Id != 0)
        {
            await ValidarDescendientesAsync(consulta, centro.Id, nivel);
        }

        centro.Codigo = codigo;
        centro.Nombre = nombre;
        centro.Descripcion = string.IsNullOrEmpty(descripcion) ? null : descripcion;
        centro.ResponsableEmpleadoId = solicitud.ResponsableEmpleadoId;
        centro.CentroCostoPadreId = solicitud.CentroCostoPadreId;
    }

    internal static async Task<int> ValidarAncestrosAsync(
        IQueryable<CentroCosto> consulta, int id, int? padreId, bool exigirPadreActivo)
    {
        int nivel = 1;
        while (padreId.HasValue)
        {
            if (padreId == id || nivel == NivelMaximo)
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, JerarquiaInvalida);
            }

            CentroCosto? padre = await consulta.FirstOrDefaultAsync(centro => centro.Id == padreId);
            if (padre == null)
            {
                throw new ReglaNegocioException(CodigosEstado.Status404NotFound, "El centro de costo padre no existe.");
            }

            if (nivel == 1 && exigirPadreActivo && !padre.Activo)
            {
                throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "El centro de costo padre debe estar activo.");
            }

            padreId = padre.CentroCostoPadreId;
            nivel++;
        }

        return nivel;
    }

    internal static async Task GuardarAsync(IUnitOfWork unitOfWork)
    {
        try
        {
            await unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.Unicidad
            && excepcion.Restriccion == "IX_CentrosCosto_Codigo")
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, CodigoDuplicado);
        }
        catch (ViolacionRestriccionException excepcion) when (excepcion.Tipo == TipoViolacionRestriccion.ReferenciaForanea
            && excepcion.Restriccion is "FK_CentrosCosto_Empleados" or "FK_CentrosCosto_CentroCostoPadre")
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, "No se pudo guardar el centro de costo porque una referencia ya no existe.");
        }
    }

    private static bool TextoValido(string texto, int minimo, int maximo)
    {
        int longitud = texto.EnumerateRunes().Count();
        return longitud >= minimo && longitud <= maximo && !texto.Contains('\0');
    }

    private static async Task ValidarDescendientesAsync(IQueryable<CentroCosto> consulta, int id, int nivel)
    {
        if (nivel == 1)
        {
            return;
        }

        IQueryable<int> hijos = consulta.Where(centro => centro.CentroCostoPadreId == id).Select(centro => centro.Id);
        bool superaNivelMaximo = nivel == NivelMaximo
            ? await hijos.AnyAsync()
            : await consulta.AnyAsync(centro => centro.CentroCostoPadreId.HasValue && hijos.Contains(centro.CentroCostoPadreId.Value));

        if (superaNivelMaximo)
        {
            throw new ReglaNegocioException(CodigosEstado.Status409Conflict, JerarquiaInvalida);
        }
    }
}
