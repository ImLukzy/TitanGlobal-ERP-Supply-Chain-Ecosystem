using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo65_VacacionesPermisos;

// Lógica compartida por los commands y queries de Ausencias.
internal static class OperacionesAusencias
{
    internal static void Decidir(Ausencia ausencia, string usuario)
    {
        string nombre = usuario.Length > ValidacionAusencia.MaximoDecididoPor
            ? usuario.Substring(0, ValidacionAusencia.MaximoDecididoPor)
            : usuario;
        ausencia.DecididoPor = nombre;
        ausencia.FechaDecision = DateTime.UtcNow;
    }
}
