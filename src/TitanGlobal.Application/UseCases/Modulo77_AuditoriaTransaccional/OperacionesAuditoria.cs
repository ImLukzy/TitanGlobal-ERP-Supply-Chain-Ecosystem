using TitanGlobal.Application.DTOs.Modulo77_AuditoriaTransaccional;

namespace TitanGlobal.Application.UseCases.Modulo77_AuditoriaTransaccional;

// Lógica compartida por los commands y queries de Auditoria.
internal static class OperacionesAuditoria
{
    internal static List<CambioCampoDto> LeerCambios(string cambios)
    {
        List<CambioCampoDto>? resultado = System.Text.Json.JsonSerializer.Deserialize<List<CambioCampoDto>>(
            cambios,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        return resultado ?? new List<CambioCampoDto>();
    }
}
