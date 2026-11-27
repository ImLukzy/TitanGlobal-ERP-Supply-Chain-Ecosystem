using TitanGlobal.Application.DTOs.Modulo78_NotificacionesAlertas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo78_NotificacionesAlertas;

// Lógica compartida por los commands y queries de Alertas.
internal static class OperacionesAlertas
{
    internal static async Task<List<AlertaDto>> ObtenerAlertasAsync(IUnitOfWork unitOfWork)
    {
        return await GeneradorAlertas.GenerarAsync(unitOfWork);
    }

    internal static string ValidarClave(string? claveEntrada)
    {
        string clave = claveEntrada?.Trim() ?? string.Empty;
        if (clave.Length == 0)
        {
            throw new ReglaNegocioException(400, "La clave de la alerta es obligatoria.");
        }

        if (clave.Length > 100)
        {
            throw new ReglaNegocioException(400, "La clave de la alerta no puede superar 100 caracteres.");
        }

        return clave;
    }
}
