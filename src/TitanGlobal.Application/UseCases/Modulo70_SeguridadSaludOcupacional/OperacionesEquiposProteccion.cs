using TitanGlobal.Application.DTOs.Modulo70_SeguridadSaludOcupacional;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional;

// Lógica compartida por los commands y queries de EquiposProteccion.
internal static class OperacionesEquiposProteccion
{
    internal static EquipoProteccionListadoDto Convertir(EquipoProteccion equipo)
    {
        return new EquipoProteccionListadoDto
        {
            Id = equipo.Id,
            Codigo = equipo.Codigo,
            Nombre = equipo.Nombre,
            VidaUtilDias = equipo.VidaUtilDias,
            Activo = equipo.Activo
        };
    }
}
