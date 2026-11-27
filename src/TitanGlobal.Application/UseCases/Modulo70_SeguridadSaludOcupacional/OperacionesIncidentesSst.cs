using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo70_SeguridadSaludOcupacional;

// Lógica compartida por los commands y queries de IncidentesSst.
internal static class OperacionesIncidentesSst
{
    internal static void ExigirAbierto(IncidenteSst incidente)
    {
        if (incidente.Estado != ValidacionSst.EstadoAbierto)
        {
            throw ValidacionSst.IncidenteCerrado(incidente.Numero);
        }
    }
}
